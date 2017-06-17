using System;
using System.IO;
using System.Net;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using AgodaFileDownloader.Helper;
using AgodaFileDownloader.Model;
using AgodaFileDownloader.Service.ServiceInterface;
using static System.String;

namespace AgodaFileDownloader.Service.ServiceImplementaion
{
    public class HttpProtocolDownloader : BaseProtocolDownloader, IProtocolDownloader
    {
        #region IProtocolProvider Members
        public ResponseBase<Stream> CreateStream(ResourceDetail rl, long initialPosition, long endPosition)
        {
            try
            {
                HttpWebRequest request = (HttpWebRequest) GetRequest(rl);
                request.Timeout = 100000;

                if (initialPosition != 0)
                {
                    if (endPosition == 0)
                    {
                        request.AddRange((int) initialPosition);
                    }
                    else
                    {
                        request.AddRange((int) initialPosition, (int) endPosition);
                    }
                }

                var response = request.GetResponse();
                var responseStream=response.GetResponseStream();
                return new ResponseBase<Stream>() {Denied = false,ReturnedValue = responseStream };
            }
            catch (Exception ex)
            {
                Serilog.Log.Error(ex, "An exception was raised in Createstream.cs ");
                while (ex.InnerException != null) ex = ex.InnerException;
                var responseFailed=new ResponseBase<Stream>() {Denied = true};
                responseFailed.Messages.Add(ex.Message);
                return responseFailed;
            }
        }
        public ResponseBase<RemoteFileDetail> GetFileInfo(ResourceDetail rl)
        {
            try
            {
                HttpWebRequest request = (HttpWebRequest)GetRequest(rl);
                //request.Timeout = 100000;

                HttpWebResponse response = (HttpWebResponse)request.GetResponse();
                RemoteFileDetail result = new RemoteFileDetail
                {
                    MimeType = response.ContentType,
                    LastModified = response.LastModified,
                    FileSize = response.ContentLength,
                    AcceptRanges =Compare(response.Headers["Accept-Ranges"], "bytes", StringComparison.OrdinalIgnoreCase) == 0
                };
                return new ResponseBase<RemoteFileDetail>() {Denied = false,ReturnedValue = result };
            }
            catch (Exception ex)
            {
                while (ex.InnerException != null) ex = ex.InnerException;
                Serilog.Log.Error(ex, "---Could not get remote file information trial number");
                var responseFailed=new ResponseBase<RemoteFileDetail>() {Denied = true};
                responseFailed.AddMessage(ex.Message);
                return responseFailed;
            }
        }

        #endregion
    }
}
