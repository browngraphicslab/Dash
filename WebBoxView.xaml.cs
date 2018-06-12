using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
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

// The User Control item template is documented at https://go.microsoft.com/fwlink/?LinkId=234236

namespace Dash
{
    public sealed partial class WebBoxView : UserControl
    {
        //public RenderTargetBitmap Preview
        //{
        //    get
        //    {
        //        GetPreview();
        //        return _screenCap;
        //    }
        //}

        //private RenderTargetBitmap _screenCap;
        public WebBoxView()
        {
            this.InitializeComponent();
        }

        public async Task<WebViewBrush> GetPreview()
        {
            // TODO: rect not rendered
            //xRect.Width = xGrid.ActualWidth;
            //xRect.Height = xGrid.ActualHeight;
            WebViewBrush b = new WebViewBrush();
            b.SourceName = xWebView.Name;
            b.Redraw();
            //xRect.Fill = b;
            //xWebView.Visibility = Visibility.Collapsed;
            //RenderTargetBitmap bitmap = new RenderTargetBitmap();
            //await bitmap.RenderAsync(xGrid);
            //xRect.Fill = new SolidColorBrush();
            return b;
        }

        //public async Task<WebViewBrush> GetWebViewBrush(WebView webView)
        //{
        //    // resize width to content
        //    double originalWidth = webView.Width;
        //    var widthString = await webView.InvokeScriptAsync("eval", new[] { "document.body.scrollWidth.toString()" });
        //    int contentWidth;

        //    if (!int.TryParse(widthString, out contentWidth))
        //    {
        //        throw new Exception(string.Format("failure/width:{0}", widthString));
        //    }

        //    webView.Width = contentWidth;

        //    // resize height to content
        //    double originalHeight = webView.Height;
        //    var heightString = await webView.InvokeScriptAsync("eval", new[] { "document.body.scrollHeight.toString()" });
        //    int contentHeight;

        //    if (!int.TryParse(heightString, out contentHeight))
        //    {
        //        throw new Exception(string.Format("failure/height:{0}", heightString));
        //    }

        //    webView.Height = contentHeight;

        //    // create brush
        //    var originalVisibilty = webView.Visibility;
        //    webView.Visibility = Windows.UI.Xaml.Visibility.Visible;

        //    WebViewBrush brush = new WebViewBrush
        //    {
        //        SourceName = webView.Name,
        //        Stretch = Stretch.Uniform
        //    };

        //    brush.Redraw();

        //    // reset, return
        //    webView.Width = originalWidth;
        //    webView.Height = originalHeight;
        //    webView.Visibility = originalVisibilty;

        //    return brush;
        //}

        public WebView GetView()
        {
            return xWebView;
        }
    }
}
