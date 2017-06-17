using AgodaFileDownloader.Service;
using AgodaFileDownloader.Service.ServiceImplementaion;
using NUnit.Framework;
using Assert = Microsoft.VisualStudio.TestTools.UnitTesting.Assert;

namespace AgodaFileDownloader.UTest
{
    [TestFixture]
    public class UTDownloadProtocols
    {


        


        private HttpProtocolDownloader _httpProtocolDownlaoder;
        private FtpProtocolDownloader _ftpProtocolDownlaoder;
        private ResourceDetail _resourceDetailFtp;
        private ResourceDetail _resourceDetailhttp;
       [SetUp]
        public void Init()
        {
            //these 2 urls are well knwon resource for testing
            _resourceDetailFtp = new ResourceDetail()
            {
                Url = "ftp://speedtest.tele2.net/1MB.zip"
            };

            _resourceDetailhttp = new ResourceDetail()
            {
                Url = "http://httpbin.org/robots.txt"
            };
            
            _ftpProtocolDownlaoder = new FtpProtocolDownloader();
            _httpProtocolDownlaoder = new HttpProtocolDownloader();
        }

        [Test]
        public void IsHttpGetFileInfoWorks()
        {
            var response = _httpProtocolDownlaoder.GetFileInfo(_resourceDetailhttp);

            Assert.IsFalse(false);
            Assert.IsNotNull(response.ReturnedValue);
        }

        [Test]
        public void IsHttpCreateStreamWorks()
        {
            var response = _httpProtocolDownlaoder.GetFileInfo(_resourceDetailhttp);

            Assert.IsFalse(false);
            Assert.IsNotNull(response.ReturnedValue);


        }

        [Test]
        public void IsFTPGetFileInfoWorks()
        {
            var responseGetInfo = _ftpProtocolDownlaoder.GetFileInfo(_resourceDetailFtp);

            var response = _ftpProtocolDownlaoder.CreateStream(_resourceDetailFtp, 0, responseGetInfo.ReturnedValue.FileSize);

            Assert.IsFalse(false);
            Assert.IsNotNull(response.ReturnedValue);
        }


        [Test]
        public void IsFTPCreateStreamWorks()
        {
            var responseFileSize = _ftpProtocolDownlaoder.GetFileInfo(_resourceDetailFtp);
            
            var response = _ftpProtocolDownlaoder.CreateStream(_resourceDetailFtp,0, responseFileSize.ReturnedValue.FileSize);

            Assert.IsFalse(false);
            Assert.IsNotNull(response.ReturnedValue);
        }
    }
}
