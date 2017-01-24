using System;
using System.Windows.Controls.Primitives;
using System.Windows;
using System.Windows.Controls;
using System.ComponentModel;
using System.Windows.Input;
using Microsoft.Practices.Prism.Commands;
using MediaPlaybackLib.MultiThumbSlider.Adorners;
using MediaPlayback.MultiThumbSlider;

namespace MediaPlaybackLib.MultiThumbSlider
{
    /// <summary>
    /// A slider that provides the a range
    /// </summary>
    [DefaultEvent("RangeSelectionChanged"),
    TemplatePart(Name = "PART_RangeSliderContainer", Type = typeof(Canvas)),
    TemplatePart(Name = "PART_LeftThumb", Type = typeof(Thumb)),
    TemplatePart(Name = "PART_MiddleThumb", Type = typeof(Thumb)),
    TemplatePart(Name = "PART_RightThumb", Type = typeof(Thumb))]
    [TemplatePart(Name = "PART_AdornerBehavior", Type = typeof(ContentTemplateAdornerBehavior))]
    [Localizability(LocalizationCategory.NeverLocalize)]
    public sealed class MarkerRange : Control
    {
        #region Data members

        private bool isInternalSet = false;
        const double DefaultSplittersThumbWidth = 5;
        private const int MinRangeOffsetHelper = 0;
        Thumb centerThumb;  
        Thumb leftThumb;    
        Thumb rightThumb;   

        //canvas to store the visual elements for this control
        Canvas visualElementsContainer;

        // editing adorner control
        private ContentTemplateAdornerBehavior editingAdorner;

        private string previousDescription = string.Empty;

        /// <summary>
        /// have we actually started to drag the thumb
        /// </summary>
        private bool isDragging;

        /// <summary>
        /// was editng previous open prior to dragging started
        /// </summary>
        private bool wasEditingOpen;

        #endregion

        #region properties and events

        /// <summary>
        /// Get the state of the thumb editing.
        /// </summary>
        /// <returns>true if editing, false otherwise</returns>
        public static readonly DependencyProperty IsEditingProperty =
          DependencyProperty.Register("IsEditing", typeof(bool), typeof(MarkerRange), new PropertyMetadata(false));

        /// <summary>
        /// Get the state of the thumb editing.
        /// </summary>
        /// <returns>true if editing, false otherwise</returns>
        public bool IsEditing
        {
            get { return (bool)GetValue(IsEditingProperty); }
            set { SetValue(IsEditingProperty, value); }
        }

        /// <summary>
        /// Get or Set if the thumb is selected
        /// </summary>
        public static readonly DependencyProperty IsSelectedProperty =
          DependencyProperty.Register("IsSelected", typeof(bool), typeof(MarkerRange),
                                      new PropertyMetadata(default(bool)));
        
        /// <summary>
        /// Get or Set if the thumb is selected
        /// </summary>
        public bool IsSelected
        {
            get { return (bool)GetValue(IsSelectedProperty); }
            set { SetValue(IsSelectedProperty, value); }
        }

        /// <summary>
        /// Gets or Sets the range to display.
        /// </summary>
        /// <exception cref="Exception">Thrown when Range is invalid</exception>
        public static readonly DependencyProperty RangeProperty =
            DependencyProperty.Register("Range", typeof (Tuple<double, double>), typeof (MarkerRange),
            new UIPropertyMetadata(default(Tuple<double, double>),
                 (sender, eventArgs)=>
                     {
                         var slider = (MarkerRange)sender;

                         var val = (Tuple<double, double>) eventArgs.NewValue;
                         
                         // start and end can't be less than 0
                         if (val.Item1 < 0 || val.Item2 < 0 )
                             throw new Exception("Invalid range");

                         // start can't be after end and end can't be before start (plus min range).
                         if ( (val.Item1 + slider.MinRange) > val.Item2 ) 
                         {
                             throw new Exception("Invalid range");
                         }
                      
                         if (!slider.isInternalSet)
                            slider.UpdateVisualsToCurrentRange();

                     }));

        /// <summary>
        /// Gets or Sets the range to display.
        /// </summary>
        /// <exception cref="Exception">Thrown when Range is invalid</exception>
        public Tuple<double, double> Range
        {
            get { return (Tuple<double, double>) GetValue(RangeProperty); }
            set { SetValue(RangeProperty, value); }
        }
        
      

