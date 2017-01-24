using System;
using System.Windows;

namespace MediaPlaybackLib.MultiThumbSlider
{
    /// <summary>
    /// Event arguments for the Range slider RangeSelectionChanged event
    /// </summary>
    public class MarkerRangeSelectionChangedEventArgs : RoutedEventArgs
    {
        /// <summary>
        /// The new range start selected in the range slider
        /// </summary>
        public double NewRangeStart { get; set; }

        /// <summary>
        /// The new range stop selected in the range slider
        /// </summary>
        public double NewRangeStop { get; set; }

        public Guid RangeId { get; set; }

        /// <summary>
        /// sets the range start and range stop for the event args
        /// </summary>
        /// <param name="newRangeStart">The new range start set</param>
        /// <param name="newRangeStop">The new range stop set</param>
        /// <param name="rangeId">unique id of the range changing </param>
        internal MarkerRangeSelectionChangedEventArgs(double newRangeStart, double newRangeStop, Guid rangeId)
        {
            NewRangeStart = newRangeStart;
            NewRangeStop = newRangeStop;
            RangeId = rangeId;
        }

        /// <summary>
        /// sets the range start and range stop for the event args by using the slider RangeStartSelected and RangeStopSelected properties
        /// </summary>
        /// <param name="slider">The slider to get the info from</param>
        internal MarkerRangeSelectionChangedEventArgs(MarkerRange slider)
            : this(slider.Range.Item1, slider.Range.Item2, slider.Id)
        { }

    }
}