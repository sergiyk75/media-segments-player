using System;

namespace MediaPlaybackLib
{
    public class MediaSegment
    {
        public TimeSpan Offset { get; private set; }
        public TimeSpan Duration { get; private set; }
        public Uri Source { get; private set; }
        public object Data { get; set; }

        public MediaSegment(TimeSpan offset, TimeSpan duration, Uri source)
        {
            Offset = offset;
            Duration = duration;
            Source = source;
        }

        public bool InRange(TimeSpan position)
        {
            return position.InRange(Offset, Offset + Duration);
        }
    }
}
