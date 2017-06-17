namespace AgodaFileDownloader.Model
{
    public class AuthenticatedUrl
    {
        public string Url { get; set; }
        public string Login { get; set; }
        public string Password { get; set; }
    }

    public class ConfigurationSetting
    {
        public string LocalFilePath { get; set; }
        public int NumberOfTrial { get; set; }
        public int RetrialDelay { get; set; }
        public int NumberOfSegments { get; set; }
        public long MinSizeSegment { get; set; }
        public int Timeout { get; set; }
        public string LogFilePath{get; set; }
    }
}
