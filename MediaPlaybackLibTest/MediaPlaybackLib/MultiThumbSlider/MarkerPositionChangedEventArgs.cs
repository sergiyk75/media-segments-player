using System.Windows;

namespace MediaPlaybackLib.MultiThumbSlider
{
    public class MarkerPositionChangedEventArgs : RoutedEventArgs
    {
        public string Key { get; private set; }
        public double Move { get; private set; }

        public MarkerPositionChangedEventArgs(string key, double move)
        {
            Key = key;
            Move = move;
        }
    }
}