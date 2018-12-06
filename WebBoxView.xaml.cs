using System;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Graphics.Imaging;
using Windows.Storage.Streams;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media.Imaging;
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

            var text = document.GetDataDocument().GetField<DateTimeController>(KeyStore.DateCreatedKey).Data
                           .ToString("g") + " | Navigated to " + document.GetDataDocument()
                           .GetDereferencedField<HtmlController>(KeyStore.DataKey, null);

            if (EventManager.HasEvent(text))
            {
                return;
            }

            var eventDoc = new RichTextNote(text).Document;
            var tags = "website, ";
            var splitBySlash = (document.GetDataDocument().GetDereferencedField<HtmlController>(KeyStore.DataKey, null)?
                                    .Data
                                ?? document.Title).Split("/", StringSplitOptions.RemoveEmptyEntries);
            if (splitBySlash.Length >= 2)
            {
                tags += splitBySlash[1];
            }

            eventDoc.GetDataDocument().SetField<TextController>(KeyStore.EventTagsKey, tags, true);

            Loaded += ContainerHandler;

            void ContainerHandler(object sender, RoutedEventArgs args)
            {
                var containerDoc = this.GetFirstAncestorOfType<DocumentView>().ParentCollection?.ViewModel.ContainerDocument;
                if (containerDoc != null) {
                    eventDoc.GetDataDocument().SetField(KeyStore.EventCollectionKey, containerDoc, true);
                }
                Loaded -= ContainerHandler;
            }

            var copy = document.GetCopy();
            copy.SetHeight(150);
            copy.SetWidth(500);
            eventDoc.SetField(KeyStore.EventDisplay1Key, copy, true);
            var displayXaml =
                @"<Grid
                    xmlns=""http://schemas.microsoft.com/winfx/2006/xaml/presentation""
                    xmlns:x=""http://schemas.microsoft.com/winfx/2006/xaml""
                    xmlns:dash=""using:Dash""
                    xmlns:mc=""http://schemas.openxmlformats.org/markup-compatibility/2006"">
                    <Grid.RowDefinitions>
                        <RowDefinition Height=""Auto""></RowDefinition>
                        <RowDefinition Height=""*""></RowDefinition>
                        <RowDefinition Height=""*""></RowDefinition>
                    </Grid.RowDefinitions>
                    <Border BorderThickness=""2"" BorderBrush=""CadetBlue"" Background=""White"">
                        <TextBlock x:Name=""xTextFieldData"" HorizontalAlignment=""Stretch"" Height=""Auto"" VerticalAlignment=""Top""/>
                    </Border>
                    <StackPanel Orientation=""Horizontal"" Grid.Row=""2"">
                        <dash:DocumentView x:Name=""xDocumentField_EventDisplay1Key""
                            Foreground=""White"" HorizontalAlignment=""Stretch"" Grid.Row=""2""
                            VerticalAlignment=""Top"" />
                    </StackPanel>
                    </Grid>";
            EventManager.EventOccured(eventDoc, displayXaml);
        }

        private void WebBoxView_Unloaded(object sender, RoutedEventArgs e)
        {
            SelectionManager.SelectionChanged -= SelectionManager_SelectionChangedAsync;
        }

        private void WebBoxView_Loaded(object s, RoutedEventArgs e)
        {
            SelectionManager.SelectionChanged += SelectionManager_SelectionChangedAsync;
            if (SelectionManager.GetSelectedDocs().Contains(this.GetFirstAncestorOfType<DocumentView>()) ||
                xWebViewRectangleBrush.Fill == null)
            {
                Unfreeze();
            }
        }
        private void SelectionManager_SelectionChangedAsync(DocumentSelectionChangedEventArgs args)
        {
            var docView = this.GetFirstAncestorOfType<DocumentView>();

            if (args.SelectedViews.Contains(docView) && SelectionManager.GetSelectedDocs().Contains(docView) && SelectionManager.GetSelectedDocs().Count == 1)
            {
                var dt = new DispatcherTimer();
                dt.Interval = new TimeSpan(0, 0, 0, 0, 200);
                dt.Tick += (ss, ee) => { dt.Stop(); Unfreeze(); };
                dt.Start();
            }
            else if (_xWebView != null && ((args.DeselectedViews.Contains(docView) ||
                (xWebViewRectangleBrush.Visibility == Visibility.Collapsed && SelectionManager.GetSelectedDocs().Count > 1))))
            {
                Freeze();
            }
        }

        private void Freeze()
        {
            var b = new WebViewBrush();
            b.SourceName = "_xWebView";
            b.Redraw();
            xWebViewRectangleBrush.Fill = b;

            xWebViewRectangleBrush.Visibility = Visibility.Visible;
            _xWebView.Visibility = Visibility.Collapsed;
            if (xTextBlock != null)
                xTextBlock.Visibility = Visibility.Collapsed;
            if (_xWebView != null)
                _xWebView.Tag = AllowManipulation;
        }

        private void Unfreeze()
        {
            if (_xWebView == null)
            { 
                constructWebBrowserViewer();
                xOuterGrid.Children.Add(_xWebView);
            }
            if (_xWebView.Visibility == Visibility.Collapsed)
            {
                xWebViewRectangleBrush.Visibility = Windows.UI.Xaml.Visibility.Collapsed;
                _xWebView.Visibility = Visibility.Visible;
                if (xTextBlock != null)
                    xTextBlock.Visibility = Visibility.Visible;
            }
        }

        private TextBlock xTextBlock = null;

        private void constructWebBrowserViewer()
        {
            _xWebView = new WebView(WebViewExecutionMode.SeparateThread);
            _xWebView.Name = "_xWebView";
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
            }

            _xWebView.LoadCompleted += Web_LoadCompleted;
        }

        private static async void Web_LoadCompleted(object sender, Windows.UI.Xaml.Navigation.NavigationEventArgs e)
        {
            var _WebView = sender as WebView;

            _WebView.ScriptNotify -= _WebView_ScriptNotify;
            _WebView.ScriptNotify += _WebView_ScriptNotify;

            if (double.IsNaN(_WebView.GetFirstAncestorOfType<DocumentView>()?.ViewModel?.LayoutDocument.GetWidth() ?? double.NaN))
            {
                await _WebView.InvokeScriptAsync("eval", new[] { "window.external.notify(document.body.scrollWidth.toString() + ' ' + document.body.scrollHeight.toString());" });
            }
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

            var splits = (e.Value as string).Split(' ');
            var x = Math.Max(200, double.Parse(splits[0]));
            var y = Math.Min(500, double.Parse(splits[1]));
            parent.ViewModel.LayoutDocument.SetWidth(x);
            parent.ViewModel.LayoutDocument.SetHeight(y);
            web.UpdateLayout();

            //parent.ViewModel?.LayoutDocument.SetWidth(x);
            //parent.ViewModel?.LayoutDocument.SetHeight(y);

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
            var docview = webBoxView?.GetFirstAncestorOfType<DocumentView>();
            if (!SelectionManager.GetSelectedDocs().Contains(docview) || SelectionManager.GetSelectedDocs().Count > 1)
            {
                webBoxView?.Freeze();
            }
        }
    }
}
