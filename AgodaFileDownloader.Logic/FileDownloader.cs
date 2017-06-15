using System;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AgodaFileDownloader.Helper;
using AgodaFileDownloader.Model;
using AgodaFileDownloader.Service;
using AgodaFileDownloader.Service.ServiceInterface;

namespace AgodaFileDownloader.Logic
{
    public class FileDownloader
    {
        

        readonly List<Segment> _segments;
        readonly List<Task> _tasks;
        readonly IProtocolDownloader _protocolDownloader;
        readonly ISegmentProcessor _segmentProcessor;
        DownloaderState state { get; set; }
        ResourceDetail _detail;
        string _localFile;
        long _fileSize;
        int _maxTries;
        int _retryDelay;
        string _path;
        int _numberOfSegments;

        public FileDownloader( ISegmentProcessor segmentProcessor,IProtocolDownloader protocolDownloader)
        {
            _segments = new List<Segment>();
            _tasks = new List<Task>();
            _segmentProcessor = segmentProcessor;
            _protocolDownloader = protocolDownloader;
        }

        public Task StartAsynch(ResourceDetail detail)
        {
            _detail = detail;
            var mainTask = Task.Run(() => StartDownload());
            return mainTask;
        }


        public ResponseBase StartDownload()
        {
            try
            {
                //0- Get configuration Data
                var responseInit = InitConfigData();
                if (responseInit.Denied)
                {
                    SetState(DownloaderState.EndedWithError);
                    Serilog.Log.Fatal( "Task #" + Task.CurrentId + "---Download Failed not able to get configuration data for resource: " + _detail.Url);

                    return responseInit;
                }

                
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
                        Serilog.Log.Information("Task #" + Task.CurrentId + "---Goes to sleep for " + _retryDelay +" seconds");
                        Thread.Sleep(TimeSpan.FromSeconds(_retryDelay));
                        if ( numberOfTrial >= _maxTries) return new ResponseBase() { Denied = true };
                    }
                    else
                    {
                        Serilog.Log.Information("Task #" + Task.CurrentId + "---Got Remote File Information for: "+_localFile);
                        remoteFileDetail = responseProtocolDownloader.ReturnedValue;
                        break;
                    }
                }

