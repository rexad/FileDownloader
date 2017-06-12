using System.Net;
using AgodaFileDownloader.Logic.Model;

namespace AgodaFileDownloader.Logic.ServiceInterface
{
    public class BaseProtocolDownloader
    {
        protected WebRequest GetRequest(ResourceDetail location)
        {
            WebRequest request = WebRequest.Create(location.Url);
            request.Timeout = 90000;
            return request;
        }


    }
}