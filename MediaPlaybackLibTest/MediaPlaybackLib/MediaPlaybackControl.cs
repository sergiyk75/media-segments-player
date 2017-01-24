using System;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Linq;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using System.Windows.Threading;
using MediaPlayback.MultiThumbSlider;
using MediaPlaybackLib.MultiThumbSlider;
using Microsoft.Practices.Prism.Commands;
using Microsoft.Practices.Prism.Mvvm;
using Brush = System.Windows.Media.Brush;
using Color = System.Windows.Media.Color;
using Image = System.Windows.Controls.Image;
using Point = System.Windows.Point;
using Rectangle = System.Windows.Shapes.Rectangle;

namespace MediaPlaybackLib
{
    [TemplatePart(Name = "PART_Timescale", Type = typeof(Canvas)),
     TemplatePart(Name = "PART_MediaElement", Type = typeof(MediaPlaybackElement)),
     TemplatePart(Name = "PART_MediaImage", Type = typeof(Image)),
     TemplatePart(Name = "PART_Selection", Type = typeof(Canvas)),
     TemplatePart(Name = "PART_Progress", Type = typeof(Canvas)),
     TemplatePart(Name = "PART_TimerText", Type = typeof(TextBlock)),
     TemplatePart(Name = "PART_RewindButton", Type = typeof(Button)),
     TemplatePart(Name = "PART_FastForwardButton", Type = typeof(Button)),
     TemplatePart(Name = "PART_MarkersExtended", Type = typeof(Button)),
     TemplatePart(Name = "PART_ProgressThumb", Type = typeof(Thumb)),
     TemplatePart(Name = "PART_MultiSlider", Type = typeof(MultiThumbSlider.MultiThumbSlider))]
    [ExcludeFromCodeCoverage]
    public class MediaPlaybackControl : Control
    {
        private enum MediaTimerType
        {
            Elapsed,
            ElapsedWithDuration,
            Remaining
        }

        // controls
        // set as a list for future multi selection. each tuple is the left and right brace
        private Thumb leftSelectThumb;
        private Thumb rightSelectThumb;

        private readonly Line progressLine;
        private readonly Rectangle progressLineArea;

        private readonly Rectangle selectionRegion;
        private Canvas markersExtendedCanvas;
        private MediaPlaybackElement mediaElement;
        private Point mouseDownPoint;
        private MultiThumbSlider.MultiThumbSlider multiThumbSlider;
        private Canvas progressCanvas;
        private Thumb progressThumb;
        private Canvas selectionCanvas;
        private Canvas timescaleCanvas;
        private TextBlock timerText;

        private TimeSpan? startSelectedRegion;
        private TimeSpan? endSelectedRegion;
        private bool changingSelectedRegion;

        private PropertyChangeNotifier isPlayingChangeNotifier;
        private PropertyChangeNotifier positionChangeNotifier;
        private PropertyChangeNotifier repeatChangeNotifier;

        private MediaTimerType mediaTimerType = MediaTimerType.ElapsedWithDuration;

        private Cursor addMarkerCursor;
        public const string TimeFormat = "{0:00}:{1:00}:{2:00}.{3:000}";
        public const string TimeNoHoursFormat = "{1:00}:{2:00}.{3:000}";

        private const double MouseMoveTolerance = 3;

        private DispatcherTimer fastForwardRewindTimer;
        private Subject<object> resumeSubject = new Subject<object>();
        private object selectedRangeMarkerId;

        private bool enableAddMarkerState = true;

        public MediaPlaybackControl()
        {
            InitAddMarkerCursor();

            ToggleMediaTimerTypeCommand = new DelegateCommand(ToggleMediaTimerType);

            SkipPreviousCommand = new DelegateCommand(() =>
            {
                if (Keyboard.IsKeyDown(Key.LeftShift) || Keyboard.IsKeyDown(Key.RightShift))
                {
                    SeekPosition(TimeSpan.Zero);
                    // if present select a marker at the start of the media. unselect if not present
                    var marker = MediaMarkers?.OrderBy(x => x.StartTime).FirstOrDefault(x => x.IsVisible && x.StartTime == mediaElement.Position);
                    SeekToMarker(marker);
                }
                else
                {
                    SkipPrevious();
                }
            });

            SkipNextCommand = new DelegateCommand(SkipNext);

            AddMarkerCommand = new DelegateCommand(AddMarker, () => CanEditMediaMarker);

            selectionRegion = new Rectangle();
            selectionRegion.SetBinding(Shape.FillProperty, new Binding { Source = this, Path = new PropertyPath(SelectionRegionBrushProperty) });

            progressLine = new Line { };
            progressLine.SetBinding(Shape.StrokeProperty, new Binding { Source = this, Path = new PropertyPath(ProgressBarBrushProperty) });
            progressLineArea = new Rectangle { Width = 9, Fill = Brushes.Transparent };

            fastForwardRewindTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(10) };
            fastForwardRewindTimer.Tick += (s, e) =>
            {
                SeekOffset(TimeSpan.FromMilliseconds(100 * 2 * (int)fastForwardRewindTimer.Tag));
            };

            resumeSubject
                .Throttle(TimeSpan.FromMilliseconds(450))
                .ObserveOnDispatcher()
                .Subscribe(x =>
                {
                    if (mediaElement.Position < MediaDuration)
                        mediaElement.Play();
                });
        }

        public event EventHandler Activity;
        public event EventHandler<ExceptionEventArgs> MediaFailed;
        public event EventHandler<EventArgs<MediaMarker>> SeekToMarkerEvent;

        private void NotifyActivity()
        {
            Activity?.Invoke(this, EventArgs.Empty);
        }

        public DelegateCommand ToggleMediaTimerTypeCommand { get; private set; }

        public DelegateCommand SkipPreviousCommand { get; private set; }

        public DelegateCommand SkipNextCommand { get; private set; }

        public DelegateCommand AddMarkerCommand { get; private set; }

        public void ToggleMediaTimerType()
        {
            switch (mediaTimerType)
            {
                case MediaTimerType.Elapsed:
                    mediaTimerType = MediaTimerType.ElapsedWithDuration;
                    break;
                case MediaTimerType.ElapsedWithDuration:
                    mediaTimerType = MediaTimerType.Remaining;
                    break;
                case MediaTimerType.Remaining:
                    mediaTimerType = MediaTimerType.Elapsed;
                    break;
            }

            UpdateMediaTimer();
        }

        private void InitAddMarkerCursor()
        {
            var path = new Path
            {
                Width = 12,
                Height = 12,
                StrokeThickness = 2,
                Data = Geometry.Parse("M 10,10 L 20,10 L 20,0 L 30,0 L 30,10 L 40,10 L 40,20 L 30,20 L 30,30 L 20,30 L 20,20 L 10,20 L 10,10"),
                Fill = new SolidColorBrush(Color.FromRgb(0x64, 0x95, 0xED)),
                Stretch = Stretch.Uniform,
            };

            addMarkerCursor = CursorHelper.CreateCursor(path, 6, 6);
        }

        public static readonly DependencyProperty AllowEditMediaMarkersProperty =
           DependencyProperty.Register("AllowEditMediaMarkers", typeof(bool), typeof(MediaPlaybackControl), new PropertyMetadata(false, (d, e) => ((MediaPlaybackControl)d).OnAllowEditMediaMarkers()));

