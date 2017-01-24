using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Interactivity;
using Microsoft.Practices.Prism.Commands;

namespace MediaPlaybackLib.MultiThumbSlider.Adorners
{

    /// <summary>
    /// Behavior managing an adorner datatemplate
    /// </summary>
    public class ContentTemplateAdornerBehavior : Behavior<DependencyObject>
    {
        /// <summary>
        /// Custom Adorner class
        /// </summary>
        ContentTemplateAdorner templatedAdorner;

        /// <summary>
        /// Adorner control holder. This object is passed to the Adorner
        /// </summary>
        ContentControl adornerControl;

        /// <summary>
        /// Preset actions to handle delayed contruction/destruction
        /// </summary>
        Func<bool> delayedFactory;
        Func<bool> delayedDestruction;
        Func<bool> nonDelayedFactory;
        Func<bool> nonDelayedDestruction;

        /// <summary>
        /// Preset actions
        /// </summary>
        readonly Func<bool> factor;
        readonly Func<bool> dispose;
        readonly Func<bool> emptyAction;


        #region Commands

        /// <summary>
        /// ShowAdornerCommand
        /// </summary>
        public ICommand ShowAdornerCommand
        {
            get;
            private set;
        }


        /// <summary>
        /// HideAdornerCommand
        /// </summary>
        public ICommand HideAdornerCommand
        {
            get;
            private set;
        }
        #endregion

        #region VerticalAlignment

        public static readonly DependencyProperty VerticalAlignmentProperty =
            DependencyProperty.Register("VerticalAlignment",
            typeof(AdornerVerticalAlignment),
            typeof(ContentTemplateAdornerBehavior),
             new PropertyMetadata(default(AdornerVerticalAlignment), (d, o) =>
             {
                 if (null != ((ContentTemplateAdornerBehavior)d).templatedAdorner)
                     ((ContentTemplateAdornerBehavior)d).templatedAdorner.VerticalAdornerAlignment = (AdornerVerticalAlignment)o.NewValue;
             }));

        public AdornerVerticalAlignment VerticalAlignment
        {
            get { return (AdornerVerticalAlignment)GetValue(VerticalAlignmentProperty); }
            set { SetValue(VerticalAlignmentProperty, value); }
        }


        #endregion

        #region HorizontalAlignment

        public static readonly DependencyProperty HorizontalAlignmentProperty =
            DependencyProperty.Register("HorizontalAlignment",
            typeof(AdornerHorizontalAlignment),
            typeof(ContentTemplateAdornerBehavior),
             new PropertyMetadata(default(AdornerHorizontalAlignment), (d, o) =>
             {
                 if (null != ((ContentTemplateAdornerBehavior)d).templatedAdorner)
                     ((ContentTemplateAdornerBehavior)d).templatedAdorner.HorizontalAdornerAlignment = (AdornerHorizontalAlignment)o.NewValue;
             }));

        public AdornerHorizontalAlignment HorizontalAlignment
        {
            get { return (AdornerHorizontalAlignment)GetValue(HorizontalAlignmentProperty); }
            set { SetValue(HorizontalAlignmentProperty, value); }
        }


        #endregion

        #region Placement

        public static readonly DependencyProperty PlacementProperty =
            DependencyProperty.Register("Placement",
            typeof(AdornerPlacement),
            typeof(ContentTemplateAdornerBehavior),
             new PropertyMetadata(default(AdornerPlacement), (d, o) =>
             {
                 if (null != ((ContentTemplateAdornerBehavior)d).templatedAdorner)
                     ((ContentTemplateAdornerBehavior)d).templatedAdorner.AdornerPlacement = (AdornerPlacement)o.NewValue;
             }));


        public AdornerPlacement Placement
        {
            get { return (AdornerPlacement)GetValue(PlacementProperty); }
            set { SetValue(PlacementProperty, value); }
        }

        #endregion

        #region Margin

        /// <summary>
        /// Adorner Margin
        /// </summary>
        public static readonly DependencyProperty AdornerMarginProperty = DependencyProperty.Register(
            "ctAdornerMargin",
            typeof(Thickness),
            typeof(ContentTemplateAdornerBehavior)
          );

