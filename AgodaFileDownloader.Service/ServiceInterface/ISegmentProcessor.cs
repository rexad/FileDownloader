using System.Collections.Generic;
using AgodaFileDownloader.Helper;
using AgodaFileDownloader.Model;

namespace AgodaFileDownloader.Service.ServiceInterface
{
    public interface ISegmentProcessor
    {
        ResponseBase ProcessSegment(ResourceDetail rl, IProtocolDownloader protocolDownloader,Segment segment);
        ResponseBase<List<Segment>> GetSegments(int segmentCount, RemoteFileDetail remoteFileInfo, string fileName);
        
    }
}
