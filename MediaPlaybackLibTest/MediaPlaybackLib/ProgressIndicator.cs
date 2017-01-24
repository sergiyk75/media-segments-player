using System;
using System.Linq;
using System.Diagnostics.CodeAnalysis;
using System.Windows;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using System.Windows.Media;
using System.Windows.Controls;
using System.Windows.Markup;
using System.Windows.Data;

namespace MediaPlaybackLib
{
    [ExcludeFromCodeCoverage]  // Gui control - not unit testable; automated tests cover this
    public class ProgressIndicator : FrameworkElement
    {
        public static readonly DependencyProperty StrokeProperty = DependencyProperty.Register("Stroke", typeof(Brush), typeof(ProgressIndicator), new FrameworkPropertyMetadata(new SolidColorBrush(Color.FromArgb(0xB0, 0x0, 0x0, 0x0))));
        public static readonly DependencyProperty CyclesPerMinuteProperty = DependencyProperty.Register("CyclesPerMinute", typeof(int), typeof(ProgressIndicator), new FrameworkPropertyMetadata(60));

        static ProgressIndicator()
        {
            IsEnabledProperty.OverrideMetadata(typeof(ProgressIndicator), new UIPropertyMetadata(false));
            IsHitTestVisibleProperty.OverrideMetadata(typeof(ProgressIndicator), new UIPropertyMetadata(false));
        }

        private Canvas canvas;
        private bool isActive;

        public ProgressIndicator()
        {
            this.canvas = new Canvas
            {
                Width = 16,
                Height = 16,
                RenderTransformOrigin = new Point(.5, .5),
                RenderTransform = new RotateTransform()
            };
            AddVisualChild(new Viewbox { Child = canvas });
        }

        public Brush Stroke
        {
            get { return (Brush)base.GetValue(StrokeProperty); }
            set { base.SetValue(StrokeProperty, value); }
        }

        public int CyclesPerMinute
        {
            get { return (int)GetValue(CyclesPerMinuteProperty); }
            set { SetValue(CyclesPerMinuteProperty, value); }
        }

        protected override int VisualChildrenCount
        {
            get { return 1; }
        }

        protected override Visual GetVisualChild(int index)
        {
            return (Visual)this.canvas.Parent;
        }

        protected override Size ArrangeOverride(Size arrangeBounds)
        {
            ((UIElement)this.GetVisualChild(0)).Arrange(new Rect(arrangeBounds));
            return arrangeBounds;
        }

        protected override Size MeasureOverride(Size constraint)
        {
            var visualChild = (UIElement)this.GetVisualChild(0);
            visualChild.Measure(constraint);
            return visualChild.DesiredSize;
        }

        protected override void OnPropertyChanged(DependencyPropertyChangedEventArgs e)
        {
            base.OnPropertyChanged(e);

            if (e.Property == CyclesPerMinuteProperty)
            {
                if (this.IsEnabled && this.IsVisible)
                {
                    StopAnimation();
                    BeginAnimation();
                }
            }
            else if (e.Property == IsEnabledProperty || e.Property == IsVisibleProperty)
            {
                if (this.IsEnabled && this.IsVisible)
                {
                    BeginAnimation();
                }
                else
                {
                    StopAnimation();
                }
            }
        }

        public const int LineCount = 15;

        private void BeginAnimation()
        {
            if (System.ComponentModel.DesignerProperties.GetIsInDesignMode(this)) // alternativly System.ComponentModel.LicenseManager.UsageMode == System.ComponentModel.LicenseUsageMode.Designtime can be used
                return;

            StopAnimation();

            int lapDurationInMsec = 60000 / this.CyclesPerMinute;
            var animation = new DoubleAnimationUsingKeyFrames
            {
                Duration = new Duration(TimeSpan.FromMilliseconds(lapDurationInMsec)),
                RepeatBehavior = RepeatBehavior.Forever
            };

            var lineThickness = this.canvas.Height * 1.7 / LineCount;

            for (var i = 0; i < LineCount; i++)
            {
                var line = new Line()
                {
                    X1 = canvas.Width / 2,
                    Y1 = lineThickness / 2,
                    X2 = canvas.Width / 2,
                    Y2 = (this.canvas.Height - lineThickness) / 4,
                    Width = canvas.Width,
                    Height = this.canvas.Height,
                    StrokeThickness = lineThickness,
                    Opacity = (double)i / LineCount,
                    StrokeStartLineCap = PenLineCap.Round,
                    StrokeEndLineCap = PenLineCap.Round,
                    VerticalAlignment = VerticalAlignment.Center,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    RenderTransformOrigin = new Point(.5, .5),
                    RenderTransform = new RotateTransform((i * 360) / LineCount),
                    SnapsToDevicePixels = true,
                };

                line.SetBinding(Line.StrokeProperty, new Binding { Source = this, Path = new PropertyPath(StrokeProperty) });

                this.canvas.Children.Add(line);

                var ts = TimeSpan.FromMilliseconds(lapDurationInMsec - (lapDurationInMsec * (LineCount - i)) / LineCount);
                var frame = new DiscreteDoubleKeyFrame((i * 360) / LineCount, KeyTime.FromTimeSpan(ts));

                animation.KeyFrames.Add(frame);
            }

            this.canvas.RenderTransform.BeginAnimation(RotateTransform.AngleProperty, animation);

            this.isActive = true;
        }

        private void StopAnimation()
        {
            if (!this.isActive)
                return;

            this.canvas.RenderTransform.BeginAnimation(RotateTransform.AngleProperty, null);

            this.canvas.Children.Clear();
        }
    }
}
