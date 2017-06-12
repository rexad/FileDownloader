namespace AgodaFileDownloader.Model
{
    public struct CalculatedSegment
    {
        public long StartPosition { get; }

        public long EndPosition { get; }

        public CalculatedSegment(long startPos, long endPos)
        {
            EndPosition = endPos;
            StartPosition = startPos;
        }
    }
}
