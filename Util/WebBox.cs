using System;
using System.Collections.Generic;
using System.Diagnostics;
using Windows.Foundation;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;
using Dash;
using DashShared;
using Windows.UI.Xaml.Media;
using Windows.UI;
using Windows.UI.Xaml.Data;
using System.Numerics;
using System.Net.Http;
using System.Threading.Tasks;
using System.Linq;

namespace Dash
{    /// <summary>
     /// A generic document type containing a single text element.
     /// </summary>
    public class WebBox : CourtesyDocument
    {
        public static DocumentType DocumentType =
            new DocumentType("1C17B38F-C9DC-465D-AC3E-43EA105D18C6", "Web Box");
        public WebBox(FieldControllerBase refToDoc, double x = 0, double y = 0, double w = 200, double h = 20)
        {
            var fields = DefaultLayoutFields(new Point(x, y), new Size(w, h), refToDoc);
            Document = new DocumentController(fields, DocumentType);
            //SetLayoutForDocument(Document, Document);
        }
        protected static void SetupTextBinding(FrameworkElement element, DocumentController controller, Context context)
        {
            var data = controller.GetField(KeyStore.DataKey);
            if (data is ReferenceController)
            {
                var reference = data as ReferenceController;
                var dataDoc = reference.GetDocumentController(context);
                dataDoc.AddFieldUpdatedListener(reference.FieldKey,
                    delegate (FieldControllerBase sender, FieldUpdatedEventArgs args, Context context1)
                    {
                        DocumentController doc = (DocumentController) sender;
                        var dargs = (DocumentController.DocumentFieldUpdatedEventArgs) args;
                        if (args.Action == DocumentController.FieldUpdatedAction.Update || dargs.FromDelegate)
                        {
                            return;
                        }
                        BindTextSource(element, doc, context1, reference.FieldKey);
                    });
            }
            BindTextSource(element, controller, context, KeyStore.DataKey);
        }
        protected static void BindTextSource(FrameworkElement element, DocumentController docController, Context context, KeyController key)
        {
            var data = docController.GetDereferencedField(key, context);
            if (data == null)
            {
                return;
            }
            var textData = data as TextController;
            var sourceBinding = new Binding
            {
                Source = textData,
                Path = new PropertyPath(nameof(textData.Data)),
                Mode = BindingMode.TwoWay,
                UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged
            };
            if (sourceBinding != null)
                element.SetBinding(WebView.SourceProperty, sourceBinding);
        }


        protected new static void SetupBindings(FrameworkElement element, DocumentController docController, Context context)
        {
            CourtesyDocument.SetupBindings(element, docController, context);
            SetupTextBinding(element, docController, context);
        }
        public static FrameworkElement MakeView(DocumentController docController, Context context, Dictionary<KeyController,FrameworkElement> keysToFrameworkElementsIn = null)
        {
            // the document field model controller provides us with the DATA
            // the Document on this courtesty document provides us with the parameters to display the DATA.
            // X, Y, Width, and Height etc....

            ///* 
            var fieldModelController = GetDereferencedDataFieldModelController(docController, context, 
                new DocumentController(new Dictionary<KeyController, FieldControllerBase>(), TextingBox.DocumentType), out ReferenceController refToData);

            var textfieldModelController = fieldModelController as TextController;
            Debug.Assert(textfieldModelController != null);

            var grid = new Grid {Background = new SolidColorBrush(Colors.Transparent), Name = "webGridRoot"};
            var web = new WebView
            {
                IsHitTestVisible = true,
                HorizontalAlignment = HorizontalAlignment.Stretch,
                VerticalAlignment = VerticalAlignment.Stretch,
               
            };
            var html = docController.GetDereferencedField<TextController>(KeyStore.DataKey, context)?.Data;
            if (html != null)
                if (html.StartsWith("http"))
                {
                    // web.AllowedScriptNotifyUris.Add(new Uri(html)); // have to whitelist URI's to run scripts in package manifest
                    web.Navigate(new Uri(html));
                }
                else
                {
                    var modHtml = html.Substring(html.ToLower().IndexOf("<html"), html.Length - html.ToLower().IndexOf("<html"));
                    var correctedHtml = modHtml.Replace("<html>", "<html><head><style>img {height: auto !important;}</style></head>");
                    web.NavigateToString(html.StartsWith("http") ? html : correctedHtml);
                }
            else web.Source = new Uri(textfieldModelController.Data);
            web.LoadCompleted += Web_LoadCompleted;
            grid.Children.Add(web);
            var overgrid = new Grid
            {
                HorizontalAlignment = HorizontalAlignment.Stretch,
                VerticalAlignment = VerticalAlignment.Stretch,
                Background = new SolidColorBrush(Color.FromArgb(0x20, 0xff, 0xff, 0xff)),
                Name = "overgrid"
                ,IsHitTestVisible = false
            };
            grid.Children.Add(overgrid);
            web.Tag = new Tuple<Point,bool,Point>(new Point(), false, new Point()); // hack for allowing web page to be dragged with right mouse button

            if (html == null)
                SetupBindings(web, docController, context);

            //add to key to framework element dictionary
            var reference = docController.GetField(KeyStore.DataKey) as ReferenceController;
            if (keysToFrameworkElementsIn != null) keysToFrameworkElementsIn[reference?.FieldKey] = web;
            
            return grid;
        }

