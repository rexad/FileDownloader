using System.Collections.Generic;
using AgodaFileDownloader.Helper;
using AgodaFileDownloader.Model;

namespace AgodaFileDownloader.Service.ServiceInterface
{
    public interface ISegmentProcessor
    {
        ResponseBase ProcessSegment(ResourceDetail rl, Segment segment);
        ResponseBase<List<Segment>> GetSegments(int segmentCount, RemoteFileDetail remoteFileInfo);

        IProtocolDownloader ProtocolDownloader { get; set; }
    }
}
