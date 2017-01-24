using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;

namespace MediaPlaybackLib.MultiThumbSlider
{
    /// <summary>
    /// Custom Control that can handle multiple MarkerThumb sliders
    /// </summary>
    /// <remarks>Specifically designed for managing the editing of MediaMarkers. Although it could be used elsewhere, there may be some specifics or details 
    /// thare are undesired.</remarks>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1724:TypeNamesShouldNotMatchNamespaces")]
    [Localizability(LocalizationCategory.NeverLocalize)]
    public class MultiThumbSlider : Canvas
    {
        #region Fields
        /// <summary>
        /// all of the MarkerThumb's contained in a dictionary keyed on the markers guid. The double is the current marker position.
        /// </summary>
        private readonly Dictionary<Guid, MarkerThumb> thumbsliders = new Dictionary<Guid, MarkerThumb>();
        private readonly Dictionary<Guid, double> thumbSlidersStartTime = new Dictionary<Guid, double>();

        /// <summary>
        /// zordering of the thumbs (always from 0 to total thumbs)
        /// </summary>
        private readonly Dictionary<Guid, int> zordering = new Dictionary<Guid, int>();

        private readonly Dictionary<Guid, MarkerRange> ranges = new Dictionary<Guid, MarkerRange>();
        
        #endregion

        #region Public Properties
        /// <summary>
        /// Get all single marker thumbs
        /// </summary>
        public Dictionary<Guid, MarkerThumb> MarkerThumbs
        {
            get { return this.thumbsliders; }
        }

        // get all range markers
        public Dictionary<Guid, MarkerRange> MarkerRanges
        {
            get { return this.ranges; }
        }

        public List<Guid> Order
        {
            get { return (from entry in zordering orderby entry.Value ascending select entry.Key).ToList(); }
        }

        public Dictionary<Guid, int> ZOrder
        {
            get { return zordering; }
        }

        #endregion

        #region Dependency Properties

        #region RoutedEvents
        /// <summary>
        /// Marker Position Changed Event
        /// </summary>
        public event EventHandler<MarkerPositionChangedEventArgs> MarkerPositionChanged
        {
            add { AddHandler(MarkerPositionChangedEvent, value); }
            remove { RemoveHandler(MarkerPositionChangedEvent, value); }
        }

        public static readonly RoutedEvent MarkerPositionChangedEvent = EventManager.RegisterRoutedEvent("MarkerPositionChanged",
            RoutingStrategy.Bubble, typeof(EventHandler<MarkerPositionChangedEventArgs>), typeof(MultiThumbSlider));

        /// <summary>
        /// Marker Position Drag Started Event
        /// </summary>
        public event EventHandler<MarkerPositionDragStartedEventArgs> MarkerPositionDragStarted
        {
            add { AddHandler(MarkerPositionDragStartedEvent, value); }
            remove { RemoveHandler(MarkerPositionDragStartedEvent, value); }
        }

        public static readonly RoutedEvent MarkerPositionDragStartedEvent = EventManager.RegisterRoutedEvent("MarkerPositionDragStarted",
            RoutingStrategy.Bubble, typeof(EventHandler<MarkerPositionDragStartedEventArgs>), typeof(MultiThumbSlider));

        /// <summary>
        /// Marker Position Drag Ended Event
        /// </summary>
        public event EventHandler<MarkerPositionDragEndedEventArgs> MarkerPositionDragEnded
        {
            add { AddHandler(MarkerPositionDragEndedEvent, value); }
            remove { RemoveHandler(MarkerPositionDragEndedEvent, value); }
        }

        public static readonly RoutedEvent MarkerPositionDragEndedEvent = EventManager.RegisterRoutedEvent("MarkerPositionDragEnded",
            RoutingStrategy.Bubble, typeof(EventHandler<MarkerPositionDragEndedEventArgs>), typeof(MultiThumbSlider));

