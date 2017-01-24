using System;
using System.Windows;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Shapes;
using MediaPlaybackLib.MultiThumbSlider.Adorners;
using Microsoft.Practices.Prism.Commands;

namespace MediaPlaybackLib.MultiThumbSlider
{
    /// <summary>
    /// Media Marker Thub. Allows gripping and sliding horizontally. Also has an edit mode.
    /// </summary>
    [TemplatePart(Name = "PART_Grip", Type = typeof(Path))]
    [TemplatePart(Name = "PART_AdornerBehavior", Type = typeof(ContentTemplateAdornerBehavior))]
    [Localizability(LocalizationCategory.NeverLocalize)]
    public class MarkerThumb : Thumb
    {
        // templated items
        private ContentTemplateAdornerBehavior editingAdorner;
        private Path grip;
        private string previousDescription = string.Empty;

        /// <summary>
        /// have we actually started to drag the thumb
        /// </summary>
        /// <remarks>we cannot use base.IsDragging as it's set to true on MouseDown and doesn't necessarly indicate that we have started dragging. (delta change)</remarks>
        private bool isDragging;

        /// <summary>
        /// was editing previous open prior to dragging started
        /// </summary>
        private bool wasEditingOpen;

        #region DependencyProperties

        public static readonly DependencyProperty IsReadOnlyProperty =
            DependencyProperty.Register("IsReadOnly", typeof(bool), typeof(MarkerThumb),
                                        new PropertyMetadata(default(bool)));

        public static readonly DependencyProperty IsFrozenProperty =
            DependencyProperty.Register("IsFrozen", typeof(bool), typeof(MarkerThumb),
                                        new PropertyMetadata(default(bool)));

        public static readonly DependencyProperty IsSelectedProperty =
            DependencyProperty.Register("IsSelected", typeof(bool), typeof(MarkerThumb),
                                        new PropertyMetadata(default(bool)));

        public static readonly DependencyProperty DisplayNameProperty =
            DependencyProperty.Register("DisplayName", typeof(string), typeof(MarkerThumb),
                                        new PropertyMetadata(default(string)));

        public static readonly DependencyProperty DescriptionProperty =
            DependencyProperty.Register("Description", typeof(string), typeof(MarkerThumb),
                                        new PropertyMetadata(default(string)));

        public static readonly DependencyProperty IsEditingProperty =
            DependencyProperty.Register("IsEditing", typeof(bool), typeof(MarkerThumb), new PropertyMetadata(false));

        public static readonly DependencyProperty IsSystemProperty =
            DependencyProperty.Register("IsSystem", typeof(bool), typeof(MarkerThumb), new PropertyMetadata(false));

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
        /// Get or Set if the thumb is selected
        /// </summary>
        /// <remarks>if true, the marker will have a glow. </remarks>
        public bool IsSelected
        {
            get { return (bool)GetValue(IsSelectedProperty); }
            set { SetValue(IsSelectedProperty, value); }
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
        /// Deterimes if this came from a system time tag
        /// </summary>
        /// <returns>If true then the brush will change</returns>
        public bool IsSystem
        {
            get { return (bool)GetValue(IsSystemProperty); }
            set { SetValue(IsSystemProperty, value); }
        }

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
        /// Editing canceled event (lost focus or manual close)
        /// </summary>
        public event EventHandler EditorCanceled;
        #endregion

        /// <summary>
        /// Unique Guid value of this marker.
        /// </summary>
        public Guid Key { get; private set; }

        #region Constructors

        /// <summary>
        /// Create a new MarkerThumb with the provided unique guid
        /// </summary>
        /// <param name="key">guid id/key of the markerthumb</param>
        public MarkerThumb(Guid key)
        {
            Key = key;

            // since we derrive from Thumb we need to make sure we explicitly set the DefaultStyleKey to MarkerThumb, otherwise it defaults to Thumb
            DefaultStyleKey = GetType();

            DragCompleted += OnDragCompleted;
            DragDelta += OnDragDelta;

            IsTabStop = false;

            this.Loaded += OnLoaded;

        }

        /// <summary>
        /// Create a MarkerThumb with a default key value
        /// </summary>
        public MarkerThumb()
            : this(Guid.NewGuid())
        {
        }

        #endregion

        #region Overrides

        /// <summary>
        /// Override OnMouseDown
        /// </summary>
        /// <param name="eventArgs">Event arguments</param>
        protected override void OnMouseDown(MouseButtonEventArgs eventArgs)
        {
            base.OnMouseDown(eventArgs);
            // don't show the editing sticky when clicked
            if (IsReadOnly || isDragging)
                return;

            IsSelected = true;
            OnMarkerThumbSelect();
        }

        /// <summary>
        /// Override of OnMouseDown. 
        /// </summary>
        /// <remarks>If the marker is editable the edit control will be shown.</remarks>
        /// <param name="eventArgs">event arguments</param>
        protected override void OnMouseUp(MouseButtonEventArgs eventArgs)
        {
            base.OnMouseUp(eventArgs);

            // don't show the editing sticky when clicked
            if (IsReadOnly || isDragging)
                return;

            // if already focused then just toggle visibility (clicking on marker multiple times in a row)
            if (grip != null && grip.IsFocused && editingAdorner != null)
            {
                if (editingAdorner.IsVisible)
                    OnCloseEditor();
                else
                    editingAdorner.ToggleVisibility();
            }
            // try to make this the logical focus
            else if (grip != null)
            {
                FocusManager.SetFocusedElement(FocusManager.GetFocusScope(this), grip);
            }
        }

        /// <summary>
        /// Override OnApplyTemplate
        /// </summary>
        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();

            // get our templated components
            grip = GetTemplateChild("PART_Grip") as Path;
            editingAdorner = GetTemplateChild("PART_AdornerBehavior") as ContentTemplateAdornerBehavior;

            if (grip == null || editingAdorner == null)
                throw new NullReferenceException("PART_Grip and PART_AdornerBehavior must exit in template.");

            // setup focus manager events on the grip
            grip.GotFocus += OnGripOnGotFocus;
            grip.LostFocus += OnGripOnLostFocus;
            
        }
        
