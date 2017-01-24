using System;
using System.Diagnostics.CodeAnalysis;

namespace MediaPlaybackLib.MultiThumbSlider
{
    [ExcludeFromCodeCoverage]
    public class MediaMarker : NotifyObject
    {
        public MediaMarker(Guid id, string typeKey, bool isReadOnly)
        {
            Id = id;
            TypeKey = typeKey;
            IsReadOnly = isReadOnly;
            IsVisible = true;
            IsDirty = false;
            IsSystemTag = false;
        }

        public MediaMarker(Guid id, string typeKey, bool isReadOnly, TimeSpan startTime, TimeSpan? length, string name, string desc, bool isSystemTag)
        {
            Id = id;
            TypeKey = typeKey;
            IsReadOnly = isReadOnly;
            IsVisible = true;

            Name = name;
            StartTime = startTime;
            Length = length;
            Description = desc;
            IsSystemTag = isSystemTag;

            IsDirty = false;
        }

        public Guid Id
        {
            get { return Get(() => Id); }
            private set { Set(() => Id, value); }
        }

        /// <summary>
        /// Gets or sets the type term key of this media marker.  The type is used to distinguish this marker between
        /// call start, call end, section tags, time tags, etc.
        /// </summary>
        public string TypeKey
        {
            get { return Get(() => TypeKey); }
            set { Set(() => TypeKey, value); }
        }

        public TimeSpan StartTime
        {
            get { return Get(() => StartTime); }
            set { Set(() => StartTime, value);}
        }

        public TimeSpan? Length
        {
            get { return Get(() => Length); }
            set
            {
                if (string.IsNullOrEmpty(Name))
                    Name = value.HasValue ? Res.MediaMarkerName_SectionTag : Res.MediaMarkerName_TimeTag;
                Set(() => Length, value);
            }
        }

        public string Name
        {
            get { return Get(() => Name); }
            set { Set(() => Name, value); }
        }

        public string Description
        {
            get { return Get(() => Description); }
            set { Set(() => Description, value); }
        }

        public bool IsVisible
        {
            get { return Get(() => IsVisible); }
            set { Set(() => IsVisible, value); }
        }

        public bool IsReadOnly
        {
            get { return Get(() => IsReadOnly); }
            private set { Set(() => IsReadOnly, value); }
        }

        public bool IsDirty
        {
            get { return Get(() => IsDirty, false); }
            private set { Set(() => IsDirty, value); }
        }

        public bool IsSystemTag
        {
            get { return Get(() => IsSystemTag, false); }
            private set { Set(() => IsSystemTag, value); }
        }

        [DependsUpon("StartTime")]
        [DependsUpon("Length")]
        [DependsUpon("Name")]
        [DependsUpon("Description")]
        private void WhenPropertyChanges()
        {
            IsDirty = true;
        }

    }
}