        private void OnAllowEditMediaMarkers()
        {
            AddMarkerCommand.RaiseCanExecuteChanged();
        }

        public bool AllowEditMediaMarkers
        {
            get { return (bool)GetValue(AllowEditMediaMarkersProperty); }
            set { SetValue(AllowEditMediaMarkersProperty, value); }
        }

        public static readonly DependencyProperty MediaImageProperty =
            DependencyProperty.Register("MediaImage", typeof(ImageSource), typeof(MediaPlaybackControl));

        public ImageSource MediaImage
        {
            get { return (ImageSource)GetValue(MediaImageProperty); }
            set { SetValue(MediaImageProperty, value); }
        }

        public static readonly DependencyProperty MediaSourceProperty =
            DependencyProperty.Register("MediaSource", typeof(MediaSource), typeof(MediaPlaybackControl), new PropertyMetadata(null, (d, e) => ((MediaPlaybackControl)d).OnMediaSourceChanged()));

        public static readonly DependencyProperty ShowMediaImageProperty =
            DependencyProperty.Register("ShowMediaImage", typeof(bool), typeof(MediaPlaybackControl), new PropertyMetadata(true));

        public bool ShowMediaImage
        {
            get { return (bool)GetValue(ShowMediaImageProperty); }
            set { SetValue(ShowMediaImageProperty, value); }
        }

        public static readonly DependencyProperty MediaImageStretchProperty =
            DependencyProperty.Register("MediaImageStretch", typeof(Stretch), typeof(MediaPlaybackControl), new PropertyMetadata(Stretch.Fill));

        public Stretch MediaImageStretch
        {
            get { return (Stretch)GetValue(MediaImageStretchProperty); }
            set { SetValue(MediaImageStretchProperty, value); }
        }

        private void OnMediaSourceChanged()
        {
            ClearSelectedRange();
            UpdateAllRegions();

            ((DelegateCommand)AddMarkerCommand).RaiseCanExecuteChanged();

            // Video media source preview images should use uniform stretch to keep the aspect ratio of the video,
            // where audio previews (waveforms) need to fill the media player to ensure the the waveform is kept in
            // proper alignment with the player timeline
            MediaImageStretch = (MediaSource != null && MediaSource.HasVideo)
                ? Stretch.Uniform
                : Stretch.Fill;

            //The AddMarkerCommand command depends on this property to function correctly.  In the 
            //event that the property changes after CanExecute is executed force it to recalculate.
            AddMarkerCommand.RaiseCanExecuteChanged();
        }

        public MediaSource MediaSource
        {
            get { return (MediaSource)GetValue(MediaSourceProperty); }
            set { SetValue(MediaSourceProperty, value); }
        }

        /// <summary>
        /// Identifies the <see cref="MediaMarkers" /> dependency property. 
        /// </summary>
        public static readonly DependencyProperty MediaMarkersProperty =
            DependencyProperty.Register("MediaMarkers", typeof(ObservableCollection<MediaMarker>), typeof(MediaPlaybackControl), new UIPropertyMetadata(null, OnMediaMarkersChanged));