        //public static async void getHtml(Uri url, WebView web)
        //{
        //    var MobileUserAgent = "Mozilla/5.0 (iPhone; U; CPU like Mac OS X; en) AppleWebKit/420+ (KHTML, like Gecko) Version/3.0 Mobile/1A543a Safari/419.3";
        //    var handler = new HttpClientHandler { AllowAutoRedirect = true };
        //    var client = new HttpClient(handler);
        //   // client.DefaultRequestHeaders.Add("user-agent", MobileUserAgent);
        //    var response = await client.GetAsync(url);
        //    response.EnsureSuccessStatusCode();
        //    var html = await response.Content.ReadAsStringAsync();

        //    var modHtml = html.Substring(html.ToLower().IndexOf("<html"), html.Length - html.ToLower().IndexOf("<html"));
        //    var correctedHtml = modHtml.Replace("<html>", "<html><head><style>img {height: auto !important;}</style></head>");
        //    web.NavigateToString(html.StartsWith("http") ? html : correctedHtml);
        //}
        private static async void Web_LoadCompleted(object sender, Windows.UI.Xaml.Navigation.NavigationEventArgs e)
        {
            var _WebView = sender as WebView;

            _WebView.ScriptNotify -= _WebView_ScriptNotify;
            _WebView.ScriptNotify += _WebView_ScriptNotify;
            
            await _WebView.InvokeScriptAsync("eval", new[] { "function x(e) { window.external.notify(e.button.toString()); } document.onmousedown=x;" });
            await _WebView.InvokeScriptAsync("eval", new[] { "function x(e) { window.external.notify('move'); } document.onmousemove=x;" });
            await _WebView.InvokeScriptAsync("eval", new[] { "function x(e) { window.external.notify('up'); } document.onmouseup=x;" });
            //await _WebView.InvokeScriptAsync("eval", new[]
            //{"function tableToJson(table) { var data = []; var headers = []; for (var i = 0; i < table.rows[0].cells.length; i++) {headers[i] = table.rows[0].cells[i].textContent.toLowerCase().replace(' ', ''); } for (var i = 1; i < table.rows.length; i++) { var tableRow = table.rows[i]; var rowData = { }; " +
            //"for (var j = 0; j < tableRow.cells.length; j++) { rowData[headers[j]] = tableRow.cells[j].textContent; } data.push(rowData); } return data; } window.external.notify( JSON.stringify( tableToJson( document.getElementsByTagName('table')[0]) ))"

            //});


            _WebView.NavigationStarting -= Web_NavigationStarting;
            _WebView.NavigationStarting += Web_NavigationStarting;
            _WebView.NavigationCompleted -= _WebView_NavigationCompleted;
            _WebView.NavigationCompleted += _WebView_NavigationCompleted;
            _WebView_NavigationCompleted(_WebView, null);
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
        }
        // document.getElementsByTagName('table')
        //tableToJson = function(table)
        //{
        //    var data = [];

        //    // first row needs to be headers
        //    var headers = [];
        //    for (var i = 0; i < table.rows[0].cells.length; i++)
        //    {
        //        headers[i] = table.rows[0].cells[i].textContent.toLowerCase().replace(/ / gi, '');
        //    }

        //    // go through cells
        //    for (var i = 1; i < table.rows.length; i++)
        //    {

        //        var tableRow = table.rows[i];
        //        var rowData = { };

        //        for (var j = 0; j < tableRow.cells.length; j++)
        //        {

        //            rowData[headers[j]] = tableRow.cells[j].textContent;

