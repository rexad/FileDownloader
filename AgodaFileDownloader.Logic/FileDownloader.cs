using System;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AgodaFileDownloader.Helper;
using AgodaFileDownloader.Logic.Helper;
using AgodaFileDownloader.Model;
using AgodaFileDownloader.Service;
using AgodaFileDownloader.Service.ServiceInterface;

namespace AgodaFileDownloader.Logic
{
    public class FileDownloader
    {


        readonly List<Segment> _segments;
        readonly List<Task> _tasks;
        DownloaderState state { get; set; }
        ResourceDetail _detail;


        #region Injected Properties
        private ConfigurationSetting _configurationSetting;
        readonly IProtocolDownloader _protocolDownloader;
        readonly ISegmentProcessor _segmentProcessor;
        private IInitializeDonwload _initializeDonwload;


        #endregion


        private string _localFile;
        private long _fileSize;
       

        public FileDownloader( ISegmentProcessor segmentProcessor,IProtocolDownloader protocolDownloader, IInitializeDonwload initializeDonwload)
        {
            _segments = new List<Segment>();
            _tasks = new List<Task>();
            _segmentProcessor = segmentProcessor;
            _protocolDownloader = protocolDownloader;
            _initializeDonwload = initializeDonwload;
        }

        public Task StartAsynch(ResourceDetail detail,ConfigurationSetting configurationSetting)
        {
            var mainTask = Task.Run(() => StartDownload(detail, configurationSetting));
            return mainTask;
        }


        public ResponseBase StartDownload(ResourceDetail detail,ConfigurationSetting configurationSetting)
        {
            try
            {
                _detail = detail;
                //0- Get configuration Data
                _configurationSetting = configurationSetting;
                SetState(DownloaderState.Preparing);
               
                //1- Try and get Information on the remote file
                var numberOfTrial = 0;
                RemoteFileDetail remoteFileDetail;
                while (true)
                {
                    numberOfTrial++;
                    var responseProtocolDownloader= _protocolDownloader.GetFileInfo(_detail);
                    if (responseProtocolDownloader.Denied)
                    {
                        SetState(DownloaderState.WaitingForReconnect);
                        Serilog.Log.Information("Task #" + Task.CurrentId + "---Goes to sleep for " + _configurationSetting.RetrialDelay + " seconds");
                        Thread.Sleep(TimeSpan.FromSeconds(_configurationSetting.RetrialDelay));
                        if ( numberOfTrial >= _configurationSetting.NumberOfTrial) return new ResponseBase() { Denied = true };
                    }
                    else
                    {
                        Serilog.Log.Information("Task #" + Task.CurrentId + "---Got Remote File Information for: "+_localFile);
                        remoteFileDetail = responseProtocolDownloader.ReturnedValue;
                        break;
                    }
                }

                //2-Create the file receiving the data and extract name from the URL
                var responseAccolacte= _initializeDonwload.AllocateSpace(_detail.Url, _configurationSetting.LocalFilePath);
                if (responseAccolacte.Denied)
                {
                    return responseAccolacte;
                }
                _localFile = responseAccolacte.ReturnedValue;

                //3-if the file accept ranges we will split it and download multiple segments at the same time
                //there will be as muck threads as segments
                if (!remoteFileDetail.AcceptRanges)
                _segments.Add(new Segment()
                {
                    Index = 0,
                    InitialStartPosition = 0,
                    StartPosition = 0,
                    EndPosition = remoteFileDetail.FileSize,
                    CurrentURL = _localFile
                });
                else
                {
                    var calculatedSegmentsResponse = _segmentProcessor.GetSegments(_configurationSetting.NumberOfSegments, _configurationSetting.MinSizeSegment, remoteFileDetail,_localFile);
                    if(calculatedSegmentsResponse.Denied || calculatedSegmentsResponse.ReturnedValue==null) return new ResponseBase() {Denied = true};
                    _segments.AddRange(calculatedSegmentsResponse.ReturnedValue); 
                }

                //5-Launch the downloads of segments
                using (FileStream fs = new FileStream(_localFile, FileMode.Open, FileAccess.Write))
                {
                    lock (_segments)
                    {
                        foreach (var segment in _segments)
                        {
                            segment.OutputStream = fs;
                            Serilog.Log.Information("Task #" + Task.CurrentId + " File : " + segment.CurrentURL + " Current segment " + segment.Index + " ---Started the segment download");
                            segment.LastError = null;
                            lock (_tasks)
                            {
                                var task=Task.Run(() => _segmentProcessor.ProcessSegment(_detail, _protocolDownloader, segment));
                                _tasks.Add(task);
                            }
                        }
                    }

                    lock (_tasks)
                    {
                        Task.WaitAll(_tasks.ToArray());
                    }

                    lock (_tasks)
                    {
                        do
                        {
                            while (RestartFailedSegments())
                            if (state == DownloaderState.EndedWithError)
                            {
                                Serilog.Log.Fatal("Download Failed canceling all the tasks started: "+_localFile);
                            }
                            
                        } while (_tasks.Count(e => e.Status == TaskStatus.Running) > 0 );
                       
                        if (state == DownloaderState.EndedWithError)
                        {
                            //if a single segments can't be downloaded and reaches maximum tries delete the file
                            Serilog.Log.Fatal("Download Failed canceling all the tasks started: " + _localFile);
                            fs.Close();
                            _segmentProcessor.DeleteFile(_localFile);
                            return new ResponseBase() { Denied = true };
                        }

                    }

                }

                Serilog.Log.Information("Task #" + Task.CurrentId + "---Download successful for file: " +_localFile);
                return new ResponseBase() {Denied = false};
            }
            catch (Exception ex)
            {
                //just in case an unsupported exception is raised for a particular file we make sure to delete the file
                _segmentProcessor.DeleteFile(_localFile);

                
                while (ex.InnerException != null) ex = ex.InnerException;
                Serilog.Log.Fatal(ex, "---An exception was raised during download for " + _localFile);

                var response = new ResponseBase()
                {
                    Denied = true,
                };
                response.Messages.Add("exception raised during preparation");
                return response;
            }
        }