                //2-Create the file receiving the data and extract name from the URL
                var responseAccolacte=AllocLocalFile();
                if (responseAccolacte.Denied)
                {
                    return responseAccolacte;
                }
                
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
                    var calculatedSegmentsResponse = _segmentProcessor.GetSegments(_numberOfSegments, remoteFileDetail,_localFile);
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
                            if (File.Exists(_localFile)) File.Delete(_localFile);
                            return new ResponseBase() { Denied = true };
                        }

                    }

                }

                Serilog.Log.Information("Task #" + Task.CurrentId + "---Download successful for file: " +_localFile);
                return new ResponseBase() {Denied = false};
            }
            catch (Exception ex)
            {
                //just in case an unsupported exception is raised for a particular file we make sure to delte the file
                if (File.Exists(_localFile))
                    File.Delete(_localFile);
                

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
            if (_segments.Exists(e => e.CurrentTry >= _maxTries))
            {
                //will always exist thus the first(we checked the existence) 
                var segment = _segments.First(e => e.CurrentTry >= _maxTries);
                SetState(DownloaderState.EndedWithError);
                Serilog.Log.Fatal("Task #" + Task.CurrentId + " File : " + segment.CurrentURL + " Current segment " + segment.Index + "Current Try: " + segment.CurrentTry + " ---this segment reached the max trials and the download will be ended");
                return false;
            }
            foreach (var segment in _segments)
            {
                if (segment.State == SegmentState.Error && segment.LastErrorDateTime != DateTime.MinValue && (_maxTries == 0 || segment.CurrentTry < _maxTries) && !tokenSource.IsCancellationRequested)
                {
                    
                    hasErrors = true;
                    TimeSpan ts = DateTime.Now - segment.LastErrorDateTime;

                    if (ts.TotalSeconds >= _retryDelay )
                    {
                        
                        Serilog.Log.Information("Task #" + Task.CurrentId + " File : " + segment.CurrentURL + " Current segment " + segment.Index + "Current Try: " + segment.CurrentTry + " ---segment started again");
                        _tasks.Add(Task.Run(() =>_segmentProcessor.ProcessSegment(_detail, _protocolDownloader, segment)));
                    }
                    else
                    {
                        delay = Math.Max(delay, _retryDelay * 1000 - ts.TotalMilliseconds);
                    }
                }
            }
            Task.WaitAll(_tasks.ToArray());
            Thread.Sleep((int)delay);
            return hasErrors;
        }
        private ResponseBase AllocLocalFile()
        {
            try
            {
                Uri uri = new Uri(_detail.Url);
                string filename = Path.GetFileName(uri.LocalPath);
                _localFile = _path + "/" + filename;
                FileInfo fileInfo = new FileInfo(_localFile);
                if (!Directory.Exists(fileInfo.DirectoryName))
                {
                    if (fileInfo.DirectoryName != null) Directory.CreateDirectory(fileInfo.DirectoryName);
                }

                if (fileInfo.Exists)
                {
                    // auto rename the file...
                    int count = 1;

                    string fileExitWithoutExt = Path.GetFileNameWithoutExtension(_localFile);
                    string ext = Path.GetExtension(_localFile);

                    string newFileName;

                    do
                    {
                        newFileName = PathHelper.GetWithBackslash(fileInfo.DirectoryName) + fileExitWithoutExt + $"_{count++}" + ext;
                    } while (File.Exists(newFileName));

                    _localFile = newFileName;
                }

                using (FileStream fs = new FileStream(_localFile, FileMode.Create, FileAccess.Write))
                {
                    fs.SetLength(0);
                }
                return new ResponseBase() { Denied = false };
            }
            catch (Exception ex)
            {
                while (ex.InnerException != null) ex = ex.InnerException;
                Serilog.Log.Error(ex, "List of URLs requiring authentication passed down");
                return new ResponseBase() { Denied = true };
            }

        }
        private ResponseBase InitConfigData()
        {
            SetState(DownloaderState.Preparing);
            var conversion = int.TryParse(ConfigurationManager.AppSettings["NumberOfTrial"], out _maxTries);
            if (!conversion)
            {
                Serilog.Log.Error("Task #" + Task.CurrentId + "Could not Fetch max trial from config");
                var response = new ResponseBase()
                {
                    Denied = true,

                };
                response.Messages.Add("Could not Fetch max trial for this URL " + _detail.Url);
            }

            conversion = int.TryParse(ConfigurationManager.AppSettings["RetrialDelay"], out _retryDelay);
            if (!conversion)
            {
                Serilog.Log.Error("Task #" + Task.CurrentId + "Could not Fetch RetrialDelay from config ");
                var response = new ResponseBase()
                {
                    Denied = true,

                };
                response.Messages.Add("Could not Fetch max trial for this URL " + _detail.Url);
            }

            conversion = int.TryParse(ConfigurationManager.AppSettings["NumberOfSegments"], out _numberOfSegments);
            if (!conversion)
            {
                Serilog.Log.Error("Task #" + Task.CurrentId + "Could not Fetch number of segments from config");
                var response = new ResponseBase()
                {
                    Denied = true,

                };
                response.Messages.Add("Could not Fetch max trial ");
            }

            _path = ConfigurationManager.AppSettings["LocalFilePath"];
            if (string.IsNullOrEmpty(_path))
            {
                Serilog.Log.Error("Task #" + Task.CurrentId + "Could not Fetch path from config");
                var response = new ResponseBase()
                {
                    Denied = true,

                };
                response.Messages.Add("Could not Fetch max trial ");
            }
            return new ResponseBase() { Denied = false };
        }
        private void SetState(DownloaderState value)
        {
            state = value;

        }
        #endregion region
    }

  
}