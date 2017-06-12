using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using AgodaFileDownloader.Logic.Model;
using AgodaFileDownloader.Logic.ServiceInterface;
using AgodaFileDownloader.Model;

namespace AgodaFileDownloader.Logic.ServiceImplemetnation
{
    public class FtpProtocolDownloader :  BaseProtocolDownloader,IProtocolDownloader
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

   

        public Stream CreateStream(ResourceDetail rl, long initialPosition, long endPosition)
        {
            FtpWebRequest request = (FtpWebRequest)GetRequest(rl);
            FillCredentials(request, rl);

            request.UsePassive = true;
            request.Timeout = -1;
            request.Method = WebRequestMethods.Ftp.DownloadFile;
            request.ContentOffset = initialPosition;

            WebResponse response = request.GetResponse();

            return response.GetResponseStream();
        }

        public RemoteFileDetail GetFileInfo(ResourceDetail rl)
        {
            FtpWebRequest request;
            RemoteFileDetail result = new RemoteFileDetail();
            result.AcceptRanges = true;

            

            request = (FtpWebRequest)GetRequest(rl);

            request.UsePassive = true;
            request.Timeout = 500000;
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

            return result;
        }

        

      

      
    }
}
