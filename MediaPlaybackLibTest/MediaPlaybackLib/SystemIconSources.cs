using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Media;
using System.Windows;
using System.Windows.Media.Imaging;
using System.Drawing;
using System.Windows.Interop;

namespace MediaPlaybackLib
{
    /// <summary>
    /// Each property of the SystemIconSources class is an ImageSource object for Windows system-wide icons.
    /// </summary>
    [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage] // Nothing to test
    public static class SystemIconSources
    {
        public static ImageSource ToImageSource(this Icon icon)
        {
            return Imaging.CreateBitmapSourceFromHIcon(icon.Handle, Int32Rect.Empty, BitmapSizeOptions.FromEmptyOptions());
        }

        private static Lazy<ImageSource> application = new Lazy<ImageSource>(SystemIcons.Application.ToImageSource);
        public static ImageSource Application
        {
            get { return application.Value; }
        }

        private static Lazy<ImageSource> asterisk = new Lazy<ImageSource>(SystemIcons.Asterisk.ToImageSource);
        public static ImageSource Asterisk
        {
            get { return asterisk.Value; }
        }

        private static Lazy<ImageSource> error = new Lazy<ImageSource>(SystemIcons.Error.ToImageSource);
        public static ImageSource Error
        {
            get { return error.Value; }
        }

        private static Lazy<ImageSource> exclamation = new Lazy<ImageSource>(SystemIcons.Exclamation.ToImageSource);
        public static ImageSource Exclamation
        {
            get { return exclamation.Value; }
        }

        private static Lazy<ImageSource> hand = new Lazy<ImageSource>(SystemIcons.Hand.ToImageSource);
        public static ImageSource Hand
        {
            get { return hand.Value; }
        }

        private static Lazy<ImageSource> information = new Lazy<ImageSource>(SystemIcons.Information.ToImageSource);
        public static ImageSource Information
        {
            get { return information.Value; }
        }

        private static Lazy<ImageSource> question = new Lazy<ImageSource>(SystemIcons.Question.ToImageSource);
        public static ImageSource Question
        {
            get { return question.Value; }
        }

        private static Lazy<ImageSource> shield = new Lazy<ImageSource>(SystemIcons.Shield.ToImageSource);
        public static ImageSource Shield
        {
            get { return shield.Value; }
        }

        private static Lazy<ImageSource> warning = new Lazy<ImageSource>(SystemIcons.Warning.ToImageSource);
        public static ImageSource Warning
        {
            get { return warning.Value; }
        }

        private static Lazy<ImageSource> winLogo = new Lazy<ImageSource>(SystemIcons.WinLogo.ToImageSource);
        public static ImageSource WinLogo
        {
            get { return winLogo.Value; }
        }
    }
}
