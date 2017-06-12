using System;

namespace AgodaFileDownloader.Model
{
    public class RemoteFileDetail
    {
        public string MimeType { get; set; }
        public bool AcceptRanges { get; set; }
        public long FileSize { get; set; }
        public DateTime LastModified { get; set; } = DateTime.MinValue;
    }
}