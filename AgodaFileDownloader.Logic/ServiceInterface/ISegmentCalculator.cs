using AgodaFileDownloader.Model;

namespace AgodaFileDownloader.Logic.ServiceInterface
{
    public interface ISegmentCalculator
    {
        CalculatedSegment[] GetSegments(int segmentCount, RemoteFileDetail fileSize);
    }
}