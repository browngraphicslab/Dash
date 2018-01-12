using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using Microsoft.Toolkit.Uwp;

// The User Control item template is documented at https://go.microsoft.com/fwlink/?LinkId=234236

namespace Dash
{
    public sealed partial class WebAndPdfView : UserControl
    {

        public Uri Source
        {
            get => xWebContent.Source;
            set => xWebContent.Source = value;
        }

        public WebAndPdfView()
        {
            this.InitializeComponent();
        }

        public WebAndPdfView(Uri source)
        {
            Source = source;
        }

        private void WebView_OnNavigationStarting(WebView sender, WebViewNavigationStartingEventArgs args)
        {
            HidePdfDisplayWeb();
        }

        private void WebView_OnNavigationCompleted(WebView sender, WebViewNavigationCompletedEventArgs args)
        {
            //throw new NotImplementedException();
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
            if (xWebContent != null) xWebContent.Visibility = Visibility.Visible;
        }

        private void DisplayPdfHideWeb(Stream stream)
        {
            if (xPdfContent != null)
            {
                xPdfContent.Pdf.LoadDocument(stream);
                xPdfContent.Visibility = Visibility.Visible;
            }
            if (xWebContent != null) xWebContent.Visibility = Visibility.Collapsed;
        }

        private void OuterGridOnSizeChanged(object sender, SizeChangedEventArgs e)
        {
            xWebContent.Width = xOuterGrid.ActualWidth;
            xWebContent.Height = xOuterGrid.ActualHeight;
            xPdfContent.Width = xOuterGrid.ActualWidth;
            xPdfContent.Height = xOuterGrid.ActualHeight;
        }
    }
}
