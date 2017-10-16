using System;
using System.Collections.Generic;
using Windows.Foundation;
using DashShared;

namespace Dash
{
    public class AnnotatedImage : CourtesyDocument
    {
        public static DocumentType ImageDocType = new DocumentType("41E1280D-1BA9-4C3F-AE72-4080677E199E", "Image Doc");
        public static KeyController Image1FieldKey = new KeyController("827F581B-6ECB-49E6-8EB3-B8949DE0FE21", "Annotate Image");
        static DocumentController _prototypeDoc = CreatePrototypeDoc();
        static DocumentController _prototypeLayout = CreatePrototypeLayout();

        static DocumentController CreatePrototypeDoc()
        {
            var fields = new Dictionary<KeyController, FieldModelController>
            {
                [KeyStore.PrimaryKeyKey] = new ListFieldModelController<TextFieldModelController>(new TextFieldModelController[] { new TextFieldModelController(KeyStore.TitleKey.ToString() ) } )
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
            var prototypeTextLayout = new TextingBox(new ReferenceFieldModelController(_prototypeDoc.GetId(), KeyStore.TitleKey), 0, 0, double.NaN, 20);
            var prototypeImage1Layout = new ImageBox(new ReferenceFieldModelController(_prototypeDoc.GetId(), Image1FieldKey), 0, 0, double.NaN, double.NaN);

            var prototypeLayout = new StackLayout(new DocumentController[] { prototypeTextLayout.Document, prototypeImage1Layout.Document}, false);

            return prototypeLayout.Document;
        }
        public AnnotatedImage(Uri imageUri, string imageBytes, double width = 200, double height = 250, double x = 0, double y = 0)
        {
            Document = _prototypeDoc.MakeDelegate();
            Document.SetField(Image1FieldKey, new ImageFieldModelController(imageUri, imageBytes), true);
            Document.SetField(KeyStore.TitleKey, new TextFieldModelController(imageUri.AbsolutePath), true);
            var docLayout = new ImageBox(new ReferenceFieldModelController(_prototypeDoc.GetId(), Image1FieldKey), 0, 0, double.NaN, double.NaN).Document;

            docLayout.SetField(KeyStore.PositionFieldKey, new PointFieldModelController(new Point(x, y)), true);
            docLayout.SetField(KeyStore.HeightFieldKey, new NumberFieldModelController(height), true);
            docLayout.SetField(KeyStore.WidthFieldKey, new NumberFieldModelController(width), true);

            docLayout.SetField(KeyStore.DocumentContextKey, new DocumentFieldModelController(Document), true);
            Document = docLayout;
           // SetLayoutForDocument(Document, docLayout, forceMask: true, addToLayoutList: true);
        }
        public AnnotatedImage(Uri imageUri, string imageBytes, string title, double width = 200, double height = 250, double x=0, double y=0)
        {
            Document = _prototypeDoc.MakeDelegate();
            Document.SetField(Image1FieldKey, new ImageFieldModelController(imageUri, imageBytes), true);
            Document.SetField(KeyStore.TitleKey, new TextFieldModelController(title), true);
            var docLayout = _prototypeLayout.MakeDelegate();
            docLayout.SetField(KeyStore.PositionFieldKey, new PointFieldModelController(new Point(x, y)), true);
            docLayout.SetField(KeyStore.HeightFieldKey, new NumberFieldModelController(height), true);
            docLayout.SetField(KeyStore.WidthFieldKey, new NumberFieldModelController(width), true);
            docLayout.SetField(KeyStore.DocumentContextKey, new DocumentFieldModelController(Document), true);
            Document = docLayout;
            //SetLayoutForDocument(Document, docLayout, forceMask: true, addToLayoutList: true);
        }

        public AnnotatedImage(Uri imageUri, string title)
        {
            Document = _prototypeDoc.MakeDelegate();
            Document.SetField(Image1FieldKey, new ImageFieldModelController(imageUri), true);
            Document.SetField(KeyStore.TitleKey, new TextFieldModelController(title), true);
            var docLayout = _prototypeLayout.MakeDelegate();
            docLayout.SetField(KeyStore.PositionFieldKey, new PointFieldModelController(new Point(0, 0)), true);
            docLayout.SetField(KeyStore.HeightFieldKey, new NumberFieldModelController(250), true);
            docLayout.SetField(KeyStore.WidthFieldKey, new NumberFieldModelController(200), true);
            docLayout.SetField(KeyStore.DocumentContextKey, new DocumentFieldModelController(Document), true);
            Document = docLayout;
            //SetLayoutForDocument(Document, docLayout, forceMask: true, addToLayoutList: true);
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