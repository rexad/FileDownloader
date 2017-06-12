using System.IO;
using FileDownloader.Logic.Infrastucture;

namespace FileDownloader.Logic.ProtocolProvider
{
    public interface IProtocolProvider
    {
        
        void Initialize(Downloader downloader);

        Stream CreateStream(ResourceLocation rl, long initialPosition, long endPosition);

        RemoteFileInfo GetFileInfo(ResourceLocation rl, out Stream stream);
    }
}
