using System;
using System.Collections.Generic;
using AgodaFileDownloader.Logic;
using AgodaFileDownloader.Model;
using AgodaFileDownloader.Service;
using AgodaFileDownloader.Service.ServiceImplementaion;
using AgodaFileDownloader.Service.ServiceInterface;
using Microsoft.Practices.Unity;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Serilog;
using Serilog.Exceptions;
using Serilog.Formatting.Json;

namespace AgodaFileDownloader.UTest
{
    [TestClass]
    public class UtDownloadFeature
    {
        UnityContainer container = new UnityContainer();
        [TestInitialize]
        public void InitialMethod()
        {
            Log.Logger = new LoggerConfiguration()
                .Enrich.WithExceptionDetails()
                .MinimumLevel.Debug()
                .WriteTo.LiterateConsole()
                .WriteTo.RollingFile("C:\\temp\\log.txt")
                .CreateLogger();

            ProtocolProviderFactory.RegisterProtocolHandler("http", typeof(HttpProtocolDownloader));
            ProtocolProviderFactory.RegisterProtocolHandler("ftp", typeof(FtpProtocolDownloader));
            container.RegisterType<ISegmentProcessor,SegmentProcessor>();
        }
        [TestMethod]
        public void IsSupportingMultipleProtocols()
        {
           var downloadManger= container.Resolve<DownloadManager>();
            downloadManger.init(
                new List<string>()
                {
                   "ftp://150.140.208.250/share/ebooks/COMICS%20-%20The%20Ultimate%20Collection/%CE%A4%CE%B5%CF%8D%CF%87%CE%BF%CF%82%20252%20-%20%CE%97%20%CE%A7%CE%B1%CE%BC%CE%AD%CE%BD%CE%B7%20%CE%9C%CE%BF%CF%8D%CE%BC%CE%B9%CE%B1.pdf",
                     "ftp://150.140.208.250/share/ebooks/COMICS%20-%20The%20Ultimate%20Collection/%CE%A4%CE%B5%CF%8D%CF%87%CE%BF%CF%82%20258%20-%20%CE%A4%CE%B1%CE%BE%CE%AF%CE%B4%CE%B9%20%CE%9C%CE%B5%20%CE%A4%CE%BF%CE%BD%20%CE%9A%CE%BF%CE%BB%CF%8C%CE%BC%CE%B2%CE%BF.pdf",
                     "ftp://150.140.208.250/share/ebooks/COMICS%20-%20The%20Ultimate%20Collection/%CE%A4%CE%B5%CF%8D%CF%87%CE%BF%CF%82%20272%20-%20%CE%A3%CF%84%CE%B7%CE%BD%20%CE%9C%CE%AD%CF%83%CE%B7%20%CE%A4%CE%BF%CF%85%20%CE%A0%CE%BF%CF%85%CE%B8%CE%B5%CE%BD%CE%AC.pdf",
                     "ftp://150.140.208.250/share/ebooks/COMICS%20-%20The%20Ultimate%20Collection/%CE%A4%CE%B5%CF%8D%CF%87%CE%BF%CF%82%20044%20-%20%CE%A0%CE%B1%CE%B3%CF%89%CE%BC%CE%AD%CE%BD%CE%BF%20%CF%87%CF%81%CF%85%CF%83%CE%AC%CF%86%CE%B9.pdf",
                     "http://clicnet.swarthmore.edu/boudjedra.pdf",
                     "http://www.gideonphoto.com/blog/wp-content/uploads/2012/12/IMG_8940_2.jpg",
                     },
                new List<AuthenticatedUrl>());
            downloadManger.Download();
        }



