using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using MediaPlaybackLib;
using MediaPlaybackLib.MultiThumbSlider;
using Microsoft.Practices.Prism.Commands;

namespace MediaPlaybackLibTest
{
    public class ViewModel : NotifyObject
    {
        public ViewModel()
        {
            MarkerModifiedCommand = new DelegateCommand<MarkerModifiedEventArgs>(OnMarkerModified);
            MarkerAddRequestCommand = new DelegateCommand<MarkerAddRequestEventArgs>(OnMarkerAddRequest);
            ThemeIdChanged();
        }

        public int ThemeId
        {
            get { return Get(() => ThemeId, 1); }
            set { Set(() => ThemeId, value, ThemeIdChanged); }
        }

        private void ThemeIdChanged()
        {
            var applicationResources = Application.Current.Resources.MergedDictionaries;

            applicationResources.Clear();

            switch (ThemeId)
            {
                case 0:
                    applicationResources.Add(new ResourceDictionary() { Source = new Uri("/MediaPlaybackLib;component/Themes/Arctic/MediaControlArctic.xaml", UriKind.Relative) });
                    break;
                case 1:
                    applicationResources.Add(new ResourceDictionary() { Source = new Uri("/MediaPlaybackLib;component/Themes/Midnight/MediaControlMidnight.xaml", UriKind.Relative) });
                    break;

            }
        }

        public MediaSource MediaSource
        {
            get { return Get(() => MediaSource); }
            set { Set(() => MediaSource, value); }
        }


        public ObservableCollection<MediaMarker> MediaMarkers
        {
            get { return Get(() => MediaMarkers); }
            set { Set(() => MediaMarkers, value); }
        }


        public ICommand MarkerModifiedCommand { get; private set; }

        public ICommand MarkerAddRequestCommand { get; private set; }


        public void OnActivity()
        {
            Trace.WriteLine("ON ACTIVITY!");
        }

        public void OnMarkerModified(MarkerModifiedEventArgs args)
        {
            var marker = MediaMarkers.First(m => m.Id == args.Key);
            if (args.Delete)
                MediaMarkers.Remove(marker);
            else
            {
                if (args.Description != null)
                    marker.Description = args.Description;

                if (args.StartTime.HasValue)
                    marker.StartTime = args.StartTime.Value;

                if (args.Length.HasValue)
                    marker.Length = args.Length.Value;

            }
        }

        public void OnMarkerAddRequest(MarkerAddRequestEventArgs args)
        {
            MediaMarkers.Add(new MediaMarker(Guid.NewGuid(), "user-marker", false) { StartTime = args.StartTime, Length = args.Duration });
        }
    }
}
