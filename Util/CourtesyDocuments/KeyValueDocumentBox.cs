﻿using System;
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
        public KeyValueDocumentBox(FieldControllerBase refToDoc, double x = 0, double y = 0, double w = 200, double h = 20)
        {
            var fields = DefaultLayoutFields(new Point(x, y), new Size(w, h), refToDoc);
            Document = new DocumentController(fields, DocumentType);
        }
        public static FrameworkElement MakeView(DocumentController docController, Context context, DocumentController dataDocument, Dictionary<KeyController, FrameworkElement> keysToFrameworkElementsIn = null, bool isInterfaceBuilderLayout = false)
        {
            // the document field model controller provides us with the DATA
            // the Document on this courtesty document provides us with the parameters to display the DATA.
            // X, Y, Width, and Height etc....

            ReferenceController refToData;
            var fieldModelController = GetDereferencedDataFieldModelController(docController, context, new DocumentController(new Dictionary<KeyController, FieldControllerBase>(), TextingBox.DocumentType), out refToData);

            if (fieldModelController is ImageController)
                return ImageBox.MakeView(docController, context, keysToFrameworkElementsIn, isInterfaceBuilderLayout);
            if (fieldModelController is TextController)
                return TextingBox.MakeView(docController, context, keysToFrameworkElementsIn, isInterfaceBuilderLayout);
            var documentfieldModelController = fieldModelController as DocumentController ?? 
                                         docController.GetField(KeyStore.DocumentContextKey) as DocumentController; // use DocumentContext if no explicit reference
            Debug.Assert(documentfieldModelController != null);

            var border = new Border();

            var docView = new KeyValuePane() { TypeColumnWidth = new GridLength(0) };
            docView.SetUpForDocumentBox(documentfieldModelController);
            border.Child = docView;

            //add to key to framework element dictionary
            if (keysToFrameworkElementsIn != null && refToData != null)
                keysToFrameworkElementsIn[refToData.FieldKey] = border;

            if (isInterfaceBuilderLayout)
            {
                return new SelectableContainer(border, docController, dataDocument);
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
