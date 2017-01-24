using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Threading;
using Microsoft.Practices.Prism.Commands;

namespace MediaPlaybackLib
{
    public class MediaPlaybackElement : FrameworkElement
    {
        public readonly static DependencyProperty SourceProperty =
            DependencyProperty.Register("Source", typeof(MediaSource), typeof(MediaPlaybackElement), new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.AffectsMeasure, (d, e) => ((MediaPlaybackElement)d).OnMediaSourceChanged()));

        public readonly static DependencyProperty PositionProperty =
            DependencyProperty.Register("Position", typeof(TimeSpan), typeof(MediaPlaybackElement), new FrameworkPropertyMetadata(TimeSpan.Zero, (d, e) => ((MediaPlaybackElement)d).OnPositionChanged(), (d, value) => ((MediaPlaybackElement)d).CoercePosition((TimeSpan)value, true)));

        public readonly static DependencyPropertyKey MinPositionPropertyKey =
            DependencyProperty.RegisterReadOnly("MinPosition", typeof(TimeSpan?), typeof(MediaPlaybackElement), null);
        public readonly static DependencyProperty MinPositionProperty = MinPositionPropertyKey.DependencyProperty;

        public readonly static DependencyPropertyKey MaxPositionPropertyKey =
            DependencyProperty.RegisterReadOnly("MaxPosition", typeof(TimeSpan?), typeof(MediaPlaybackElement), null);
        public readonly static DependencyProperty MaxPositionProperty = MaxPositionPropertyKey.DependencyProperty;

        public readonly static DependencyProperty RepeatEnabledProperty =
            DependencyProperty.Register("RepeatEnabled", typeof(bool), typeof(MediaPlaybackElement));

        public readonly static DependencyProperty IsMutedProperty =
            DependencyProperty.Register("IsMuted", typeof(bool), typeof(MediaPlaybackElement), new FrameworkPropertyMetadata(false, (d, e) => ((MediaPlaybackElement)d).UpdateMediaPlayerProperties()));

        public readonly static DependencyProperty VolumeProperty =
            DependencyProperty.Register("Volume", typeof(double), typeof(MediaPlaybackElement), new FrameworkPropertyMetadata(1.0, (d, e) => ((MediaPlaybackElement)d).UpdateMediaPlayerProperties()));

        public readonly static DependencyProperty BalanceProperty =
            DependencyProperty.Register("Balance", typeof(double), typeof(MediaPlaybackElement), new FrameworkPropertyMetadata(0.0, (d, e) => ((MediaPlaybackElement)d).UpdateMediaPlayerProperties()));

        private readonly static DependencyPropertyKey IsPlayingPropertyKey =
            DependencyProperty.RegisterReadOnly("IsPlaying", typeof(bool), typeof(MediaPlaybackElement), null);
        public readonly static DependencyProperty IsPlayingProperty = IsPlayingPropertyKey.DependencyProperty;

        private readonly static DependencyPropertyKey IsMediaOpeningPropertyKey =
            DependencyProperty.RegisterReadOnly("IsMediaOpening", typeof(bool), typeof(MediaPlaybackElement), null);
        public readonly static DependencyProperty IsMediaOpeningProperty = IsMediaOpeningPropertyKey.DependencyProperty;

        private readonly static DependencyPropertyKey IsMediaBufferingPropertyKey =
            DependencyProperty.RegisterReadOnly("IsMediaBuffering", typeof(bool), typeof(MediaPlaybackElement), null);
        public readonly static DependencyProperty IsMediaBufferingProperty = IsMediaBufferingPropertyKey.DependencyProperty;

        private readonly static DependencyPropertyKey BufferingProgressPropertyKey =
            DependencyProperty.RegisterReadOnly("BufferingProgress", typeof(double), typeof(MediaPlaybackElement), null);
        public readonly static DependencyProperty BufferingProgressProperty = BufferingProgressPropertyKey.DependencyProperty;

        private readonly static DependencyPropertyKey IsMediaOpenedPropertyKey =
            DependencyProperty.RegisterReadOnly("IsMediaOpened", typeof(bool), typeof(MediaPlaybackElement), null);
        public readonly static DependencyProperty IsMediaOpenedProperty = IsMediaOpenedPropertyKey.DependencyProperty;

