using System;
using System.IO;

namespace AgodaFileDownloader.Model
{
    public class Segment
    {
        private long startPosition;
        private string lastError;
        private SegmentState state;
        private bool started = false;
        private DateTime lastReception = DateTime.MinValue;
        private double rate;
        private long start;
        private TimeSpan left = TimeSpan.Zero;

        public int CurrentTry { get; set; }

        public SegmentState State
        {
            get
            {
                return state;
            }
            set
            {
                state = value;

                switch (state)
                {
                    case SegmentState.Downloading:
                        BeginWork();
                        break;

                    case SegmentState.Connecting:
                    case SegmentState.Finished:
                    case SegmentState.Error:
                        rate = 0.0;
                        left = TimeSpan.Zero;
                        break;
                }
            }
        }

        public DateTime LastErrorDateTime { get; set; } = DateTime.MinValue;

        public string LastError
        {
            get
            {
                return lastError;
            }
            set
            {
                LastErrorDateTime = value != null ? DateTime.Now : DateTime.MinValue;
                lastError = value;
            }
        }

        public int Index { get; set; }

        public long InitialStartPosition { get; set; }

        public long StartPosition
        {
            get
            {
                return startPosition;
            }
            set
            {
                startPosition = value;
            }
        }

        public long Transfered => StartPosition - InitialStartPosition;

        public long TotalToTransfer => (EndPosition <= 0 ? 0 : EndPosition - InitialStartPosition);

        public long MissingTransfer => (EndPosition <= 0 ? 0 : EndPosition - StartPosition);


        public double Progress => (EndPosition <= 0 ? 0 : Transfered / (double)TotalToTransfer * 100.0f);

        public long EndPosition { get; set; }

        public Stream OutputStream { get; set; }

        public Stream InputStream { get; set; }

        public string CurrentURL { get; set; }

        public double Rate
        {
            get
            {
                if (State == SegmentState.Downloading)
                {
                    IncreaseStartPosition(0);
                    return rate;
                }
                return 0;
            }
        }

        public TimeSpan Left => left;

        public void BeginWork()
        {
            start = startPosition;
            lastReception = DateTime.Now;
            started = true;
        }

        public void IncreaseStartPosition(long size)
        {
            lock (this)
            {
                DateTime now = DateTime.Now;

                startPosition += size;

                if (started)
                {
                    TimeSpan ts = (now - lastReception);
                    if (ts.TotalSeconds == 0)
                    {
                        return;
                    }

                    // bytes per seconds
                    rate = ((double)(startPosition - start)) / ts.TotalSeconds;

                    if (rate > 0.0)
                    {
                        left = TimeSpan.FromSeconds(MissingTransfer / rate);
                    }
                    else
                    {
                        left = TimeSpan.MaxValue;
                    }
                }
                else
                {
                    start = startPosition;
                    lastReception = now;
                    started = true;
                }
            }
        }
    }
}
