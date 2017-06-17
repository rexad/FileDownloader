using System.Configuration;
using System.Net;
using AgodaFileDownloader.Helper;

namespace AgodaFileDownloader.Service.ServiceInterface
{
    public class BaseProtocolDownloader
    {
        protected WebRequest GetRequest(ResourceDetail location)
        {
            WebRequest request = WebRequest.Create(location.Url);

            int timeout;
            var conversion = int.TryParse(ConfigurationManager.AppSettings["Timeout"], out timeout);
            if (!conversion)
            {
                Serilog.Log.Error("Could not Fetch timeout from config");
                var response = new ResponseBase()
                {
                    Denied = true,

                };
                response.Messages.Add("Could not Fetch timeout from config");
            }
            request.Timeout = timeout;
            return request;
        }


    }
}