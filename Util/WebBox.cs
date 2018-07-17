using System;
using System.Collections.Generic;
using System.Diagnostics;
using Windows.Foundation;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using DashShared;
using Windows.UI.Xaml.Data;
using Windows.UI.Core;
using Windows.System;

namespace Dash
{    /// <summary>
     /// A generic document type containing a single text element.
     /// </summary>
    public class WebBox : CourtesyDocument
    {
        public static DocumentType DocumentType = new DocumentType("1C17B38F-C9DC-465D-AC3E-43EA105D18C6", "Web Box");
        private static readonly string PrototypeId = "9190B041-CC40-4B32-B99B-E7A1CDE3C1C9";
        public WebBox(FieldControllerBase refToDoc, double x = 0, double y = 0, double w = 200, double h = 20)
        {
            var fields = DefaultLayoutFields(new Point(x, y), new Size(w, h), refToDoc);
            SetupDocument(DocumentType, PrototypeId, "WebBox Prototype Layout", fields);
        }
        protected static void SetupTextBinding(FrameworkElement element, DocumentController controller, Context context)
        {
            var data = controller.GetField(KeyStore.DataKey);
            if (data is ReferenceController)
            {
                var reference = data as ReferenceController;
                var dataDoc = reference.GetDocumentController(context);
                dataDoc.AddFieldUpdatedListener(reference.FieldKey,
                    delegate (DocumentController sender, DocumentController.DocumentFieldUpdatedEventArgs args, Context context1)
                    {
                        DocumentController doc = sender;
                        var dargs = args;
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

        public static FrameworkElement MakeView(DocumentController docController, Context context)
        {
            // the document field model controller provides us with the DATA
            // the Document on this courtesty document provides us with the parameters to display the DATA.
            // X, Y, Width, and Height etc....

            var fieldModelController = GetDereferencedDataFieldModelController(docController, context, 
                new DocumentController(new Dictionary<KeyController, FieldControllerBase>(), TextingBox.DocumentType), out ReferenceController refToData);

            var textfieldModelController = fieldModelController as TextController;
            Debug.Assert(textfieldModelController != null);

            var webView = new WebBoxView();
            var web = webView.GetView();
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

            SetupBindings(web, docController, context);
            if (html == null)
                SetupTextBinding(web, docController, context);
            
            return webView;
        }

        private static async void Web_LoadCompleted(object sender, Windows.UI.Xaml.Navigation.NavigationEventArgs e)
        {
            var _WebView = sender as WebView;

            _WebView.ScriptNotify -= _WebView_ScriptNotify;
            _WebView.ScriptNotify += _WebView_ScriptNotify;

            await _WebView.InvokeScriptAsync("eval", new[] { "function x(e) { window.external.notify(e.button.toString()); } document.onmousedown=x;" });
            await _WebView.InvokeScriptAsync("eval", new[] { "function x(e) { window.external.notify('move');  } document.onmousemove=x;" });
            await _WebView.InvokeScriptAsync("eval", new[] { "function x(e) { window.external.notify('up');    } document.onmouseup=x;" });
            await _WebView.InvokeScriptAsync("eval", new[] { "function x(e) { window.external.notify('enter'); } document.onpointerenter=x;" });
            await _WebView.InvokeScriptAsync("eval", new[] { "function x(e) { window.external.notify('leave'); } document.onmouseout=x;" });
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
            var parent = web?.GetFirstAncestorOfType<DocumentView>();
            if (parent == null)
                return;

            var shiftState = web.IsShiftPressed();
            switch (e.Value as string)
            {
                case "2":    web.Tag = new ManipulationControlHelper(web, null, shiftState, true); break;  // "2" is the 2nd mouse button = "Right" button
                case "move": parent.DocumentView_PointerEntered(null, null);
                             (web.Tag as ManipulationControlHelper)?.PointerMoved(web, null); break;
                case "leave": if (!parent.IsPointerOver())
                                   parent.DocumentView_PointerExited(null, null);
                              break;
                case "up":    parent.ToFront();
                              if (DocumentView.FocusedDocument != parent)
                                  parent.ForceLeftTapped();
                              (web.Tag as ManipulationControlHelper)?.PointerReleased(web, null);
                              web.Tag = null; break;
            }
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
    }
}
