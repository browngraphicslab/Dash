﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

// The User Control item template is documented at https://go.microsoft.com/fwlink/?LinkId=234236

namespace Dash
{
    public sealed partial class KVPListText : UserControl
    {

        public string Text
        {
            get => _text;
            set { _text = value; }
        }

        public Color Color
        {
            get => _color;
            set { _color = value; }
        }
        private string _text;
        private Color _color;
      
        public KVPListText(String text, Color color)
        {
            this.InitializeComponent();

            xListTextContainer.Background = new SolidColorBrush(color);
            xText.Text = text;
            _text = text;
            _color = color;
        }

        private void DeleteButton_PointerEntered(object sender, PointerRoutedEventArgs e)
        {
            xDeleteIcon.Opacity = 1;
            Window.Current.CoreWindow.PointerCursor = new CoreCursor(CoreCursorType.Hand, 1);
        }

        private void DeleteButton_PointerExited(object sender, PointerRoutedEventArgs e)
        {
            xDeleteIcon.Opacity = 0.5;
            Window.Current.CoreWindow.PointerCursor = new CoreCursor(CoreCursorType.Arrow, 1);
        }


        private void UIElement_OnTapped(object sender, TappedRoutedEventArgs e)
        {
            //throw new NotImplementedException();
        }
    }
}
