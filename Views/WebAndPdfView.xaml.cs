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


        private Uri _source;

        public Uri Source
        {
            get => _source;
            set
            {
                try
                {
                    Debug.WriteLine($"WEBPDF SOURCE: {Source}");
                    if (Source != null && Source.Equals(value)) return;
                    _source = value;
                    OnPropertyChanged();
                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
                    throw;
                }
            }
        }

        public WebAndPdfView()
        {
            this.InitializeComponent();
            Loaded += WebAndPdfView_Loaded;
        }

        public WebAndPdfView(Uri source) : this()
        {
            try
            {
                Source = source;
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }
        }

        private void WebAndPdfView_Loaded(object sender, RoutedEventArgs e)
        {
            xWebContent.Source = Source;
        }

        private void WebView_OnNavigationStarting(WebView sender, WebViewNavigationStartingEventArgs args)
        {
            HidePdfDisplayWeb();
            xProgressRing.IsActive = true;
        }

        private void WebView_OnNavigationCompleted(WebView sender, WebViewNavigationCompletedEventArgs args)
        {
            xProgressRing.IsActive = false;
        }

        private async void WebView_OnUnviewableContentIdentified(WebView sender, WebViewUnviewableContentIdentifiedEventArgs args)
        {
            // render pdfs
            if (args.Uri.AbsoluteUri.ToLower().EndsWith(".pdf"))
            {
                Stream stream;
                if (args.Uri.IsFile)
                {
                    stream = File.Open(args.Uri.LocalPath, FileMode.Open, FileAccess.Read, FileShare.Read);
                }
                else
                {
                    stream = (await args.Uri.GetHttpStreamAsync()).AsStream();
                }

                Debug.Assert(stream.Length != 0);
                DisplayPdfHideWeb(stream);
            }
        }

        private void HidePdfDisplayWeb()
        {
            if (xPdfContent != null)
            {
                xPdfContent.Visibility = Visibility.Collapsed;
                xPdfContent.Pdf.Unload(true);
            }
            if (xWebContent != null)
            {
                xWebContent.Visibility = Visibility.Visible;
            }
        }

        private void DisplayPdfHideWeb(Stream stream)
        {
            if (xPdfContent != null)
            {
                xPdfContent.Pdf.LoadDocument(stream);
                xPdfContent.Visibility = Visibility.Visible;
            }
            if (xWebContent != null)
            {
                xWebContent.Visibility = Visibility.Collapsed;
            }
        }

        private void OuterGridOnSizeChanged(object sender, SizeChangedEventArgs e)
        {
            xWebContent.Width = xOuterGrid.ActualWidth;
            xWebContent.Height = xOuterGrid.ActualHeight;
            xPdfContent.Width = xOuterGrid.ActualWidth;
            xPdfContent.Height = xOuterGrid.ActualHeight;

        }

        public event PropertyChangedEventHandler PropertyChanged;

        [NotifyPropertyChangedInvocator]
        private void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

    }
}
