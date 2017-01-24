using System;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Media;

namespace MediaPlaybackLib.MultiThumbSlider.Adorners
{
    /// <summary>
    /// Custom Adorner hosting a ContentControl with a ContentTemplate
    /// </summary>
    public class ContentTemplateAdorner : Adorner
    {
        /// <summary>
        /// the frameworkelement adorner
        /// </summary>
        private FrameworkElement adornerElement;
        private readonly FrameworkElement adornedElement;
        private Size adornerSize;
        internal Rect? arrangedRect;

        private AdornerVerticalAlignment verticalAdornerAlignment;
        private AdornerHorizontalAlignment horizontalAdornerAlignment;
        private AdornerPlacement adornerPlacement;
        private Thickness adornerMargin;

        public UIElement AdornerElement
        {
            get { return adornerElement; }
            set { adornerElement = value as FrameworkElement; this.InvalidateVisual(); }
        }

        public AdornerVerticalAlignment VerticalAdornerAlignment
        {
            get { return verticalAdornerAlignment; }
            set { verticalAdornerAlignment = value; this.InvalidateVisual(); }
        }

        public AdornerHorizontalAlignment HorizontalAdornerAlignment
        {
            get { return horizontalAdornerAlignment; }
            set { horizontalAdornerAlignment = value; this.InvalidateVisual(); }
        }

        public AdornerPlacement AdornerPlacement
        {
            get { return adornerPlacement; }
            set { adornerPlacement = value; this.InvalidateVisual(); }
        }

        public Thickness AdornerMargin
        {
            get { return adornerMargin; }
            set { adornerMargin = value; this.InvalidateVisual(); }
        }

        /// <summary>
        /// constructor
        /// </summary>
        /// <param name="uiAdornedElement"></param>
        /// <param name="uiAdornerElement">framework element </param>
        /// <param name="verticalAlignment"> </param>
        /// <param name="horizontalAlignment"> </param>
        /// <param name="placement"> </param>
        /// <param name="ctAdornerMargin"> </param>
        public ContentTemplateAdorner(UIElement uiAdornedElement,
                                        UIElement uiAdornerElement,
                                        AdornerVerticalAlignment verticalAlignment,
                                        AdornerHorizontalAlignment horizontalAlignment,
                                        AdornerPlacement placement,
                                        Thickness ctAdornerMargin
                                        )
            : base(uiAdornedElement)
        {
            if (uiAdornedElement == null) throw new ArgumentNullException("uiAdornedElement");
            if (uiAdornerElement == null) throw new ArgumentNullException("uiAdornerElement");
            if (!(uiAdornedElement is FrameworkElement)) throw new ArgumentException("uiAdornedElement must be a FrameworkElement");
            if (!(uiAdornerElement is FrameworkElement)) throw new ArgumentException("uiAdornerElement must be a FrameworkElement");

            adornerElement = uiAdornerElement as FrameworkElement;
            adornedElement = uiAdornedElement as FrameworkElement;
            
            adornerElement.DataContext = adornedElement.DataContext;
            verticalAdornerAlignment = verticalAlignment;
            horizontalAdornerAlignment = horizontalAlignment;
            adornerPlacement = placement;
            adornerMargin = ctAdornerMargin;
            
            adornerElement.Measure(new Size(Double.PositiveInfinity, Double.PositiveInfinity));
            adornerSize = uiAdornerElement.DesiredSize;
            adornerElement.Height = adornerSize.Height;
            adornerElement.Width = adornerSize.Width;

            AddVisualChild(uiAdornerElement);
            AddLogicalChild(uiAdornerElement);
        }
        
        /// <summary>
        /// number of visual children
        /// </summary>
        protected override int VisualChildrenCount
        {
            get { return adornerElement == null ? 0 : 1; }
        }

