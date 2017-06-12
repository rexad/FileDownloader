using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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
        public DownloadManager(ISegmentProcessor segmentProcessor)
        {
            _segmentProcessor = segmentProcessor;
        }

        public void init(IList<string> urls, IList<AuthenticatedUrl> urlsWithAuthentification = null)
        {
            Log.Information("Starting the download process");

            if (urlsWithAuthentification == null) urlsWithAuthentification = new List<AuthenticatedUrl>();




            Log.Information("List of URLs passed down : " + string.Join(";", urls));
            Log.Information("List of URLs requiring authentication passed down : " + string.Join(";", urlsWithAuthentification));

            //Extract data related to the resource we want to download (protocol/downloader impelemntation)
            _resourceDetails = ResourceDetail.FromListUrl(urls, urlsWithAuthentification).ToList();
            
        }

        public void Download()
        {
            //EAch Url will be downloaded on it's own thread
            List<Task> tasks = new List<Task>();
            foreach (var resourceDetail in _resourceDetails)
            {
                _protocolDownloader=ProtocolProviderFactory.CreateProvider(resourceDetail.ProtocolType);
                var fileDownloader = new FileDownloader(_segmentProcessor,_protocolDownloader);
                tasks.Add(fileDownloader.StartAsynch(resourceDetail));
            }
            Task.WaitAll(tasks.ToArray());
        }

        
        
    }
}
