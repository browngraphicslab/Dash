using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using Windows.UI.Xaml.Shapes;

// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=234238

namespace Dash
{
    /// <summary>
    /// An image wrapper class that enables image editing (resize, crop) upon double click 
    /// </summary>
    public sealed partial class EditableImage
    {
        public Image Image { get { return xImage; } }

        public Rect ClipRect { get; } = new Rect(0, 0, 100, 100);

        private bool _isImageDraggerVisible;
        public bool IsImageDraggerVisible
        {
            get { return _isImageDraggerVisible; }
            set
            {
                _isImageDraggerVisible = value;
                Visibility visibility = value ? Visibility.Visible : Visibility.Collapsed;

                xBottomLeftDragger.Visibility = visibility;
                xBottomRightDragger.Visibility = visibility;
                xTopLeftDragger.Visibility = visibility;
                xTopRightDragger.Visibility = visibility;
            }
        }

        private bool _isClipRectVisible;
        public bool IsClipRectVisible
        {
            get { return _isClipRectVisible; }
            set
            {
                _isClipRectVisible = value;
                Visibility visibility = value ? Visibility.Visible : Visibility.Collapsed;

                xCLIPBottomLeftDragger.Visibility = visibility;
                xCLIPBottomRightDragger.Visibility = visibility;
                xCLIPTopLeftDragger.Visibility = visibility;
                xCLIPTopRightDragger.Visibility = visibility;
                xClipRectangle.Visibility = visibility;
            }
        }

        public EditableImage()
        {
            InitializeComponent();

            IsClipRectVisible = false;
            IsImageDraggerVisible = false; 
            SetUpBindings();
        }


        private void SetUpBindings()
        {
            // bind the rectangle dimensions to ClipRect 
            var widthBinding = new Binding
            {
                Source = ClipRect,
                Path = new PropertyPath("Width")
            };
            xClipRectangle.SetBinding(WidthProperty, widthBinding);

            var heightBinding = new Binding
            {
                Source = ClipRect,
                Path = new PropertyPath("Height")
            };
            xClipRectangle.SetBinding(HeightProperty, heightBinding);
        }
        

        /// <summary>
        /// Brings up the editorview upon doubleclick 
        /// </summary>
        private void xImage_DoubleTapped(object sender, DoubleTappedRoutedEventArgs e)
        {
            xEditStackPanel.Visibility = Visibility.Visible;
        }

        private void DoneButton_Tapped(object sender, TappedRoutedEventArgs e)
        {
            xEditStackPanel.Visibility = Visibility.Collapsed;
            IsImageDraggerVisible = false;
            IsClipRectVisible = false;
        }

        /// <summary>
        /// Toggles Visibility of Image's draggerEllipses 
        /// </summary>
        private void xImageButton_Tapped(object sender, TappedRoutedEventArgs e)
        {
            if (xImageButton.Background == new SolidColorBrush(Colors.Blue)) return;
            xImageButton.Background = new SolidColorBrush(Colors.Blue);
            xClipButton.Background = new SolidColorBrush(Colors.Gray);

            IsImageDraggerVisible = true;
            IsClipRectVisible = false;
        }

        /// <summary>
        /// Toggles Visibility of ClipRect's draggerEllipses 
        /// </summary>
        private void xClipbutton_Tapped(object sender, TappedRoutedEventArgs e)
        {
            if (xClipButton.Background == new SolidColorBrush(Colors.Blue)) return;
            xClipButton.Background = new SolidColorBrush(Colors.Blue);
            xImageButton.Background = new SolidColorBrush(Colors.Gray);

            IsClipRectVisible = true;
            IsImageDraggerVisible = false;
        }
    }
}
