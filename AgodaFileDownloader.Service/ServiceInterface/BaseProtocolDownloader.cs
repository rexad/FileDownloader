using System.Net;

namespace AgodaFileDownloader.Service.ServiceInterface
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