namespace AgodaFileDownloader.Model
{
    public enum SegmentState
    {
        Idle,
        Connecting,
        Downloading,
        Finished,
        Error,
    }
}