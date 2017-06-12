using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AgodaFileDownloader.Model;
using AgodaFileDownloader.Service;
using AgodaFileDownloader.Service.ServiceImplementaion;
using Serilog;

namespace AgodaFileDownloader.Logic
{
    public class DownloadManager
    {
        public DownloadManager(IList<string> urls, IList<AuthenticatedUrl> urlsWithAuthentification=null)
        {
            Log.Information("Starting the download process");

            if (urlsWithAuthentification==null) urlsWithAuthentification=new List<AuthenticatedUrl>();
           
            
            

            Log.Information("List of URLs passed down : " + string.Join(";", urls));
            Log.Information("List of URLs requiring authentication passed down : " + string.Join(";", urlsWithAuthentification));

            //Extract data related to the resource we want to download (protocol/downloader impelemntation)
            var resourceToDownload=ResourceDetail.FromListUrl(urls,urlsWithAuthentification);

           //this should be moved to a dependency injection container 
            //MinSizeSegmentCalculator isntance =new MinSizeSegmentCalculator();
            

            //EAch Url will be downloaded on it's own thread
            List<Task> tasks=new List<Task>();
            foreach (var resourceDetail in resourceToDownload)
            {
                var fileDownloader=new FileDownloader();
                tasks.Add(fileDownloader.StartAsynch(resourceDetail));
            }
            Task.WaitAll(tasks.ToArray());

        }

        
        
    }
}
