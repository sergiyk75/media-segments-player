# media-segments-player

Originally this experimental project was conceived to implement multi-segment audio file playback.
Other features like themes, time tags, section tags and system(readonly) tags were added later.

This project is implemented using Microsoft Windows Presentation Framework or WPF.

Media source is the class that allows specifying multiple media segments. Each media segment has following properties: uri, offset and duration. Media segment uri can be a local file as well as a http url.

```csharp
   public class MediaSource
    {
        public IEnumerable<MediaSegment> Segments { get; private set; }
        public bool HasVideo { get; set; }
        public TimeSpan Duration { get; private set; }
    }
    
    public class MediaSegment
    {
        public TimeSpan Offset { get; private set; }
        public TimeSpan Duration { get; private set; }
        public Uri Source { get; private set; }
    }
    
```

### Cool stuff

* **ProgressIndicator** - a pretty neat visual control that shows an animated wheel
* **NotifyObject** - a super cool class. It simplifies MVVM binding a lot
* **MediaPlaybackElement** - is the meat of this project. All segment media playback code is here
* **MediaPlaybackControl** - is a visual control that adds player buttons and support for media tags. It is essentially a visual presentation control for **MediaPlaybackElement**
* **MediaTimescalePresenter** - visaul control that knows how to display timeline: hours, minutes or seconds, depending on the media duration

#### Playback loop
![Alt text](/../snapshots/loop.png?raw=true "Playback loop")

#### Time tags
![Alt text](/../snapshots/time-tag.png?raw=true "Time Tags")

#### Section tags
![Alt text](/../snapshots/section-tag.png?raw=true "Section Tags")

#### System(Readonly) Tags
![Alt text](/../snapshots/system-tags.png?raw=true "System(Readonly) Tags")
