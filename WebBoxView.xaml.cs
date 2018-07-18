using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

// The User Control item template is documented at https://go.microsoft.com/fwlink/?LinkId=234236

namespace Dash
{
    public sealed partial class WebBoxView
    {
        private readonly WebView _xWebView;

        public WebBoxView()
        {
            InitializeComponent();
            _xWebView = new WebView(WebViewExecutionMode.SeparateThread);
            xPanel.Children.Add(_xWebView);
            _xWebView.Visibility = Visibility.Collapsed;
            _xWebView.LoadCompleted += delegate { _xWebView.Visibility = Visibility.Visible; };
        }

        public WebView GetView() => _xWebView;
    }
}
