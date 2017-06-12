using System;
using System.IO;
using System.Net;
using System.Net.Security;
using System.Reflection;
using System.Security.Cryptography.X509Certificates;
using AgodaFileDownloader.Logic.Model;
using AgodaFileDownloader.Logic.ServiceInterface;
using AgodaFileDownloader.Model;
using static System.String;

namespace AgodaFileDownloader.Logic.ServiceImplemetnation
{
    public class HttpProtocolDownloader : BaseProtocolDownloader, IProtocolDownloader
    {
        static HttpProtocolDownloader()
        {
            ServicePointManager.ServerCertificateValidationCallback = CertificateCallBack;
        }

        static bool CertificateCallBack(object sender,X509Certificate certificate,X509Chain chain,SslPolicyErrors sslPolicyErrors)
        {
            return true;
        }

        private void FillCredentials(HttpWebRequest request, ResourceDetail rl)
        {
            if (rl.Authenticate)
            {
                string login = rl.Login;
                string domain = Empty;

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

        #region IProtocolProvider Members

        


        public virtual Stream CreateStream(ResourceDetail rl, long initialPosition, long endPosition)
        {
            HttpWebRequest request = (HttpWebRequest)GetRequest(rl);
            request.Timeout = 100000;
            FillCredentials(request, rl);

            if (initialPosition != 0)
            {
                if (endPosition == 0)
                {
                    request.AddRange((int)initialPosition);
                }
                else
                {
                    request.AddRange((int)initialPosition, (int)endPosition);
                }
            }

            WebResponse response = request.GetResponse();

            return response.GetResponseStream();
        }

       

        public virtual RemoteFileDetail GetFileInfo(ResourceDetail rl)
        {
            HttpWebRequest request = (HttpWebRequest)GetRequest(rl);
            request.Timeout = 100000;
            FillCredentials(request, rl);

            HttpWebResponse response = (HttpWebResponse)request.GetResponse();
            RemoteFileDetail result = new RemoteFileDetail
            {
                MimeType = response.ContentType,
                LastModified = response.LastModified,
                FileSize = response.ContentLength,
                AcceptRanges =
                    Compare(response.Headers["Accept-Ranges"], "bytes", StringComparison.OrdinalIgnoreCase) == 0
            };
            

            return result;
        }

        #endregion
    }
}
