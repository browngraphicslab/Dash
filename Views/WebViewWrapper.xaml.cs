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
        //public WebView WebContext => xWebContext;
        //public Uri WebContextUri { get; set; }

        public BrowserView WebContext => BrowserView.Current;

        public WebViewWrapper()
        {
            this.InitializeComponent();
        }

        public void TryNavigate(string uri)
        {
                /*
            Uri uriResult;
            var result = Uri.TryCreate(uri, UriKind.Absolute, out uriResult);
            if (!result)
                if (!(uri.Contains(".") && Uri.TryCreate("http://" + uri, UriKind.Absolute, out uriResult)))
                    uriResult = new Uri("https://google.com/search?q=" + uri);
            UrlBox.Text = uriResult.AbsoluteUri;
            */
            //WebContext.Navigate(uriResult);
            WebContext.SetUrl(uri);
        }

        private async void WebContext_NavigationCompleted(WebView sender, WebViewNavigationCompletedEventArgs args)
        {
            if (args.IsSuccess)
            {
               // WebContextUri = args.Uri;
                //UrlBox.Text = WebContextUri.AbsoluteUri;
                /*
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
            });*/
            }
            else
            {
                //WebContext.GoBack();
            }
        }

        private void WebBackButton_OnTapped(object sender, TappedRoutedEventArgs e)
        {
            //if(WebContext.CanGoBack) WebContext.GoBack();
        }

        private void WebForwardButton_OnTapped(object sender, TappedRoutedEventArgs e)
        {
            //if(WebContext.CanGoForward) WebContext.GoForward();
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
            //WebContext.Refresh();
        }

        private void xWebContext_NavigationStarting(WebView sender, WebViewNavigationStartingEventArgs args)
        {
        }

        private void xWebContext_FrameNavigationStarting(WebView sender, WebViewNavigationStartingEventArgs args)
        {

        }
    }
}