        /// <summary>
        /// Adorner Margin
        /// </summary>
        public Thickness AdornerMargin
        {
            get { return (Thickness)GetValue(AdornerMarginProperty); }
            set { SetValue(AdornerMarginProperty, value); }
        }


        #endregion

        #region AdornerTemplate
        /// <summary>
        /// Template to use in adorner. The template is hosted as a content control template with full 
        /// HitTest.
        /// </summary>
        public static readonly DependencyProperty AdornerTemplateProperty = DependencyProperty.Register(
            "AdornerTemplate",
            typeof(DataTemplate),
            typeof(ContentTemplateAdornerBehavior),
            new PropertyMetadata((d, o) =>
            {
                if (null != ((ContentTemplateAdornerBehavior)d).adornerControl)
                    ((ContentTemplateAdornerBehavior)d).adornerControl.ContentTemplate = (DataTemplate)o.NewValue;
            }));

        /// <summary>
        /// Data template for the adroner. Used inside a ContentControl. 
        /// </summary>
        public DataTemplate AdornerTemplate
        {
            get { return (DataTemplate)GetValue(AdornerTemplateProperty); }
            set { SetValue(AdornerTemplateProperty, value); }
        }
        #endregion

        #region AdornerVisible

        /// <summary>
        /// AdornerVisibleProperty
        /// </summary>
        public static readonly DependencyProperty AdornerVisibleProperty = DependencyProperty.Register(
            "AdornerVisible",
            typeof(Visibility),
            typeof(ContentTemplateAdornerBehavior),
            new PropertyMetadata(Visibility.Hidden, (d, o) =>
            {
                if (null != ((ContentTemplateAdornerBehavior)d).adornerControl)
                    ((ContentTemplateAdornerBehavior)d).adornerControl.Visibility = (Visibility)o.NewValue;
            }));

        /// <summary>
        /// Data template for the adroner. Used inside a ContentControl. 
        /// </summary>
        public Visibility AdornerVisible
        {
            get { return (Visibility)GetValue(AdornerVisibleProperty); }
            set { SetValue(AdornerVisibleProperty, value); }
        }

        #endregion

        #region DelayConstruction
        /// <summary>
        /// True = Construct and dispose adorner on demand. Slower user experience.
        /// False = (Default) Create aodrner when attached. Faster user experience.
        /// </summary>
        public static readonly DependencyProperty DelayConstructionProperty = DependencyProperty.Register(
            "DelayConstruction",
            typeof(bool),
            typeof(ContentTemplateAdornerBehavior),
            new PropertyMetadata(false, (d, o) =>
            {
                if (null != d)
                    ((ContentTemplateAdornerBehavior)d).SetDelayedState((bool)o.NewValue);
            }
          ));

        /// <summary>
        /// Data template for the adroner. Used inside a ContentControl. 
        /// </summary>
        public bool DelayConstruction
        {
            get { return (bool)GetValue(DelayConstructionProperty); }
            set { SetValue(DelayConstructionProperty, value); }
        }

        /// <summary>
        /// Data template for the adroner. Used inside a ContentControl. 
        /// </summary>
        /// <param name="delayed"></param>
        private void SetDelayedState(bool delayed)
        {
            delayedFactory = delayed ? factor : emptyAction;
            delayedDestruction = delayed ? dispose : emptyAction;
            nonDelayedFactory = !delayed ? factor : emptyAction;
            nonDelayedDestruction = !delayed ? dispose : emptyAction;
        }
        #endregion

        /// <summary>
        /// is the adorned control currently visible
        /// </summary>
        public bool IsVisible
        {
            get
            {
                // note using adornerControl.IsVisible doesn't seem to work?
                return adornerControl != null && adornerControl.Visibility == Visibility.Visible;
            }
        }

        public object DataContext { get; set; }

