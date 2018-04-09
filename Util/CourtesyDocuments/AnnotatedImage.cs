﻿using System;
using System.Collections.Generic;
using Windows.Foundation;
using DashShared;

namespace Dash
{
    public class AnnotatedImage : CourtesyDocument
    {
        public static DocumentType  ImageDocType  = new DocumentType("41E1280D-1BA9-4C3F-AE72-4080677E199E", "Image Doc");
        public static KeyController ImageFieldKey = new KeyController("827F581B-6ECB-49E6-8EB3-B8949DE0FE21", "Annotate Image");
        static DocumentController _prototypeDoc = CreatePrototypeDoc();
        static DocumentController _prototypeLayout = CreatePrototypeLayout();

        static DocumentController CreatePrototypeDoc()
        {
            var fields = new Dictionary<KeyController, FieldControllerBase>
            {
            };
            return new DocumentController(fields, ImageDocType);
        }

        /// <summary>
        /// Creates a default Layout for a Two Images document.  This requires that a prototype of a Two Images document exist so that
        /// this layout can reference the fields of the prototype.  When a delegate is made of a Two Images document,  this layout's 
        /// field references will automatically point to the delegate's (not the prototype's) values for those fields because of the
        /// context list used in MakeView().
        /// </summary>
        /// <returns></returns>
        static DocumentController CreatePrototypeLayout()
        {
            // set the default layout parameters on prototypes of field layout documents
            // these prototypes will be overridden by delegates when an instance is created
            var prototypeTextLayout  = new TextingBox(new DocumentReferenceController(_prototypeDoc.GetId(), KeyStore.DocumentTextKey), 0, 0, double.NaN, 25);
            var prototypeImageLayout = new ImageBox(new DocumentReferenceController(_prototypeDoc.GetId(), ImageFieldKey), 0, 0, double.NaN, double.NaN);
            var prototypeLayout      = new StackLayout(new DocumentController[] { prototypeImageLayout.Document, prototypeTextLayout.Document }, false);

            return prototypeLayout.Document;
        }
        public AnnotatedImage(Uri imageUri, string imageBytes, string text=null, string title="", double width = 200, double height = 250, double x=0, double y=0)
        {
            Document = _prototypeDoc.MakeDelegate();
            Document.SetField(ImageFieldKey, new ImageController(imageUri, imageBytes), true);

            if (text != null)
                Document.SetField(KeyStore.DocumentTextKey, new TextController(text), true);
            Document.SetField(KeyStore.TitleKey, new TextController(title), true);

            var docLayout = text == null ? new ImageBox(new DocumentReferenceController(_prototypeDoc.GetId(), ImageFieldKey), 0, 0, double.NaN, double.NaN).Document: _prototypeLayout.MakeDelegate();
            docLayout.SetField(KeyStore.PositionFieldKey, new PointController(new Point(x, y)), true);
            docLayout.SetField(KeyStore.HeightFieldKey, new NumberController(height), true);
            docLayout.SetField(KeyStore.WidthFieldKey, new NumberController(width), true);
            docLayout.SetField(KeyStore.DocumentContextKey, Document, true);
            Document = docLayout;
        }

        public AnnotatedImage(Uri imageUri, string title, double w=200, double h=250, double x=0, double y =0)
        {
            Document = _prototypeDoc.MakeDelegate();
            Document.SetField(ImageFieldKey, new ImageController(imageUri), true);
            Document.SetField(KeyStore.TitleKey, new TextController(title), true);
            var docLayout = new ImageBox(new DocumentReferenceController(_prototypeDoc.GetId(), ImageFieldKey), x, y, w, h).Document;
            docLayout.SetField(KeyStore.DocumentContextKey, Document, true);
            Document = docLayout;
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