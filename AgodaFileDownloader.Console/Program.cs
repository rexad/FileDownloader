using System.Collections.Generic;
using System.Configuration;
using System.Threading.Tasks;
using AgodaFileDownloader.Helper;
using AgodaFileDownloader.Logic;
using AgodaFileDownloader.Service;
using AgodaFileDownloader.Service.ServiceImplementaion;
using AgodaFileDownloader.Service.ServiceInterface;
using Microsoft.Practices.Unity;
using Serilog;
using Serilog.Exceptions;

namespace AgodaFileDownloader.Console
{
    class Program
    {
        static void Main(string[] args)
        {

            //get log file path
            var path = ConfigurationManager.AppSettings["LogFilePath"];
            if (string.IsNullOrEmpty(path))
            {
                return;
            }
            //init the logging system
            Log.Logger = new LoggerConfiguration()
                .Enrich.WithExceptionDetails()
                .MinimumLevel.Debug()
                .WriteTo.LiterateConsole()
                .WriteTo.RollingFile(path)
                .CreateLogger();

            //thats where the magic happens, register a protocol with its handler and it will be 
            //resolved automatically thanks to the variance
            ProtocolProviderFactory.RegisterProtocolHandler("http", typeof(HttpProtocolDownloader));
            ProtocolProviderFactory.RegisterProtocolHandler("ftp", typeof(FtpProtocolDownloader));

            //init the DI continer
            UnityContainer container = new UnityContainer();
            container.RegisterType<ISegmentProcessor, SegmentProcessor>();
            container.RegisterType<IInitializeDonwload, InitializeDownload>();

            // a set of url examples
            var downloadManger = container.Resolve<DownloadManager>();
            var respose=downloadManger.Init(new List<string>()
                {
                    "ftp://oceane.obs-vlfr.fr/pub/prieur/publications_LP/pubLP_before2004/PDF-Finis/livre_Bassin_Ligure_donn%C3%A9es_hydro_1950_1973.pdf",
                    "http://clicnet.swarthmore.edu/boudjedra.pdf",
                   
                 
                     });
            if (respose.Denied)
            {
                Log.Fatal("Ending the execution config failed to get loaded");
                return;
            }
            //init the download
            var tasks = downloadManger.Download();
            Task.WaitAll(tasks.ToArray());
        }
    }
}
