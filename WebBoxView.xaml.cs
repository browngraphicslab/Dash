using System;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Documents;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Imaging;
using Windows.UI.Xaml.Navigation;

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

        public void SetText(string url)
        {
            if (!String.IsNullOrEmpty(url))
            {
                var headline = CollectionViewModel.GetTitlesUrl(url);
                Run run = new Run() { Text = " " +  headline };

                Hyperlink hyperlink = new Hyperlink()
                {
                    NavigateUri = new System.Uri(url)
                };
                hyperlink.Inlines.Add(run);
               
               TextBlock.Visibility = Visibility.Visible;
               TextBlock.Inlines.Add(hyperlink);
            }
        }
    }
}
