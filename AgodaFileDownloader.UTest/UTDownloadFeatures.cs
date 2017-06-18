using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using AgodaFileDownloader.Logic;
using AgodaFileDownloader.Model;
using AgodaFileDownloader.Service;
using AgodaFileDownloader.Service.ServiceImplementaion;
using AgodaFileDownloader.Service.ServiceInterface;
using AgodaFileDownloader.Helper;
using Microsoft.Practices.Unity;
using NUnit.Framework;
using Serilog;
using Serilog.Exceptions;
using Moq;

namespace AgodaFileDownloader.UTest
{
    [TestFixture]
    public class UtDownloadFeature
    {
        UnityContainer container = new UnityContainer();
        Mock<IInitializeDonwload> _mockinitializeDownload;

        Mock<IProtocolDownloader> _mockProtcolGetFileInfoRetries;

        Mock<IProtocolDownloader> _mockProtcolDownloadSegmentRetries;
        Mock<ISegmentProcessor> _mockSegementProcessorDownloadSegmentRetries;
        private int _maxTries;
        [SetUp]
        public void InitialMethod()
        {
            _mockinitializeDownload = new Mock<IInitializeDonwload>();
            _mockinitializeDownload.Setup(e => e.InitConfigData())
                .Returns(() => new ResponseBase<ConfigurationSetting>()
                {
                    Denied = false,
                    ReturnedValue =
                    {
                        NumberOfTrial = 10,
                        LocalFilePath = "C:\\temp",
                        MinSizeSegment = 200000,
                        RetrialDelay = 10,
                        NumberOfSegments = 5,
                        Timeout = -1,
                        LogFilePath = "C:\\temp\\log.txt"
                    }
                });
            

            Log.Logger = new LoggerConfiguration()
                   .Enrich.WithExceptionDetails()
                   .MinimumLevel.Debug()
                   .WriteTo.LiterateConsole()
                   .WriteTo.RollingFile("C:\\temp\\log.txt")
                   .CreateLogger();


            #region implementation to test getfile retry 
            _mockProtcolGetFileInfoRetries = new Mock<IProtocolDownloader>();
            _mockProtcolGetFileInfoRetries.Setup(e => e.GetFileInfo(It.IsAny<ResourceDetail>())).Returns(() => new ResponseBase<RemoteFileDetail>(){Denied = true});
            #endregion


            #region implementation to  test the download retry 
            _mockProtcolDownloadSegmentRetries = new Mock<IProtocolDownloader>();
            _mockProtcolDownloadSegmentRetries.Setup(e => e.GetFileInfo(It.IsAny<ResourceDetail>())).Returns(() => new ResponseBase<RemoteFileDetail>()
               {
                   Denied = false,
                   ReturnedValue = new RemoteFileDetail()
                   {
                       AcceptRanges = true,
                       FileSize = 0
                   }
               });

            _mockProtcolDownloadSegmentRetries.Setup(e => e.CreateStream(It.IsAny<ResourceDetail>(), It.IsAny<long>(), It.IsAny<long>()))
               .Returns(() => new ResponseBase<Stream>()
                {
                    Denied = true
                });

    
            _mockSegementProcessorDownloadSegmentRetries =new Mock<ISegmentProcessor>();

            _mockSegementProcessorDownloadSegmentRetries.Setup(
                    e => e.GetSegments(It.IsAny<int>(), It.IsAny<long>(),It.IsAny<RemoteFileDetail>(), It.IsAny<string>()))
                .Returns(() => new ResponseBase<List<Segment>>()
                    {
                        Denied = false,
                        ReturnedValue = new List<Segment>() { new Segment()
                        {
                            State = SegmentState.Error,
                            LastError = "error test",
                            LastErrorDateTime = DateTime.Now,
                            CurrentTry = 10
                        } }
                    }
                );


            _mockSegementProcessorDownloadSegmentRetries.Setup(
                   e => e.ProcessSegment(It.IsAny<ResourceDetail>(),  It.IsAny<IProtocolDownloader>(),It.IsAny<Segment>()))
               .Returns(() => new ResponseBase<List<Segment>>()
               {
                   Denied = false,
                   ReturnedValue = new List<Segment>() { new Segment()
                        {
                            State = SegmentState.Error,
                            LastError = "error test",
                            LastErrorDateTime = DateTime.Now,
                            CurrentTry = 10
                        } }
               }
               );
            #endregion

            
        }

      
        
