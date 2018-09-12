using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Graphics.Imaging;
using Windows.Storage;
using Windows.Storage.Streams;
using Windows.UI;
using Windows.UI.Core;
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
        private WebView _xWebView;
        public const string BlockManipulation = "true";// bcz: block dragging of web view when it's selected by itself so that we can fully interact with its content
        public const string AllowManipulation = null;
        private DocumentController LayoutDocument = null;
        public WebBoxView(DocumentController document)
        {
            LayoutDocument = document;
            InitializeComponent();
            Loaded += WebBoxView_Loaded;
            Unloaded += WebBoxView_Unloaded;

            var binding = new FieldBinding<ImageController>
            {
                Document = LayoutDocument,
                Key = KeyStore.SettingsBackupIntervalKey,
                Mode = BindingMode.OneWay,
                Converter = UriToBitmapImageConverter.Instance
            };
            xCacheBitmap.AddFieldBinding(Image.SourceProperty, binding);
        }

        private void WebBoxView_Unloaded(object sender, RoutedEventArgs e)
        {
            SelectionManager.SelectionChanged -= SelectionManager_SelectionChangedAsync;
        }

        private void WebBoxView_Loaded(object s, RoutedEventArgs e)
        {
            SelectionManager.SelectionChanged += SelectionManager_SelectionChangedAsync;
            if (SelectionManager.GetSelectedDocs().Contains(this.GetFirstAncestorOfType<DocumentView>()) ||
                LayoutDocument.GetField<ImageController>(KeyStore.SettingsBackupIntervalKey)?.Data == null)
            {
                Unfreeze();
            }
        }
        private async void SelectionManager_SelectionChangedAsync(DocumentSelectionChangedEventArgs args)
        {
            var docView = this.GetFirstAncestorOfType<DocumentView>();

            if (SelectionManager.GetSelectedDocs().Contains(docView) && SelectionManager.GetSelectedDocs().Count == 1)
            {
                Unfreeze();
            } 
            else if (args.DeselectedViews.Contains(docView) || 
                (xCacheBitmap.Visibility == Visibility.Collapsed && SelectionManager.GetSelectedDocs().Count > 1))
            {
                Freeze();
            }
        }

        private async Task Freeze()
        {
            var rtb = new RenderTargetBitmap();
            var size = new Point(Math.Floor(ActualWidth), Math.Floor(ActualHeight));
            //var transformToVisual = _xWebView.TransformToVisual(Window.Current.Content);
            //var rect = transformToVisual.TransformBounds(new Rect(0, 0, size.X, size.Y));
            //size = new Point(rect.Width, rect.Height);
            if (size.X > 0 && size.Y > 0)
            {
                await rtb.RenderAsync(_xWebView, (int)size.X, (int)size.Y);
                var buf = await rtb.GetPixelsAsync();

                var sb = SoftwareBitmap.CreateCopyFromBuffer(buf, BitmapPixelFormat.Bgra8, rtb.PixelWidth, rtb.PixelHeight, BitmapAlphaMode.Premultiplied);
                var localFile = await ImageToDashUtil.CreateUniqueLocalFile();
                await Util.SaveSoftwareBitmapToFile(sb, localFile);
                LayoutDocument.SetField<ImageController>(KeyStore.SettingsBackupIntervalKey, new Uri(localFile.Path), true);
                xOuterGrid.Children.Remove(_xWebView);
                xCacheBitmap.Visibility = Visibility.Visible;
                if (xTextBlock != null)
                    xTextBlock.Visibility = Visibility.Collapsed;
                if (_xWebView != null)
                    _xWebView.Tag = AllowManipulation;
            } 
            else // if the web view hasn't been constructed fully yet, wait 250ms and try again
            {
                var dp = new DispatcherTimer() { Interval = new TimeSpan(0, 0, 0, 0, 250) };
                dp.Tick += (s, e) => { Freeze();  dp.Stop(); };
                dp.Start();
            }
        }

        private void Unfreeze()
        {
            if (_xWebView == null)
            {
                constructWebBrowserViewer();
            }
            if (!xOuterGrid.Children.Contains(_xWebView))
            {
                xCacheBitmap.Visibility = Visibility.Collapsed;
                xOuterGrid.Children.Add(_xWebView);
                _xWebView.Tag = BlockManipulation;
                if (xTextBlock != null)
                    xTextBlock.Visibility = Visibility.Visible;
            }
        }

        private TextBlock xTextBlock = null;

        private void constructWebBrowserViewer()
        {
            _xWebView = new WebView(WebViewExecutionMode.SeparateThread);
            _xWebView.Tag = BlockManipulation;
            var html = LayoutDocument.GetDereferencedField<HtmlController>(KeyStore.DataKey, null)?.Data;
            var htmlAddress = LayoutDocument.GetDataDocument().GetField<TextController>(KeyStore.SourceUriKey)?.Data;
            if (html.StartsWith("http"))
            {
                htmlAddress = html;
                // web.AllowedScriptNotifyUris.Add(new Uri(html)); // have to whitelist URI's to run scripts in package manifest
                _xWebView.Navigate(new Uri(html));
            } 
            else
            {
                var correctedHtml = html;
                var htmlIndex = html.ToLower().IndexOf("<html");
                if (htmlIndex != -1)
                {
                    var modHtml = html.Substring(htmlIndex, html.Length - htmlIndex);
                    correctedHtml = modHtml.Replace("<html>", "<html><head><style>img {height: auto !important;}</style></head>");
                    correctedHtml = modHtml.Replace("<HTML>", "<HTML><head><style>img {height: auto !important;}</style></head>");
                    correctedHtml = correctedHtml.Replace(" //", " http://").Replace("\"//", "\"http://");
                } 
                _xWebView.NavigateToString(html.StartsWith("http") ? html : correctedHtml);
            };

            // add a hyperlink that points to the source webpage.
            xTextBlock = new TextBlock();
            xTextBlock.VerticalAlignment = VerticalAlignment.Bottom;
            xTextBlock.Margin = new Thickness(0, 0, 0, -40);
            xTextBlock.Text = "Website from";
            try
            {
                var hyperlink = new Hyperlink() { NavigateUri = new System.Uri(htmlAddress) };
                hyperlink.Inlines.Add(new Run() { Text = " " + HtmlToDashUtil.GetTitlesUrl(htmlAddress) });

                xTextBlock.Inlines.Add(hyperlink);
                xOuterGrid.Children.Add(xTextBlock);
            } catch (Exception)
            {

            }

            _xWebView.LoadCompleted += Web_LoadCompleted;
        }

        private static void Web_LoadCompleted(object sender, Windows.UI.Xaml.Navigation.NavigationEventArgs e)
        {
            var _WebView = sender as WebView;

            _WebView.ScriptNotify -= _WebView_ScriptNotify;
            _WebView.ScriptNotify += _WebView_ScriptNotify;

            //await _WebView.InvokeScriptAsync("eval", new[] { "function x(e) { window.external.notify(e.button.toString()); } document.onmousedown=x;" });
            //await _WebView.InvokeScriptAsync("eval", new[] { "function x(e) { window.external.notify('move');  } document.onmousemove=x;" });
            //await _WebView.InvokeScriptAsync("eval", new[] { "function x(e) { window.external.notify('up');    } document.onmouseup=x;" });
            //await _WebView.InvokeScriptAsync("eval", new[] { "function x(e) { window.external.notify('enter'); } document.onpointerenter=x;" });
            //await _WebView.InvokeScriptAsync("eval", new[] { "function x(e) { window.external.notify('leave'); } document.onmouseout=x;" });
            ////await _WebView.InvokeScriptAsync("eval", new[]
            ////{"function tableToJson(table) { var data = []; var headers = []; for (var i = 0; i < table.rows[0].cells.length; i++) {headers[i] = table.rows[0].cells[i].textContent.toLowerCase().replace(' ', ''); } for (var i = 1; i < table.rows.length; i++) { var tableRow = table.rows[i]; var rowData = { }; " +
            ////"for (var j = 0; j < tableRow.cells.length; j++) { rowData[headers[j]] = tableRow.cells[j].textContent; } data.push(rowData); } return data; } window.external.notify( JSON.stringify( tableToJson( document.getElementsByTagName('table')[0]) ))"

            ////});
            
            _WebView.NavigationStarting -= Web_NavigationStarting;
            _WebView.NavigationStarting += Web_NavigationStarting;
            _WebView.NavigationCompleted -= _WebView_NavigationCompleted;
            _WebView.NavigationCompleted += _WebView_NavigationCompleted;
            _WebView_NavigationCompleted(_WebView, null);
        }

        private static void _WebView_ScriptNotify(object sender, NotifyEventArgs e)
        {
            var web = sender as WebView;
            var parent = web?.GetFirstAncestorOfType<DocumentView>();
            if (parent == null)
                return;

            //var shiftState = web.IsShiftPressed();
            //switch (e.Value as string)
            //{
            //    case "2":    //web.Tag = (string)web.Tag != WebBoxView.BlockManipulation ? new ManipulationControlHelper(web, null, shiftState, true) : web.Tag; break;  // "2" is the 2nd mouse button = "Right" button
            //    case "move": //(web.Tag as ManipulationControlHelper)?.PointerMoved(web, null);
            //                  break;
            //    case "leave": break;
            //    case "up":    if (!MainPage.Instance.IsRightBtnPressed())
            //                  {
            //                        parent.tofront();
            //                        if (documentview.focuseddocument != parent)
            //                        {
            //                            documentview.focuseddocument = parent;
            //                            parent.forcelefttapped();
            //                        }
            //                        web.Tag = (string)web.Tag == WebBoxView.BlockManipulation ? web.Tag : null;
            //                   }
            //                   break;
            //}
        }
        private static void Web_NavigationStarting(WebView sender, WebViewNavigationStartingEventArgs args)
        {
            if (args.Uri != null)
            {
                args.Cancel = true;
                if (MainPage.Instance.WebContext != null)
                    MainPage.Instance.WebContext.SetUrl(args.Uri.AbsoluteUri);
                else
                {
                    var docSize = sender.GetFirstAncestorOfType<DocumentView>().ViewModel.ActualSize;
                    var docPos = sender.GetFirstAncestorOfType<DocumentView>().ViewModel.Position;
                    var docViewPt = new Point(docPos.X + docSize.X, docPos.Y);
                    var theDoc = FileDropHelper.GetFileType(args.Uri.AbsoluteUri) == FileType.Image ? new ImageNote(args.Uri, new Point()).Document :
                        new HtmlNote(args.Uri.ToString(), args.Uri.AbsoluteUri, new Point(), new Size(200, 300)).Document;
                    Actions.DisplayDocument(sender.GetFirstAncestorOfType<CollectionView>()?.ViewModel, theDoc.GetSameCopy(docViewPt));
                }
            }
        }

        private async static void _WebView_NavigationCompleted(WebView sender, WebViewNavigationCompletedEventArgs args)
        {
            var _WebView = sender as WebView;
            await _WebView.InvokeScriptAsync("eval", new[]
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
            var webBoxView = _WebView.GetFirstAncestorOfType<WebBoxView>();
            var docview = webBoxView.GetFirstAncestorOfType<DocumentView>();
            if (!SelectionManager.GetSelectedDocs().Contains(docview) || SelectionManager.GetSelectedDocs().Count > 1) {
                webBoxView.Freeze();
            }
        }
        
    }
}
