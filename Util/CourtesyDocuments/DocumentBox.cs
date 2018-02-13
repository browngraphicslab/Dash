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
using Dash.Views.Document_Menu;

namespace Dash
{
    /// <summary>
    /// A generic document type containing a single text element.
    /// </summary>
    public class DocumentBox : CourtesyDocument
    {
        public static DocumentType DocumentType =
            new DocumentType("7C92378E-C38E-4B28-90C4-F5EF495878E5", "Document Box");
        public DocumentBox(FieldControllerBase refToDoc, double x = 0, double y = 0, double w = 200, double h = 20)
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
                Debug.Fail("Why are we here");
                //TODO tfs: This doesn't work anymore
                //var binding = new FieldBinding<FieldControllerBase>()
                //{
                //    Document = docController,
                //    Key = KeyStore.DataKey,
                //    Mode = Windows.UI.Xaml.Data.BindingMode.TwoWay,
                //    Context = context,
                //    GetConverter = GetFieldConverter
                //};
                //element.AddFieldBinding(DocumentView.DataContextProperty, binding);
            }
        }

        //public class DocumentToViewModelConverter : SafeDataToXamlConverter<DocumentController, DocumentViewModel>
        //{
        //    public DocumentToViewModelConverter()
        //    {
        //    }


        //    public override DocumentViewModel ConvertDataToXaml(DocumentController data, object parameter = null)
        //    {
        //        return new DocumentViewModel(data);
        //    }

        //    public override DocumentController ConvertXamlToData(DocumentViewModel xaml, object parameter = null)
        //    {
        //        return xaml.DocumentController;
        //    }
        //}
        //protected static Windows.UI.Xaml.Data.IValueConverter GetFieldConverter(FieldControllerBase fieldModelController)
        //{
        //    if (fieldModelController is DocumentController)
        //    {
        //        return new DocumentToViewModelConverter();
        //    }
        //    return null;
        //}


        public static FrameworkElement MakeView(DocumentController docController, Context context, Dictionary<KeyController, FrameworkElement> keysToFrameworkElementsIn = null)
        {
            // the document field model controller provides us with the DATA
            // the Document on this courtesty document provides us with the parameters to display the DATA.
            // X, Y, Width, and Height etc....

            ///* 
            ReferenceController refToData;
            var fieldModelController = GetDereferencedDataFieldModelController(docController, context, new DocumentController(new Dictionary<KeyController, FieldControllerBase>(), TextingBox.DocumentType), out refToData);

            if (fieldModelController is ImageController)
                return ImageBox.MakeView(docController, context, keysToFrameworkElementsIn);
            if (fieldModelController is TextController)
                return TextingBox.MakeView(docController, context, keysToFrameworkElementsIn, true);
            var documentfieldModelController = fieldModelController as DocumentController;
            Debug.Assert(documentfieldModelController != null);

            //var doc = fieldModelController.DereferenceToRoot<DocumentFieldModelController>(context);
            //var docView = new KeyValuePane();
            //docView.SetDataContextToDocumentController(documentfieldModelController.Data);
            //documentfieldModelController.Data.MakeViewUI(context);

            var docView = new DocumentView(new DocumentViewModel(documentfieldModelController, context));

            var border = new Border();
            border.Child = docView;

            SetupDocumentBinding(docView, docController, context);

            //Add to key to framework element dictionary
            var reference = docController.GetField(KeyStore.DataKey) as ReferenceController;
            if(keysToFrameworkElementsIn != null) keysToFrameworkElementsIn[reference?.FieldKey] = border; 

            // bind the text height
            //var docheightController = GetHeightField(docController, context);
            //if (docheightController != null)
            //BindHeight(docView, docheightController);

            // bind the text width
            //var docwidthController = GetWidthField(docController, context);
            //if (docwidthController != null)
            //BindWidth(docView, docwidthController);
            
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