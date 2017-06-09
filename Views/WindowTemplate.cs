using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Dynamic;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.ApplicationModel.Store;
using Windows.Foundation;
using Windows.UI;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Documents;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Dash.StaticClasses;

// The Templated Control item template is documented at http://go.microsoft.com/fwlink/?LinkId=234235

namespace Dash
{
    [TemplatePart(Name = InnerContentName, Type=typeof(ContentControl))]
    [TemplatePart(Name = ResizerName, Type = typeof(UIElement))]
    [TemplatePart(Name = ContainerName, Type = typeof(UIElement))]
    [TemplatePart(Name = HeaderName, Type = typeof(UIElement))]
    [TemplatePart(Name = CloseButtonName, Type = typeof(UIElement))]
    public class WindowTemplate : Control
    {

        // variable names for accessing parts from xaml!
        private const string InnerContentName = "PART_InnerContent";
        private const string ResizerName = "PART_Resizer";
        private const string ContainerName = "PART_Container";
        private const string HeaderName = "PART_Header";
        private const string CloseButtonName = "PART_CloseButton";

        /// <summary>
        /// Private variable to get the container which determines the size of the window
        /// so we don't have to look for it on manipulation delta
        /// </summary>
        private FrameworkElement _container;

        public WindowTemplate()
        {
            this.DefaultStyleKey = typeof(WindowTemplate);

            this.MinWidth = 50;
            this.MinHeight = 50;
            this.MaxWidth = 3000;
            this.MaxHeight = 3000;
        }


        /// <summary>
        /// The inner content of the window can be anything!
        /// </summary>
        public object InnerContent
        {
            get { return (object)GetValue(InnerContentProperty); }
            set { SetValue(InnerContentProperty, value); }
        }

        // Using a DependencyProperty as the backing store for InnerContent.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty InnerContentProperty =
            DependencyProperty.Register("InnerContent", typeof(object), typeof(WindowTemplate), new PropertyMetadata(null));


        /// <summary>
        /// The inner content of the window can be anything!
        /// </summary>
        public Color HeaderColor
        {
            get { return (Color)GetValue(HeaderColorProperty); }
            set { SetValue(HeaderColorProperty, value); }
        }

        // Using a DependencyProperty as the backing store for InnerContent.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty HeaderColorProperty =
            DependencyProperty.Register("HeaderColor", typeof(Color), typeof(WindowTemplate), new PropertyMetadata(Colors.Pink));

        /// <summary>
        /// On apply template we add events and get parts from xaml
        /// </summary>
        protected override void OnApplyTemplate()
        {
            // apply resize events
            var resizer = GetTemplateChild(ResizerName) as UIElement;
            Debug.Assert(resizer != null);
            resizer.ManipulationMode = ManipulationModes.TranslateX | ManipulationModes.TranslateY;
            resizer.ManipulationDelta += ResizerOnManipulationDelta;

            // get the container private variable
            _container = GetTemplateChild(ContainerName) as FrameworkElement;
            Debug.Assert(_container != null);

            // apply close button events
            var closeButton = GetTemplateChild(CloseButtonName) as UIElement;
            Debug.Assert(closeButton != null);
            closeButton.Tapped += CloseButton_Tapped;

            // apply header events
            var header = GetTemplateChild(HeaderName) as UIElement;
            Debug.Assert(header != null);
            header.ManipulationMode = ManipulationModes.TranslateX | ManipulationModes.TranslateY;
            header.ManipulationDelta += HeaderOnManipulationDelta;
        }

        private void HeaderOnManipulationDelta(object sender, ManipulationDeltaRoutedEventArgs e)
        {
            TransformGroup group = new TransformGroup();
            Point p = Util.DeltaTransformFromVisual(e.Delta.Translation, this);
            TranslateTransform translate = new TranslateTransform
            {
                X = p.X,
                Y = p.Y
            };

            group.Children.Add(this.RenderTransform);
            group.Children.Add(translate);
            this.RenderTransform = new MatrixTransform { Matrix = group.Value };

            e.Handled = true; 
        }

        /// <summary>
        /// Called whenever the close button is tapped; closes window 
        /// </summary>
        private void CloseButton_Tapped(object sender, TappedRoutedEventArgs e)
        {
            var panel = this.Parent as Panel;
            if (panel != null)
            {
                panel.Children.Remove(this);
                return;
            }

            var contentPresenter = this.Parent as ContentPresenter;
            if (contentPresenter != null)
            {
                if (contentPresenter.Content == this)
                    contentPresenter.Content = null;
                return;
            }

            var contentControl = this.Parent as ContentControl;
            if (contentControl != null)
            {
                if (contentControl.Content == this)
                    contentControl.Content = null;
                return;
            }
            var itemsControl = this.Parent as ItemsControl;
            itemsControl?.Items?.Remove(this);
        }

        /// <summary>
        /// Called whenever the resizer is manipulated
        /// </summary>
        private void ResizerOnManipulationDelta(object sender, ManipulationDeltaRoutedEventArgs e)
        {
            // Transform e.Delta.Translation to relative size of FreeformView; scaling takes into account zoom factor  
            Point p = Util.DeltaTransformFromVisual(e.Delta.Translation, this);
            var newWidth = this.ActualWidth + p.X; 
            var newHeight = this.ActualHeight + p.Y; 

            // clamp width and height to max and min
            this.Width = Math.Max(this.MinWidth, Math.Min(this.MaxWidth, newWidth));
            this.Height = Math.Max(Math.Min(this.MaxHeight, newHeight), this.MinHeight);
            e.Handled = true;

        }
    }
}
