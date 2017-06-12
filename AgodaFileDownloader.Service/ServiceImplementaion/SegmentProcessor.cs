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

        public IProtocolDownloader ProtocolDownloader { get; set; }

        public ResponseBase ProcessSegment( ResourceDetail rl,Segment segment)
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
                segment.InputStream = ProtocolDownloader.CreateStream(rl, segment.StartPosition, segment.EndPosition);
                
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

                    segment.State = SegmentState.Finished;
                    response.Denied = false;
                }

            }
            catch (Exception ex)
            {
                
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


        public ResponseBase<CalculatedSegment[]> GetSegments(int segmentCount, RemoteFileDetail remoteFileInfo)
        {
            long minSize = 200000;
            long segmentSize = remoteFileInfo.FileSize / segmentCount;

            while (segmentCount > 1 && segmentSize < minSize)
            {
                segmentCount--;
                segmentSize = remoteFileInfo.FileSize / segmentCount;
            }

            long startPosition = 0;

            List<CalculatedSegment> segments = new List<CalculatedSegment>();

            for (int i = 0; i < segmentCount; i++)
            {
                if (segmentCount - 1 == i)
                {
                    segments.Add(new CalculatedSegment(startPosition, remoteFileInfo.FileSize));
                }
                else
                {
                    segments.Add(new CalculatedSegment(startPosition, startPosition + (int)segmentSize));
                }

                startPosition = segments[segments.Count - 1].EndPosition;
            }

            return new ResponseBase<CalculatedSegment[]>()
            {
                Denied = false,
                ReturnedValue = segments.ToArray()
        };
        }
    }
}
