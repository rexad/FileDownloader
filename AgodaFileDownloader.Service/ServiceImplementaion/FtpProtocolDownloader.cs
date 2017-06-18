using System;
using System.IO;
using System.Net;
using AgodaFileDownloader.Helper;
using AgodaFileDownloader.Model;
using AgodaFileDownloader.Service.ServiceInterface;

namespace AgodaFileDownloader.Service.ServiceImplementaion
{
    public class FtpProtocolDownloader : BaseProtocolDownloader, IProtocolDownloader
    {
        private void FillCredentials(FtpWebRequest request, ResourceDetail rl)
        {
            
                if (rl.Authenticate)
                {
                    string login = rl.Login;
                    string domain = string.Empty;

                    int slashIndex = login.IndexOf('\\');

                    if (slashIndex >= 0)
                    {
                        domain = login.Substring(0, slashIndex);
                        login = login.Substring(slashIndex + 1);
                    }

                    NetworkCredential myCred = new NetworkCredential(login, rl.Password);
                    myCred.Domain = domain;

                    request.Credentials = myCred;
                }
           
        }

   

        public ResponseBase<Stream> CreateStream(ResourceDetail rl, long initialPosition, long endPosition)
        {
            try { 
                FtpWebRequest request = (FtpWebRequest)GetRequest(rl);
                FillCredentials(request, rl);

                request.UsePassive = true;
                //request.Timeout = -1;
                request.Method = WebRequestMethods.Ftp.DownloadFile;
                request.ContentOffset = initialPosition;

                WebResponse response = request.GetResponse();
                var responseStream= response.GetResponseStream();
                return new ResponseBase<Stream>() {ReturnedValue = responseStream ,Denied = false};
            }
            catch (Exception ex)
            {
                Serilog.Log.Error(ex,"An exception was raised in Createstream.cs ");

                while (ex.InnerException != null) ex = ex.InnerException;
                var responseFailed = new ResponseBase<Stream>() { Denied = true };
                responseFailed.Messages.Add(ex.Message);
                return responseFailed;
            }
        }

        public ResponseBase<RemoteFileDetail> GetFileInfo(ResourceDetail rl)
        {
            try { 
                FtpWebRequest request;
                RemoteFileDetail result = new RemoteFileDetail();
                result.AcceptRanges = true;

            

                request = (FtpWebRequest)GetRequest(rl);

                request.UsePassive = true;
                //request.Timeout = 500000;
                request.Method = WebRequestMethods.Ftp.GetFileSize;
                FillCredentials(request, rl);

                using (FtpWebResponse response = (FtpWebResponse)request.GetResponse())
                {
                    result.FileSize = response.ContentLength;
                }

                request = (FtpWebRequest)GetRequest(rl);
                request.Method = WebRequestMethods.Ftp.GetDateTimestamp;
                FillCredentials(request, rl);

                using (FtpWebResponse response = (FtpWebResponse)request.GetResponse())
                {
                    result.LastModified = response.LastModified;
                }

                return new ResponseBase<RemoteFileDetail>() {Denied = false,ReturnedValue = result };
            }
            catch (Exception ex)
            {
                while (ex.InnerException != null) ex = ex.InnerException;
                var responseFailed = new ResponseBase<RemoteFileDetail>() { Denied = true };
                responseFailed.Messages.Add(ex.Message);
                return responseFailed;
            }
        }

        

      

      
    }
}
