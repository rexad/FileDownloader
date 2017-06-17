using System.IO;
using System.Text;
using AgodaFileDownloader.Model;
using AgodaFileDownloader.Service;
using AgodaFileDownloader.Service.ServiceInterface;
using AgodaFileDownloader.Helper;
using AgodaFileDownloader.Service.ServiceImplementaion;
using Moq;
using NUnit.Framework;
using Assert = NUnit.Framework.Assert;

namespace AgodaFileDownloader.UTest
{
    [TestFixture]
    public class UtIOSegments
    {
        Mock<IInitializeDonwload> _mockintializeDownload;
        Mock<IProtocolDownloader> _protocolDownloadSuccessMock = new Mock<IProtocolDownloader>();
        Mock<IProtocolDownloader> _protocolFailureMock = new Mock<IProtocolDownloader>();

        private MemoryStream _testInStream=new MemoryStream();
        private long _testStreamLength;
        Segment _segment = new Segment();
        Segment _segmentCheckPosition = new Segment();
        ResourceDetail resourceDetail = new ResourceDetail();
        RemoteFileDetail remoteFileInfo=new RemoteFileDetail();
        [SetUp]
        public void Initialize()
        {
            MockDataCreation();
            
        }

        [Test]
        public void IsSegmentWritten()
        {
            var processor = new SegmentProcessor();
            var result=processor.ProcessSegment(resourceDetail, _protocolDownloadSuccessMock.Object, _segment);
            Assert.IsFalse(result.Denied);
            Assert.IsNotNull(_segment.OutputStream);
            Assert.AreEqual(_segment.StartPosition, _segment.EndPosition);
            string valueWritten;
            _segment.OutputStream.Position = 0;
            using (StreamReader reader = new StreamReader(_segment.OutputStream))
            {
                valueWritten=reader.ReadToEnd();
            }
          
            Assert.AreEqual(valueWritten, "a test string for the stream");
            _segment.OutputStream.Close();
        }


        [Test]
        public void IsSegmentStreamCreationSupportFail()
        {
            var processor = new SegmentProcessor();
            ResponseBase result = processor.ProcessSegment(resourceDetail, _protocolFailureMock.Object, _segment);
            Assert.IsTrue(result.Denied);
            Assert.AreEqual(_segment.CurrentTry, 1);
            Assert.AreEqual(_segment.StartPosition, 0);

        }
        
        [Test]
        public void IsSegmentStreamWritingSupportFail()
        {
            var processor = new SegmentProcessor();
            _segment.OutputStream = null;
            _segment=new Segment();
            ResponseBase result = processor.ProcessSegment(resourceDetail, _protocolDownloadSuccessMock.Object, _segment);
            Assert.IsTrue(result.Denied);
            Assert.AreEqual(_segment.CurrentTry,1);
            Assert.AreEqual(_segment.StartPosition, 0);
        }

        [Test]
        public void IsStartedPositionMoved()
        {
            var processor = new SegmentProcessor();
            _segmentCheckPosition.OutputStream = new MemoryStream();
            var result = processor.ProcessSegment(resourceDetail, _protocolDownloadSuccessMock.Object, _segmentCheckPosition);
            Assert.IsFalse(result.Denied);
            Assert.IsNotNull(_segmentCheckPosition.OutputStream);
            Assert.AreEqual(_segmentCheckPosition.OutputStream.Position, _testStreamLength);
            _segmentCheckPosition.OutputStream.Close();

        }

        [Test]
        public void IsFileSplitToSegments()
        {
            var processor = new SegmentProcessor();
            int numberOfSegments = 10;
            remoteFileInfo.FileSize = 2500000;
            var listSegmentResponse = processor.GetSegments(numberOfSegments, 200000, remoteFileInfo, "FileName");
            Assert.IsFalse(listSegmentResponse.Denied);
            Assert.IsNotNull(listSegmentResponse.ReturnedValue);
            var listSegment = listSegmentResponse.ReturnedValue;
            Assert.AreEqual(listSegment.Count,10);
            long startPosition = 0;
            long endPosition = 250000;
            foreach (Segment t in listSegment)
            {
                Assert.AreEqual(t.StartPosition, startPosition);
                Assert.AreEqual(t.EndPosition, endPosition);
                startPosition = endPosition;
                endPosition=endPosition+ remoteFileInfo.FileSize/10;
            }
        }