        /// <summary>
        /// Event raised whenever the selected range is changed
        /// </summary>
        public static readonly RoutedEvent RangeSelectionChangedEvent =
            EventManager.RegisterRoutedEvent("RangeSelectionChanged",
            RoutingStrategy.Bubble, typeof(EventHandler<MarkerRangeSelectionChangedEventArgs>), typeof(MultiThumbSlider));

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
            RoutingStrategy.Bubble, typeof(EventHandler<MarkerRangeSelectionCompletedEventArgs>), typeof(MultiThumbSlider));

        /// <summary>
        /// Event raised whenever the selected range is change is finished
        /// </summary>
        public event EventHandler<MarkerRangeSelectionCompletedEventArgs> RangeSelectionCompleted
        {
            add { AddHandler(RangeSelectionCompletedEvent, value); }
            remove { RemoveHandler(RangeSelectionCompletedEvent, value); }
        }

        /// <summary>
        /// Event raised whenever the editor is cancelled
        /// </summary>
        public static readonly RoutedEvent EditorCancelledEvent =
            EventManager.RegisterRoutedEvent("EditorCancelled",
            RoutingStrategy.Bubble, typeof(EventHandler<RoutedEventArgs>), typeof(MultiThumbSlider));

        /// <summary>
        /// Event raised whenever the editor is cancelled
        /// </summary>
        public event EventHandler<RoutedEventArgs> EditorCancelled
        {
            add { AddHandler(EditorCancelledEvent, value); }
            remove { RemoveHandler(EditorCancelledEvent, value); }
        }
        
        #endregion

        #endregion

        #region Events

        /// <summary>
        /// Marker Modified Event
        /// </summary>
        public event EventHandler<MarkerModifiedEventArgs> MarkerModified;

        /// <summary>
        /// Send out the marker modify request
        /// </summary>
        /// <param name="requestEventArgs">event args</param>
        protected virtual void OnMarkerModied(MarkerModifiedEventArgs requestEventArgs)
        {
            if (MarkerModified != null)
                MarkerModified(this, requestEventArgs);
        }

        #endregion

        #region Public Functions

        /// <summary>
        /// Constructor
        /// </summary>
        public MultiThumbSlider()
        {
            // get the base container
            SizeChanged += VisualElementsContainerOnSizeChanged;
            ClipToBounds = false;
        }

        /// <summary>
        /// Override the OnApplyTempalte
        /// </summary>
        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();

