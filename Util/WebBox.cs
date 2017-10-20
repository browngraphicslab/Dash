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
            if (data is ReferenceFieldModelController)
            {
                var reference = data as ReferenceFieldModelController;
                var dataDoc = reference.GetDocumentController(context);
                dataDoc.AddFieldUpdatedListener(reference.FieldKey,
                    delegate (DocumentController sender, DocumentController.DocumentFieldUpdatedEventArgs args)
                    {
                        if (args.Action == DocumentController.FieldUpdatedAction.Update || args.FromDelegate)
                        {
                            return;
                        }
                        BindTextSource(element, sender, args.Context, reference.FieldKey);
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
            var textData = data as TextFieldModelController;
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
            SetupTextBinding(element, docController, context);
        }
        public static FrameworkElement MakeView(DocumentController docController, Context context, Dictionary<KeyController,FrameworkElement> keysToFrameworkElementsIn = null, bool isInterfaceBuilderLayout = false)
        {
            // the document field model controller provides us with the DATA
            // the Document on this courtesty document provides us with the parameters to display the DATA.
            // X, Y, Width, and Height etc....

            ///* 
            var fieldModelController = GetDereferencedDataFieldModelController(docController, context, 
                new DocumentFieldModelController(new DocumentController(new Dictionary<KeyController, FieldControllerBase>(), TextingBox.DocumentType)), out ReferenceFieldModelController refToData);

            var textfieldModelController = fieldModelController as TextFieldModelController;
            Debug.Assert(textfieldModelController != null);

            var grid = new Grid {Background = new SolidColorBrush(Colors.Blue), Name = "webGridRoot"};
            var web = new WebView
            {
                Source = new Uri(textfieldModelController.Data),
                IsHitTestVisible = false,
                HorizontalAlignment = HorizontalAlignment.Stretch,
                VerticalAlignment = VerticalAlignment.Stretch
            };
            grid.Children.Add(web);
            var overgrid = new Grid
            {
                HorizontalAlignment = HorizontalAlignment.Stretch,
                VerticalAlignment = VerticalAlignment.Stretch,
                Background = new SolidColorBrush(Color.FromArgb(0x20, 0xff, 0xff, 0xff)),
                Name="overgrid"
            };
            grid.Children.Add(overgrid);

            SetupBindings(web, docController, context);

            // bind the text height
            var docheightController = GetHeightField(docController, context);
            if (docheightController != null)
                BindHeight(grid, docheightController);

            // bind the text width
            var docwidthController = GetWidthField(docController, context);
            if (docwidthController != null)
                BindWidth(grid, docwidthController);

            //add to key to framework element dictionary
            var reference = docController.GetField(KeyStore.DataKey) as ReferenceFieldModelController;
            if (keysToFrameworkElementsIn != null) keysToFrameworkElementsIn[reference?.FieldKey] = web;

            if (isInterfaceBuilderLayout)
            {
                return new SelectableContainer(grid, docController);
            }
            return grid;
            //*/ 

            //return new TextBox();
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
