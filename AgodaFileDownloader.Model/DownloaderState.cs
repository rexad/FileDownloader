namespace AgodaFileDownloader.Model
{
    public enum DownloaderState : byte
    {
        NeedToPrepare = 0,
        Preparing,
        WaitingForReconnect,
        Prepared,
        Working,
        Paused,
        Ended,
        EndedWithError
    }
}