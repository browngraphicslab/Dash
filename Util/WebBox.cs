using System;
using System.Collections.Generic;
using System.Diagnostics;
using Windows.Foundation;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Dash;
using DashShared;
using Windows.UI.Xaml.Media;
using Windows.UI;
using Windows.UI.Xaml.Data;


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
        public static FrameworkElement MakeView(DocumentController docController, Context context, Dictionary<KeyController,FrameworkElement> keysToFrameworkElementsIn = null, bool isInterfaceBuilderLayout = false)
        {
            // the document field model controller provides us with the DATA
            // the Document on this courtesty document provides us with the parameters to display the DATA.
            // X, Y, Width, and Height etc....

            ///* 
            var fieldModelController = GetDereferencedDataFieldModelController(docController, context, 
                new DocumentController(new Dictionary<KeyController, FieldControllerBase>(), TextingBox.DocumentType), out ReferenceController refToData);

            var textfieldModelController = fieldModelController as TextController;
            Debug.Assert(textfieldModelController != null);

            var grid = new Grid {Background = new SolidColorBrush(Colors.Blue), Name = "webGridRoot"};
            var web = new WebView
            {
                IsHitTestVisible = true,
                HorizontalAlignment = HorizontalAlignment.Stretch,
                VerticalAlignment = VerticalAlignment.Stretch
            };
            web.NavigationStarting += Web_NavigationStarting;
            var html = docController.GetDereferencedField<TextController>(KeyStore.DataKey, context)?.Data;
            if (html != null)
                web.NavigateToString(html.Substring(html.ToLower().IndexOf("<html"), html.Length-html.ToLower().IndexOf("<html")));
            else web.Source = new Uri(textfieldModelController.Data);

            grid.Children.Add(web);
            var overgrid = new Grid
            {
                HorizontalAlignment = HorizontalAlignment.Stretch,
                VerticalAlignment = VerticalAlignment.Stretch,
                Background = new SolidColorBrush(Color.FromArgb(0x20, 0xff, 0xff, 0xff)),
                Name = "overgrid"
            };
          //  grid.Children.Add(overgrid);

            if (html == null)
                SetupBindings(web, docController, context);

            //add to key to framework element dictionary
            var reference = docController.GetField(KeyStore.DataKey) as ReferenceController;
            if (keysToFrameworkElementsIn != null) keysToFrameworkElementsIn[reference?.FieldKey] = web;

            if (isInterfaceBuilderLayout)
            {
                return new SelectableContainer(grid, docController);
            }
            return grid;
        }

        private static async void Web_NavigationStarting(WebView sender, WebViewNavigationStartingEventArgs args)
        {
            if (args.Uri != null)
            {
                args.Cancel = true;
                await Windows.System.Launcher.LaunchUriAsync(args.Uri);


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
