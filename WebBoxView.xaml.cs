using System;
using Windows.Foundation;
using Windows.Graphics.Imaging;
using Windows.UI;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Documents;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Imaging;
using Windows.UI.Xaml.Navigation;
using Windows.UI.Xaml.Shapes;
using Image = Windows.UI.Xaml.Controls.Image;

// The User Control item template is documented at https://go.microsoft.com/fwlink/?LinkId=234236

namespace Dash
{
    public sealed partial class WebBoxView
    {
        readonly WebView _xWebView;
        Image _bitmapImage = new Image();
        public const string BlockManipulation = "true";// bcz: block dragging of web view when it's selected by itself so that we can fully interact with its content
        public const string AllowManipulation = null;
        public WebBoxView()
        {
            InitializeComponent();
            _xWebView = new WebView(WebViewExecutionMode.SeparateThread) { Visibility = Visibility.Collapsed };
            _xWebView.CacheMode = new BitmapCache();
            _xWebView.Tag = BlockManipulation;
            _xWebView.LoadCompleted += (s, e) => _xWebView.Visibility = Visibility.Visible;
            xOuterGrid.Children.Add(_xWebView);
            Loaded   += (s,e) => SelectionManager.SelectionChanged += SelectionManager_SelectionChangedAsync;
            Unloaded += (s, e) => SelectionManager.SelectionChanged -= SelectionManager_SelectionChangedAsync;
        }

        private async void SelectionManager_SelectionChangedAsync(DocumentSelectionChangedEventArgs args)
        {
            var docView = this.GetFirstAncestorOfType<DocumentView>();

            if (SelectionManager.GetSelectedDocs().Contains(docView) && SelectionManager.GetSelectedDocs().Count == 1)
            {
                if (xOuterGrid.Children.Contains(_bitmapImage) && !xOuterGrid.Children.Contains(_xWebView))
                {
                    xOuterGrid.Children.Remove(_bitmapImage);
                    xOuterGrid.Children.Add(_xWebView);
                    _xWebView.Tag = BlockManipulation; 
                }
            }
            else if (args.DeselectedViews.Contains(docView) || (SelectionManager.GetSelectedDocs().Count > 1 && _xWebView.Tag is string))
            {
                if (!xOuterGrid.Children.Contains(_bitmapImage) && xOuterGrid.Children.Contains(_xWebView))
                {
                    var rtb = new RenderTargetBitmap();
                    var s = new Point(Math.Floor(_xWebView.ActualWidth), Math.Floor(_xWebView.ActualHeight));
                    var transformToVisual = _xWebView.TransformToVisual(Window.Current.Content);
                    var rect = transformToVisual.TransformBounds(new Rect(0, 0, s.X, s.Y));
                    s = new Point(rect.Width, rect.Height);
                    if (s.X > 0 && s.Y > 0)
                    {
                        await rtb.RenderAsync(_xWebView, (int)s.X, (int)s.Y);
                        var buf = await rtb.GetPixelsAsync();
                        var sb = SoftwareBitmap.CreateCopyFromBuffer(buf, BitmapPixelFormat.Bgra8, rtb.PixelWidth, rtb.PixelHeight, BitmapAlphaMode.Premultiplied);
                        var source = new SoftwareBitmapSource();
                        await source.SetBitmapAsync(sb);
                        _bitmapImage = new Image
                        {
                            Source = source,
                            VerticalAlignment = VerticalAlignment.Stretch,
                            HorizontalAlignment = HorizontalAlignment.Stretch
                        };
                        xOuterGrid.Children.Add(_bitmapImage);
                    }
                    xOuterGrid.Children.Remove(_xWebView);
                    _xWebView.Tag = AllowManipulation;
                }
            }
        }

        public WebView GetView() => _xWebView;

        public void SetText(string url)
        {
            if (!String.IsNullOrEmpty(url))
            {
                var headline = HtmlToDashUtil.GetTitlesUrl(url);
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