        private readonly static DependencyPropertyKey HasMediaContentPropertyKey =
            DependencyProperty.RegisterReadOnly("HasMediaContent", typeof(bool), typeof(MediaPlaybackElement), null);
        public readonly static DependencyProperty HasMediaContentProperty = HasMediaContentPropertyKey.DependencyProperty;

        private readonly static DependencyPropertyKey MediaErrorPropertyKey =
            DependencyProperty.RegisterReadOnly("MediaError", typeof(Exception), typeof(MediaPlaybackElement), null);
        public readonly static DependencyProperty MediaErrorProperty = MediaErrorPropertyKey.DependencyProperty;

        private static object CoerseMinPositionProperty(DependencyObject d, object baseValue)
        {
            if (baseValue == null)
                return null;

            var control = (MediaPlaybackElement)d;
            var newValue = (TimeSpan?)baseValue;

            if (newValue < TimeSpan.Zero)
                return TimeSpan.Zero;

            if (newValue > control.MaxPosition)
                return control.MaxPosition;

            return baseValue;
        }

        private static object CoerseMaxPositionProperty(DependencyObject d, object baseValue)
        {
            if (baseValue == null)
                return null;

            var control = (MediaPlaybackElement)d;
            var newValue = (TimeSpan?)baseValue;

            if (newValue < TimeSpan.Zero)
                return TimeSpan.Zero;

            if (control.Source != null && newValue > control.Source.Duration)
                return control.Source.Duration;

            return newValue;
        }

