using System;
using System.Collections.Generic;
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

// The User Control item template is documented at https://go.microsoft.com/fwlink/?LinkId=234236

namespace Dash
{
    public sealed partial class WebViewWrapper : UserControl
    {
        public WebView WebContext => xWebContext;
        public Uri WebContextUri { get; set; }

        private readonly Stack<Uri> _backUris = new Stack<Uri>();
        private readonly Stack<Uri> _forwardUris = new Stack<Uri>();
        private bool _backNavigation, _fwdNavigation;

        public WebViewWrapper()
        {
            this.InitializeComponent();
        }

        public void TryNavigate(string uri)
        {
            Uri uriResult;
            bool result = Uri.TryCreate(uri, UriKind.Absolute, out uriResult);
            if (!result)
            {
                if(uri.Contains(".")) Uri.TryCreate("http://" + uri, UriKind.Absolute, out uriResult);
                else uriResult = new Uri("https://google.com/search?q=" + uri);
            }
            UrlBox.Text = uriResult.AbsoluteUri;
            WebContext.Navigate(uriResult);
        }

        private void WebContext_NavigationCompleted(WebView sender, WebViewNavigationCompletedEventArgs args)
        {
            if (args.IsSuccess)
            {
                if (!_backNavigation && WebContextUri != null) _backUris.Push(WebContextUri);
                bool newPage = !_backNavigation && !_fwdNavigation;
                if (newPage)
                {
                    _forwardUris.Clear();
                }
                _backNavigation = _fwdNavigation = false;
                WebContextUri = args.Uri;
                UrlBox.Text = WebContextUri.AbsoluteUri;
            }
            else
            {
                _backNavigation = true;
                _fwdNavigation = false;
                WebContext.Navigate(WebContextUri);
            }
        }

        private void WebBackButton_OnTapped(object sender, TappedRoutedEventArgs e)
        {
            if (_backUris.Count <= 0) return;
            var uri = _backUris.Pop();
            _forwardUris.Push(WebContextUri);
            _backNavigation = true;
            UrlBox.Text = uri.AbsoluteUri;
            WebContext.Navigate(uri);
        }

        private void WebForwardButton_OnTapped(object sender, TappedRoutedEventArgs e)
        {
            if (_forwardUris.Count <= 0) return;
            var uri = _forwardUris.Pop();
            _fwdNavigation = true;
            UrlBox.Text = uri.AbsoluteUri;
            WebContext.Navigate(uri);
        }

        private void UrlBox_KeyUp(object sender, KeyRoutedEventArgs e)
        {
            if (e.Key == VirtualKey.Enter && !UrlBox.Text.Equals(string.Empty))
            {
                TryNavigate(UrlBox.Text);
            }
        }
        

        private void WebRefreshButton_OnTapped(object sender, TappedRoutedEventArgs e)
        {
            WebContext.Refresh();
        }
    }
}