        [Test]
        public void IsFileSplitSingleIfInferiorToMiSize()
        {
            var processor = new SegmentProcessor();
            int numberOfSegments = 10;
            remoteFileInfo.FileSize = 170000;
            var listSegmentResponse = processor.GetSegments(numberOfSegments, 200000, remoteFileInfo, "FileName");
            Assert.IsFalse(listSegmentResponse.Denied);
            Assert.IsNotNull(listSegmentResponse.ReturnedValue);
            var listSegment = listSegmentResponse.ReturnedValue;
            Assert.AreEqual(listSegment.Count, 1);
            long startPosition = 0;
            long endPosition = 170000;

            Assert.AreEqual(listSegment[0].StartPosition, startPosition);
            Assert.AreEqual(listSegment[0].EndPosition, endPosition);
           
        }
        
        [Test]
        public void IsFileSplitInferiorToMiSize()
        {
            var processor = new SegmentProcessor();
            int numberOfSegments = 10;
            remoteFileInfo.FileSize = 230000;
            var listSegmentResponse = processor.GetSegments(numberOfSegments, 200000, remoteFileInfo, "FileName");
            Assert.IsFalse(listSegmentResponse.Denied);
            Assert.IsNotNull(listSegmentResponse.ReturnedValue);
            var listSegment = listSegmentResponse.ReturnedValue;
            Assert.AreEqual(listSegment.Count, 1);
            long startPosition = 0;
            long endPosition = 230000;

            Assert.AreEqual(listSegment[0].StartPosition, startPosition);
            Assert.AreEqual(listSegment[0].EndPosition, endPosition);

        }
        
        [Test]
        public void IsSegmentSupportErrors()
        {
            
        }
        
        void MockDataCreation()
        {


            _mockintializeDownload = new Mock<IInitializeDonwload>();
            _mockintializeDownload.Setup(e => e.InitConfigData())
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


            #region initSuccessProtocolDownloader
            _testInStream = new MemoryStream(Encoding.UTF8.GetBytes("a test string for the stream"));
            MemoryStream testOutStream = new MemoryStream();
            _testStreamLength= _testInStream.Length;
            _segmentCheckPosition.EndPosition = _testStreamLength;
            _segment.OutputStream = testOutStream;
            _segment.EndPosition = _testInStream.Length;
            _protocolDownloadSuccessMock.Setup(e => e.GetFileInfo(It.IsAny<ResourceDetail>()))
               .Returns(() => new ResponseBase<RemoteFileDetail>()
               {
                   Denied = false,
                   ReturnedValue = new RemoteFileDetail()
                   {
                       
                   }
               });
            _protocolDownloadSuccessMock.Setup(e => e.CreateStream(It.IsAny<ResourceDetail>(), It.IsAny<long>(), It.IsAny<long>()))
                .Returns(() => new ResponseBase<Stream>()
                {
                    Denied = false,
                    ReturnedValue = _testInStream
                });
            #endregion

            _protocolFailureMock.Setup(e => e.GetFileInfo(It.IsAny<ResourceDetail>()))
               .Returns(() => new ResponseBase<RemoteFileDetail>()
               {
                   Denied = false,
                   ReturnedValue = new RemoteFileDetail()
                   {

                   }
               });
            _protocolFailureMock.Setup(e => e.CreateStream(It.IsAny<ResourceDetail>(), It.IsAny<long>(), It.IsAny<long>()))
                .Returns(() => new ResponseBase<Stream>()
                {
                    Denied = true,
                    ReturnedValue = null
                });
            
        }
    }
}
