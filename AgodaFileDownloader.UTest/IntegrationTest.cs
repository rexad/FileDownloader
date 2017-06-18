using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AgodaFileDownloader.Helper;
using AgodaFileDownloader.Logic;
using AgodaFileDownloader.Model;
using AgodaFileDownloader.Service;
using AgodaFileDownloader.Service.ServiceImplementaion;
using AgodaFileDownloader.Service.ServiceInterface;
using Microsoft.Practices.Unity;
using Moq;
using NUnit.Framework;
using Serilog;
using Serilog.Exceptions;

namespace AgodaFileDownloader.UTest
{
    public class IntegrationTest
    {
        UnityContainer container = new UnityContainer();

        [SetUp]
        public void InitialMethod()
        {
         


            Log.Logger = new LoggerConfiguration()
                   .Enrich.WithExceptionDetails()
                   .MinimumLevel.Debug()
                   .WriteTo.LiterateConsole()
                   .WriteTo.RollingFile("C:\\temp\\log.txt")
                   .CreateLogger();
            
        }

        [Test]
        public void IsSupportingMultipleProtocols()
        {

            ProtocolProviderFactory.RegisterProtocolHandler("http", typeof(HttpProtocolDownloader));
            ProtocolProviderFactory.RegisterProtocolHandler("ftp", typeof(FtpProtocolDownloader));

            container.RegisterType<ISegmentProcessor, SegmentProcessor>();
            container.RegisterType<IInitializeDonwload, InitializeDownload>();


            var downloadManger = container.Resolve<DownloadManager>();
            downloadManger.Init(
                new List<string>()
                {
                    "ftp://speedtest.tele2.net/10MB.zip",
                    "http://clicnet.swarthmore.edu/boudjedra.pdf",
                    "http://www.gideonphoto.com/blog/wp-content/uploads/2012/12/IMG_8940_2.jpg",
                    "ftp://speedtest.tele2.net/50MB.zip",
                },
                new List<AuthenticatedUrl>());
            var tasks = downloadManger.Download();
            Task.WaitAll(tasks.ToArray());

            var initializeDownload = new InitializeDownload();
            var config=initializeDownload.InitConfigData();
            Assert.IsTrue(File.Exists(config.ReturnedValue.LocalFilePath+ "\\IMG_8940_2.jpg"));
            Assert.IsTrue(File.Exists(config.ReturnedValue.LocalFilePath + "\\boudjedra.pdf"));
            Assert.IsTrue(File.Exists(config.ReturnedValue.LocalFilePath + "\\10MB.zip"));
            Assert.IsTrue(File.Exists(config.ReturnedValue.LocalFilePath + "\\50MB.zip"));
            File.Delete(config.ReturnedValue.LocalFilePath + "\\IMG_8940_2.jpg");
            File.Delete(config.ReturnedValue.LocalFilePath + "\\boudjedra.pdf");
            File.Delete(config.ReturnedValue.LocalFilePath + "\\10MB.zip");
            File.Delete(config.ReturnedValue.LocalFilePath + "\\50MB.zip");
        }
    }
}
