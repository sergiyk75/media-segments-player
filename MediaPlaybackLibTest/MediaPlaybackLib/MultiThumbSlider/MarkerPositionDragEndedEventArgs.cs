using System.Windows;

namespace MediaPlaybackLib.MultiThumbSlider
{
    public class MarkerPositionDragEndedEventArgs : RoutedEventArgs
    {
        public string Key { get; private set; }
        public double Position { get; private set; }

        public MarkerPositionDragEndedEventArgs(string key, double position)
        {
            Key = key;
            Position = position;
        }
    }
}