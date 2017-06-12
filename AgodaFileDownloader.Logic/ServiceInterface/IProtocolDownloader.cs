using System.IO;
using AgodaFileDownloader.Logic.Model;
using AgodaFileDownloader.Model;

namespace AgodaFileDownloader.Logic.ServiceInterface
{
    public interface IProtocolDownloader
    {

        Stream CreateStream(ResourceDetail rl, long initialPosition, long endPosition);

        RemoteFileDetail GetFileInfo(ResourceDetail rl);
    }
}
