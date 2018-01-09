using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.System;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using Windows.Web.Http.Filters;
using Microsoft.Toolkit.Uwp;
using Syncfusion.Pdf.Parsing;

// The User Control item template is documented at https://go.microsoft.com/fwlink/?LinkId=234236

namespace Dash
{
    public sealed partial class WebViewWrapper : UserControl
    {
        public WebView WebContext => xWebContent;
        public Uri CurrentUri { get; set; }

        /// <summary>
        /// Stack to store back context
        /// </summary>
        private readonly Stack<Uri> _backContext;

        /// <summary>
        /// Stack to store forward context
        /// </summary>
        private readonly Stack<Uri> _forwardContext;

        private bool _pushToBackOnNav;

        public WebViewWrapper()
        {
            this.InitializeComponent();
            _backContext = new Stack<Uri>();
            _forwardContext = new Stack<Uri>();
            _pushToBackOnNav = false;
        }

        /// <summary>
        /// Called when the user tries to navigate using the url box
        /// </summary>
        /// <param name="uri"></param>
        public void UrlBoxNavigate(string uri)
        {
            // try to create the uri for navigation
            if (!Uri.TryCreate(uri, UriKind.Absolute, out Uri navUri))
            {
                // if creating the uri fails use google search
                if (!(uri.Contains(".") && Uri.TryCreate("http://" + uri, UriKind.Absolute, out navUri)))
                    navUri = new Uri("https://google.com/search?q=" + uri);
            }

            // clear the forward context history
            _forwardContext.Clear();

            SetCurrentContent(navUri);
        }

        /// <summary>
        /// Set the uri for the content we want to display in the view
        /// </summary>
        /// <param name="contentPath"></param>
        private void SetCurrentContent(Uri contentPath)
        {
            WebContext.Navigate(contentPath);
        }

        /// <summary>
        /// Called when the back button on the navbar is tapped
        /// </summary>
        private void WebBackButton_OnTapped(object sender, TappedRoutedEventArgs e)
        {
            // manage forward and backward stacks and set the current content
            if (_backContext.Any())
            {
                var contentPath = _backContext.Pop();
                if (CurrentUri != null) _forwardContext.Push(CurrentUri);
                _pushToBackOnNav = false;
                SetCurrentContent(contentPath);
            }
        }

        /// <summary>
        /// Called when the forward button on the navbar is tapped
        /// </summary>
        private void WebForwardButton_OnTapped(object sender, TappedRoutedEventArgs e)
        {
            // manage forward and backward stacks and set the current content
            if (_forwardContext.Any())
            {
                var contentPath = _forwardContext.Pop();
                SetCurrentContent(contentPath);
            }
        }

        /// <summary>
        /// Called when the refresh button on the navbar is tapped
        /// </summary>
        private void WebRefreshButton_OnTapped(object sender, TappedRoutedEventArgs e)
        {
            // only refresh webcontent if it is visible
            if (xWebContent.Visibility == Visibility.Visible) xWebContent.Refresh();
        }

        /// <summary>
        /// Called when the user finishes pressing a key in the urlbox in the nav bar
        /// </summary>
        private void UrlBox_KeyUp(object sender, KeyRoutedEventArgs e)
        {

            if (e.Key == VirtualKey.Enter && !xUrlBox.Text.Equals(string.Empty))
            {
                UrlBoxNavigate(xUrlBox.Text);
            }
        }

        private async void WebContext_NavigationCompleted(WebView sender, WebViewNavigationCompletedEventArgs args)
        {
            if (args.IsSuccess)
            {
                UpdateCurrentUri(args.Uri);

                await WebContext.InvokeScriptAsync("eval", new[]
                {
                    @"(function()
                {
                    var hyperlinks = document.getElementsByTagName('a');
                    for(var i = 0; i < hyperlinks.length; i++)
                    {
                        if(hyperlinks[i].getAttribute('target') != null ||
                            hyperlinks[i].getAttribute('target') != '_blank')
                        {
                            hyperlinks[i].setAttribute('target', '_self');
                        }
                    }
                })()"
                });
            }
            else
            {
                //WebContext.GoBack();
            }
        }

        private void xWebContext_NavigationStarting(WebView sender, WebViewNavigationStartingEventArgs args)
        {
            HidePdfDisplayWeb();
        }

        private void xWebContext_FrameNavigationStarting(WebView sender, WebViewNavigationStartingEventArgs args)
        {
            HidePdfDisplayWeb();
        }

        private async void XWebContent_OnUnviewableContentIdentified(WebView sender, WebViewUnviewableContentIdentifiedEventArgs args)
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

        private void UpdateCurrentUri(Uri uri)
        {
            // Try to push the current uri to the back history
            TryPushUriToBack(CurrentUri);

            // set the new current uri and update the url box to reflect that
            CurrentUri = uri;
            xUrlBox.Text = CurrentUri.AbsoluteUri;

            // make sure nav buttons are properly enabled and disabled
            UpdateNavButtons();
        }

        private void TryPushUriToBack(Uri uriForBack)
        {
            if (_pushToBackOnNav)
            {
                _backContext.Push(uriForBack);
            }
            else
            {
                _pushToBackOnNav = true;
            }
        }

        private void UpdateNavButtons()
        {
            xBackButton.IsEnabled = _backContext.Any();
            xForwardButton.IsEnabled = _forwardContext.Any();
            xRefreshButton.IsEnabled = WebContext.Visibility == Visibility.Visible;
        }
    }
}
