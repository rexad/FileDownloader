using System.IO;

namespace AgodaFileDownloader.Model
{
    public interface IProtocolDownloader
    {

        Stream CreateStream(ResourceDetail rl, long initialPosition, long endPosition);

        RemoteFileDetail GetFileInfo(ResourceDetail rl);
    }
}
