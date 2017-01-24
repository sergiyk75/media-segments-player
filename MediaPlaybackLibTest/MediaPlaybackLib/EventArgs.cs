using System;

namespace MediaPlaybackLib
{
    public class EventArgs<TPayload> : EventArgs
    {
        public EventArgs(TPayload payload)
        {
            Payload = payload;
        }

        public TPayload Payload { get; private set; }
    }
}
