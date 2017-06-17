using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AgodaFileDownloader.Helper;
using AgodaFileDownloader.Model;
using AgodaFileDownloader.Service;
using AgodaFileDownloader.Service.ServiceImplementaion;
using AgodaFileDownloader.Service.ServiceInterface;
using Serilog;

namespace AgodaFileDownloader.Logic
{
    public class DownloadManager
    {
        private ISegmentProcessor _segmentProcessor;
        private IProtocolDownloader _protocolDownloader;
        private List<ResourceDetail> _resourceDetails;
        private ConfigurationSetting _configurationSetting;
        private IInitializeDonwload _initializeDonwload;
        public DownloadManager(ISegmentProcessor segmentProcessor, IInitializeDonwload initializeDonwload)
        {
            _segmentProcessor = segmentProcessor;
            _initializeDonwload = initializeDonwload;
        }

        public ResponseBase Init(IList<string> urls, IList<AuthenticatedUrl> urlsWithAuthentification = null)
        {
            Log.Information("Starting the download process");
            if (urlsWithAuthentification == null) urlsWithAuthentification = new List<AuthenticatedUrl>();
            Log.Information("List of URLs passed down : " + string.Join(";", urls));
            Log.Information("List of URLs requiring authentication passed down : " + string.Join(";", urlsWithAuthentification));

            var responseInit = _initializeDonwload.InitConfigData();
            if (responseInit.Denied)
            {
                Log.Fatal("Could not retrieve config");
                var failResponse=new ResponseBase() {Denied = true};
                failResponse.AddListMessage(responseInit.Messages);
            }
            _configurationSetting = responseInit.ReturnedValue;
            //Prepare the file to be downloaded extract the protocol Type
            _resourceDetails = ResourceDetail.FromListUrl(urls, urlsWithAuthentification).ToList();
            return new ResponseBase() {Denied = false};
        }

        public List<Task>Download()
        {
            //Each URL will be downloaded on it's own thread
            List<Task> tasks = new List<Task>();
            foreach (var resourceDetail in _resourceDetails)
            {
                //Resolve the Download provider
                _protocolDownloader=ProtocolProviderFactory.ResolveProvider(resourceDetail.ProtocolType);
                var fileDownloader = new FileDownloader(_segmentProcessor,_protocolDownloader, _initializeDonwload);
                tasks.Add(fileDownloader.StartAsynch(resourceDetail, _configurationSetting));
            }
            return tasks;
            
        }

        
        
    }
}