        /// <summary>
        /// 
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Globalization", "CA1305:SpecifyIFormatProvider", MessageId = "System.String.Format(System.String,System.Object)")]
        public ContentTemplateAdornerBehavior()
        {
            //
            // create three static Actions to work with delayed, or not, construction
            //
            emptyAction = () => false;

            //
            // Delay factory action
            //
            factor = () =>
                {
                    if (AdornerTemplate != null)
                    {
                        var element = AssociatedObject as UIElement;
                        if (element == null)
                            throw new NullReferenceException("element");

                        AdornerLayer adornerLayer = AdornerLayer.GetAdornerLayer(element);

                        if (null == adornerLayer) throw new NullReferenceException(string.Format("No adorner found in attached object: {0}", AssociatedObject));
                        

                        // Create adorner
                        adornerControl = new ContentControl();
                        adornerControl.Focusable = false;
                        templatedAdorner = new ContentTemplateAdorner(element, adornerControl, VerticalAlignment, HorizontalAlignment, Placement, AdornerMargin);

                        // Add to adorner
                        adornerLayer.Add(templatedAdorner);

                        // set realted bindings
                        adornerControl.Content = AdornerTemplate.LoadContent();
                        adornerControl.Visibility = AdornerVisible;

                        if (DataContext != null)
                            adornerControl.DataContext = DataContext;

                        // bug 11079: When we have focus on an element in the adorner we do not receive a lost focus event when the parent adorned control is no longer visible.
                        element.IsVisibleChanged += (sender, args) => { if (!element.IsVisible) HideAdorner(); };
                    }

                    return true;
                };

            //
            // proper dispose
            //
            dispose = () =>
                {
                    if (null != templatedAdorner)
                    {
                        var element = AssociatedObject as UIElement;
                        if (element == null)
                            return false;

                        AdornerLayer adornerLayer = AdornerLayer.GetAdornerLayer(element);
                        if (null == adornerLayer)
                            return false;

                        adornerLayer.Remove(templatedAdorner);
                        templatedAdorner = null;
                        adornerControl = null;
                    }
                    return true;
                };

            // set intial actions 
            SetDelayedState(DelayConstruction);

            // Behavior events
            ShowAdornerCommand = new DelegateCommand(ShowAdorner);
            HideAdornerCommand = new DelegateCommand(HideAdorner);
        }

        /// <summary>
        /// Standard behavior OnAttached() override
        /// </summary>
        protected override void OnAttached()
        {
            base.OnAttached();
            nonDelayedFactory();
        }


        /// <summary>
        /// Standard behavior OnDetaching() override
        /// </summary>
        protected override void OnDetaching()
        {
            base.OnDetaching();
            nonDelayedDestruction();
        }

        /// <summary>
        /// Show the current adorner control
        /// </summary>
        public void Show()
        {
            if (adornerControl != null && adornerControl.Visibility != Visibility.Visible)
            {
                adornerControl.Visibility = Visibility.Visible;
            }
            else if (adornerControl == null)
            {
                ShowAdorner();

            }
        }

        /// <summary>
        /// Hide the adorner control
        /// </summary>
        public void Hide()
        {
            if (adornerControl != null && adornerControl.Visibility != Visibility.Hidden)
            {
                adornerControl.Visibility = Visibility.Hidden;

            }
        }

        /// <summary>
        /// Toggle the visibility of the adorner control
        /// </summary>
        public void ToggleVisibility()
        {
            if (adornerControl != null)
            {
                adornerControl.Visibility = adornerControl.IsVisible ? Visibility.Hidden : Visibility.Visible;

            }
            else
            {
                ShowAdorner();
            }
        }


        /// <summary>
        /// ShowAdorner
        /// </summary>
        private void ShowAdorner()
        {
            delayedFactory();
            adornerControl.Visibility = Visibility.Visible;
        }


        /// <summary>
        /// HideAdorner
        /// </summary>
        private void HideAdorner()
        {
            if (delayedDestruction()) return;

            if (adornerControl.IsMouseOver)
            {
                adornerControl.MouseLeave -= OnAdornerControlOnMouseLeave;
                adornerControl.MouseLeave += OnAdornerControlOnMouseLeave;
            }
            else
            {
                adornerControl.Visibility = AdornerVisible;

            }
        }

        private void OnAdornerControlOnMouseLeave(object s, MouseEventArgs e)
        {
            adornerControl.Visibility = AdornerVisible;
        }
    }
}

