using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Windows;
using System.Windows.Media;

namespace MediaPlaybackLib
{
    [ExcludeFromCodeCoverage]
    internal class MediaTimescalePresenter : FrameworkElement
    {
        private class TickInfo
        {
            public string Label;
            public int Level;
            public TimeSpan Timestamp;
        }

        private MediaPlaybackControl timeline;
        private PropertyChangeNotifier mediaSourceChangeNotifier;

        public MediaTimescalePresenter()
        {
            RenderOptions.SetEdgeMode(this, EdgeMode.Aliased);
            timeline = VisualTreeHelperEx.FindParent<MediaPlaybackControl>(this);

            Loaded += delegate
            {
                timeline = VisualTreeHelperEx.FindParent<MediaPlaybackControl>(this);

                if (timeline != null)
                {
                    this.mediaSourceChangeNotifier = new PropertyChangeNotifier(timeline, MediaPlaybackControl.MediaSourceProperty);
                    this.mediaSourceChangeNotifier.ValueChanged += (s, e) => InvalidateVisual();

                    InvalidateVisual();
                }
            };
        }

        [SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
        public Brush MinimizedAudioBrush { get; set; }

        private TimeSpan Duration
        {
            get { return timeline.MediaDuration; }
        }

        private double AreaThreshold
        {
            get { return ActualWidth / 500; }
        }

        protected override void OnRender(DrawingContext dc)
        {
            if (timeline == null)
                return;

            if (timeline.MediaSource == null)
                return;

            var width = ActualWidth;
            var height = ActualHeight;

            if (MinimizedAudioBrush != null)
                dc.DrawRectangle(MinimizedAudioBrush, null, new Rect(new Size(ActualWidth, ActualHeight)));

            // render no media content segments
            foreach (var segment in timeline.MediaSource.Segments.Where(x => x.Source != null))
            {
                var rect = new Rect(TimeSpanToXPos(segment.Offset), 0, TimeSpanToXPos(segment.Duration), height);
                dc.DrawRectangle(timeline.Background, null, rect);
            }

            // render time ticks
            foreach (var tick in GetTimeTicks())
            {
                if (tick.Timestamp == TimeSpan.Zero)
                    continue;


                var scaleFactor = (tick.Level == 1) ? 1 : Math.Pow(0.92, (tick.Level - 1));
                var lineHeight = height * scaleFactor;
                var x = TimeSpanToXPos(tick.Timestamp);

                if (tick.Label != null)
                {
                    var ft = new FormattedText(
                        tick.Label,
                        CultureInfo.CurrentCulture,
                        timeline.FlowDirection,
                        new Typeface(timeline.FontFamily, timeline.FontStyle, timeline.FontWeight, timeline.FontStretch),
                        //timeline.FontSize - tick.Level,
                        tick.Label.Contains(Environment.NewLine) ? 10 - 1 - tick.Level : 10,
                        timeline.Foreground);

                    //Check for RTL culture
                    if (Thread.CurrentThread.CurrentUICulture.TextInfo.IsRightToLeft)
                    {
                        //If RTL, the way the text is drawn will cause the date's to be flipped.
                        //Using the matrix transform to correct this
                        //Because of the transform the x point is also flipped across the x-axis,
                        //That is corrected by setting the point up negatively along x-axis.
                        dc.PushTransform(new MatrixTransform(-1, 0, 0, 1, 0, 0));
                        dc.DrawText(ft, new Point(-(x - ft.Width / 2), 0));
                        //remove the transformation from the stack so it doesn't interfere with anything else.
                        dc.Pop();
                    }
                    else
                        dc.DrawText(ft, new Point(x - ft.Width / 2, 0));

                    lineHeight -= ft.Height - 4;
                }

                dc.DrawLine(new Pen(timeline.TimelineTickBrush, 1), new Point(x, height), new Point(x, height - lineHeight));
            }
        }

        private double TimeSpanToXPos(TimeSpan value)
        {
            return ActualWidth * ((double)value.Ticks / Duration.Ticks);
        }

        private TimescaleLevel GetTimescaleLevel()
        {
            var threshold = ActualWidth / 500;
            var length = Duration.TotalSeconds;

            if (length <= 1)
                return TimescaleLevel.Millisecond;

            if (length < 800 * threshold)
                return TimescaleLevel.Second;

            if ((length / 60) <= 150 * threshold)
                return TimescaleLevel.Minute;

            if ((length / 3600) < 50 * threshold)
                return TimescaleLevel.Hour;

            if ((length / 86400) < 50 * threshold)
                return TimescaleLevel.Day;

            if ((length / 86400) < 800 * threshold)
                return TimescaleLevel.Month;

            if ((length / 86400) / 365 < 30 * threshold)
                return TimescaleLevel.Year;

            return TimescaleLevel.None;
        }

        private IEnumerable<TickInfo> GetTimeTicks()
        {
            switch (GetTimescaleLevel())
            {
                case TimescaleLevel.Second:
                    return GetTimeTicksSeconds();
                case TimescaleLevel.Minute:
                    return GetTimeTicksMinutes();
                case TimescaleLevel.Hour:
                    return GetTimeTicksHours();
                default:
                    return Enumerable.Empty<TickInfo>();
            }
        }

        /// <summary>
        /// Gets a <see cref="TickInfo"/> for the provided value. This generated tick may contain a text label with a tick mark, a
        /// medium-height tick mark with no text, or a short tick mark with no text.
        /// </summary>
        private TickInfo GetTimescaleTick(double currentValue, int fullTickValue, int halfTickValue, TimeSpan stepValue, string dateTimeformat, ref TimeSpan timestamp)
        {
            var timescaleTick = new TickInfo
            {
                Timestamp = timestamp,
            };

            // Determine if a full-height tick with text is to be drawn, or if a blank tick is to be used
            if (currentValue % fullTickValue == 0)
            {
                timescaleTick.Level = 3;
                timescaleTick.Label = timestamp.ToString(dateTimeformat, CultureInfo.InvariantCulture);
            }
            else
                timescaleTick.Level = (timestamp.TotalSeconds % halfTickValue == 0) ? 8 : 16;

            // Update the incremental tracker for the next marker in the scale
            timestamp = timestamp.Add(stepValue);

            return timescaleTick;
        }

        private IEnumerable<TickInfo> GetTimeTicksSeconds()
        {
            var startTime = TimeSpan.Zero;
            const string SeocondsFormat = @"m\:ss";

            while (startTime < Duration)
            {
                if (Duration.TotalSeconds < 30 * AreaThreshold)
                    yield return GetTimescaleTick(startTime.TotalSeconds, 2, 1, TimeSpan.FromSeconds(1), SeocondsFormat, ref startTime);
                else if (Duration.TotalSeconds < 175 * AreaThreshold)
                    yield return GetTimescaleTick(startTime.TotalSeconds, 15, 5, TimeSpan.FromSeconds(1), SeocondsFormat, ref startTime);
                else if (Duration.TotalSeconds < 300 * AreaThreshold)
                    yield return GetTimescaleTick(startTime.TotalSeconds, 30, 15, TimeSpan.FromSeconds(5), SeocondsFormat, ref startTime);
                else
                    yield return GetTimescaleTick(startTime.TotalSeconds, 60, 30, TimeSpan.FromSeconds(15), SeocondsFormat, ref startTime);
            }
        }

        private IEnumerable<TickInfo> GetTimeTicksMinutes()
        {
            var startTime = TimeSpan.Zero;
            const string MinutesFormat = @"h\:mm";

            while (startTime < Duration)
            {
                if (Duration.TotalMinutes < 30 * AreaThreshold)
                    yield return GetTimescaleTick(startTime.TotalMinutes, 2, 1, TimeSpan.FromMinutes(1.0), MinutesFormat, ref startTime);
                else if (Duration.TotalMinutes < 175 * AreaThreshold)
                    yield return GetTimescaleTick(startTime.TotalMinutes, 15, 5, TimeSpan.FromMinutes(1), MinutesFormat, ref startTime);
                else if (Duration.TotalMinutes < 300 * AreaThreshold)
                    yield return GetTimescaleTick(startTime.TotalMinutes, 30, 15, TimeSpan.FromMinutes(5), MinutesFormat, ref startTime);
                else
                    yield return GetTimescaleTick(startTime.TotalMinutes, 60, 30, TimeSpan.FromMinutes(15), MinutesFormat, ref startTime);
            }
        }

        private IEnumerable<TickInfo> GetTimeTicksHours()
        {
            var startTime = TimeSpan.Zero;
            const string HoursFormat = @"d\.hh";

            while (startTime < Duration)
            {
                if (Duration.TotalHours < 30 * AreaThreshold)
                    yield return GetTimescaleTick(startTime.TotalHours, 2, 1, TimeSpan.FromHours(1), HoursFormat, ref startTime);
                else if (Duration.TotalHours < 175 * AreaThreshold)
                    yield return GetTimescaleTick(startTime.TotalHours, 3, 1, TimeSpan.FromHours(1), HoursFormat, ref startTime);
                else if (Duration.TotalHours < 300 * AreaThreshold)
                    yield return GetTimescaleTick(startTime.TotalHours, 6, 3, TimeSpan.FromHours(1), HoursFormat, ref startTime);
                else
                    yield return GetTimescaleTick(startTime.TotalHours, 24, 12, TimeSpan.FromHours(6), HoursFormat, ref startTime);
            }
        }
    }
}