        /// <summary>
        /// The min range value that you can have for the range slider
        /// </summary>
        /// <exception cref="Exception">Thrown when MinRange is set less than 0</exception>
        public static readonly DependencyProperty MinRangeProperty =
            DependencyProperty.Register("MinRange", typeof(double), typeof(MarkerRange),
            new UIPropertyMetadata((double)0,
                                   (sender, e) =>
                                   {
                                       if ((double)e.NewValue < 0)
                                           throw new Exception("value for MinRange cannot be less than 0");

                                       var slider = (MarkerRange)sender;
                                       slider.UpdateVisualsToCurrentRange();

                                   }));

        /// <summary>
        /// The min range value that you can have for the range slider
        /// </summary>
        /// <exception cref="Exception">Thrown when MinRange is set less than 0</exception>
        public double MinRange
        {
            get { return (double)GetValue(MinRangeProperty); }
            set { SetValue(MinRangeProperty, value); }
        }

        /// <summary>
        /// Event raised whenever the selected range is changed
        /// </summary>
        public static readonly RoutedEvent RangeSelectionChangedEvent =
            EventManager.RegisterRoutedEvent("RangeSelectionChanged",
            RoutingStrategy.Bubble, typeof(EventHandler<MarkerRangeSelectionChangedEventArgs>), typeof(MarkerRange));
        
        /// <summary>
        /// Event raised whenever the selected range is changed
        /// </summary>
        public event EventHandler<MarkerRangeSelectionChangedEventArgs> RangeSelectionChanged
        {
            add { AddHandler(RangeSelectionChangedEvent, value); }
            remove { RemoveHandler(RangeSelectionChangedEvent, value); }
        }

        /// <summary>
        /// Event raised whenever the selected range is completed
        /// </summary>
        public static readonly RoutedEvent RangeSelectionCompletedEvent =
            EventManager.RegisterRoutedEvent("RangeSelectionCompleted",
            RoutingStrategy.Bubble, typeof(EventHandler<MarkerRangeSelectionCompletedEventArgs>), typeof(MarkerRange));

        /// <summary>
        /// Event raised whenever the selected range is change is finished
        /// </summary>
        public event EventHandler<MarkerRangeSelectionCompletedEventArgs> RangeSelectionCompleted
        {
            add { AddHandler(RangeSelectionCompletedEvent, value); }
            remove { RemoveHandler(RangeSelectionCompletedEvent, value); }
        }



        public static readonly DependencyProperty IsReadOnlyProperty =
            DependencyProperty.Register("IsReadOnly", typeof(bool), typeof(MarkerRange),
                                        new PropertyMetadata(default(bool)));

        public static readonly DependencyProperty IsFrozenProperty =
            DependencyProperty.Register("IsFrozen", typeof(bool), typeof(MarkerRange),
                                        new PropertyMetadata(default(bool)));

        public static readonly DependencyProperty DisplayNameProperty =
            DependencyProperty.Register("DisplayName", typeof(string), typeof(MarkerRange),
                                        new PropertyMetadata(default(string)));

        public static readonly DependencyProperty DescriptionProperty =
            DependencyProperty.Register("Description", typeof(string), typeof(MarkerRange),
                                        new PropertyMetadata(default(string)));

        /// <summary>
        /// Indicated that the Thumb is read-only. 
        /// </summary>
        /// <remarks>if true, the marker will be a different color and the editing pop-up adorner will never show. It will not be movable.</remarks>
        public bool IsReadOnly
        {
            get { return (bool)GetValue(IsReadOnlyProperty); }
            set { SetValue(IsReadOnlyProperty, value); }
        }

        /// <summary>
        /// Get or Set the frozen state of the thumb.
        /// </summary>
        /// <remarks>If true, the thumb cannot be moved, it is however still editable.</remarks>
        public bool IsFrozen
        {
            get { return (bool)GetValue(IsFrozenProperty); }
            set { SetValue(IsFrozenProperty, value); }
        }

        /// <summary>
        /// Get or Set the Display name of the marker. 
        /// </summary>
        /// <remarks>Shown in the editing adorner; not editable</remarks>
        public string DisplayName
        {
            get { return (string)GetValue(DisplayNameProperty); }
            set { SetValue(DisplayNameProperty, value); }
        }

        /// <summary>
        /// Get or Set the Description of the marker
        /// </summary>
        /// <remarks>Shown in the editing adorner; editable</remarks>
        public string Description
        {
            get { return (string)GetValue(DescriptionProperty); }
            set { SetValue(DescriptionProperty, value); }
        }