        #region Private methods

        private bool RestartFailedSegments()
        {
            bool hasErrors = false;
            double delay = 0;
            var tokenSource = new CancellationTokenSource();
            if (_segments.Exists(e => e.CurrentTry >= _configurationSetting.NumberOfTrial))
            {
                //will always exist thus the first(we checked the existence) 
                var segment = _segments.First(e => e.CurrentTry >= _configurationSetting.NumberOfTrial);
                SetState(DownloaderState.EndedWithError);
                Serilog.Log.Fatal("Task #" + Task.CurrentId + " File : " + segment.CurrentURL + " Current segment " + segment.Index + "Current Try: " + segment.CurrentTry + " ---this segment reached the max trials and the download will be ended");
                return false;
            }

            if (_segments.Exists(e => e.State >= SegmentState.Error)) hasErrors = true;
            foreach (var segment in _segments)
            {
                if (segment.State == SegmentState.Error /*&& segment.LastErrorDateTime != DateTime.MinValue*/ && (_configurationSetting.NumberOfTrial == 0 || segment.CurrentTry < _configurationSetting.NumberOfTrial) && !tokenSource.IsCancellationRequested)
                {
                    
                    hasErrors = true;
                    TimeSpan ts = DateTime.Now - segment.LastErrorDateTime;

                    if (ts.TotalSeconds >= _configurationSetting.RetrialDelay)
                    {
                        
                        Serilog.Log.Information("Task #" + Task.CurrentId + " File : " + segment.CurrentURL + " Current segment " + segment.Index + "Current Try: " + segment.CurrentTry + " ---segment started again");
                        _tasks.Add(Task.Run(() =>_segmentProcessor.ProcessSegment(_detail, _protocolDownloader, segment)));
                    }
                    else
                    {
                        delay = Math.Max(delay, _configurationSetting.RetrialDelay * 1000 - ts.TotalMilliseconds);
                    }
                }
            }
            Task.WaitAll(_tasks.ToArray());

            if(delay>0)
            Serilog.Log.Information("----- goes to sleep for :+"+ delay/ 1000 +" seconds to respect the delay ");
            Thread.Sleep((int)delay);
            return hasErrors;
        }
        private void SetState(DownloaderState value)
        {
            state = value;

        }
        #endregion region
    }

  
}