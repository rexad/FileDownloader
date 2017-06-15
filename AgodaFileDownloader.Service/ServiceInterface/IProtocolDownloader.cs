using System.IO;
using AgodaFileDownloader.Helper;
using AgodaFileDownloader.Model;

namespace AgodaFileDownloader.Service.ServiceInterface
{
    public interface IProtocolDownloader
    {

        ResponseBase<Stream> CreateStream(ResourceDetail rl, long initialPosition, long endPosition);

        ResponseBase<RemoteFileDetail> GetFileInfo(ResourceDetail rl);
    }
}
