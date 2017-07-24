using System;
using System.Collections.Generic;
using Windows.Foundation;
using DashShared;

namespace Dash
{
    public class AnnotatedImage : CourtesyDocument
    {
        public static DocumentType ImageDocType = new DocumentType("41E1280D-1BA9-4C3F-AE72-4080677E199E", "Image Doc");
        public static Key Image1FieldKey = new Key("827F581B-6ECB-49E6-8EB3-B8949DE0FE21", "Annotate Image");
        public static Key TextFieldKey = new Key("73A8E9AB-A798-4FA0-941E-4C4A5A2BF9CE", "TextField");
        static DocumentController _prototypeDoc = CreatePrototypeDoc();
        static DocumentController _prototypeLayout = CreatePrototypeLayout();

        static DocumentController CreatePrototypeDoc()
        {
            return new DocumentController(new Dictionary<Key, FieldModelController>(), ImageDocType);
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
            var prototypeTextLayout = new TextingBox(new ReferenceFieldModelController(_prototypeDoc.GetId(), TextFieldKey), 0, 0, 200, 50);
            var prototypeImage1Layout = new ImageBox(new ReferenceFieldModelController(_prototypeDoc.GetId(), Image1FieldKey), 0, 50, 200, 200);

            var prototypeLayout = new StackingPanel(new DocumentController[] { prototypeImage1Layout.Document, prototypeTextLayout.Document }, true);

            prototypeTextLayout.Document.SetField(DashConstants.KeyStore.WidthFieldKey, new ReferenceFieldModelController(prototypeLayout.Document.GetId(), DashConstants.KeyStore.WidthFieldKey), true);
            prototypeImage1Layout.Document.SetField(DashConstants.KeyStore.WidthFieldKey, new ReferenceFieldModelController(prototypeLayout.Document.GetId(), DashConstants.KeyStore.WidthFieldKey), true);
            prototypeImage1Layout.Document.SetField(DashConstants.KeyStore.HeightFieldKey, new ReferenceFieldModelController(prototypeLayout.Document.GetId(), DashConstants.KeyStore.HeightFieldKey), true);

            return prototypeLayout.Document;
        }

        public AnnotatedImage(Uri imageUri, string text)
        {
            Document = _prototypeDoc.MakeDelegate();
            Document.SetField(Image1FieldKey, new ImageFieldModelController(imageUri), true);
            Document.SetField(TextFieldKey, new TextFieldModelController(text), true);
            var docLayout = _prototypeLayout.MakeDelegate();
            docLayout.SetField(DashConstants.KeyStore.PositionFieldKey, new PointFieldModelController(new Point(0, 0)), true);
            docLayout.SetField(DashConstants.KeyStore.HeightFieldKey, new NumberFieldModelController(250), true);
            docLayout.SetField(DashConstants.KeyStore.WidthFieldKey, new NumberFieldModelController(200), true);
            //SetLayoutForDocument(Document, docLayout);
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