        [TestMethod]
        public void IsSupportingMultiThreadingForMultipleFiles()
        {
            var downloadManger = container.Resolve<DownloadManager>();
            downloadManger.init(
                new List<string>()
                {
                    "ftp://150.140.208.250/share/ebooks/COMICS%20-%20The%20Ultimate%20Collection/%CE%A4%CE%B5%CF%8D%CF%87%CE%BF%CF%82%20258%20-%20%CE%A4%CE%B1%CE%BE%CE%AF%CE%B4%CE%B9%20%CE%9C%CE%B5%20%CE%A4%CE%BF%CE%BD%20%CE%9A%CE%BF%CE%BB%CF%8C%CE%BC%CE%B2%CE%BF.pdf",
                    "ftp://150.140.208.250/share/ebooks/COMICS%20-%20The%20Ultimate%20Collection/%CE%A4%CE%B5%CF%8D%CF%87%CE%BF%CF%82%20272%20-%20%CE%A3%CF%84%CE%B7%CE%BD%20%CE%9C%CE%AD%CF%83%CE%B7%20%CE%A4%CE%BF%CF%85%20%CE%A0%CE%BF%CF%85%CE%B8%CE%B5%CE%BD%CE%AC.pdf",
                    "ftp://150.140.208.250/share/ebooks/COMICS%20-%20The%20Ultimate%20Collection/%CE%A4%CE%B5%CF%8D%CF%87%CE%BF%CF%82%20044%20-%20%CE%A0%CE%B1%CE%B3%CF%89%CE%BC%CE%AD%CE%BD%CE%BF%20%CF%87%CF%81%CF%85%CF%83%CE%AC%CF%86%CE%B9.pdf",
                },
                new List<AuthenticatedUrl>());
            downloadManger.Download();
        }


        [TestMethod]
        public void IsSupportingMultiThreadingForMultipleSegments()
        {
            var downloadManger = container.Resolve<DownloadManager>();
            downloadManger.init(
                new List<string>()
                {
                    "ftp://150.140.208.250/share/ebooks/COMICS%20-%20The%20Ultimate%20Collection/%CE%A4%CE%B5%CF%8D%CF%87%CE%BF%CF%82%20258%20-%20%CE%A4%CE%B1%CE%BE%CE%AF%CE%B4%CE%B9%20%CE%9C%CE%B5%20%CE%A4%CE%BF%CE%BD%20%CE%9A%CE%BF%CE%BB%CF%8C%CE%BC%CE%B2%CE%BF.pdf",
                    "ftp://150.140.208.250/share/ebooks/COMICS%20-%20The%20Ultimate%20Collection/%CE%A4%CE%B5%CF%8D%CF%87%CE%BF%CF%82%20272%20-%20%CE%A3%CF%84%CE%B7%CE%BD%20%CE%9C%CE%AD%CF%83%CE%B7%20%CE%A4%CE%BF%CF%85%20%CE%A0%CE%BF%CF%85%CE%B8%CE%B5%CE%BD%CE%AC.pdf",
                    "ftp://150.140.208.250/share/ebooks/COMICS%20-%20The%20Ultimate%20Collection/%CE%A4%CE%B5%CF%8D%CF%87%CE%BF%CF%82%20044%20-%20%CE%A0%CE%B1%CE%B3%CF%89%CE%BC%CE%AD%CE%BD%CE%BF%20%CF%87%CF%81%CF%85%CF%83%CE%AC%CF%86%CE%B9.pdf",
                },
                new List<AuthenticatedUrl>());
            downloadManger.Download();
        }



        [TestMethod]
        public void IsDeletingIfIncomplete()
        {
            var downloadManger = container.Resolve<DownloadManager>();
            downloadManger.init(
                new List<string>()
                {
                    "ftp://150.140.208.250/share/ebooks/COMICS%20-%20The%20Ultimate%20Collection/%CE%A4%CE%B5%CF%8D%CF%87%CE%BF%CF%82%20044%20-%20%CE%A0%CE%B1%CE%B3%CF%89%CE%BC%CE%AD%CE%BD%CE%BF%20%CF%87%CF%81%CF%85%CF%83%CE%AC%CF%86%CE%B9.pdf",
                },
                new List<AuthenticatedUrl>());
            downloadManger.Download();
        }


        [TestMethod]
        public void IsFailingIfGetInfoFails()
        {
            /* "ftp://173-25-49-226.client.mchsi.com/Public/NAHCA%20HD%20Backup/nahca/Backup%20from%20office%20FLASH/all%20seasons/revised/2x5Vandykbanner.pdf",
                "ftp://83.247.138.9/CT1068879/MB10027%20-%20Projecte%20constructiu%20%20barreres%20ac%FAstiques%20carretera%20C-17.%20Tram%20Montcada%20i%20Reixac.pdf",
                "ftp://cda.cfa.harvard.edu/pub/.snapshot/weekly.2017-05-21_0015/science/ao16/cat7/18745/secondary/acisf18745_000N001_evt1.fits.gz",
            */
        }


    }
}
