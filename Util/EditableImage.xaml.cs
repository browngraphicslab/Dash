﻿using System;
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

// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=234238

namespace Dash
{
    /// <summary>
    /// An image wrapper class that enables image editing (resize, crop) upon double click 
    /// </summary>
    public sealed partial class EditableImage 
    {
        public Image Image { get { return xImage; } }

        public Rect ClipRect { get; }

        private bool _imageDragerVisible;
        private bool _clipRectVisible;

        public bool IsEditPanelVisible { get; set; }

        public EditableImage()
        {
            InitializeComponent();

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
            // make other things invisible 
        }

        /// <summary>
        /// Toggles Visibility of Image's draggerEllipses 
        /// </summary>
        private void xImageButton_Tapped(object sender, TappedRoutedEventArgs e)
        {
            if (xImageButton.Background == new SolidColorBrush(Colors.Blue)) return;
            xImageButton.Background = new SolidColorBrush(Colors.Blue);
            xClipButton.Background = new SolidColorBrush(Colors.Gray);
        }

        /// <summary>
        /// Toggles Visibility of ClipRect's draggerEllipses 
        /// </summary>
        private void xClipbutton_Tapped(object sender, TappedRoutedEventArgs e)
        {
            if (xClipButton.Background == new SolidColorBrush(Colors.Blue)) return;
            xClipButton.Background = new SolidColorBrush(Colors.Blue);
            xImageButton.Background = new SolidColorBrush(Colors.Gray);
        }
    }
}
