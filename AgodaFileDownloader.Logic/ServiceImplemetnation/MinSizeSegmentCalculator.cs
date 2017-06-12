using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AgodaFileDownloader.Logic.Model;
using AgodaFileDownloader.Logic.ServiceInterface;
using AgodaFileDownloader.Model;

namespace AgodaFileDownloader.Logic.ServiceImplemetnation
{
    public class MinSizeSegmentCalculator : ISegmentCalculator
    {

        public CalculatedSegment[] GetSegments(int segmentCount, RemoteFileDetail remoteFileInfo)
        {
            long minSize = 200000;
            long segmentSize = remoteFileInfo.FileSize/segmentCount;

            while (segmentCount > 1 && segmentSize < minSize)
            {
                segmentCount--;
                segmentSize = remoteFileInfo.FileSize/segmentCount;
            }

            long startPosition = 0;

            List<CalculatedSegment> segments = new List<CalculatedSegment>();

            for (int i = 0; i < segmentCount; i++)
            {
                if (segmentCount - 1 == i)
                {
                    segments.Add(new CalculatedSegment(startPosition, remoteFileInfo.FileSize));
                }
                else
                {
                    segments.Add(new CalculatedSegment(startPosition, startPosition + (int) segmentSize));
                }

                startPosition = segments[segments.Count - 1].EndPosition;
            }

            return segments.ToArray();
        }
    }
}
