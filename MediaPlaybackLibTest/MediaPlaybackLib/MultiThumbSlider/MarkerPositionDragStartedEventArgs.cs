using System.Windows;

namespace MediaPlaybackLib.MultiThumbSlider
{
    public class MarkerPositionDragStartedEventArgs : RoutedEventArgs
    {
        public string Key { get; private set; }

        public MarkerPositionDragStartedEventArgs(string key)
        {
            Key = key;
        }
    }
}