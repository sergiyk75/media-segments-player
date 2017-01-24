using System;

namespace MediaPlaybackLib.MultiThumbSlider
{
    public class MarkerAddRequestEventArgs : EventArgs
    {
        public TimeSpan StartTime { get; private set; }
        public TimeSpan? Duration { get; private set; }

        public MarkerAddRequestEventArgs(TimeSpan startTime, TimeSpan? duration)
        {
            StartTime = startTime;
            Duration = duration;
        }
    }
}