        public Guid Id { get; private set; }

        public double MoveableWidth { get; set; }

        #endregion

        #region Commands

        private ICommand modifyRequestCommand;

        /// <summary>
        /// Modify Reuqest. 
        /// </summary>
        /// <remarks>Will send out a Modified event if able to commit.</remarks>
        public ICommand ModifyRequestCommand
        {
            get { return modifyRequestCommand ?? (modifyRequestCommand = new DelegateCommand(() => OnModifyRequest(EventArgs.Empty), () => !IsReadOnly)); }
        }

        private ICommand deleteCommand;

        /// <summary>
        /// Delete Request
        /// </summary>
        /// <remarks>will send out a delete request if not readonly</remarks>
        public ICommand DeleteCommand
        {
            get { return deleteCommand ?? (deleteCommand = new DelegateCommand(() => OnDeleteRequest(EventArgs.Empty), () => !IsReadOnly)); }
        }

        private ICommand closeEditCommand;

        /// <summary>
        /// Close Edit adorner command
        /// </summary>
        /// <remarks>closes the editing control</remarks>
        public ICommand CloseEditCommand
        {
            get { return closeEditCommand ?? (closeEditCommand = new DelegateCommand(OnCloseEditor)); }
        }

        #endregion

        #region Events

        /// <summary>
        /// Delete Request Event
        /// </summary>
        public event EventHandler<EventArgs> DeleteRequest;

        /// <summary>
        /// Modify Request Event
        /// </summary>
        public event EventHandler<EventArgs> Modified;

        /// <summary>
        /// Marker selected Event
        /// </summary>
        public event EventHandler MarkerThumbSelect;

        /// <summary>
        /// Editing cancelled event (lost focus or manual close)
        /// </summary>
        public event EventHandler EditorCancelled;
        #endregion

        /// <summary>
        /// Send out the modify request event
        /// </summary>
        /// <param name="eventArgs">Empty</param>
        private void OnModifyRequest(EventArgs eventArgs)
        {
            previousDescription = Description;
            EventHandler<EventArgs> handler = this.Modified;
            if (handler != null) handler(this, eventArgs);
            editingAdorner.Hide();
            IsEditing = false;
        }

        /// <summary>
        /// Send out a delete request event.
        /// </summary>
        /// <param name="eventArgs">Empty</param>
        private void OnDeleteRequest(EventArgs eventArgs)
        {
            EventHandler<EventArgs> handler = this.DeleteRequest;
            if (handler != null) handler(this, eventArgs);
            OnCloseEditor();
        }

        /// <summary>
        /// Send out the marker selected event
        /// </summary>
        private void OnMarkerThumbSelect()
        {
            EventHandler handler = MarkerThumbSelect;
            if (handler != null) handler(this, EventArgs.Empty);
        }

        private void OnCloseEditor()
        {
            editingAdorner.Hide();
            Description = previousDescription;
            EventHandler handler = EditorCancelled;
            if (handler != null && IsEditing) handler(this, EventArgs.Empty);
            IsEditing = false;
        }

        /// <summary>
        /// Default constructor
        /// </summary>
        /// <param name="uniqueId"> </param>
        public MarkerRange(Guid uniqueId)
        {
            MoveableWidth = 0;
            Width = 0; // force width to 0 so that multiple ranges do not cause canvas overlap and thus not able to control anything but top marker range
            Id = uniqueId;

            Loaded += OnLoaded;
        }

        public MarkerRange():this(Guid.NewGuid())
        {
            
        }

