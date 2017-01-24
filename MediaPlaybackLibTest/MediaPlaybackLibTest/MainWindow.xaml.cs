using System;
using System.Linq;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Windows;
using MediaPlaybackLib;
using MediaPlaybackLib.MultiThumbSlider;

namespace MediaPlaybackLibTest
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();

            var model = new ViewModel
            {

                MediaSource = new MediaSource(new List<MediaSegment>
                {
                    new MediaSegment (TimeSpan.FromSeconds(0), TimeSpan.FromSeconds(5.5), new Uri(@"../../files/fellow.wav", UriKind.Relative)),
                    //new MediaSegment (TimeSpan.FromSeconds(12), TimeSpan.FromSeconds(4), new Uri(@"../../files/DustyF1-vibesSustain2.wav", UriKind.Relative)),
                    //new MediaSegment (TimeSpan.FromSeconds(20), TimeSpan.FromSeconds(5.5), new Uri(@"../../files/befriend.wav", UriKind.Relative)),
                    //new MediaSegment(TimeSpan.FromSeconds(29), TimeSpan.FromSeconds(15), new Uri(@"../../files/h264_sintel_trailer-1080p.mp4", UriKind.Relative)),
                })
                { HasVideo = false },
                MediaMarkers = new ObservableCollection<MediaMarker>
                {
                    new MediaMarker(Guid.NewGuid(), "system-marker", true,  TimeSpan.FromSeconds(3), null, "System tag 1", null, true),
                    new MediaMarker(Guid.NewGuid(), "system-marker", true,  TimeSpan.FromSeconds(13), null, "System tag 2", null, true),
                }
            };

            DataContext = model;
        }
    }

}
