using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AgodaFileDownloader.Helper;
using AgodaFileDownloader.Model;
using AgodaFileDownloader.Service.ServiceInterface;

namespace AgodaFileDownloader.Service.ServiceImplementaion
{
    public class SegmentProcessor : ISegmentProcessor
    {
        

        public ResponseBase ProcessSegment( ResourceDetail rl, IProtocolDownloader protocolDownloader,Segment segment)
        {
            var response = new ResponseBase();
            segment.LastError = null;
            try
            {
                if (segment.EndPosition > 0 && segment.StartPosition >= segment.EndPosition)
                {
                    segment.State = SegmentState.Finished;
                    return new ResponseBase<CalculatedSegment[]>() {Denied = false};
                }

                int buffSize = 8192;
                byte[] buffer = new byte[buffSize];

                segment.State = SegmentState.Connecting;
                segment.InputStream = protocolDownloader.CreateStream(rl, segment.StartPosition, segment.EndPosition);
                
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
                            segment.OutputStream.Write(buffer, 0, (int)readSize);
                        }

                        segment.IncreaseStartPosition(readSize);

                        if (segment.EndPosition > 0 && segment.StartPosition >= segment.EndPosition)
                        {
                            segment.StartPosition = segment.EndPosition;
                            break;
                        }
                    } while (readSize > 0);

                    if (segment.State == SegmentState.Downloading)
                    {
                        
                        Serilog.Log.Information("Task #" + Task.CurrentId + " File : " + segment.CurrentURL + " Current segment " + segment.Index + "Current Try: " + segment.CurrentTry + " finished successfully");
                        segment.State = SegmentState.Finished;
                        response.Denied = false;

                    }
                    
                }

            }
            catch (Exception ex)
            {
                Serilog.Log.Error(ex,"Task #" + Task.CurrentId + " File : " + segment.CurrentURL + " Current segment " + segment.Index + "Current Try: " + segment.CurrentTry + " An exception was raised");

                segment.State = SegmentState.Error;
                segment.LastError = ex;
                while (ex.InnerException != null) ex = ex.InnerException;
                response.Denied = true;
                response.Messages.Add(ex.Message); 
            }
            finally
            {
                segment.InputStream = null;
            }
            return response;
        }


        public ResponseBase<List<Segment>> GetSegments(int segmentCount, RemoteFileDetail remoteFileInfo, string fileName)
        {
            long minSize = 200000;
            long segmentSize = remoteFileInfo.FileSize / segmentCount;

            while (segmentCount > 1 && segmentSize < minSize)
            {
                segmentCount--;
                segmentSize = remoteFileInfo.FileSize / segmentCount;
            }

            long startPosition = 0;

            List<CalculatedSegment> calculatedSegments = new List<CalculatedSegment>();

            for (int i = 0; i < segmentCount; i++)
            {
                if (segmentCount - 1 == i)
                {
                    calculatedSegments.Add(new CalculatedSegment(startPosition, remoteFileInfo.FileSize));
                }
                else
                {
                    calculatedSegments.Add(new CalculatedSegment(startPosition, startPosition + (int)segmentSize));
                }

                startPosition = calculatedSegments[calculatedSegments.Count - 1].EndPosition;
            }

            var segments = new List<Segment>();
            for (var i = 0; i < calculatedSegments.Count; i++)
            {
                Segment segment = new Segment
                {
                    Index = i,
                    InitialStartPosition = calculatedSegments[i].StartPosition,
                    StartPosition = calculatedSegments[i].StartPosition,
                    EndPosition = calculatedSegments[i].EndPosition,
                    CurrentURL = fileName
                };
                segments.Add(segment);
            }

            return new ResponseBase<List<Segment>>()
            {
                Denied = false,
                ReturnedValue = segments
            };
        }
    }
}
