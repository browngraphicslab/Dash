using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Runtime.CompilerServices;
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
using Windows.UI.Xaml.Media.Imaging;
using Windows.UI.Xaml.Navigation;
using Dash.Annotations;
using Microsoft.Toolkit.Uwp;
using Microsoft.Toolkit.Uwp.Helpers;

// The User Control item template is documented at https://go.microsoft.com/fwlink/?LinkId=234236

namespace Dash
{

    public class ContextWebView
    {
        public WebAndPdfView View;
        public double ScaleFactor;
        public double ActualWidth => Width * ScaleFactor;
        public double ActualHeight => Height * ScaleFactor;
        public double Width;
        public double Height;

        public ContextWebView(WebAndPdfView view, double scaleFactor, double width, double height)
        {
            View = view;
            ScaleFactor = scaleFactor;
            Width = width;
            Height = height;
        }
    }

    public sealed partial class WebAndPdfView : UserControl, INotifyPropertyChanged
    {

        private BitmapImage _bitmapImage;

        public BitmapImage BitmapImage
        {
            get => _bitmapImage;
            set
            {
                _bitmapImage = value;
                OnPropertyChanged();
            }
        }

        public WebAndPdfView(DocumentContext context)
        {
            InitializeComponent();
            BitmapImage = context.GetImage();
        }


        public event PropertyChangedEventHandler PropertyChanged;

        [NotifyPropertyChangedInvocator]
        private void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