        /// <summary>
        /// Static constructor
        /// </summary>
        static MarkerRange()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(MarkerRange), new FrameworkPropertyMetadata(typeof(MarkerRange)));
        }

        /// <summary>
        /// Update the display logic of all thumbs for the slider
        /// </summary>
        private void UpdateVisualsToCurrentRange()
        {
            if (double.IsNaN(this.MoveableWidth) || MediaHelper.IsZero(this.MoveableWidth) || centerThumb == null || rightThumb == null || leftThumb == null)
                return;

            // need to setup the slider accordingly
            if (MediaHelper.IsGreaterThan(Range.Item2, this.MoveableWidth) || MediaHelper.IsGreaterThan(Range.Item1, this.MoveableWidth))
                throw new InvalidOperationException("Marker is positioned outside of the range of the media length.");

            var rangeWidth = Range.Item2 - Range.Item1;
            centerThumb.Width = rangeWidth;

            Canvas.SetLeft(centerThumb, Range.Item1);
            Canvas.SetLeft(rightThumb, Range.Item2);
            Canvas.SetLeft(leftThumb, Range.Item1 - leftThumb.Width);
        }
   
        #region event handlers for visual elements to drag the range

        /// <summary>
        /// Start functionality always performed when attempting to drag
        /// </summary>
        /// <returns>false if we don't want to allow dragging</returns>
        private bool DragStartHelper()
        {
            // don't allow drag.
            if (IsReadOnly || IsFrozen)
                return false;

            // if not dragging
            if (!isDragging)
            {
                wasEditingOpen = editingAdorner.IsVisible;
                editingAdorner.Hide();
                IsEditing = false;
            }

            isDragging = true;

            return true;
        }

        /// <summary>
        /// Help determine the move amount for the drag (for moving thumbs)
        /// </summary>
        /// <param name="moveLeft">available space to move left</param>
        /// <param name="moveRight">available stapce to move right</param>
        /// <param name="moveAmount">requested move amount</param>
        /// <returns>the amount to move</returns>
        private double DragMoveHelper(double moveLeft, double moveRight, double moveAmount)
        {
            if (moveAmount > 0) // move right
            {
                // max change reached
                if (MediaHelper.IsGreaterThan(moveAmount, moveRight))
                    moveAmount = moveRight;
            }
            else if (moveAmount < 0) // move left
            {
                if (MediaHelper.IsGreaterThan(Math.Abs(moveAmount), moveLeft))
                    moveAmount = 0 - moveLeft;
            }

            return moveAmount;
        }

        /// <summary>
        /// Move the center thumb and either right or left thumb 
        /// </summary>
        /// <param name="thumb">the left or right thumb to move</param>
        /// <param name="moveAmount">amount to move</param>
        /// <param name="negateCenterWidth">true if left thumb, false if right</param>
        private void DragLeftRightHelper(Thumb thumb, double moveAmount, bool negateCenterWidth)
        {
            // if left thumb we want center thumb size to grow
            var centerChange = negateCenterWidth ? 0 - moveAmount : moveAmount;

            // change width of center thumb
            var currentWidth = centerThumb.Width;
            var newCenterWidth = currentWidth + centerChange;
            if (newCenterWidth < MinRange)
                newCenterWidth = MinRange;

            centerThumb.Width = newCenterWidth;

            // move the center thumb
            if (negateCenterWidth)
                Canvas.SetLeft(centerThumb, Canvas.GetLeft(centerThumb) + moveAmount);

            // set the left thumb
            var newSpot = Canvas.GetLeft(thumb) + moveAmount;
            
            Canvas.SetLeft(thumb, newSpot);
            OnRangeSelectionChanged();   
        }
        
        /// <summary>
        /// Left thumb drag
        /// </summary>
        /// <param name="sender">sender of the event</param>
        /// <param name="eventArgs">event arguments</param>
        private void LeftThumbDragDelta(object sender, DragDeltaEventArgs eventArgs)
        {
            if (!DragStartHelper())
                return;

            // determine if the change will cause left thumb to reach the right most limit (right thumb less minwidth)
            var maxPositionLTRE = Canvas.GetLeft(rightThumb) - MinRange - MinRangeOffsetHelper;
            var rightEdgeLeftThumb = Canvas.GetLeft(leftThumb) + leftThumb.Width;

            var movableAmountToRight = maxPositionLTRE - rightEdgeLeftThumb;
            var movableAmountToLeft = Canvas.GetLeft(leftThumb) + leftThumb.Width;

            var moveAmount = DragMoveHelper(movableAmountToLeft, movableAmountToRight, eventArgs.HorizontalChange);
            DragLeftRightHelper(leftThumb, moveAmount, true);
        }

        /// <summary>
        /// Right thumb drag
        /// </summary>
        /// <param name="sender">sender of the evnet</param>
        /// <param name="eventArgs">event arguments</param>
        private void RightThumbDragDelta(object sender, DragDeltaEventArgs eventArgs)
        {
            if (!DragStartHelper())
                return;

            var currentLeftEdgeRightThumb = Canvas.GetLeft(rightThumb);

            //note: the left edge is the range, we do not include the width of the marker, thus we must allow cliptobounds false to display over edge of control
            var movableAmountToRight = MoveableWidth - currentLeftEdgeRightThumb;
            var movableAmountToLeft = centerThumb.Width - MinRange - MinRangeOffsetHelper;

            var moveAmount = DragMoveHelper(movableAmountToLeft, movableAmountToRight, eventArgs.HorizontalChange);

            DragLeftRightHelper(rightThumb, moveAmount, false);
        }

       
        /// <summary>
        /// Center thumb drag
        /// </summary>
        /// <param name="sender">sender of the event</param>
        /// <param name="evneArgs">event arguments</param>
        private void CenterThumbDragDelta(object sender, DragDeltaEventArgs evneArgs)
        {
            if (!DragStartHelper())
                return;

            var availableMoveLeft = Canvas.GetLeft(leftThumb) + leftThumb.Width;
            var availableMoveRight = MoveableWidth - Canvas.GetLeft(rightThumb);
            
            var move = DragMoveHelper(availableMoveLeft, availableMoveRight,evneArgs.HorizontalChange);

            // move all items
            Canvas.SetLeft(leftThumb, Canvas.GetLeft(leftThumb) + move);
            Canvas.SetLeft(rightThumb, Canvas.GetLeft(rightThumb) + move);
            Canvas.SetLeft(centerThumb, Canvas.GetLeft(centerThumb) + move);

            OnRangeSelectionChanged();
        }
        #endregion

        private void OnLoaded(object sender, RoutedEventArgs routedEventArgs)
        {
            // when we want to automatically show the editing adorner when created, we have to make sure we set focus once we are loaded, otherwise the 
            // correct bindings will not exist for the editing adorner
            if (!IsEditing) return;
            var scope = FocusManager.GetFocusScope(this);
            FocusManager.SetFocusedElement(scope, centerThumb);
        }

        /// <summary>
        /// Raise the MarkerRangeSelectionChangedEvent
        /// </summary>
        private void OnRangeSelectionChanged()
        {
            ReCalculateRange();   
            var eventToRaise = new MarkerRangeSelectionChangedEventArgs(this);
            eventToRaise.RoutedEvent = RangeSelectionChangedEvent;
            RaiseEvent(eventToRaise);
        }

        private void OnRangeSelectionComplete()
        {
            var eventToRaise = new MarkerRangeSelectionCompletedEventArgs(Id, Range.Item1, Range.Item2);
            eventToRaise.RoutedEvent = RangeSelectionCompletedEvent;
            RaiseEvent(eventToRaise);
        }

        private void ReCalculateRange()
        {
            isInternalSet = true;
            
            var rangeWidth = centerThumb.Width;
            var startRange = Canvas.GetLeft(centerThumb);

            Range = new Tuple<double, double>(startRange, startRange+ rangeWidth);

            isInternalSet = false;
        }


        /// <summary>
        /// Overide to get the visuals from the control template
        /// </summary>
        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();

            visualElementsContainer = EnforceInstance<Canvas>("PART_RangeSliderContainer");
            centerThumb = EnforceInstance<Thumb>("PART_MiddleThumb");
            leftThumb = EnforceInstance<Thumb>("PART_LeftThumb");
            rightThumb = EnforceInstance<Thumb>("PART_RightThumb");
            Canvas.SetZIndex(rightThumb, 0);
            Canvas.SetZIndex(centerThumb, 10);
            Canvas.SetZIndex(leftThumb, 5);

            editingAdorner = GetTemplateChild("PART_AdornerBehavior") as ContentTemplateAdornerBehavior;

            if (editingAdorner == null)
                throw new NullReferenceException("PART_AdornerBehavior must exit in template.");

            editingAdorner.DataContext = this;

            InitializeVisualElementsContainer();
            UpdateVisualsToCurrentRange();
        }

        #region Helper

        /// <summary>
        /// Enforce the UI elements to exist if not found in template
        /// </summary>
        /// <typeparam name="T">type</typeparam>
        /// <param name="partName">template name</param>
        /// <returns></returns>
        T EnforceInstance<T>(string partName)
            where T : FrameworkElement, new()
        {
            return GetTemplateChild(partName) as T ?? new T();
        }

        /// <summary>
        /// Initalize the controls
        /// </summary>
        private void InitializeVisualElementsContainer()
        {
            leftThumb.Width = DefaultSplittersThumbWidth;
            leftThumb.Tag = "left";
            leftThumb.Height = visualElementsContainer.Height;

            centerThumb.Height = visualElementsContainer.Height;
            centerThumb.Width = MinRange;
            centerThumb.Tag = "center";
           
            rightThumb.Width = DefaultSplittersThumbWidth;
            rightThumb.Tag = "right";
            rightThumb.Height = visualElementsContainer.Height;
            
            // set initial center width
            if (double.IsNaN(centerThumb.Width) || centerThumb.Width < centerThumb.MinWidth)
                centerThumb.Width = centerThumb.MinWidth;

            var leftpos = Canvas.GetLeft(leftThumb);
            if (double.IsNaN(leftpos))
            {
                leftpos = 0;
                Canvas.SetLeft(leftThumb, 0);
            }

            var centerpos = leftpos + leftThumb.Width;
            
            Canvas.SetLeft(centerThumb, centerpos);

            var rightPos = centerpos + centerThumb.Width;
            Canvas.SetLeft(rightThumb, rightPos);

            //handle the drag delta
            centerThumb.DragDelta += CenterThumbDragDelta;
            leftThumb.DragDelta += LeftThumbDragDelta;
            rightThumb.DragDelta += RightThumbDragDelta;

            // since MouseUp doesn't occuer (not sure why) we tie into DragStarted and DragEnded (since they occur on mouse down/up)
            centerThumb.DragStarted += ThumbOnDragStarted;
            centerThumb.DragCompleted += ThumbOnDragCompleted;
            centerThumb.GotFocus += OnThumbOnGotFocus;
            centerThumb.LostFocus += OnThumbOnLostFocus;

            leftThumb.DragStarted += ThumbOnDragStarted;
            leftThumb.DragCompleted += ThumbOnDragCompleted;
            leftThumb.GotFocus += OnThumbOnGotFocus;
            leftThumb.LostFocus += OnThumbOnLostFocus;

            rightThumb.DragStarted += ThumbOnDragStarted;
            rightThumb.DragCompleted += ThumbOnDragCompleted;
            rightThumb.GotFocus += OnThumbOnGotFocus;
            rightThumb.LostFocus += OnThumbOnLostFocus;
        }

        /// <summary>
        /// On thumb grip lost logical focus
        /// </summary>
        /// <param name="sender">sender of the event</param>
        /// <param name="args">arguments</param>
        private void OnThumbOnLostFocus(object sender, RoutedEventArgs args)
        {
            // set not editing and hide editing control
            OnCloseEditor();
        }

        /// <summary>
        /// On thumb grip got logical focus
        /// </summary>
        /// <param name="sender">sender of the event</param>
        /// <param name="args">arguments</param>
        private void OnThumbOnGotFocus(object sender, RoutedEventArgs args)
        {
            // set editing and show the editing control
            IsEditing = true;
            previousDescription = Description;
            editingAdorner.Show();
        }

        /// <summary>
        /// Thumb drag completed handler
        /// </summary>
        /// <param name="sender">sender of the evnet</param>
        /// <param name="dragCompletedEventArgs">event args</param>
        private void ThumbOnDragCompleted(object sender, DragCompletedEventArgs dragCompletedEventArgs)
        {
            ThumbOnMouseUp(sender as Thumb);

            // must be last due to ThumbOnMouseUp needing check
            isDragging = false;

            if (wasEditingOpen)
            {
                editingAdorner.Show();
                IsEditing = true;
            }

            wasEditingOpen = false;

            OnRangeSelectionComplete();
        }

        /// <summary>
        /// Thumb drag started handler
        /// </summary>
        /// <param name="sender">sender of the event</param>
        /// <param name="dragStartedEventArgs">event arguments</param>
        private void ThumbOnDragStarted(object sender, DragStartedEventArgs dragStartedEventArgs)
        {
            // don't show the editing sticky when clicked
            if (IsReadOnly || isDragging)
                return;

            IsSelected = true;
            OnMarkerThumbSelect();
        }

        /// <summary>
        /// Handle OnMouseDown. 
        /// </summary>
        /// <remarks>If the marker is editable the edit control will be shown.</remarks>
        private void ThumbOnMouseUp(Thumb sender)
        {
            // don't show the editing sticky when clicked
            if (IsReadOnly || isDragging )
                return;
           
            // if already focused then just toggle visibility (clicking on marker multiple times in a row)
            if (sender != null && sender.IsFocused && editingAdorner != null)
            {
                if (editingAdorner.IsVisible)
                    OnCloseEditor();
                else
                    editingAdorner.ToggleVisibility();
            }
            // try to make this the logical focus
            else if (sender != null)
            {
                FocusManager.SetFocusedElement(FocusManager.GetFocusScope(this), sender);
            }
        }
        
        #endregion
    }
}