        [Test]
        public void IsSupportingMultiThreadingForMultipleFiles()
        {

            ProtocolProviderFactory.RegisterProtocolHandler("http", typeof(HttpProtocolDownloader));
            ProtocolProviderFactory.RegisterProtocolHandler("ftp", typeof(FtpProtocolDownloader));

            container.RegisterType<ISegmentProcessor, SegmentProcessor>();
            container.RegisterType<IInitializeDonwload, InitializeDownload>();


            var downloadManger = container.Resolve<DownloadManager>();
            downloadManger.Init(
                new List<string>()
                {
                    "ftp://test.com/test",
                    "http://test.com/test",
                    "http://test.com/test"
                },
                new List<AuthenticatedUrl>());
            var tasks=downloadManger.Download();
            Assert.AreEqual(tasks.Count,3);
            Assert.IsTrue(tasks.TrueForAll(e=>e.Status==TaskStatus.Running));
        }
        
        [Test]
        public void IsDeletingIfIncomplete()
        {
            var configSetting = new ConfigurationSetting()
            {
                NumberOfTrial = 10,
                LocalFilePath = "C:\\temp",
                MinSizeSegment = 200000,
                RetrialDelay = 10,
                NumberOfSegments = 5,
                Timeout = -1,
                LogFilePath = "C:\\temp\\log.txt"
            };
            var rd = new ResourceDetail() { Url = "http://testutl.example/file.txt" };
            FileDownloader fileDownloader = new FileDownloader(_mockSegementProcessorDownloadSegmentRetries.Object, _mockProtcolDownloadSegmentRetries.Object, new InitializeDownload());
            ResponseBase response = fileDownloader.StartDownload(rd, configSetting);
            Assert.IsTrue(response.Denied);
            _mockSegementProcessorDownloadSegmentRetries.Verify(m => m.DeleteFile(It.IsAny<string>()));

        }

        [Test]
        public void IsFailingIfGetInfoFails()
        {
            var configSetting = new ConfigurationSetting()
            {
                NumberOfTrial = 10,
                LocalFilePath = "C:\\temp",
                MinSizeSegment = 200000,
                RetrialDelay = 10,
                NumberOfSegments = 5,
                Timeout = -1,
                LogFilePath = "C:\\temp\\log.txt"
            };
            var rd = new ResourceDetail() { Url = "http://testutl.example/file.txt" };
            FileDownloader fileDownloader = new FileDownloader(_mockSegementProcessorDownloadSegmentRetries.Object, _mockProtcolDownloadSegmentRetries.Object, new InitializeDownload());
            ResponseBase response = fileDownloader.StartDownload(rd, configSetting);
            Assert.IsTrue(response.Denied);
            _mockSegementProcessorDownloadSegmentRetries.Verify(m => m.DeleteFile(It.IsAny<string>()));

        }
        
        [Test]
        public void IsDownloadFailsAfterMaxRetries()
        {
            var configSetting = new ConfigurationSetting()
            {
                NumberOfTrial = 10,
                LocalFilePath = "C:\\temp",
                MinSizeSegment = 200000,
                RetrialDelay = 10,
                NumberOfSegments = 5,
                Timeout = -1,
                LogFilePath = "C:\\temp\\log.txt"
            };
            var rd=new ResourceDetail() {Url = "http://testutl.example/file.txt"};
            FileDownloader fileDownloader = new FileDownloader(_mockSegementProcessorDownloadSegmentRetries.Object, _mockProtcolDownloadSegmentRetries.Object, new InitializeDownload());
            ResponseBase response = fileDownloader.StartDownload(rd, configSetting);
            Assert.IsTrue(response.Denied);
            _mockSegementProcessorDownloadSegmentRetries.Verify(m=>m.ProcessSegment(It.IsAny<ResourceDetail>(), It.IsAny<IProtocolDownloader>(), It.IsAny<Segment>()));
        }

        [Test]
        public void IsGetFileInfoSupportRetries()
        {
            _mockinitializeDownload.Setup(e => e.AllocateSpace(It.IsAny<string>(), It.IsAny<string>()))
                .Returns(() => new ResponseBase<string>() {Denied = false});
            FileDownloader fileDownloader = new FileDownloader(new SegmentProcessor (), _mockProtcolGetFileInfoRetries.Object, _mockinitializeDownload.Object);
            ResponseBase response=fileDownloader.StartDownload(new ResourceDetail(), new ConfigurationSetting());
            Assert.IsTrue(response.Denied);
            _mockProtcolGetFileInfoRetries.Verify(m=>m.GetFileInfo(new ResourceDetail()), Times.Exactly(_maxTries));
        }

    }
}
