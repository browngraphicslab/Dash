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
using Dash.Converters;

namespace Dash
{
    /// <summary>
    /// A generic document type containing a single text element.
    /// </summary>
    public class DocumentBox : CourtesyDocument
    {
        public static DocumentType DocumentType =
            new DocumentType("7C92378E-C38E-4B28-90C4-F5EF495878E5", "Document Box");
        public DocumentBox(FieldModelController refToDoc, double x = 0, double y = 0, double w = 200, double h = 20)
        {
            var fields = DefaultLayoutFields(new Point(x, y), new Size(w, h), refToDoc);
            Document = new DocumentController(fields, DocumentType);
            //SetLayoutForDocument(Document, Document);
        }
        protected static void SetupDocumentBinding(DocumentView element, DocumentController docController, Context context)
        {
            var data = docController.GetDereferencedField(KeyStore.DataKey, context);
            if (data != null)
            {
                var binding = new FieldBinding<FieldModelController>()
                {
                    Document = docController,
                    Key = KeyStore.DataKey,
                    Mode = Windows.UI.Xaml.Data.BindingMode.TwoWay,
                    Context = context,
                    GetConverter = GetFieldConverter
                };
                element.AddFieldBinding(DocumentView.DataContextProperty, binding);
            }
        }
        protected static Windows.UI.Xaml.Data.IValueConverter GetFieldConverter(FieldModelController fieldModelController)
        {
            if (fieldModelController is DocumentFieldModelController)
            {
                return new DocumentToViewModelConverter();
            }
            return null;
        }


        public static FrameworkElement MakeView(DocumentController docController, Context context, Dictionary<KeyController, FrameworkElement> keysToFrameworkElementsIn = null, bool isInterfaceBuilderLayout = false)
        {
            // the document field model controller provides us with the DATA
            // the Document on this courtesty document provides us with the parameters to display the DATA.
            // X, Y, Width, and Height etc....

            ///* 
            ReferenceFieldModelController refToData;
            var fieldModelController = GetDereferencedDataFieldModelController(docController, context, new DocumentFieldModelController(new DocumentController(new Dictionary<KeyController, FieldModelController>(), TextingBox.DocumentType)), out refToData);

            if (fieldModelController is ImageFieldModelController)
                return ImageBox.MakeView(docController, context, keysToFrameworkElementsIn, isInterfaceBuilderLayout);
            if (fieldModelController is TextFieldModelController)
                return TextingBox.MakeView(docController, context, keysToFrameworkElementsIn, isInterfaceBuilderLayout, true);
            var documentfieldModelController = fieldModelController as DocumentFieldModelController;
            Debug.Assert(documentfieldModelController != null);

            //var doc = fieldModelController.DereferenceToRoot<DocumentFieldModelController>(context);
            //var docView = new KeyValuePane();
            //docView.SetDataContextToDocumentController(documentfieldModelController.Data);
                //documentfieldModelController.Data.MakeViewUI(context, isInterfaceBuilderLayout);
            
            var docView = new DocumentView(new DocumentViewModel(documentfieldModelController.Data, isInterfaceBuilderLayout, context));

            var border = new Border();
            border.Child = docView;

            SetupDocumentBinding(docView, docController, context);

            // bind the text height
            //var docheightController = GetHeightField(docController, context);
            //if (docheightController != null)
                //BindHeight(docView, docheightController);

            // bind the text width
            //var docwidthController = GetWidthField(docController, context);
            //if (docwidthController != null)
                //BindWidth(docView, docwidthController);

            if (isInterfaceBuilderLayout)
            {
                return new SelectableContainer(border, docController);
            }
            return border;
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