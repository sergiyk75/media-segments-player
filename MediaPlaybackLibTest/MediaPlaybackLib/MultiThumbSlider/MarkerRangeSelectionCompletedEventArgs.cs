using System;
using System.Windows;

namespace MediaPlaybackLib.MultiThumbSlider
{
    public class MarkerRangeSelectionCompletedEventArgs : RoutedEventArgs
    {
        public Guid Key { get; private set; }
        public double RangeStart { get; private set; }
        public double RangeEnd { get; private set; }

        public MarkerRangeSelectionCompletedEventArgs(Guid key, double rangeStart, double rangeEnd)
        {
            this.Key = key;
            this.RangeStart = rangeStart;
            this.RangeEnd = rangeEnd;
        }
    }
}