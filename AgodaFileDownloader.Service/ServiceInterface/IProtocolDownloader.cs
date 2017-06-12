using System.IO;
using AgodaFileDownloader.Model;

namespace AgodaFileDownloader.Service.ServiceInterface
{
    public interface IProtocolDownloader
    {

        Stream CreateStream(ResourceDetail rl, long initialPosition, long endPosition);

        RemoteFileDetail GetFileInfo(ResourceDetail rl);
    }
}
