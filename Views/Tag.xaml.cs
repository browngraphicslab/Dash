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

// The User Control item template is documented at https://go.microsoft.com/fwlink/?LinkId=234236

namespace Dash
{
    public sealed partial class Tag : UserControl
    {

        public string Text
        {
            get => _text;
            set { _text = value; }
        }
        private string _text;
        private DocumentDecorations _docdecs;
        public Tag(DocumentDecorations docdecs, String text, Color color)
        {
            this.InitializeComponent();
            xTagContainer.Background = new SolidColorBrush(color);
            xTagText.Text = text;
            _text = text;
            _docdecs = docdecs;
        }


        private void XTagContainer_OnTapped(object sender, TappedRoutedEventArgs e)
        {
            if (xTagContainer.BorderThickness.Equals(new Thickness(0)))
            {
                xTagContainer.BorderThickness = new Thickness(2);
                xTagContainer.BorderBrush = new SolidColorBrush(Colors.DodgerBlue);
                bool unique = true;
                foreach (var recent in _docdecs.RecentTags)
                {
                    if (recent.Text == _text)
                    {
                        unique = false;
                    }
                }

                if (unique)
                {
                    if (_docdecs.RecentTags.Count < 5)
                    {
                        _docdecs.RecentTags.Enqueue(this);
                    }
                    else
                    {
                        _docdecs.RecentTags.Dequeue();
                        _docdecs.RecentTags.Enqueue(this);
                    }
                }
            }
            else
            {
                xTagContainer.BorderThickness = new Thickness(0);

            }
        }
    }
}
