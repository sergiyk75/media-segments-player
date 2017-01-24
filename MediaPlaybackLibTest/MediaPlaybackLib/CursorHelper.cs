using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Security.Permissions;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Microsoft.Win32.SafeHandles;

namespace MediaPlaybackLib
{
    /// <summary>
    /// Help create a cursor from any UIElement
    /// </summary>
    public sealed class CursorHelper
    {
        /// <summary>
        /// Private native methods helper class
        /// </summary>
        private static class NativeMethods
        {
            public struct IconInfo
            {
                public bool FIcon;
                public int XHotspot;
                public int YHotspot;
                public IntPtr HbmMask;
                public IntPtr HbmColor;
            }

            [DllImport("user32.dll")]
            public static extern SafeIconHandle CreateIconIndirect(ref IconInfo icon);

            [DllImport("user32.dll")]
            public static extern bool DestroyIcon(IntPtr hIcon);

            [DllImport("user32.dll")]
            [return: MarshalAs(UnmanagedType.Bool)]
            public static extern bool GetIconInfo(IntPtr hIcon, ref IconInfo pIconInfo);
        }

        /// <summary>
        /// Safe Icon Handle to correctly dispose icon.
        /// </summary>
        [SecurityPermission(SecurityAction.LinkDemand, UnmanagedCode = true)]
        private class SafeIconHandle : SafeHandleZeroOrMinusOneIsInvalid
        {
            public SafeIconHandle()
                : base(true)
            {
            }

            override protected bool ReleaseHandle()
            {
                return NativeMethods.DestroyIcon(handle);
            }
        }

        private CursorHelper()
        {
        }

        /// <summary>
        /// Create cursor from Bitmap
        /// </summary>
        /// <param name="bmp">Bitmap to create cursor from</param>
        /// <param name="xHotSpot">mouse hot spot x</param>
        /// <param name="yHotSpot">moust hot spot y</param>
        /// <returns></returns>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Security", "CA2122:DoNotIndirectlyExposeMethodsWithLinkDemands")]
        private static Cursor InternalCreateCursor(System.Drawing.Bitmap bmp, int xHotSpot, int yHotSpot)
        {
            var iconInfo = new NativeMethods.IconInfo();
            NativeMethods.GetIconInfo(bmp.GetHicon(), ref iconInfo);

            iconInfo.XHotspot = xHotSpot;
            iconInfo.YHotspot = yHotSpot;
            iconInfo.FIcon = false;

            SafeIconHandle cursorHandle = NativeMethods.CreateIconIndirect(ref iconInfo);
            return CursorInteropHelper.Create(cursorHandle);
        }

        /// <summary>
        /// Create Cursor from any UIElement
        /// </summary>
        /// <param name="element">UIElement to create cursor from</param>
        /// <param name="hotspotx">mouse hot spot x</param>
        /// <param name="hotspoty">mouse hot spot y</param>
        /// <returns></returns>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Security", "CA2122:DoNotIndirectlyExposeMethodsWithLinkDemands")]
        public static Cursor CreateCursor(UIElement element, int hotspotx, int hotspoty)
        {
            element.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
            element.Arrange(new Rect(new Point(), element.DesiredSize));

            RenderTargetBitmap rtb =
              new RenderTargetBitmap(
                (int)element.DesiredSize.Width,
                (int)element.DesiredSize.Height,
                96, 96, PixelFormats.Pbgra32);

            rtb.Render(element);

            var encoder = new PngBitmapEncoder();
            encoder.Frames.Add(BitmapFrame.Create(rtb));

            using (var ms = new MemoryStream())
            {
                encoder.Save(ms);
                using (var bmp = new System.Drawing.Bitmap(ms))
                {
                    return InternalCreateCursor(bmp, hotspotx, hotspoty);
                }
            }
        }

    }
}