            UpdateThumbs();
        }

        /// <summary>
        /// Bring the given MarkerThumb to the front if the uniqueId exists
        /// </summary>
        /// <remarks>Will automatically make the marker the selected</remarks>
        /// <param name="uniqueId"></param>
        public void BringToFront(Guid uniqueId)
        {
            // only arrange if it exists
            if (!thumbsliders.ContainsKey(uniqueId) && !ranges.ContainsKey(uniqueId)) return;
            
            // get the current order of the request
            UpdateZOrder(uniqueId);
            Select(uniqueId);
            UpdateThumbs();
        }

        /// <summary>
        /// Select the marker with the given Guid.
        /// </summary>
        /// <remarks>If marker with Guid doesn't exist, nothing happens</remarks>
        /// <param name="uniqueId">unique Guid of the marker to set selected</param>
        public void Select(Guid uniqueId)
        {
            if (thumbsliders.ContainsKey(uniqueId))
            {
                // make all markers not selected, then select the given
                foreach (var t in thumbsliders)
                    t.Value.IsSelected = false;
                foreach (var r in ranges)
                    r.Value.IsSelected = false;
                thumbsliders[uniqueId].IsSelected = true;
            }
            else if (ranges.ContainsKey(uniqueId))
            {
                // make all markers not selected, then select the given
                foreach (var t in thumbsliders)
                    t.Value.IsSelected = false;
                foreach (var r in ranges)
                    r.Value.IsSelected = false;
                ranges[uniqueId].IsSelected = true;
            }
        }

        /// <summary>
        /// Add a new Thumb
        /// </summary>
        /// <param name="uniqueId">The unique id of the marker. If already exists, it will be replaced</param>
        /// <param name="startPosition">starting position</param>
        /// <param name="displayName">display name</param>
        /// <param name="description">description</param>
        /// <param name="isFrozen">is frozen</param>
        /// <param name="alwaysFrozen">always frozen</param>
        /// <param name="isSystem">connected to a system time tag</param>
        /// <returns>the added thumb</returns>
        public MarkerThumb AddThumb(Guid uniqueId, double startPosition, string displayName, string description, bool isFrozen, bool alwaysFrozen, bool isSytem)
        {
            // create the new marker
            var t = new MarkerThumb(uniqueId)
            {
                DisplayName = displayName,
                Description = description,
                IsReadOnly = alwaysFrozen,
                IsFrozen = isFrozen,
                IsSystem = isSytem
            };

            // add marker events
            t.DragDelta += OnMarkerDragDelta;
            t.DragStarted += OnMarkerDragStarted;
            t.DragCompleted += OnMarkerDragCompleted;
            t.MarkerThumbSelect += OnMarkerThumbSelect;
            t.DeleteRequest += OnMarkerThumbDeleteRequest;
            t.Modified += OnMarkerThumbModifed;

            // add the thumb or replace the current
            if (thumbsliders.ContainsKey(uniqueId))
            {
                // remove old events
                thumbsliders[uniqueId].DragDelta -= OnMarkerDragDelta;
                thumbsliders[uniqueId].DragStarted -= OnMarkerDragStarted;
                thumbsliders[uniqueId].DragCompleted -= OnMarkerDragCompleted;
                thumbsliders[uniqueId].MarkerThumbSelect -= OnMarkerThumbSelect;
                thumbsliders[uniqueId].DeleteRequest -= OnMarkerThumbDeleteRequest;
                thumbsliders[uniqueId].Modified -= OnMarkerThumbModifed;

                thumbsliders[uniqueId] = t;
                thumbSlidersStartTime[uniqueId] = startPosition;
            }
            else
            {
                thumbsliders.Add(uniqueId, t);
                thumbSlidersStartTime.Add(uniqueId, startPosition);
                zordering.Add(uniqueId, zordering.Count + 1);
            }

            UpdateThumbs();

            return t;
        }

        /// <summary>
        /// Add a new range
        /// </summary>
        /// <param name="uniqueId">unique id</param>
        /// <param name="startPosition">start position (px)</param>
        /// <param name="endPosition">end position (px)</param>
        /// <param name="displayName">Display name</param>
        /// <param name="description">description</param>
        /// <param name="isFrozen">is it currently frozen (cannot move)</param>
        /// <param name="alwaysFrozen">is it always frozen</param>
        /// <returns>the new range</returns>
        public MarkerRange AddRange(Guid uniqueId, double startPosition, double endPosition, string displayName, string description, bool isFrozen, bool alwaysFrozen)
        {
            // create the range
            var range = new MarkerRange(uniqueId)
                            {
                                DisplayName = displayName,
                                Description = description,
                                IsReadOnly = alwaysFrozen,
                                IsFrozen = isFrozen,
                                Range = new Tuple<double, double>(startPosition, endPosition)
                            };

            // add to our collection as required, replace if necessary
            if (ranges.ContainsKey(uniqueId))
            {
                // remove old events
                ranges[uniqueId].RangeSelectionChanged -= RangeOnRangeSelectionChanged;
                ranges[uniqueId].Modified -= OnMarkerThumbModifed;
                ranges[uniqueId].RangeSelectionCompleted -= RangeOnRangeSelectionCompleted;
                ranges[uniqueId].DeleteRequest -= OnRangeDeleteRequest;
                ranges[uniqueId].EditorCancelled -= OnEditorCancelled;

                // set new
                ranges[uniqueId] = range;
            }
            else
            {
                ranges.Add(uniqueId, range);
                zordering.Add(uniqueId, zordering.Count + 1);
            }

            // tie up events
            range.RangeSelectionChanged += RangeOnRangeSelectionChanged;
            range.Modified += OnMarkerThumbModifed;
            range.RangeSelectionCompleted += RangeOnRangeSelectionCompleted;
            range.DeleteRequest += OnRangeDeleteRequest;
            range.EditorCancelled += OnEditorCancelled;

            UpdateThumbs();
            return range;
        }

        /// <summary>
        /// Remove a marker with given Id
        /// </summary>
        /// <param name="id">id of marker or range to remove</param>
        public void Remove(Guid id)
        {
            RemoveRange(id);
            RemoveThumb(id);
        }

        /// <summary>
        /// Remove a range with provided id
        /// </summary>
        /// <param name="rangeId">id of range to remove</param>
        public void RemoveRange(Guid rangeId)
        {
            // if the range doesn't exist just return
            if (!ranges.ContainsKey(rangeId)) return;

            // get the range and detatch events
            var r = ranges[rangeId];
            r.RangeSelectionChanged -= RangeOnRangeSelectionChanged;
            r.Modified -= OnMarkerThumbModifed;
            r.RangeSelectionCompleted -= RangeOnRangeSelectionCompleted;
            r.DeleteRequest -= OnRangeDeleteRequest;
            r.EditorCancelled -= OnEditorCancelled;

            ranges.Remove(rangeId);

            RemoveFromZOrder(rangeId);
            UpdateThumbs();
        }

        /// <summary>
        /// Remove the thumb
        /// </summary>
        /// <param name="uniqueId">the unique id to remove.</param>
        public void RemoveThumb(Guid uniqueId)
        {
            // if the thumb doesn't exist just return
            if (!thumbsliders.ContainsKey(uniqueId)) return;

            // get the thumb and detatch events
            var t = thumbsliders[uniqueId];
            t.DragDelta -= OnMarkerDragDelta;
            t.DragStarted -= OnMarkerDragStarted;
            t.DragCompleted -= OnMarkerDragCompleted;
            t.MarkerThumbSelect -= OnMarkerThumbSelect;
            t.DeleteRequest -= OnMarkerThumbDeleteRequest;
            t.Modified -= OnMarkerThumbModifed;

            thumbsliders.Remove(uniqueId);
            thumbSlidersStartTime.Remove(uniqueId);

            RemoveFromZOrder(uniqueId);
            UpdateThumbs();
        }

        /// <summary>
        /// Update a current marker/range values
        /// </summary>
        /// <param name="uniqueId">the id of the marker/range to update</param>
        /// <param name="startPixel">start time (px)</param>
        /// <param name="endPixel">endPixel (px)</param>
        /// <param name="description">description</param>
        public void Update(Guid uniqueId, double startPixel, double endPixel, string description)
        {
            // if the range doesn't exist just return
            if (ranges.ContainsKey(uniqueId))
            {
                // get the range 
                var r = ranges[uniqueId];
                r.Description = description;

                if (startPixel < 0) 
                    startPixel = 0;
                if (endPixel > r.MoveableWidth) 
                    endPixel = r.MoveableWidth;
                r.Range = new Tuple<double, double>(startPixel, endPixel);
            }

            // if the thumb doesn't exist just return
            if (thumbsliders.ContainsKey(uniqueId))
            {
                // get the thumb 
                var t = thumbsliders[uniqueId];
                t.Description = description;
                thumbSlidersStartTime[uniqueId] = startPixel;
                SetLeft(t, startPixel);
            }
        }

        /// <summary>
        /// Freeze the Thumb with the given id
        /// </summary>
        /// <param name="uniqueId">id of thumb</param>
        /// <param name="isFrozen">true if you want to freeze it, false if you don't</param>
        public void Freeze(Guid uniqueId, bool isFrozen)
        {
            if (thumbsliders.ContainsKey(uniqueId))
                thumbsliders[uniqueId].IsFrozen = isFrozen;

            if (ranges.ContainsKey(uniqueId))
                ranges[uniqueId].IsFrozen = isFrozen;
        }

        /// <summary>
        /// Clear all the thumbs.
        /// </summary>
        public void Clear()
        {
            thumbsliders.Clear();
            thumbSlidersStartTime.Clear();
            ranges.Clear();
            zordering.Clear();
            Children.Clear();

        }
        
        public void ShowEdit(Guid uniqueId)
        {
            if (thumbsliders.ContainsKey(uniqueId))
            {
                foreach (var t in thumbsliders)
                    t.Value.IsEditing = false;
                thumbsliders[uniqueId].IsEditing = true;
            }

            if (ranges.ContainsKey(uniqueId))
            {
                foreach (var r in ranges)
                    r.Value.IsEditing = false;
                ranges[uniqueId].IsEditing = true;
            }

        }
        #endregion

        #region Private

        /// <summary>
        /// Remove an item from the zording list
        /// </summary>
        /// <param name="uniqueId">id of the item to remove</param>
        private void RemoveFromZOrder(Guid uniqueId)
        {
            if (!zordering.ContainsKey(uniqueId)) return;

            // shift all zording
            var value = zordering[uniqueId];

            var keys = new List<Guid>(zordering.Keys);

            foreach (var k in keys.Where(k => zordering[k] > value))
            {
                zordering[k] -= 1;
            }

            // remove
            zordering.Remove(uniqueId);

        }

        /// <summary>
        /// Update the Z Order of the marker with the given id
        /// </summary>
        /// <param name="uniqueId">id of the marker/range to update</param>
        private void UpdateZOrder(Guid uniqueId)
        {
            if (!zordering.ContainsKey(uniqueId)) return;

            // get the current order of the request
            var itemCurrentZ = zordering[uniqueId];
            var max = zordering.Max(d => d.Value);
            Guid itemMaxId = zordering.Single(s => s.Value == max).Key;

            zordering[itemMaxId] = itemCurrentZ;
            zordering[uniqueId] = max;
        }
        
        /// <summary>
        /// Update thumbs (entire refresh/clear of screen)
        /// </summary>
        private void UpdateThumbs()
        {
            // don't bother if there is nothing visible
            if (this.ActualHeight <= 0) return;

            // clear all markers/thumbs
            Children.Clear();

            // add all thumbs
            foreach (var thumbslider in thumbsliders)
            {
                var thumb = thumbslider.Value;
                var location = thumbSlidersStartTime[thumbslider.Key];

                Children.Add(thumb);
                SetTop(thumb, ActualHeight - thumb.Height + 3);
                SetLeft(thumb, location);
                SetZIndex(thumb, zordering[thumbslider.Key]);
            }

            // add all ranges
            foreach (var markerRangeSlider in ranges)
            {
                markerRangeSlider.Value.MoveableWidth = ActualWidth;
                markerRangeSlider.Value.Height = ActualHeight;
                Children.Add(markerRangeSlider.Value);
                SetZIndex(markerRangeSlider.Value, zordering[markerRangeSlider.Key]);
            }
        }

        #endregion

        #region Event Handlers
        
        #region event raising

        /// <summary>
        /// Raise the marker delta changed event
        /// </summary>
        /// <param name="eventArgs">the event args to use</param>
        [ExcludeFromCodeCoverage]
        private void OnMarkerDeltaChanged(MarkerPositionChangedEventArgs eventArgs)
        {
            eventArgs.RoutedEvent = MarkerPositionChangedEvent;
            RaiseEvent(eventArgs);
        }

        /// <summary>
        /// Raise the marker drag start event
        /// </summary>
        /// <param name="eventArgs">the event args to use</param>
        [ExcludeFromCodeCoverage]
        private void OnMarkerDragStarted(MarkerPositionDragStartedEventArgs eventArgs)
        {
            eventArgs.RoutedEvent = MarkerPositionDragStartedEvent;
            RaiseEvent(eventArgs);
        }

        /// <summary>
        /// Raise the Marker Drag Ended
        /// </summary>
        /// <param name="eventArgs">the event args to use</param>
        [ExcludeFromCodeCoverage]
        private void OnMarkerDragEnded(MarkerPositionDragEndedEventArgs eventArgs)
        {
            eventArgs.RoutedEvent = MarkerPositionDragEndedEvent;
            RaiseEvent(eventArgs);
        }

        /// <summary>
        /// Raise the Range changed event
        /// </summary>
        /// <param name="eventArgs">the event args to use</param>
        [ExcludeFromCodeCoverage]
        private void OnRangeSelectionChanged(MarkerRangeSelectionChangedEventArgs eventArgs)
        {
            eventArgs.RoutedEvent = RangeSelectionChangedEvent;
            RaiseEvent(eventArgs);
        }

        [ExcludeFromCodeCoverage]
        private void OnRangeSelectionComplete(MarkerRangeSelectionCompletedEventArgs eventArgs)
        {
            eventArgs.RoutedEvent = RangeSelectionCompletedEvent;
            RaiseEvent(eventArgs);
        }
        #endregion
        
        /// <summary>
        /// Handler the range changed event. We just pass it on.
        /// </summary>
        /// <param name="sender">sender of the event</param>
        /// <param name="markerRangeSelectionChangedEventArgs">event arguments</param>
        [ExcludeFromCodeCoverage]
        private void RangeOnRangeSelectionChanged(object sender, MarkerRangeSelectionChangedEventArgs markerRangeSelectionChangedEventArgs)
        {
            var range = sender as MarkerRange;
            if (range != null)
            {
                OnRangeSelectionChanged(markerRangeSelectionChangedEventArgs);
            }
        }

        /// <summary>
        /// Handler the range selection completed
        /// </summary>
        /// <param name="sender">sender of the event</param>
        /// <param name="markerRangeSelectionCompletedEventArgs">event arguments</param>
        [ExcludeFromCodeCoverage]
        private void RangeOnRangeSelectionCompleted(object sender, MarkerRangeSelectionCompletedEventArgs markerRangeSelectionCompletedEventArgs)
        {
            var range = sender as MarkerRange;
            if (range != null)
            {
                OnRangeSelectionComplete(markerRangeSelectionCompletedEventArgs);
            }
        }

        /// <summary>
        /// Handle the marker thumb delete request
        /// </summary>
        /// <param name="sender">markerthumb</param>
        /// <param name="eventArgs">Empty</param>
        [ExcludeFromCodeCoverage]
        private void OnRangeDeleteRequest(object sender, EventArgs eventArgs)
        {
            var thumb = sender as MarkerRange;
            if (thumb != null && !thumb.IsReadOnly)
            {
                OnMarkerModied(new MarkerModifiedEventArgs(thumb.Id, true));
            }
        }

        /// <summary>
        /// Handle the marker thumb cancelled event
        /// </summary>
        /// <param name="sender">markerthumb</param>
        /// <param name="eventArgs">Empty</param>
        [ExcludeFromCodeCoverage]
        private void OnEditorCancelled(object sender, EventArgs eventArgs)
        {
            var routedArgs = new RoutedEventArgs(EditorCancelledEvent);
            RaiseEvent(routedArgs);
        }

        /// <summary>
        /// Handle the marker modified event
        /// </summary>
        /// <param name="sender">sender MarkerThumb</param>
        /// <param name="eventArgs">Empty</param>
        [ExcludeFromCodeCoverage]
        private void OnMarkerThumbModifed(object sender, EventArgs eventArgs)
        {
            var thumb = sender as MarkerThumb;
            if (thumb != null && !thumb.IsReadOnly)
            {
                OnMarkerModied(new MarkerModifiedEventArgs(thumb.Key, thumb.Description));
            }

            var range = sender as MarkerRange;
            if (range != null && !range.IsReadOnly)
            {
                OnMarkerModied(new MarkerModifiedEventArgs(range.Id, range.Description));
            }
        }

        /// <summary>
        /// Handle the marker thumb delete request
        /// </summary>
        /// <param name="sender">markerthumb</param>
        /// <param name="eventArgs">Empty</param>
        [ExcludeFromCodeCoverage]
        private void OnMarkerThumbDeleteRequest(object sender, EventArgs eventArgs)
        {
            var thumb = sender as MarkerThumb;
            if (thumb != null && !thumb.IsReadOnly)
            {
                OnMarkerModied(new MarkerModifiedEventArgs(thumb.Key, true));
            }
        }

        /// <summary>
        /// Handle the marker selected event
        /// </summary>
        /// <param name="sender">markerthumb</param>
        /// <param name="markerThumbSelectEventArgs">Empty</param>
        [ExcludeFromCodeCoverage]
        private void OnMarkerThumbSelect(object sender, EventArgs markerThumbSelectEventArgs)
        {
            var thumb = sender as MarkerThumb;
            if (thumb != null)
            {
                Select(thumb.Key);
            }
        }

        /// <summary>
        /// Handle size changed
        /// </summary>
        /// <param name="sender">sender</param>
        /// <param name="sizeChangedEventArgs">event args</param>
        [ExcludeFromCodeCoverage]
        private void VisualElementsContainerOnSizeChanged(object sender, SizeChangedEventArgs sizeChangedEventArgs)
        {
            UpdateThumbs();
        }

        /// <summary>
        /// Handle drag delta
        /// </summary>
        /// <param name="sender">MarkerThumb</param>
        /// <param name="eventArgs">event args</param>
        [ExcludeFromCodeCoverage]
        private void OnMarkerDragDelta(object sender, DragDeltaEventArgs eventArgs)
        {
            var thumb = sender as MarkerThumb;

            // return if this thumb can't move (frozen)
            if (thumb == null || thumb.IsReadOnly || thumb.IsFrozen)
                return;
            
            var key = thumbsliders.Single(s => s.Value == thumb).Key;

            //Move the Thumb to the mouse position during the drag operation
            var left = GetLeft(thumb);
            var move = left + eventArgs.HorizontalChange;

            // don't move if outside the canvas
            if (move < 0)
                move = 0;

            if (move > ActualWidth)
                move = ActualWidth;

            SetLeft(thumb, move);
            OnMarkerDeltaChanged(new MarkerPositionChangedEventArgs(key.ToString(), eventArgs.HorizontalChange));
        }

        /// <summary>
        /// Handler Drag Started
        /// </summary>
        /// <param name="sender">MarkerThumb</param>
        /// <param name="eventArgs">event args</param>
        [ExcludeFromCodeCoverage]
        private void OnMarkerDragStarted(object sender, DragStartedEventArgs eventArgs)
        {
            var thumb = sender as MarkerThumb;
            if (thumb == null || thumb.IsReadOnly || thumb.IsFrozen)
                return;

            // always bring the selected thumb to the top
            // NOTE: doesn't seem to work?
            var key = thumbsliders.Single(s => s.Value == thumb).Key;
            UpdateZOrder(key);
            SetZIndex(thumb, zordering[key]);
            OnMarkerDragStarted(new MarkerPositionDragStartedEventArgs(key.ToString()));
        }

        /// <summary>
        /// Handle Drag Completed
        /// </summary>
        /// <param name="sender">MarkerThumb</param>
        /// <param name="eventArgs">event args</param>
        [ExcludeFromCodeCoverage]
        private void OnMarkerDragCompleted(object sender, DragCompletedEventArgs eventArgs)
        {
            var thumb = sender as MarkerThumb;
            if (thumb == null || thumb.IsReadOnly || thumb.IsFrozen || eventArgs.HorizontalChange == 0)
                return;

            var key = thumbsliders.Single(s => s.Value == thumb).Key;
            OnMarkerDragEnded(new MarkerPositionDragEndedEventArgs(key.ToString(), Canvas.GetLeft(thumb)));
        }
        #endregion

    }
}