        private readonly DispatcherTimer bufferingTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(100) };
        private readonly DispatcherTimer checkOpenMediaTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(100) };
        private readonly DispatcherTimer positionTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(10) };

        private readonly List<MediaSegment> mediaSegments = new List<MediaSegment>();
        private readonly List<MediaPlayer> mediaPlayers = new List<MediaPlayer>();
        private MediaPlayer activePlayer;
        private bool updatingPosition;

        private class InternalMediaPlayer : MediaPlayer
        {
            public bool WasOpened;
        }

        public MediaPlaybackElement()
        {
            TogglePlayPauseCommand = new DelegateCommand(TogglePlayPause);
            checkOpenMediaTimer.Tick += (s, e) => CheckMediaOpened();
            bufferingTimer.Tick += BufferingTimer_Tick;
            positionTimer.Tick += PositionTimer_Tick;
        }

        private void PositionTimer_Tick(object sender, EventArgs e)
        {
            if (!IsPlaying)
            {
                positionTimer.Stop();
                return;
            }

            if (activePlayer == null)
                return;

            var segment = GetMediaSegment(activePlayer);

            // Only move to the next segment if we are past the end of the max range (the user has selected a range) or if the position is past the end of the current segment, but not at the end of the media.
            // Note: MediaEnded event will take care of what happens once the end of media is reached (this prevents a race condition on Windows 7 involving looping)
            var position = segment.Offset + activePlayer.Position;
            if ((position >= segment.Offset + segment.Duration && position < activePlayer.NaturalDuration) || position >= MaxPosition)
            {
                SeekNextMediaSegment(segment);
            }
            else
            {
                updatingPosition = true;
                Position = position;
                updatingPosition = false;
            }
        }

        private void BufferingTimer_Tick(object sender, EventArgs e)
        {
            IsMediaBuffering = activePlayer?.IsBuffering == true;
            if (IsMediaBuffering)
            {
                BufferingProgress = activePlayer.BufferingProgress;
            }
            else
            {
                bufferingTimer.Stop();
                BufferingProgress = 0;
            }
        }

        public event EventHandler<ExceptionEventArgs> MediaFailed;

        public bool AutoOpenMedia { get; set; }

        public void TogglePlayPause()
        {
            if (IsPlaying)
                Pause();
            else
                Play();
        }

        public ICommand TogglePlayPauseCommand { get; private set; }

        public TimeSpan Position
        {
            get { return (TimeSpan)GetValue(PositionProperty); }
            set { SetValue(PositionProperty, value); }
        }

        public TimeSpan? MinPosition
        {
            get { return (TimeSpan?)GetValue(MinPositionProperty); }
            private set { SetValue(MinPositionPropertyKey, value); }
        }

        public TimeSpan? MaxPosition
        {
            get { return (TimeSpan?)GetValue(MaxPositionProperty); }
            private set { SetValue(MaxPositionPropertyKey, value); }
        }

        public bool IsPlaying
        {
            get { return (bool)GetValue(IsPlayingProperty); }
            private set { SetValue(IsPlayingPropertyKey, value); }
        }

        public bool IsMuted
        {
            get { return (bool)GetValue(IsMutedProperty); }
            set { SetValue(IsMutedProperty, value); }
        }

        public double Volume
        {
            get { return (double)GetValue(VolumeProperty); }
            set { SetValue(VolumeProperty, value); }
        }

        public double Balance
        {
            get { return (double)GetValue(BalanceProperty); }
            set { SetValue(BalanceProperty, value); }
        }

        public bool RepeatEnabled
        {
            get { return (bool)GetValue(RepeatEnabledProperty); }
            set { SetValue(RepeatEnabledProperty, value); }
        }

        public bool IsMediaOpened
        {
            get { return (bool)GetValue(IsMediaOpenedProperty); }
            private set { SetValue(IsMediaOpenedPropertyKey, value); }
        }

        public bool IsMediaOpening
        {
            get { return (bool)GetValue(IsMediaOpeningProperty); }
            private set { SetValue(IsMediaOpeningPropertyKey, value); }
        }

        public bool IsMediaBuffering
        {
            get { return (bool)GetValue(IsMediaBufferingProperty); }
            private set { SetValue(IsMediaBufferingPropertyKey, value); }
        }

        public double BufferingProgress
        {
            get { return (double)GetValue(BufferingProgressProperty); }
            private set { SetValue(BufferingProgressPropertyKey, value); }
        }

        public MediaSource Source
        {
            get { return (MediaSource)GetValue(SourceProperty); }
            set { SetValue(SourceProperty, value); }
        }

        public bool HasMediaContent
        {
            get { return (bool)GetValue(HasMediaContentProperty); }
            private set { SetValue(HasMediaContentPropertyKey, value); }
        }

        public Exception MediaError
        {
            get { return (Exception)GetValue(MediaErrorProperty); }
            private set { SetValue(MediaErrorPropertyKey, value); }
        }

        private void OnPositionChanged()
        {
            ApplyPosition();
        }

        public void SetPositionRange(TimeSpan min, TimeSpan max)
        {
            if (Source == null)
                return;

            // coerce min and max values
            MaxPosition = max.Min(Source.Duration);
            MinPosition = min.Max(TimeSpan.Zero).Min(max);

            Position = EnsureInRange(Position);
        }

        public void ClearPositionRange()
        {
            MaxPosition = null;
            MinPosition = null;
        }

        private void UpdateMediaPlayerProperties()
        {
            foreach (var player in mediaPlayers)
            {
                player.Volume = Volume;
                player.Balance = Balance;
                player.IsMuted = IsMuted;
            }
        }

        protected void OnMediaSourceChanged()
        {
            bufferingTimer.Stop();
            checkOpenMediaTimer.Stop();
            positionTimer.Stop();

            CloseMedia();
            mediaSegments.Clear();

            activePlayer = null;
            MediaError = null;
            IsMediaOpening = false;
            IsMediaOpened = false;
            IsMediaBuffering = false;
            BufferingProgress = 0;
            MinPosition = null;
            MaxPosition = null;
            Position = TimeSpan.Zero;
            HasMediaContent = false;

            if (Source != null)
            {
                mediaSegments.AddRange(Source.Segments.Where(x => x.Source != null).OrderBy(x => x.Offset));
                HasMediaContent = mediaSegments.Count > 0;
                Seek(TimeSpan.Zero, TimeSeekOrigin.BeginTime, true);
                if (HasMediaContent && AutoOpenMedia)
                    OpenMedia();
            }
        }

        private int mediaOpenedCount;

        private void OpenMedia()
        {
            foreach (var segment in mediaSegments)
            {
                var mediaPlayer = new InternalMediaPlayer();
                mediaPlayer.MediaOpened += MediaPlayer_MediaOpened;
                mediaPlayer.MediaFailed += MediaPlayer_MediaFailed;
                // Note: CustomizeUriForClientSystem is needed to accomodate shortcomings of Windows7 media foundation. It can be removed when Win7 support is no longer required.
                mediaPlayer.Open(segment.Source);

                mediaPlayers.Add(mediaPlayer);
            }

            UpdateMediaPlayerProperties();

            checkOpenMediaTimer.Start();

            IsMediaOpening = true;
        }

        private void CloseMedia()
        {
            mediaOpenedCount = 0;
            foreach (var player in mediaPlayers)
                player.Close();
            mediaPlayers.Clear();
        }

        private void MediaPlayer_MediaFailed(object sender, System.Windows.Media.ExceptionEventArgs e)
        {
            var player = (InternalMediaPlayer)sender;
            Debug.WriteLine(player.Source + " FAILED. " + e.ErrorException);

            Dispatcher.Invoke(new Action(() =>
            {
                IsMediaOpening = false;

                string errorMessage;

                var comException = e.ErrorException as System.Runtime.InteropServices.COMException;
                if (comException != null)
                {
                    switch ((uint)comException.ErrorCode)
                    {
                        case WmpErrorCodes.NS_E_WMP_AUDIO_HW_PROBLEM:
                            errorMessage = Res.ErrorAudioHardware;
                            break;
                        case WmpErrorCodes.NS_E_WMP_DSHOW_UNSUPPORTED_FORMAT:
                            errorMessage = Res.ErrorMediaCodec;
                            break;
                        case WmpErrorCodes.NS_E_WMP_ACCESS_DENIED: // 401 Not Authorized
                        case WmpErrorCodes.NS_E_WMP_LOGON_FAILURE:
                            errorMessage = Res.ErrorMediaNotAuthorized;
                            break;
                        case WmpErrorCodes.UnexpectedFailure:
                        case WmpErrorCodes.CatastrophicFailure:
                        case WmpErrorCodes.NS_E_WMP_FILE_OPEN_FAILED:
                            // apparently we can recover from this MP errors by reopening media player
                            if (player.WasOpened)
                            {
                                ReopenMediaPlayer(player);
                                return;
                            }

                            errorMessage = string.Format(Res.ErrorMediaGeneric, comException);
                            break;
                        default:
                            errorMessage = string.Format(Res.ErrorMediaGeneric, comException);
                            break;
                    }

                }
                else
                {
                    errorMessage = string.Format(Res.ErrorMediaGeneric, e.ErrorException);
                }

                MediaError = new Exception(errorMessage, e.ErrorException);
                MediaFailed?.Invoke(this, new ExceptionEventArgs(MediaError));
            }));
        }

        private void ReopenMediaPlayer(InternalMediaPlayer player)
        {
            var source = player.Source;
            player.Close();
            player.WasOpened = false;
            mediaOpenedCount--;
            positionTimer.Stop();
            IsMediaOpened = false;
            IsMediaOpening = true;

            // Re-apply UI settings to player
            UpdateMediaPlayerProperties();

            player.Open(source);
            checkOpenMediaTimer.Start();
        }

        private void MediaPlayer_MediaOpened(object sender, EventArgs e)
        {
            var player = (InternalMediaPlayer)sender;
            Debug.WriteLine(player.Source + " opened");

            Dispatcher.Invoke(new Action(() =>
            {
                mediaOpenedCount++;
                player.WasOpened = true;
                CheckMediaOpened();
            }));
        }

        private void CheckMediaOpened()
        {
            if (mediaOpenedCount == mediaPlayers.Count && mediaPlayers.All(x => x.DownloadProgress == 1))
            {
                checkOpenMediaTimer.Stop();
                IsMediaOpening = false;
                IsMediaOpened = true;

                ApplyPosition();

                if (IsPlaying)
                    InternalPlay();
            }
        }

        private Size MeasureArrangeHelper(Size inputSize)
        {
            if (activePlayer == null)
                return new Size();

            var naturalSize = new Size(activePlayer.NaturalVideoWidth, activePlayer.NaturalVideoHeight);

            //get computed scale factor
            var scaleFactor = ComputeScaleFactor(inputSize, naturalSize, Stretch.Uniform, StretchDirection.Both);

            // Returns our minimum size & sets DesiredSize.
            return new Size(naturalSize.Width * scaleFactor.Width, naturalSize.Height * scaleFactor.Height);
        }

        protected override Size MeasureOverride(Size availableSize)
        {
            return MeasureArrangeHelper(availableSize);
        }

        /// <summary>
        /// Override for <seealso cref="FrameworkElement.ArrangeOverride" />.
        /// </summary>
        protected override Size ArrangeOverride(Size finalSize)
        {
            return MeasureArrangeHelper(finalSize);
        }

        protected override void OnRender(DrawingContext drawingContext)
        {
            if (activePlayer == null)
                return;

            drawingContext.DrawVideo(activePlayer, new Rect(new Point(), RenderSize));
        }

        public void Play()
        {
            if (IsPlaying)
                return;

            IsPlaying = true;

            if (IsMediaOpened)
            {
                if (Position == Source.Duration)
                    Position = TimeSpan.Zero;

                InternalPlay();
            }
            else if (!IsMediaOpening && HasMediaContent)
            {
                if (Position == Source.Duration)
                    Position = TimeSpan.Zero;

                OpenMedia();
            }
        }

        public void Pause()
        {
            if (!IsPlaying)
                return;

            IsPlaying = false;

            InternalPause();
        }

        private TimeSpan EnsureInRange(TimeSpan value)
        {
            if (value < MinPosition)
                return MinPosition.Value;
            if (value > MaxPosition)
                return MaxPosition.Value;

            return value;
        }

        public void Seek(TimeSpan offset, TimeSeekOrigin origin, bool forceSeekForward)
        {
            var newPosition = origin == TimeSeekOrigin.BeginTime ? offset : Position + offset;

            if (Source == null)
                newPosition = TimeSpan.Zero;
            else if (newPosition >= Source.Duration || newPosition >= MaxPosition)
                newPosition = RepeatEnabled ? TimeSpan.Zero : Source.Duration;

            newPosition = CoercePosition(newPosition, forceSeekForward || newPosition >= Position);

            Position = newPosition;
        }

        private TimeSpan CoercePosition(TimeSpan position, bool seekForward)
        {
            if (Source == null)
                return TimeSpan.Zero;

            position = EnsureInRange(position);

            var segment = FindMediaSegment(position);
            if (segment != null)
                return position;

            if (seekForward)
            {
                segment = mediaSegments.FirstOrDefault(s => s.Offset >= position);
                if (segment != null)
                    return segment.Offset;

                if (HasMediaContent)
                    return Source.Duration;
            }
            else
            {
                segment = mediaSegments.LastOrDefault(s => s.Offset < position);
                if (segment != null)
                    return segment.Offset + segment.Duration;
            }

            return TimeSpan.Zero;
        }

        private void InternalPlay()
        {
            if (activePlayer == null)
                return;

            ApplyPosition();

            activePlayer.Play();
            positionTimer.Start();
        }

        private void InternalPause()
        {
            if (activePlayer == null)
                return;

            activePlayer.Pause();
        }

        private void ApplyPosition()
        {
            if (!IsMediaOpened)
                return;

            var segment = FindMediaSegment(Position);
            var player = segment != null ? GetMediaPlayer(segment) : null;

            // if active player changes then stop the old player and switch to the new one
            if (activePlayer != player)
            {
                InternalPause();

                if (activePlayer != null)
                {
                    activePlayer.MediaEnded -= MediaPlayer_MediaEnded;
                    activePlayer.BufferingStarted -= MediaPlayer_BufferingStarted;

                    // bug fix #105165. In certain conditions MediaEnded is not called. Let's make sure we end media playback if know that we reached end of the media
                    if (player == null)
                        EndPlayback();
                }

                activePlayer = player;

                if (activePlayer != null)
                {
                    activePlayer.MediaEnded += MediaPlayer_MediaEnded;
                    activePlayer.BufferingStarted += MediaPlayer_BufferingStarted;
                    CheckIsBuffering();

                    SetPlayerPosition(activePlayer, Position - segment.Offset);
                    InvalidateVisual();

                    if (IsPlaying)
                        InternalPlay();
                }
            }
            else if (activePlayer != null && !updatingPosition)
            {
                SetPlayerPosition(activePlayer, Position - segment.Offset);
                InvalidateVisual();
            }
        }

        /// <summary>
        /// If the new play position has changed by more than the time resolution error,
        /// then set the new position (forcing a seek), otherwise do not change the position.
        /// 
        /// Clicking pause then play results in a small difference in play position due
        /// to a time tracking error between the player's current position and the current display 
        /// position.  Therefore to avoid having the player issue a seek request we ignore position
        /// changes less than the time resolution.  The difference is typically 20 to 40 ms.
        /// </summary>
        private static double positionTimeResolutionMs = 50.0; // milliseconds
        private static void SetPlayerPosition(MediaPlayer mediaPlayer, TimeSpan newPosition)
        {
            TimeSpan dt = mediaPlayer.Position - newPosition;
            if (Math.Abs(dt.TotalMilliseconds) > positionTimeResolutionMs)
            {
                mediaPlayer.Position = newPosition;
            }
        }


        private void MediaPlayer_BufferingStarted(object sender, EventArgs e)
        {
            Dispatcher.Invoke(new Action(CheckIsBuffering));
        }

        private void MediaPlayer_MediaEnded(object sender, EventArgs e)
        {
            var player = (MediaPlayer)sender;
            Debug.WriteLine(player.Source + " ended");

            Dispatcher.Invoke(new Action(() =>
            {
                var segment = GetMediaSegment(player);
                SeekNextMediaSegment(segment);
            }));
        }

        private void CheckIsBuffering()
        {
            if (!IsMediaBuffering && activePlayer?.IsBuffering == true)
                bufferingTimer.Start();
        }


        private MediaPlayer GetMediaPlayer(MediaSegment segment)
        {
            return mediaPlayers[mediaSegments.IndexOf(segment)];
        }

        private MediaSegment GetMediaSegment(MediaPlayer player)
        {
            return mediaSegments[mediaPlayers.IndexOf(player)];
        }

        private MediaSegment FindMediaSegment(TimeSpan position)
        {
            return mediaSegments.FirstOrDefault(x => x.Offset == position) ?? mediaSegments.FirstOrDefault(s => s.InRange(position));
        }

        private void SeekNextMediaSegment(MediaSegment segment)
        {
            var idx = mediaSegments.IndexOf(segment);
            var nextSegment = idx < mediaSegments.Count - 1 ? mediaSegments[idx + 1] : null;
            if (nextSegment != null && !(nextSegment.Offset >= MaxPosition))
                Position = nextSegment.Offset;
            else
                EndPlayback();
        }

        private void EndPlayback()
        {
            if (RepeatEnabled)
            {
                Position = TimeSpan.Zero;
                ApplyPosition();
            }
            else
            {
                Pause();
                Position = Source.Duration;
            }
        }

        private static Size ComputeScaleFactor(Size availableSize, Size contentSize, Stretch stretch, StretchDirection stretchDirection)
        {
            // Compute scaling factors to use for axes
            double scaleX = 1.0;
            double scaleY = 1.0;

            bool isConstrainedWidth = !double.IsPositiveInfinity(availableSize.Width);
            bool isConstrainedHeight = !double.IsPositiveInfinity(availableSize.Height);

            if ((stretch == Stretch.Uniform || stretch == Stretch.UniformToFill || stretch == Stretch.Fill) && (isConstrainedWidth || isConstrainedHeight))
            {
                // Compute scaling factors for both axes
                scaleX = contentSize.Width == 0 ? 0.0 : availableSize.Width / contentSize.Width;
                scaleY = contentSize.Height == 0 ? 0.0 : availableSize.Height / contentSize.Height;

                if (!isConstrainedWidth) scaleX = scaleY;
                else if (!isConstrainedHeight) scaleY = scaleX;
                else
                {
                    // If not preserving aspect ratio, then just apply transform to fit
                    switch (stretch)
                    {
                        case Stretch.Uniform:       //Find minimum scale that we use for both axes
                            double minscale = scaleX < scaleY ? scaleX : scaleY;
                            scaleX = scaleY = minscale;
                            break;

                        case Stretch.UniformToFill: //Find maximum scale that we use for both axes
                            double maxscale = scaleX > scaleY ? scaleX : scaleY;
                            scaleX = scaleY = maxscale;
                            break;

                        case Stretch.Fill:          //We already computed the fill scale factors above, so just use them
                            break;
                    }
                }

                //Apply stretch direction by bounding scales.
                //In the uniform case, scaleX=scaleY, so this sort of clamping will maintain aspect ratio
                //In the uniform fill case, we have the same result too.
                //In the fill case, note that we change aspect ratio, but that is okay
                switch (stretchDirection)
                {
                    case StretchDirection.UpOnly:
                        if (scaleX < 1.0) scaleX = 1.0;
                        if (scaleY < 1.0) scaleY = 1.0;
                        break;

                    case StretchDirection.DownOnly:
                        if (scaleX > 1.0) scaleX = 1.0;
                        if (scaleY > 1.0) scaleY = 1.0;
                        break;

                    case StretchDirection.Both:
                        break;

                    default:
                        break;
                }
            }
            //Return this as a size now
            return new Size(scaleX, scaleY);
        }
    }
}
