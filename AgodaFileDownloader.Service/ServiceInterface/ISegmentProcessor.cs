using AgodaFileDownloader.Helper;
using AgodaFileDownloader.Model;

namespace AgodaFileDownloader.Service.ServiceInterface
{
    public interface ISegmentProcessor
    {
        ResponseBase ProcessSegment(ResourceDetail rl, Segment segment);
        ResponseBase<CalculatedSegment[]> GetSegments(int segmentCount, RemoteFileDetail remoteFileInfo);

        IProtocolDownloader ProtocolDownloader { get; set; }
    }
}