        #endregion

        #region Event Handlers

        private void OnLoaded(object sender, RoutedEventArgs routedEventArgs)
        {
            // when we want to automatically show the editing adorner when created, we have to make sure we set focus once we are loaded, otherwise the 
            // correct bindings will not exist for the editing adorner
            if (!IsEditing || !IsSelected) return;
            var scope = FocusManager.GetFocusScope(this);
            FocusManager.SetFocusedElement(scope, grip);
        }

        /// <summary>
        /// Handle the OnDragCompleted Event
        /// </summary>
        /// <param name="sender">sender of the event; unused</param>
        /// <param name="dragCompletedEventArgs">event args</param>
        private void OnDragCompleted(object sender, DragCompletedEventArgs dragCompletedEventArgs)
        {
            isDragging = false;

            if (wasEditingOpen)
            {
                editingAdorner.Show();
                IsEditing = true;
            }

            wasEditingOpen = false;
        }

        /// <summary>
        /// Handle the OnDragDelta Event
        /// </summary>
        /// <param name="sender">sender of the event; unused</param>
        /// <param name="dragDeltaEventArgs">event args</param>
        private void OnDragDelta(object sender, DragDeltaEventArgs dragDeltaEventArgs)
        {
            // we have to use our own isDragging as OnDragStarted occurs on mouse down 

            if (!isDragging)
            {
                wasEditingOpen = editingAdorner.IsVisible;
                editingAdorner.Hide();
                IsEditing = false;
            }

            isDragging = true;
        }

        /// <summary>
        /// On thumb grip lost logical focus
        /// </summary>
        /// <param name="sender">sender of the event</param>
        /// <param name="args">arguments</param>
        private void OnGripOnLostFocus(object sender, RoutedEventArgs args)
        {
            // set not editing and hide editing control
            OnCloseEditor();
        }

        /// <summary>
        /// On thumb grip got logical focus
        /// </summary>
        /// <param name="sender">sender of the event</param>
        /// <param name="args">arguments</param>
        private void OnGripOnGotFocus(object sender, RoutedEventArgs args)
        {
            // set editing and show the editing control
            IsEditing = true;
            previousDescription = Description;
            editingAdorner.Show();
        }

        #endregion

        /// <summary>
        /// Send out the modify request event
        /// </summary>
        /// <param name="eventArgs">Empty</param>
        protected virtual void OnModifyRequest(EventArgs eventArgs)
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
        protected virtual void OnDeleteRequest(EventArgs eventArgs)
        {
            EventHandler<EventArgs> handler = this.DeleteRequest;
            if (handler != null) handler(this, eventArgs);
            
            OnCloseEditor();
        }

        /// <summary>
        /// Send out the marker selected event
        /// </summary>
        protected virtual void OnMarkerThumbSelect()
        {
            EventHandler handler = MarkerThumbSelect;
            if (handler != null) handler(this, EventArgs.Empty);
        }

        protected virtual void OnCloseEditor()
        {
            IsEditing = false;
            editingAdorner.Hide();
            Description = previousDescription;
            EventHandler handler = EditorCanceled;
            if (handler != null) handler(this, EventArgs.Empty);
        }
    }
}