        /// <summary>
        /// get the visual child. Always returned the adorner element
        /// </summary>
        protected override Visual GetVisualChild(int index)
        {
            if (index == 0 && adornerElement != null)
            {
                return adornerElement;
            }
            return base.GetVisualChild(index);
        }

        /// <summary>
        /// Measure Override
        /// </summary>
        /// <param name="constraint"></param>
        /// <returns></returns>
        protected override Size MeasureOverride(Size constraint)
        {
            if (double.IsInfinity(constraint.Width) || double.IsInfinity(constraint.Height))
                throw new ArgumentOutOfRangeException("constraint");

            adornerElement.Width = constraint.Width;
            adornerElement.Height = constraint.Height;
            adornerSize = constraint;

            return constraint;
        }

        /// <summary>
        /// Arrange Override
        /// </summary>
        /// <param name="finalSize"></param>
        /// <returns></returns>
        protected override Size ArrangeOverride(Size finalSize)
        {
            this.arrangedRect = null;
            if (adornerElement != null)
            {
                // Set vertical alignment
                var pos = new Point();
                switch (verticalAdornerAlignment)
                {
                    case AdornerVerticalAlignment.Top:
                        if (adornerPlacement == AdornerPlacement.VerticalOuterHorizontalInner || adornerPlacement == AdornerPlacement.VerticalOuterHorizontalOuter)
                            pos.Y -= adornerSize.Height;
                        else
                            pos.Y = 0;
                        break;

                    case AdornerVerticalAlignment.Center:
                        pos.Y = (adornedElement.ActualHeight - adornerSize.Height) / 2;
                        break;

                    case AdornerVerticalAlignment.Bottom:
                        if (adornerPlacement == AdornerPlacement.VerticalOuterHorizontalInner || adornerPlacement == AdornerPlacement.VerticalOuterHorizontalOuter)
                            pos.Y = adornedElement.ActualHeight;
                        else
                            pos.Y = adornedElement.ActualHeight - adornerSize.Height;
                        break;
                }

                //Swap the horizontal adorner alignment when the flow direction is right to left
                var adjustedHorizontalAdornerAlignment = this.horizontalAdornerAlignment;
                if (this.FlowDirection == FlowDirection.RightToLeft)
                {
                    switch (this.horizontalAdornerAlignment)
                    {
                        case AdornerHorizontalAlignment.Left:
                            adjustedHorizontalAdornerAlignment = AdornerHorizontalAlignment.Right;
                            break;
                        case AdornerHorizontalAlignment.Right:
                            adjustedHorizontalAdornerAlignment = AdornerHorizontalAlignment.Left;
                            break;
                    }
                }

                // Set horizontal alignment
                switch (adjustedHorizontalAdornerAlignment)
                {
                    case AdornerHorizontalAlignment.Left:
                        if (adornerPlacement == AdornerPlacement.VerticalInnerHorizontalOuter || adornerPlacement == AdornerPlacement.VerticalOuterHorizontalOuter)
                            pos.X -= adornerSize.Width;
                        else
                            pos.X = 0;
                        break;

                    case AdornerHorizontalAlignment.Center:
                        pos.X = (adornedElement.ActualWidth - adornerSize.Width) / 2;
                        break;

                    case AdornerHorizontalAlignment.Right:
                        if (adornerPlacement == AdornerPlacement.VerticalInnerHorizontalOuter || adornerPlacement == AdornerPlacement.VerticalOuterHorizontalOuter)
                            pos.X = adornedElement.ActualWidth;
                        else
                            pos.X = adornedElement.ActualWidth - adornerSize.Width;
                        break;
                }

                // Set Margin
                pos.Y += adornerMargin.Top;
                pos.Y -= adornerMargin.Bottom;
                pos.X += adornerMargin.Left;
                pos.X -= adornerMargin.Right;

                // Arrange element
                arrangedRect = new Rect(pos, adornerSize);
                adornerElement.Arrange(arrangedRect.Value);
            }
            return finalSize;
        }
    }
}