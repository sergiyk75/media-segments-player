using System;

namespace MediaPlaybackLib.MultiThumbSlider
{
    public class MarkerModifiedEventArgs : EventArgs
    {
        public Guid Key { get; private set; }
        public string Description { get; private set; }
        public bool Delete { get; private set; }
        public TimeSpan? StartTime { get; private set; }
        public TimeSpan? Length { get; private set; }

        public MarkerModifiedEventArgs(Guid key, string description)
        {
            Key = key;
            Description = description;
            Delete = false;
        }

        public MarkerModifiedEventArgs(Guid key, string description, TimeSpan? startTime, TimeSpan? length )
        {
            Key = key;
            Description = description;
            Delete = false;
            StartTime = startTime;
            Length = length;
        }

        public MarkerModifiedEventArgs(Guid key, bool delete)
        {
            Key = key;
            Delete = delete;
        }
    }
}