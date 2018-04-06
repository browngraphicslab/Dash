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

namespace Dash
{

    public class KeyValueDocumentBox : CourtesyDocument
    {
        public static DocumentType DocumentType = new DocumentType("737BB31D-52B4-4C57-AD33-D519F40B57DC", "Key Value Document Box");
        public KeyValueDocumentBox(FieldControllerBase refToDoc, double x = 0, double y = 0, double w = 650, double h = 800)
        {
            var fields = DefaultLayoutFields(new Point(x, y), new Size(w, h), refToDoc);
            Document = new DocumentController(fields, DocumentType);
        }
        public static FrameworkElement MakeView(DocumentController docController, Context context)
        {
            // the document field model controller provides us with the DATA
            // the Document on this courtesty document provides us with the parameters to display the DATA.
            // X, Y, Width, and Height etc....
            var documentfieldModelController = docController.GetDataDocument();
            Debug.Assert(documentfieldModelController != null);

            var border = new Border();

            var keyValuePane = new KeyValuePane
            {
                TypeColumnWidth = new GridLength(0),
                DataContext = documentfieldModelController
            };
            border.Child = keyValuePane;
            SetupBindings(border, docController, context);

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
