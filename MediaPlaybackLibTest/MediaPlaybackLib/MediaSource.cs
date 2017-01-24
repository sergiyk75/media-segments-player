using System;
using System.Collections.Generic;
using System.Linq;

namespace MediaPlaybackLib
{
    public class MediaSource
    {
        public MediaSource(IEnumerable<MediaSegment> segments)
        {
            Segments = segments;

            if (segments.Any())
                Duration = Segments.Max(s => s.Offset + s.Duration);
        }

        public IEnumerable<MediaSegment> Segments { get; private set; }
        public bool HasVideo { get; set; }
        public TimeSpan Duration { get; private set; }

    }
}