        /// <summary>
        /// Gets or sets the media markers
        /// </summary>
        [SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly")]
        public ObservableCollection<MediaMarker> MediaMarkers
        {
            get { return (ObservableCollection<MediaMarker>)GetValue(MediaMarkersProperty); }
            set { SetValue(MediaMarkersProperty, value); }
        }

        private static void OnMediaMarkersChanged(DependencyObject o, DependencyPropertyChangedEventArgs e)
        {
            var mediaTimeline = o as MediaPlaybackControl;
            if (mediaTimeline != null)
            {
                mediaTimeline.OnMediaMarkersChanged((ObservableCollection<MediaMarker>)e.OldValue,
                    (ObservableCollection<MediaMarker>)e.NewValue);
            }
        }

        /// <summary>
        /// Called after the <see cref="MediaMarkers"/> value has changed.
        /// </summary>
        /// <param name="oldValue">The previous value of <see cref="MediaMarkers"/></param>
        /// <param name="newValue">The new value of <see cref="MediaMarkers"/></param>
        protected virtual void OnMediaMarkersChanged(ObservableCollection<MediaMarker> oldValue, ObservableCollection<MediaMarker> newValue)
        {
            AddMarkerCommand.RaiseCanExecuteChanged();

            // remove all old handlers
            if (oldValue != null)
            {
                oldValue.CollectionChanged -= OnMarkerCollectionChanged;
                foreach (var m in oldValue)
                    m.PropertyChanged -= OnMediaMarkerPropertyChanged;
            }

            // filter first as this will change IsVisible property and we don't want to deal with receiving those changes in the handler
            ApplyMediaMarkersFilter();
            UpdateMediaMarkers();

            if (newValue == null)
                return;

            // tie into the collection changed event to watch adds/deletes
            newValue.CollectionChanged += OnMarkerCollectionChanged;
            foreach (var m in newValue)
                m.PropertyChanged += OnMediaMarkerPropertyChanged;
        }

        /// <summary>
        /// Identifies the <see cref="SelectionLeftThumb" /> dependency property. 
        /// </summary>
        public static readonly DependencyProperty SelectionRightThumbProperty =
            DependencyProperty.Register("SelectionRightThumb", typeof(Style), typeof(MediaPlaybackControl), new PropertyMetadata(default(Style)));

        public static readonly DependencyProperty SelectionLeftThumbProperty =
            DependencyProperty.Register("SelectionLeftThumb", typeof(Style), typeof(MediaPlaybackControl), new PropertyMetadata(default(Style)));

        public Style SelectionRightThumb
        {
            get { return (Style)GetValue(SelectionRightThumbProperty); }
            set { SetValue(SelectionRightThumbProperty, value); }
        }

        public Style SelectionLeftThumb
        {
            get { return (Style)GetValue(SelectionLeftThumbProperty); }
            set { SetValue(SelectionLeftThumbProperty, value); }
        }

        /// <summary>
        /// Identifies the <see cref="MediaMarkersBrush" /> dependency property. 
        /// </summary>
        public static readonly DependencyProperty MediaMarkersBrushProperty =
            DependencyProperty.Register("MediaMarkersBrush", typeof(Brush), typeof(MediaPlaybackControl), new UIPropertyMetadata(new SolidColorBrush(Color.FromArgb(0xCD, 0xBA, 0x00, 0xFF)), (d, e) => ((MediaPlaybackControl)d).UpdateMediaMarkers()));

        /// <summary>
        /// Gets or sets a brush
        /// </summary>
        [Category("Brushes")]
        public Brush MediaMarkersBrush
        {
            get { return (Brush)GetValue(MediaMarkersBrushProperty); }
            set { SetValue(MediaMarkersBrushProperty, value); }
        }

        /// <summary>
        /// Identifies the <see cref="MediaMarkersRangeBrush" /> dependency property. 
        /// </summary>
        public static readonly DependencyProperty MediaMarkersRangeBrushProperty =
            DependencyProperty.Register("MediaMarkersRangeBrush", typeof(Brush), typeof(MediaPlaybackControl), new UIPropertyMetadata(new SolidColorBrush(Color.FromArgb(0xCD, 0xBA, 0x00, 0xFF)), (d, e) => ((MediaPlaybackControl)d).UpdateMediaMarkers()));

        /// <summary>
        /// Gets or sets a brush
        /// </summary>
        [Category("Brushes")]
        public Brush MediaMarkersRangeBrush
        {
            get { return (Brush)GetValue(MediaMarkersRangeBrushProperty); }
            set { SetValue(MediaMarkersRangeBrushProperty, value); }
        }

        /// <summary>
        /// Identifies the <see cref="MediaMarkersReadOnlyBrush" /> dependency property. 
        /// </summary>
        public static readonly DependencyProperty MediaMarkersReadOnlyBrushProperty =
            DependencyProperty.Register("MediaMarkersReadOnlyBrush", typeof(Brush), typeof(MediaPlaybackControl), new UIPropertyMetadata(new SolidColorBrush(Color.FromArgb(0xCD, 0xBA, 0x00, 0xFF)), (d, e) => ((MediaPlaybackControl)d).UpdateMediaMarkers()));

        /// <summary>
        /// Gets or sets a brush
        /// </summary>
        [Category("Brushes")]
        public Brush MediaMarkersReadOnlyBrush
        {
            get { return (Brush)GetValue(MediaMarkersReadOnlyBrushProperty); }
            set { SetValue(MediaMarkersReadOnlyBrushProperty, value); }
        }

        /// <summary>
        /// Identifies the <see cref="ProgressBarBrush" /> dependency property. 
        /// </summary>
        public static readonly DependencyProperty ProgressBarBrushProperty =
            DependencyProperty.Register("ProgressBarBrush", typeof(Brush), typeof(MediaPlaybackControl), new UIPropertyMetadata(new SolidColorBrush(Color.FromArgb(0xCD, 0xBA, 0x00, 0xFF))));


        /// <summary>
        /// Gets or sets a brush used to draw the track progress indicator bar.
        /// </summary>
        [Category("Brushes")]
        public Brush ProgressBarBrush
        {
            get { return (Brush)GetValue(ProgressBarBrushProperty); }
            set { SetValue(ProgressBarBrushProperty, value); }
        }

        /// <summary>
        /// Identifies the <see cref="SelectionRegionBrush" /> dependency property. 
        /// </summary>
        public static readonly DependencyProperty SelectionRegionBrushProperty =
            DependencyProperty.Register("SelectionRegionBrush", typeof(Brush), typeof(MediaPlaybackControl), new UIPropertyMetadata(new SolidColorBrush(Color.FromArgb(0x81, 0xF6, 0xFF, 0x00))));

        /// <summary>
        /// Gets or sets a brush used to draw the Selection region on the waveform.
        /// </summary>
        [Category("Brushes")]
        public Brush SelectionRegionBrush
        {
            get { return (Brush)GetValue(SelectionRegionBrushProperty); }
            set { SetValue(SelectionRegionBrushProperty, value); }
        }

        /// <summary>
        /// Identifies the <see cref="FilterReadOnly" /> dependency property. 
        /// </summary>
        public static readonly DependencyProperty FilterReadOnlyProperty =
            DependencyProperty.Register("FilterReadOnly", typeof(bool), typeof(MediaPlaybackControl), new PropertyMetadata(false, (d, e) => ((MediaPlaybackControl)d).OnFilterReadOnlyChanged()));

        /// <summary>
        /// Gets or sets a value that indicates whether Selection regions will be created via mouse drag across the waveform.
        /// </summary>
        [Bindable(true), Category("Common")]
        public bool FilterReadOnly
        {
            get { return (bool)GetValue(FilterReadOnlyProperty); }
            set { SetValue(FilterReadOnlyProperty, value); }
        }

        protected virtual void OnFilterReadOnlyChanged()
        {
            ApplyMediaMarkersFilter();
            UpdateMediaMarkers();
        }

        /// <summary>
        /// Identifies the <see cref="TimelineTickBrush" /> dependency property. 
        /// </summary>
        public static readonly DependencyProperty TimelineTickBrushProperty =
            DependencyProperty.Register("TimelineTickBrush", typeof(Brush), typeof(MediaPlaybackControl), new UIPropertyMetadata(new SolidColorBrush(Colors.Black)));

        /// <summary>
        /// Gets or sets a brush used to draw the tickmarks on the timeline.
        /// </summary>
        [Category("Brushes")]
        public Brush TimelineTickBrush
        {
            get { return (Brush)GetValue(TimelineTickBrushProperty); }
            set { SetValue(TimelineTickBrushProperty, value); }
        }

        public static readonly DependencyProperty MarkersLockedProperty =
            DependencyProperty.Register("MarkersLocked", typeof(bool), typeof(MediaPlaybackControl), new PropertyMetadata(true, (d, e) => ((MediaPlaybackControl)d).OnMarkersLockedChanged()));

        public bool MarkersLocked
        {
            get { return (bool)GetValue(MarkersLockedProperty); }
            set { SetValue(MarkersLockedProperty, value); }
        }

        private void OnMarkersLockedChanged()
        {
            if (MediaMarkers == null)
                return;
            foreach (var maker in MediaMarkers)
                multiThumbSlider.Freeze(maker.Id, MarkersLocked);
        }

        private T GetTemplateChild<T>(string partName) where T : FrameworkElement
        {
            var child = GetTemplateChild(partName) as T;
            if (child == null)
                throw new NullReferenceException(string.Format("%s is missing in template, or not a %s.", partName, typeof(T)));
            return child;
        }

        /// <summary>
        /// When overridden in a derived class, is invoked whenever application code
        /// or internal processes call System.Windows.FrameworkElement.ApplyTemplate().
        /// </summary>
        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();

            multiThumbSlider = GetTemplateChild<MultiThumbSlider.MultiThumbSlider>("PART_MultiSlider");
            multiThumbSlider.MarkerPositionDragEnded += MultiThumbSliderOnMarkerPositionDragEnded;
            multiThumbSlider.MarkerPositionChanged += MultiThumbSliderOnMarkerPositionChanged;
            multiThumbSlider.MarkerModified += MultiThumbSliderOnMarkerModified;
            multiThumbSlider.RangeSelectionChanged += MultiThumbSliderOnRangeSelectionChanged;
            multiThumbSlider.RangeSelectionCompleted += MultiThumbSliderOnRangeSelectionCompleted;
            multiThumbSlider.SizeChanged += (s, e) => UpdateMediaMarkers();
            multiThumbSlider.EditorCancelled += MultiThumbSliderOnEditorCancelled;

            mediaElement = GetTemplateChild<MediaPlaybackElement>("PART_MediaElement");
            mediaElement.MediaFailed += (s, e) => MediaFailed?.Invoke(this, e);

            this.isPlayingChangeNotifier = new PropertyChangeNotifier(mediaElement, MediaPlaybackElement.IsPlayingProperty);
            this.isPlayingChangeNotifier.ValueChanged += (s, e) =>
            {
                // hide video preview images as soon as the user starts playback
                if (this.HasVideo)
                {
                    this.ShowMediaImage = false;
                }
                NotifyActivity();
            };

            // update media timer and media progress indicator if media element position changes
            this.positionChangeNotifier = new PropertyChangeNotifier(mediaElement, MediaPlaybackElement.PositionProperty);
            this.positionChangeNotifier.ValueChanged += (s, e) =>
            {
                UpdateProgressIndicator();
                UpdateMediaTimer();
            };

            // apply min max range if media element RepeatEnabled changes
            this.repeatChangeNotifier = new PropertyChangeNotifier(mediaElement, MediaPlaybackElement.RepeatEnabledProperty);
            this.repeatChangeNotifier.ValueChanged += (s, e) =>
            {
                // if repeat is enabled than move position to the beginning of the selection
                if (mediaElement.RepeatEnabled &&
                    HasSelectedRegion &&
                    !mediaElement.Position.InRange(startSelectedRegion.Value, endSelectedRegion.Value))
                {
                    SeekPosition(startSelectedRegion.Value);
                }
                ApplySelectedRange();
            };

            timescaleCanvas = GetTemplateChild<Canvas>("PART_Timescale");
            timescaleCanvas.Cursor = Cursors.Hand;
            timescaleCanvas.SizeChanged += (s, e) => UpdateSelectionRegion();

            selectionCanvas = GetTemplateChild<Canvas>("PART_Selection");

            CreateSelectionHandlePaths();

            progressCanvas = GetTemplateChild<Canvas>("PART_Progress");
            progressCanvas.Children.Add(progressLineArea);
            progressCanvas.Children.Add(progressLine);
            progressCanvas.SizeChanged += (s, e) => UpdateProgressIndicator();


            progressThumb = GetTemplateChild<Thumb>("PART_ProgressThumb");

            timerText = GetTemplateChild<TextBlock>("PART_TimerText");

            markersExtendedCanvas = GetTemplateChild<Canvas>("PART_MarkersExtended");
            markersExtendedCanvas.SizeChanged += (s, e) => UpdateMediaMarkerExtendedLines();

            var rewindButton = GetTemplateChild<Button>("PART_RewindButton");
            rewindButton.PreviewMouseLeftButtonDown += FastForwardRewindButton_PreviewMouseLeftButtonDown;
            rewindButton.PreviewMouseLeftButtonUp += FastForwardRewindButton_PreviewMouseLeftButtonUp;

            var fastForwardButton = GetTemplateChild<Button>("PART_FastForwardButton");
            fastForwardButton.PreviewMouseLeftButtonDown += FastForwardRewindButton_PreviewMouseLeftButtonDown;
            fastForwardButton.PreviewMouseLeftButtonUp += FastForwardRewindButton_PreviewMouseLeftButtonUp;

            UpdateAllRegions();
        }