        //        }

        //        data.push(rowData);
        //    }

        //    return data;
        //}
        private static void _WebView_ScriptNotify(object sender, NotifyEventArgs e)
        {
            var web = sender as WebView;
            var down_and_right_and_last = (Tuple<Point,bool, Point>)web.Tag;
            var down = down_and_right_and_last.Item1;
            var right = down_and_right_and_last.Item2;
            var last = down_and_right_and_last.Item3;
            var parent = web.GetFirstAncestorOfType<DocumentView>();
            if (parent == null)
                return;
            var pointerPosition = MainPage.Instance.TransformToVisual(parent.GetFirstAncestorOfType<ContentPresenter>()).TransformPoint(Windows.UI.Core.CoreWindow.GetForCurrentThread().PointerPosition);

            if (e.Value == "0")
            {
                parent.DocumentView_PointerEntered(null, null);
                var docView = web.GetFirstAncestorOfType<DocumentView>();
                docView?.ToFront();
                web.Tag = new Tuple<Point, bool, Point>(new Point(), false, new Point());
            } else if (e.Value == "2") // right mouse button == 2
            {
                var docView = web.GetFirstAncestorOfType<DocumentView>();
                docView?.ToFront();
                //var rt = parent.RenderTransform.TransformPoint(new Point());
                var rt = new Point();
                web.Tag = new Tuple<Point, bool, Point>(pointerPosition, true, pointerPosition);
                parent.ManipulationControls?.ElementOnManipulationStarted(null, null);
                parent.DocumentView_PointerEntered(null, null);
            }
            else if (e.Value == "move" && right)
            {
                var parentCollectionTransform =
                    ((web.GetFirstAncestorOfType<CollectionView>()?.CurrentView as CollectionFreeformView)?.xItemsControl.ItemsPanelRoot as Canvas)?.RenderTransform as MatrixTransform;
                if (parentCollectionTransform == null) return;

                var translation = new Point(pointerPosition.X - last.X, pointerPosition.Y - last.Y);

                translation.X *= parentCollectionTransform.Matrix.M11;
                translation.Y *= parentCollectionTransform.Matrix.M22;

                last = pointerPosition;
                if (parent.ManipulationControls != null)
                {
                    parent.ManipulationControls.TranslateAndScale(new
                        ManipulationDeltaData(new Point(pointerPosition.X, pointerPosition.Y),
                            translation,
                            1.0f), parent.ManipulationControls._grouping);

                    //Only preview a snap if the grouping only includes the current node. TODO: Why is _grouping public?
                    if (parent.ManipulationControls._grouping == null || parent.ManipulationControls._grouping.Count < 2)
                        parent.ManipulationControls.Snap(true);
                }
                web.Tag = new Tuple<Point, bool, Point>(down, right, last);
            }
            else if (e.Value == "move")
            {
                parent.DocumentView_PointerEntered(null, null);
            }
            else if (e.Value == "up")
            {
                web.Tag = new Tuple<Point, bool, Point>(new Point(), false, new Point());
                if (Math.Sqrt((pointerPosition.X - down.X) * (pointerPosition.X - down.X) + (pointerPosition.Y - down.Y) * (pointerPosition.Y - down.Y)) < 8)
                {
                    if (right)
                        parent.RightTap();
                    parent.OnTapped(null, null);
                }
                else
                {
                    parent.OnTapped(null, null);
                    parent.ManipulationControls?.ElementOnManipulationCompleted(null, null);
                }

                parent.DocumentView_PointerExited(null, null);
                parent.DocumentView_ManipulationCompleted(null, null);

                // web.InvokeScriptAsync("eval", new[] { "window.external.notify(window.scrollY.toString()); " });

                //web.InvokeScriptAsync("eval", new[] { "window.scrollTo(0, 572); " });
                // web.InvokeScriptAsync("eval", new[] { "window.open('http://www.msn.com', window.name, '');" });
            }
        }

        private static void Web_NavigationStarting(WebView sender, WebViewNavigationStartingEventArgs args)
        {
            if (args.Uri != null)
            {
                args.Cancel = true;
                MainPage.Instance.WebContext?.SetUrl(args.Uri.AbsoluteUri);
            }
        }

        protected override DocumentController GetLayoutPrototype()
        {
            throw new NotImplementedException();
        }

        protected override DocumentController InstantiatePrototypeLayout()
        {
            throw new NotImplementedException();
        }
    }
}
