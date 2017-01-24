using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics.CodeAnalysis;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;

namespace MediaPlaybackLib
{
    [ExcludeFromCodeCoverage]  // Gui control - not unit testable; automated tests cover this
    [TemplatePart(Name = "PART_Track", Type = typeof(Track))]
    public class SimpleSlider : Slider
    {
        static SimpleSlider()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(SimpleSlider), new FrameworkPropertyMetadata(typeof(SimpleSlider)));
        }

        public SimpleSlider()
        {
            Focusable = false;
        }

        private Track track;

        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();
            this.track = GetTemplateChild("PART_Track") as Track;
        }

        protected override void OnPreviewMouseDown(System.Windows.Input.MouseButtonEventArgs e)
        {
            base.OnPreviewMouseDown(e);
            CaptureMouse();
        }

        protected override void OnMouseUp(System.Windows.Input.MouseButtonEventArgs e)
        {
            base.OnMouseUp(e);
            if (this.IsMouseCaptured)
                ReleaseMouseCapture();
            else
                UpdateValue(e);
        }

        protected override void OnMouseMove(System.Windows.Input.MouseEventArgs e)
        {
            base.OnMouseMove(e);
            if (!this.IsMouseCaptured)
                return;
            
            UpdateValue(e);
        }

        private void UpdateValue(MouseEventArgs e)
        {
            if (this.track == null)
                return;

            var ratio = Math.Min(Math.Max(e.GetPosition(this.track).X / this.track.ActualWidth, 0), this.track.ActualWidth);
            this.Value = this.Minimum + (this.Maximum - this.Minimum) * ratio;
        }
    }
}