        private void FastForwardRewindButton_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            var button = (Button)sender;

            PausePlaying();
            fastForwardRewindTimer.Tag = button.Name == "PART_FastForwardButton" ? 1 : -1;
            fastForwardRewindTimer.Start();
            NotifyActivity();
        }

        private void FastForwardRewindButton_PreviewMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            fastForwardRewindTimer.Stop();
            ResumePlaying();
        }

        public event EventHandler<MarkerModifiedEventArgs> MarkerModified;

        public event EventHandler<MarkerAddRequestEventArgs> MarkerAdd;

        protected virtual void AddMarker()
        {
            if (!CanEditMediaMarker)
                return;

            TimeSpan startTime;
            TimeSpan? duration;

            if (HasSelectedRegion)
            {
                startTime = startSelectedRegion.Value;
                duration = endSelectedRegion.Value - startSelectedRegion.Value;
            }
            else
            {
                startTime = mediaElement.Position;
                duration = null;
            }

            if (MarkerAdd != null)
                MarkerAdd(this, new MarkerAddRequestEventArgs(startTime, duration));

            NotifyActivity();

            // Disable the global add marker button on the right of the timeline
            // as we do not want the user to continue adding additional markers for the exact same selection
            AddMarkerButtonToggle(false);
        }

        static MediaPlaybackControl()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(MediaPlaybackControl), new FrameworkPropertyMetadata(typeof(MediaPlaybackControl)));
        }

        protected override void OnPreviewKeyDown(KeyEventArgs eventArgs)
        {
            // if the key was a tab we handle it so that it's not passed up. This disables "tabbing" through the markers which 
            // was causing the adorners to pop open for each marker and was not sending OnLostFocus, which in turn allowed
            // all the editing adorners to be open. There should be no need to tab on this control.
            if (eventArgs.Key == Key.Tab)
                eventArgs.Handled = true;
        }

        private void MultiThumbSliderOnRangeSelectionCompleted(object sender, MarkerRangeSelectionCompletedEventArgs eventArgs)
        {
            if (eventArgs.Key.Equals(selectedRangeMarkerId))
            {
                changingSelectedRegion = false;
                ApplySelectedRange();
                if (this.startSelectedRegion.HasValue)
                {
                    SeekPosition(this.startSelectedRegion.Value);
                    ResumePlaying();
                }
            }

            var marker = MediaMarkers.Single(m => m.Id == eventArgs.Key);

            var startTime = PointToTimeSpan(eventArgs.RangeStart);
            var length = PointToTimeSpan(eventArgs.RangeEnd) - startTime;

            // make sure there was a time change, if not, don't send event
            if (marker.StartTime != startTime || marker.Length != length)
                OnMarkerModified(new MarkerModifiedEventArgs(marker.Id, marker.Description, startTime, length));
        }

        private void MultiThumbSliderOnRangeSelectionChanged(object sender, MarkerRangeSelectionChangedEventArgs eventArgs)
        {
            if (eventArgs.RangeId.Equals(selectedRangeMarkerId))
            {
                if (!changingSelectedRegion)
                {
                    changingSelectedRegion = true;
                    PausePlaying();
                }

                SetSelectedRange(this.PointToTimeSpan(eventArgs.NewRangeStart), this.PointToTimeSpan(eventArgs.NewRangeStop));
            }

            UpdateMediaMarkerExtendedLines();
        }

        private void MultiThumbSliderOnMarkerModified(object sender, MarkerModifiedEventArgs eventArgs)
        {
            OnMarkerModified(eventArgs);

            // Enable the global add marker button on the right of the timeline
            AddMarkerButtonToggle(true);
        }

        private void MultiThumbSliderOnEditorCancelled(object sender, RoutedEventArgs eventArgs)
        {
            // Enable the global add marker button on the right of the timeline
            AddMarkerButtonToggle(true);
        }

        /// <summary>
        /// Raise the MarkerModified Event
        /// </summary>
        /// <param name="eventArgs">event args</param>
        protected virtual void OnMarkerModified(MarkerModifiedEventArgs eventArgs)
        {
            if (MarkerModified != null)
                MarkerModified(this, eventArgs);
        }

        private void MultiThumbSliderOnMarkerPositionChanged(object sender, MarkerPositionChangedEventArgs markerPositionChangedEventArgs)
        {
            if (MediaMarkers == null)
                return;

            MediaMarker marker = MediaMarkers.SingleOrDefault(m => m.Id.ToString() == markerPositionChangedEventArgs.Key);

            if (marker != null && !marker.Length.HasValue && MediaDuration >= marker.StartTime)
            {
                UpdateMediaMarkerExtendedLines();
            }
        }

        private void MultiThumbSliderOnMarkerPositionDragEnded(object sender, MarkerPositionDragEndedEventArgs eventArgs)
        {
            if (MediaMarkers == null)
                return;

            var marker = MediaMarkers.SingleOrDefault(x => x.Id.ToString() == eventArgs.Key);
            if (marker == null || marker.Length.HasValue || MediaDuration < marker.StartTime)
                return;

            var newStartTime = PointToTimeSpan(eventArgs.Position);

            OnMarkerModified(new MarkerModifiedEventArgs(marker.Id, marker.Description, newStartTime, null));
        }

        private void OnMarkerCollectionChanged(object sender, NotifyCollectionChangedEventArgs notifyCollectionChangedEventArgs)
        {
            if (!IsLoaded)
                return;
            if (MediaSource == null) //Don't update markers if there is no media present. (ex. media disk not inserted).
                return;

            ApplyMediaMarkersFilter();

            if (notifyCollectionChangedEventArgs.Action == NotifyCollectionChangedAction.Add)
            {
                var marker = notifyCollectionChangedEventArgs.NewItems[0] as MediaMarker;

                if (marker.Length.HasValue)
                    selectedRangeMarkerId = marker.Id;

                var addedItem = AddMarker(marker);

                if (addedItem == Guid.Empty)
                    return;

                UpdateMediaMarkerExtendedLines();
                UpdateToolTips();

                multiThumbSlider.BringToFront(addedItem);
                multiThumbSlider.ShowEdit(addedItem);
            }
            else if (notifyCollectionChangedEventArgs.Action == NotifyCollectionChangedAction.Remove)
            {
                var removeme = notifyCollectionChangedEventArgs.OldItems[0] as MediaMarker;
                if (removeme != null)
                {
                    removeme.PropertyChanged -= OnMediaMarkerPropertyChanged;

                    multiThumbSlider.Remove(removeme.Id);

                    if (removeme.Id.Equals(selectedRangeMarkerId))
                        selectedRangeMarkerId = null;

                    UpdateMediaMarkerExtendedLines();
                    UpdateToolTips();
                }
            }
            else
            {
                // just updated everything
                UpdateMediaMarkers();
            }
        }

        private void OnMediaMarkerPropertyChanged(object sender, PropertyChangedEventArgs eventArgs)
        {
            if (multiThumbSlider == null)
                return;

            var marker = (MediaMarker)sender;

            if (eventArgs.PropertyName == PropertySupport.ExtractPropertyName(() => marker.StartTime) ||
                eventArgs.PropertyName == PropertySupport.ExtractPropertyName(() => marker.Length) ||
                eventArgs.PropertyName == PropertySupport.ExtractPropertyName(() => marker.Description))
            {
                // determine marker values
                var markerStart = TimeSpanToPoint(marker.StartTime);
                var markerEnd = TimeSpanToPoint(marker.StartTime + marker.Length.GetValueOrDefault());

                multiThumbSlider.Update(marker.Id, markerStart, markerEnd, marker.Description);

                UpdateMediaMarkerExtendedLines();
                UpdateToolTips();

                if (marker.Id.Equals(selectedRangeMarkerId))
                    SetSelectedRange(marker.StartTime, marker.StartTime + marker.Length.Value);
            }
        }

        protected override void OnMouseLeftButtonDown(MouseButtonEventArgs eventArgs)
        {
            base.OnMouseLeftButtonDown(eventArgs);

            changingSelectedRegion = false;
            if (timescaleCanvas.IsMouseOver)
            {
                mouseDownPoint = eventArgs.GetPosition(timescaleCanvas);
                CaptureMouse();

                // Enable the global add marker button on the right of the timeline
                AddMarkerButtonToggle(true);
            }
        }

        protected override void OnMouseMove(MouseEventArgs eventArgs)
        {
            base.OnMouseMove(eventArgs);

            var mousePoint = eventArgs.GetPosition(timescaleCanvas);

            if (IsMouseCaptured)
            {
                if (changingSelectedRegion || Math.Abs(mouseDownPoint.X - mousePoint.X) > MouseMoveTolerance)
                {
                    var start = this.PointToTimeSpan(Math.Min(mouseDownPoint.X, mousePoint.X));
                    var end = this.PointToTimeSpan(Math.Max(mouseDownPoint.X, mousePoint.X));

                    selectedRangeMarkerId = null;

                    if (!changingSelectedRegion)
                    {
                        changingSelectedRegion = true;
                        PausePlaying();
                    }

                    this.SetSelectedRange(start, end);

                    // Enable the global add marker button on the right of the timeline
                    AddMarkerButtonToggle(true);
                }
            }
            else
            {
                // Change mouse cursor to add marker if we are over selection region or over progress line 
                // and the control has been initalized to allow adding markers
                // and the state variable (enableAddMarkerState) that guards against certain usability issues says it's ok 
                Cursor =
                    CanEditMediaMarker
                    && enableAddMarkerState
                    && (selectionRegion.IsMouseOver || progressLineArea.IsMouseOver)
                    ? addMarkerCursor : null;
            }
        }

        /// <summary>
        /// This is a helper method to enable or disable the global add marker button on the right side of the timeline
        /// </summary>
        /// <param name="isEnabled">The new desired state of the button's isEnabled property</param>
        private void AddMarkerButtonToggle(bool isEnabled)
        {
            // Only proceed with the button change if that button is enabled for this instance of the control
            if (CanEditMediaMarker)
            {
                var addMarkerButton = GetTemplateChild<Button>("PART_AddMarkerButton");
                addMarkerButton.IsEnabled = isEnabled;

                enableAddMarkerState = isEnabled;
            }
        }

        protected override void OnMouseLeftButtonUp(MouseButtonEventArgs eventArgs)
        {
            base.OnMouseLeftButtonUp(eventArgs);

            if (IsMouseCaptured)
            {
                ReleaseMouseCapture();

                if (changingSelectedRegion)
                {
                    changingSelectedRegion = false;
                    ApplySelectedRange();
                    if (HasSelectedRegion)
                        SeekPosition(startSelectedRegion.Value);
                    ResumePlaying();
                    NotifyActivity();
                }
                else
                {
                    var mousePoint = eventArgs.GetPosition(timescaleCanvas);
                    var position = this.PointToTimeSpan(mousePoint.X);
                    if (HasSelectedRegion && !position.InRange(startSelectedRegion.Value, endSelectedRegion.Value))
                    {
                        ClearSelectedRange();
                    }

                    SeekPosition(position);
                    NotifyActivity();
                }
            }
            else if (Cursor == addMarkerCursor)
            {
                // Add the marker
                AddMarker();

                // Revert the cursor from a "+" symbol back to a regular pointer,
                // as we do not want the user to continue adding additional markers for the exact same selection
                Cursor = null;
            }
        }

        /// <summary>
        /// Show/Hide the system media markers
        /// </summary>
        private void ApplyMediaMarkersFilter()
        {
            if (MediaMarkers == null)
                return;

            foreach (var m in MediaMarkers.Where(m => m.IsSystemTag))
                m.IsVisible = FilterReadOnly;
        }

        /// <summary>
        /// Redraws all of the "extended lines" that are used to indicate marker positions on the waveform.
        /// </summary>
        private void UpdateMediaMarkerExtendedLines()
        {
            if (markersExtendedCanvas == null || multiThumbSlider == null)
                return;

            if (HasVideo)
                return;

            if (MediaMarkers == null)
                return;

            this.markersExtendedCanvas.Children.Clear();

            // Draw all of the extended lines for the time tag markers
            foreach (var markerTuple in this.multiThumbSlider.MarkerThumbs)
            {
                var markerThumb = markerTuple.Value;
                var mediaMarker = this.MediaMarkers.Single(x => x.Id == markerTuple.Key);
                var position = Canvas.GetLeft(markerThumb);
                if (double.IsNaN(position))
                    position = 0.0;

                AddExtendedLine(position, GetMediaMarkerBrush(mediaMarker));
            }

            // Draw all of the extended lines for the section markers
            foreach (var markerTuple in this.multiThumbSlider.MarkerRanges)
            {
                var markerRange = markerTuple.Value;
                var mediaMarker = this.MediaMarkers.Single(x => x.Id == markerTuple.Key);

                var brush = GetMediaMarkerBrush(mediaMarker);

                AddExtendedLine(markerRange.Range.Item1, brush);
                AddExtendedLine(markerRange.Range.Item2, brush);
            }
        }

        private Brush GetMediaMarkerBrush(MediaMarker mediaMarker)
        {
            if (mediaMarker.IsSystemTag)
                return MediaMarkersReadOnlyBrush;
            if (mediaMarker.Length.HasValue)
                return MediaMarkersRangeBrush;

            return MediaMarkersBrush;
        }

        /// <summary>
        /// Draws a dashed line over the extended markers canvas at the specifed location. This is used to represent the "dropdown"
        /// indicator over the waveform for events (times tags, sections tags, etc).
        /// </summary>
        /// <param name="position">Canvas x-position at which to draw the line.</param>
        /// <param name="brush">The fill colour for the line.</param>
        private void AddExtendedLine(double position, Brush brush)
        {
            var startLine = new Line
            {
                Stroke = brush,
                StrokeThickness = 0.5,
                StrokeDashArray = new DoubleCollection { 2.0, 1.0 },
                X1 = position,
                X2 = position,
                Y1 = 0,
                Y2 = markersExtendedCanvas.ActualHeight,
            };

            markersExtendedCanvas.Children.Add(startLine);
        }

        private void UpdateToolTips()
        {
            if (multiThumbSlider == null)
                return;

            if (MediaMarkers == null)
                return;

            string prevTooltip = string.Empty;
            double prevStartPos = -1d;

            // get list ordered by time
            var timeOrdered = MediaMarkers.OrderBy(m => m.StartTime).ToList();

            // now where start times are same, need to determine which is "on top" given the zorder from multiThumbSlider
            timeOrdered.Sort((a, b) =>
            {
                // if one or the other is not visible, is doesn't really matter what order they are in, so we mark them the same
                if (!a.IsVisible || !b.IsVisible)
                    return 0;

                if (a.StartTime < b.StartTime)
                    return -1;
                if (a.StartTime > b.StartTime)
                    return 1;

                // check if times are the same, if not, we don't care
                if (a.StartTime == b.StartTime)
                {

                    if (multiThumbSlider.ZOrder.ContainsKey(a.Id) &&
                        multiThumbSlider.ZOrder.ContainsKey(b.Id))
                    {
                        // a is "on top" of b
                        if (multiThumbSlider.ZOrder[a.Id] > multiThumbSlider.ZOrder[b.Id])
                        {
                            return 1;
                        }
                        // b is "on top" of a
                        if (multiThumbSlider.ZOrder[a.Id] < multiThumbSlider.ZOrder[b.Id])
                        {
                            return -1;
                        }
                    }
                }

                return 0;
            });

            // go through all the tags and ranges
            foreach (var mediaMarker in timeOrdered)
            {
                // if the marker is not visible (as in not shown on screen at all continue)
                if (mediaMarker == null || !mediaMarker.IsVisible)
                    continue;

                // set the tooltip for the specific marker
                string toolTip = string.IsNullOrEmpty(mediaMarker.Description)
                                     ? string.Format(CultureInfo.CurrentCulture, "{0}", string.Empty)
                                     : string.Format(CultureInfo.CurrentCulture, "{0}", mediaMarker.Description);

                if (mediaMarker.IsSystemTag)
                    toolTip = string.Format(CultureInfo.CurrentCulture, "{0}", mediaMarker.Name);


                // determine marker values
                double markerStart = TimeSpanToPoint(mediaMarker.StartTime);

                // see if the previous marker is under the current; update the tooltip as required
                if (prevStartPos >= 0 && MediaHelper.AreEqual(markerStart, prevStartPos))
                {
                    toolTip = toolTip + Environment.NewLine + prevTooltip;
                }

                // set the previous tooltip
                prevTooltip = toolTip;
                prevStartPos = markerStart;

                if (mediaMarker.Length.HasValue)
                {
                    if (multiThumbSlider.MarkerRanges.ContainsKey(mediaMarker.Id))
                    {
                        var range = multiThumbSlider.MarkerRanges[mediaMarker.Id];
                        range.ToolTip = toolTip;
                    }
                }
                else
                {
                    if (multiThumbSlider.MarkerThumbs.ContainsKey(mediaMarker.Id))
                    {
                        var thumb = multiThumbSlider.MarkerThumbs[mediaMarker.Id];
                        thumb.ToolTip = toolTip;
                    }
                }

            }
        }

        /// <summary>
        /// Update all the media markers and ranges (completly clears and re-draws everything)
        /// </summary>
        private void UpdateMediaMarkers()
        {
            if (multiThumbSlider == null || markersExtendedCanvas == null)
                return;

            if (MediaMarkers == null)
                return;

            if (MediaDuration == TimeSpan.Zero)
                return;

            // clear the markers and sliders
            multiThumbSlider.Clear();
            markersExtendedCanvas.Children.Clear();

            // go through all the tags and ranges
            foreach (var m in MediaMarkers)
                AddMarker(m);

            UpdateToolTips();
            UpdateMediaMarkerExtendedLines();
        }

        private Guid AddMarker(MediaMarker marker)
        {
            marker.PropertyChanged += OnMediaMarkerPropertyChanged;

            // if the marker is not visible (as in not shown on screen at all continue)
            if (!marker.IsVisible)
                return Guid.Empty;

            var markerStart = TimeSpanToPoint(marker.StartTime);

            if (marker.Length.HasValue)
            {
                var markerEnd = TimeSpanToPoint(marker.StartTime + marker.Length.GetValueOrDefault());
                var range = multiThumbSlider.AddRange(marker.Id, markerStart,
                                          markerEnd, marker.Name,
                                          marker.Description,
                                          MarkersLocked || marker.IsReadOnly,
                                          marker.IsReadOnly);

                range.MouseDoubleClick += Range_MouseDoubleClick;
                return range.Id;
            }


            var thumb = multiThumbSlider.AddThumb(marker.Id, markerStart, marker.Name,
                                      marker.Description,
                                      MarkersLocked || marker.IsReadOnly,
                                      marker.IsReadOnly,
                                      marker.IsSystemTag);
            thumb.MouseDoubleClick += Thumb_MouseDoubleClick;
            thumb.EditorCanceled += Thumb_EditorCanceled;
            return thumb.Key;
        }

        private void Range_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            var markerRange = (MarkerRange)sender;
            var marker = MediaMarkers.FirstOrDefault(x => x.Id == markerRange.Id);
            if (marker == null)
                return;

            SeekToMarker(marker);
        }

        private void Thumb_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            var markerThumb = (MarkerThumb)sender;
            var marker = MediaMarkers.FirstOrDefault(x => x.Id == markerThumb.Key);
            if (marker == null)
                return;

            SeekToMarker(marker);
        }

        private void Thumb_EditorCanceled(object sender, EventArgs e)
        {
            AddMarkerButtonToggle(true);
        }

        private void UpdateAllRegions()
        {
            UpdateSelectionRegion();
            UpdateMediaTimer();
            UpdateMediaMarkers();
            UpdateProgressIndicator();
        }

        private void UpdateMediaTimer()
        {
            if (mediaElement == null)
                return;

            var pos = mediaElement.Position;
            var duration = MediaDuration;

            bool showHours = duration > TimeSpan.Zero && duration.TotalHours >= 1;
            string fmt = showHours ? (TimeFormat) : (TimeNoHoursFormat);

            switch (mediaTimerType)
            {
                case MediaTimerType.Remaining:
                    TimeSpan rem = duration - pos;
                    int remhour = Convert.ToInt32(Math.Floor(rem.TotalHours));
                    timerText.Text = string.Format(CultureInfo.InvariantCulture, fmt, remhour, rem.Minutes, rem.Seconds, rem.Milliseconds);
                    break;

                case MediaTimerType.Elapsed:
                    int ehour = Convert.ToInt32(Math.Floor(pos.TotalHours));
                    timerText.Text = string.Format(CultureInfo.InvariantCulture, fmt, ehour, pos.Minutes, pos.Seconds, pos.Milliseconds);
                    break;

                case MediaTimerType.ElapsedWithDuration:
                default:
                    int hour = Convert.ToInt32(Math.Floor(pos.TotalHours));
                    timerText.Text = string.Format(CultureInfo.InvariantCulture, fmt, hour, pos.Minutes, pos.Seconds, pos.Milliseconds)
                                     + "/"
                                     + string.Format(CultureInfo.InvariantCulture, fmt, duration.Hours, duration.Minutes, duration.Seconds, duration.Milliseconds);
                    break;
            }
        }

        private void CreateSelectionHandlePaths()
        {
            leftSelectThumb = new Thumb
            {
                Style = SelectionLeftThumb,
                Focusable = false,
                IsTabStop = false,
            };
            leftSelectThumb.DragDelta += SelectThumbOnDragDelta;
            leftSelectThumb.DragCompleted += SelectThumbOnDragCompleted;

            rightSelectThumb = new Thumb
            {
                Style = SelectionRightThumb,
                Focusable = false,
                IsTabStop = false,
            };
            rightSelectThumb.DragDelta += SelectThumbOnDragDelta;
            rightSelectThumb.DragCompleted += SelectThumbOnDragCompleted;
        }

        private void SelectThumbOnDragCompleted(object sender, DragCompletedEventArgs dragCompletedEventArgs)
        {
            changingSelectedRegion = false;
            ApplySelectedRange();
            SeekPosition(this.startSelectedRegion.Value);
            ResumePlaying();

            // Enable the global add marker button on the right of the timeline
            AddMarkerButtonToggle(true);
        }

        private void SelectThumbOnDragDelta(object sender, DragDeltaEventArgs dragDeltaEventArgs)
        {
            var thumb = (Thumb)sender;

            if (!changingSelectedRegion)
            {
                changingSelectedRegion = true;
                PausePlaying();
            }

            selectedRangeMarkerId = null;

            var thumbPos = Clamp(Canvas.GetLeft(thumb) + dragDeltaEventArgs.HorizontalChange, 0, timescaleCanvas.ActualWidth);

            if (thumb == leftSelectThumb)
                startSelectedRegion = PointToTimeSpan(thumbPos);
            else if (thumb == rightSelectThumb)
                endSelectedRegion = PointToTimeSpan(thumbPos);

            Canvas.SetLeft(thumb, thumbPos);

            // Catch an overlap and switch up the start/end
            if (endSelectedRegion < startSelectedRegion)
            {
                SwapSelectThumbs();

                var newEnd = startSelectedRegion;
                startSelectedRegion = endSelectedRegion;
                endSelectedRegion = newEnd;
            }

            SetSelectedRange(startSelectedRegion.Value, endSelectedRegion.Value);
        }

        public static double Clamp(double x, double lowerBound, double upperBound)
        {
            return (x < lowerBound ? lowerBound : (x > upperBound) ? upperBound : x);
        }

        private void SwapSelectThumbs()
        {
            // Switch visual styles and variables (so devs debugging don't go insane :)
            var oldLeft = this.leftSelectThumb;
            this.leftSelectThumb = this.rightSelectThumb;
            this.leftSelectThumb.Style = SelectionLeftThumb;
            this.rightSelectThumb = oldLeft;
            this.rightSelectThumb.Style = SelectionRightThumb;
        }
        private void UpdateSelectionRegion()
        {
            if (timescaleCanvas == null && selectionCanvas == null)
                return;

            if (HasSelectedRegion)
            {
                double left = TimeSpanToPoint(startSelectedRegion.GetValueOrDefault());
                double right = TimeSpanToPoint(endSelectedRegion.GetValueOrDefault());

                Canvas.SetLeft(leftSelectThumb, left);
                if (!timescaleCanvas.Children.Contains(leftSelectThumb))
                    timescaleCanvas.Children.Add(leftSelectThumb);

                Canvas.SetLeft(rightSelectThumb, right);
                if (!timescaleCanvas.Children.Contains(rightSelectThumb))
                    timescaleCanvas.Children.Add(rightSelectThumb);

                if (!HasVideo)
                {
                    Canvas.SetLeft(selectionRegion, left);
                    selectionRegion.Width = right - left;
                    selectionRegion.Height = selectionCanvas.ActualHeight;
                    if (!selectionCanvas.Children.Contains(selectionRegion))
                        selectionCanvas.Children.Add(selectionRegion);
                }
                else
                {
                    selectionCanvas.Children.Remove(selectionRegion);
                }
            }
            else
            {
                timescaleCanvas.Children.Remove(leftSelectThumb);
                timescaleCanvas.Children.Remove(rightSelectThumb);
                selectionCanvas.Children.Remove(selectionRegion);
            }
        }

        private void UpdateProgressIndicator()
        {
            if (mediaElement == null || progressThumb == null)
                return;

            var x = TimeSpanToPoint(mediaElement.Position);

            Canvas.SetLeft(progressLineArea, x - progressLineArea.Width / 2);
            progressLineArea.Height = progressCanvas.ActualHeight;

            progressLine.X1 = progressLine.X2 = x;
            progressLine.Y2 = progressCanvas.ActualHeight;

            progressLine.Visibility = progressLineArea.Visibility = HasVideo ? Visibility.Collapsed : Visibility.Visible;

            Canvas.SetLeft(progressThumb, x);
            Canvas.SetTop(progressThumb, -progressThumb.ActualHeight);

            timescaleCanvas.InvalidateVisual(); // wpf wierd bug workaround when timescale does not get re-drawn
        }

        /// <summary>
        /// Moves the media position backwards to the marker with the closest start time.
        /// </summary>
        private void SkipPrevious()
        {
            var marker = MediaMarkers?
                    .OrderBy(x => x.StartTime)
                    .LastOrDefault(x => x.IsVisible && x.StartTime < mediaElement.Position);

            var wasPlaying = mediaElement.IsPlaying;

            SeekToMarker(marker);
            if (marker == null)
                SeekPosition(TimeSpan.Zero);

            if (wasPlaying)
            {
                mediaElement.Pause();
                resumeSubject.OnNext(null);
            }

            NotifyActivity();
        }

        /// <summary>
        /// Moves the media position forwards to the marker with the closest start time.
        /// </summary>
        private void SkipNext()
        {
            ClearSelectedRange();

            var marker = this.MediaMarkers?
                .OrderBy(x => x.StartTime)
                .FirstOrDefault(x => x.IsVisible && x.StartTime > mediaElement.Position);

            var wasPlaying = mediaElement.IsPlaying;

            SeekToMarker(marker);
            if (marker == null)
                // seeking exactly to the end of the media may cause media player internal error. Let's substruct one tick to prevent that 
                SeekPosition(MediaDuration - TimeSpan.FromTicks(1));

            if (wasPlaying)
            {
                mediaElement.Pause();
                resumeSubject.OnNext(null);
            }

            NotifyActivity();
        }

        public void SeekToMarker(MediaMarker marker)
        {
            if (marker != null)
            {
                if (marker.StartTime >= MediaDuration)
                    mediaElement.Pause();

                if (marker.Length.HasValue)
                {
                    SetSelectedRange(marker.StartTime, marker.StartTime + marker.Length.Value);
                    selectedRangeMarkerId = marker.Id;
                }
                else
                    ClearSelectedRange();

                SeekPosition(marker.StartTime);
            }
            else
                ClearSelectedRange();


            SeekToMarkerEvent?.Invoke(this, new EventArgs<MediaMarker>(marker));
        }

        public void SeekToMarkerAndPlay(MediaMarker marker)
        {
            SeekToMarker(marker);

            if (marker.StartTime < MediaDuration)
                mediaElement.Play();
        }

        /// <summary>
        /// Helper function to convert a time location (in seconds) in the media player to a location on the provided canvas.
        /// This allows the caller to go from seconds to pixels.
        /// </summary>
        /// <param name="timeSpan">The time span for which a pixel location is to be determined.</param>
        /// <returns>The pixel location for the given time span.</returns>

        private double TimeSpanToPoint(TimeSpan timeSpan)
        {
            if (MediaDuration == TimeSpan.Zero)
                return 0;

            var renderWidth = timescaleCanvas.ActualWidth;
            return Clamp(((double)timeSpan.Ticks / MediaDuration.Ticks) * renderWidth, 0, renderWidth);
        }

        /// <summary>
        /// Helper function to convert a location on a canvas into a time in the media player. This allows the caller to relate
        /// a pixel location in a timeline-related canvas to an actual spot in the current media.
        /// </summary>
        /// <param name="point">Location on the canvas for which a media time is to be determined.</param>
        /// <returns>The time represented by the given pixel location.</returns>
        private TimeSpan PointToTimeSpan(double point)
        {
            if (timescaleCanvas.ActualWidth == 0)
                return TimeSpan.Zero;

            var span = TimeSpan.FromMilliseconds((point / timescaleCanvas.ActualWidth) * MediaDuration.TotalMilliseconds);

            // Ensure within media bounds
            return span.EnsureInRange(TimeSpan.Zero, MediaDuration);
        }

        private void SetSelectedRange(TimeSpan start, TimeSpan end)
        {
            startSelectedRegion = start.Max(TimeSpan.Zero);
            endSelectedRegion = end.Min(MediaDuration);
            UpdateSelectionRegion();

            if (!changingSelectedRegion)
                ApplySelectedRange();
        }

        private void ApplySelectedRange()
        {
            if (mediaElement == null)
                return;

            if (HasSelectedRegion)
            {
                var mediaSegments = MediaSource.Segments.Where(s => s.Source != null).OrderBy(s => s.Offset).ToList();

                var min = startSelectedRegion.Value;
                var max = endSelectedRegion.Value;
                var isMinValid = mediaSegments.Any(s => s.InRange(min));
                var isMaxValid = mediaSegments.Any(s => s.InRange(max));

                // ensure that both min and max are NOT in the same minimized area. if it is then clear the selection
                if (isMinValid || isMaxValid || mediaSegments.Any(s => s.Offset.InRange(min, max)))
                {
                    // snap max value to the closest segment
                    if (!isMaxValid)
                    {
                        var segment = mediaSegments.LastOrDefault(s => s.Offset < max);
                        if (segment != null)
                            max = segment.Offset + segment.Duration;
                        else
                        {
                            segment = mediaSegments.FirstOrDefault(s => s.Offset > max);
                            if (segment != null)
                                max = segment.Offset;
                        }
                    }

                    // snap min value to the closest segment
                    if (!isMinValid)
                    {
                        var segment = mediaSegments.FirstOrDefault(s => s.Offset > min);
                        if (segment != null)
                            min = segment.Offset;
                    }

                    startSelectedRegion = min.Min(max);
                    endSelectedRegion = max;
                }
                else
                {
                    startSelectedRegion = null;
                    endSelectedRegion = null;
                }

                UpdateSelectionRegion();
            }

            if (mediaElement.RepeatEnabled && HasSelectedRegion)
                mediaElement.SetPositionRange(startSelectedRegion.Value, endSelectedRegion.Value);
            else
                mediaElement.ClearPositionRange();
        }

        private void ClearSelectedRange()
        {
            selectedRangeMarkerId = null;
            startSelectedRegion = null;
            endSelectedRegion = null;
            mediaElement?.ClearPositionRange();
            UpdateSelectionRegion();
        }

        private bool CanEditMediaMarker
        {
            get { return MediaMarkers != null && AllowEditMediaMarkers && MediaSource != null; }
        }

        private void SeekPosition(TimeSpan position)
        {
            // If the media is buffering, seeking has proven problematic and sometimes causes media playback to fail
            if (!mediaElement.IsMediaBuffering)
                mediaElement.Seek(position, TimeSeekOrigin.BeginTime, true);
        }

        // for automation tests
        public void SeekPositionInSeconds(int positionInSeconds)
        {
            SeekPosition(TimeSpan.FromSeconds(positionInSeconds));
        }

        private void SeekOffset(TimeSpan offset)
        {
            mediaElement.Seek(offset, TimeSeekOrigin.Duration, false);
        }

        internal TimeSpan MediaDuration
        {
            get { return MediaSource != null ? MediaSource.Duration : TimeSpan.Zero; }
        }

        private bool HasVideo
        {
            get { return MediaSource != null && MediaSource.HasVideo; }
        }

        private bool HasSelectedRegion
        {
            get { return startSelectedRegion.HasValue && endSelectedRegion.HasValue; }
        }

        bool wasPlaying;
        private void PausePlaying()
        {
            wasPlaying = mediaElement.IsPlaying;
            if (wasPlaying)
                mediaElement.Pause();
        }

        private void ResumePlaying()
        {
            if (wasPlaying && mediaElement.Position < MediaDuration)
                mediaElement.Play();
        }
    }
}
