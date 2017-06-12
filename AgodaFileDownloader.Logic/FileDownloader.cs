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
        private long _fileSize;
        private List<Segment> _segments;
        private List<Task> _tasks = new List<Task>();
        private Thread mainThread;
        private DownloaderState state { get; set; }

        private void SetState(DownloaderState value)
        {
            state = value;

        }

        private ResourceDetail _detail;
        private string _localFile;


        IProtocolDownloader _downloadProvider;
        readonly ISegmentProcessor _segmentProcessor;

        private int _maxTries;
        private int _retryDelay;
        private string _path;
        private int _numberOfSegments;

        public FileDownloader( ISegmentProcessor segmentProcessor,IProtocolDownloader protocolDownloader)
        {
            _segments = new List<Segment>();
            _segmentProcessor = segmentProcessor;
        }

        public Task StartAsynch(ResourceDetail detail)
        {
            _detail = detail;
            _downloadProvider = detail.BindProtocolProviderInstance();

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
                    return responseInit;
                }

                
                //2- Try and get Information on the remote file
                var numberOfTrial = 0;
                RemoteFileDetail remoteFileDetail;

                while (true)
                {
                    try
                    {
                        numberOfTrial++;
                        remoteFileDetail = _downloadProvider.GetFileInfo(_detail);
                        break;
                    }
                    catch (Exception ex)
                    {
                        while (ex.InnerException != null) ex = ex.InnerException;
                        Serilog.Log.Error(ex,"Task #" + Task.CurrentId + "---Could not get remote file information trial number:" +numberOfTrial + " for this URL " + _detail.Url);
                        if (numberOfTrial < _maxTries)
                        {
                            SetState(DownloaderState.WaitingForReconnect);
                            Serilog.Log.Information("Task #" + Task.CurrentId + "---Goes to sleep for " + _retryDelay +" seconds");
                            Thread.Sleep(TimeSpan.FromSeconds(_retryDelay));
                        }
                        else
                        {
                            Serilog.Log.Error("Task #" + Task.CurrentId + "---Maximum retrial reached for this URL" +_localFile);
                            SetState(DownloaderState.EndedWithError);
                            var response = new ResponseBase() {Denied = true};
                            response.AddMessage("Could not get remote information for " + _detail.Url);
                            return response;
                        }

                    }

                }

              

                //4-Create the file receiving the data and extract name from the URL
                var responseAccolacte=AllocLocalFile();
                if (responseAccolacte.Denied)
                {
                    return responseAccolacte;
                }


                //3-if the file accept ranges we will split it and download multiple segments at the same time
                //there will be as muck threads as segments
                CalculatedSegment[] calculatedSegments;
                if (remoteFileDetail.AcceptRanges)
                    calculatedSegments= new[] { new CalculatedSegment(0, remoteFileDetail.FileSize) };
                else
                {
                    var calculatedSegmentsResponse = _segmentProcessor.GetSegments(_numberOfSegments, remoteFileDetail);
                    if(calculatedSegmentsResponse.Denied || calculatedSegmentsResponse.ReturnedValue==null) return new ResponseBase() {Denied = true};
                    calculatedSegments = calculatedSegmentsResponse.ReturnedValue;
                }


                for (var i = 0; i < calculatedSegments.Length; i++)
                {
                    Segment segment = new Segment
                    {
                        Index = i,
                        InitialStartPosition = calculatedSegments[i].StartPosition,
                        StartPosition = calculatedSegments[i].StartPosition,
                        EndPosition = calculatedSegments[i].EndPosition,
                        CurrentURL = _localFile
                    };
                    lock (_segments)
                    {
                        _segments.Add(segment);
                    }
                }



                //5-Launch the downloads of segments
               
                using (FileStream fs = new FileStream(_localFile, FileMode.Open, FileAccess.Write))
                {
                    lock (_segments)
                    {
                        foreach (var segment in _segments)
                        {
                            segment.OutputStream = fs;
                            lock (_tasks)
                            {
                                _tasks.Add(Task.Run(() => SegmentDownload(segment)));
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
                            Serilog.Log.Fatal("Download Failed canceling all the tasks started: " + _localFile);
                            fs.Close();
                            if (File.Exists(_localFile)) File.Delete(_localFile);
                            return new ResponseBase() { Denied = true };
                        }

                    }

                }

                Serilog.Log.Information("Task #" + Task.CurrentId + "---Preparation ended successfully for" +_localFile);
                return new ResponseBase() {Denied = false};
            }
            catch (Exception ex)
            {
                //just in case 

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

        /*
        public void SegmentDownload(Segment segment)
        {
            Serilog.Log.Information("Task #" + Task.CurrentId + " File : " + segment.CurrentURL + " Current segment " +segment.Index + " ---Started the segment download");
            segment.LastError = null;
            try
            {
                if (segment.EndPosition > 0 && segment.StartPosition >= segment.EndPosition)
                {
                    segment.State = SegmentState.Finished;
                    return;
                }

                int buffSize = 8192;
                byte[] buffer = new byte[buffSize];

                segment.State = SegmentState.Connecting;
                var location = _detail;
                var provider = location.BindProtocolProviderInstance();
                segment.InputStream = provider.CreateStream(location, segment.StartPosition, segment.EndPosition);
                segment.CurrentURL = _localFile;
                
                using (segment.InputStream)
                {
                    segment.State = SegmentState.Downloading;
                    segment.CurrentTry = 0;
                    long readSize;
                    do
                    {
                        readSize = segment.InputStream.Read(buffer, 0, buffSize);

                        if (segment.EndPosition > 0 &&
                            segment.StartPosition + readSize > segment.EndPosition)
                        {
                            readSize = (segment.EndPosition - segment.StartPosition);
                            if (readSize <= 0)
                            {
                                segment.StartPosition = segment.EndPosition;
                                break;
                            }
                        }

                        lock (segment.OutputStream)
                        {
                            segment.OutputStream.Position = segment.StartPosition;
                            segment.OutputStream.Write(buffer, 0, (int) readSize);
                        }

                        segment.IncreaseStartPosition(readSize);

                        if (segment.EndPosition > 0 && segment.StartPosition >= segment.EndPosition)
                        {
                            segment.StartPosition = segment.EndPosition;
                            break;
                        }
                    } while (readSize > 0);
                    
                    segment.State = SegmentState.Finished;
                    Serilog.Log.Information("Task #" + Task.CurrentId + " File : " + segment.CurrentURL +" Current segment " + segment.Index + " with Current Try: " +segment.CurrentTry + " finished successfully");
                      
                }

            }
            catch (Exception ex)
            {
                segment.State = SegmentState.Error;
                segment.LastError = ex;
                while (ex.InnerException != null) ex = ex.InnerException;
                Serilog.Log.Error(ex,"Task #" + Task.CurrentId + " File : " + segment.CurrentURL + " Current segment " + segment.Index +"Current Try: " + segment.CurrentTry + " ---exception thrown");
               
            }
            finally
            {
                segment.InputStream = null;
            }
        }*/
        private bool RestartFailedSegments()
        {
            bool hasErrors = false;
            double delay = 0;
            var tokenSource = new CancellationTokenSource();
            foreach (var segment in _segments)
            {
                if (segment.State == SegmentState.Error && segment.LastErrorDateTime != DateTime.MinValue && (_maxTries == 0 || segment.CurrentTry < _maxTries) && !tokenSource.IsCancellationRequested)
                {
                    
                    hasErrors = true;
                    TimeSpan ts = DateTime.Now - segment.LastErrorDateTime;

                    if (ts.TotalSeconds >= _retryDelay )
                    {
                        segment.CurrentTry++;
                        Serilog.Log.Information("Task #" + Task.CurrentId + " File : " + segment.CurrentURL + " Current segment " + segment.Index + "Current Try: " + segment.CurrentTry + " ---segment started again");
                        _tasks.Add(Task.Run(() => SegmentDownload(segment), tokenSource.Token));
                        Task.WaitAll(_tasks.ToArray());
                    }
                    else
                    {
                        delay = Math.Max(delay, _retryDelay * 1000 - ts.TotalMilliseconds);
                    }
                }
                if(segment.CurrentTry >= _maxTries)
                {
                    
                    tokenSource.Cancel();
                    Serilog.Log.Fatal("Task #" + Task.CurrentId + " File : " + segment.CurrentURL + " Current segment " + segment.Index + "Current Try: " + segment.CurrentTry + " ---this segment reached the max trials and the download will be ended");
                    SetState(DownloaderState.EndedWithError);

                }
            }

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
        public ResponseBase InitConfigData()
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
    }

  
}