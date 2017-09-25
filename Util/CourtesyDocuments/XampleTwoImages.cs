using System;
using System.Collections.Generic;
using Windows.Foundation;
using Dash;
using DashShared;

namespace Dash
{
    public class TwoImages : CourtesyDocument
    {
        public static DocumentType TwoImagesType = new DocumentType("FC8EF5EB-1A0B-433C-85B6-6929B974A4B7", "Two Images");
        public static KeyControllerBase Image1FieldKey = new KeyControllerBase("827F581B-6ECB-49E6-8EB3-B8949DE0FE21", "ImageField1");
        public static KeyControllerBase Image2FieldKey = new KeyControllerBase("BCB1109C-0C55-47B7-B1E3-34CA9C66627E", "ImageField2");
        public static KeyControllerBase AnnotatedFieldKey = new KeyControllerBase("F370A8F6-22D9-4442-A528-A7FEEC29E306", "AnnotatedImage");
        public static KeyControllerBase TextFieldKey = new KeyControllerBase("73A8E9AB-A798-4FA0-941E-4C4A5A2BF9CE", "TextField");
        public static KeyControllerBase RichTextKey = new KeyControllerBase("1C46E96E-F3CB-4DEE-8799-AD71DB1FB4D1", "RichTextField");
        static DocumentController _prototypeTwoImages = CreatePrototype2Images();
        static DocumentController _prototypeLayout = CreatePrototypeLayout();

        static DocumentController CreatePrototype2Images()
        {
            // bcz: default values for data fields can be added, but should not be needed
            Dictionary<KeyControllerBase, FieldControllerBase> fields = new Dictionary<KeyControllerBase, FieldControllerBase>();
            fields.Add(TextFieldKey, new TextFieldModelController("Prototype Text"));
            fields.Add(Image1FieldKey, new ImageFieldModelController(new Uri("ms-appx://Dash/Assets/cat.jpg")));
            fields.Add(Image2FieldKey, new ImageFieldModelController(new Uri("ms-appx://Dash/Assets/cat2.jpeg")));
            fields.Add(AnnotatedFieldKey, new ImageFieldModelController(new Uri("ms-appx://Dash/Assets/cat2.jpeg")));
            return new DocumentController(fields, TwoImagesType);

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
            var prototypeImage1Layout = new ImageBox(new ReferenceFieldModelController(_prototypeTwoImages.GetId(), Image1FieldKey), 0, 0, 200, 200);
            var prototypeImage2Layout = new ImageBox(new ReferenceFieldModelController(_prototypeTwoImages.GetId(), Image2FieldKey), 0, 0, 200, 200);
            var prototypeAnnotatedLayout = new DocumentBox(new ReferenceFieldModelController(_prototypeTwoImages.GetId(), AnnotatedFieldKey), 0, 0, 200, 250);
            var prototypeTextLayout = new TextingBox(new ReferenceFieldModelController(_prototypeTwoImages.GetId(), TextFieldKey), 0, 0, 200, 50);
            var prototypeLayout = new StackLayout(new[] { prototypeTextLayout.Document, prototypeImage1Layout.Document, prototypeImage2Layout.Document, });
            prototypeLayout.Document.SetField(KeyStore.HeightFieldKey, new NumberFieldModelController(700), true);
            prototypeLayout.Document.SetField(KeyStore.WidthFieldKey, new NumberFieldModelController(200), true);

            return prototypeLayout.Document;
        }

        public TwoImages(bool displayFieldsAsDocuments)
        {
            Document = _prototypeTwoImages.MakeDelegate();
            Document.SetField(Image1FieldKey, new ImageFieldModelController(new Uri("ms-appx://Dash/Assets/cat.jpg")), true);
            Document.SetField(Image2FieldKey, new ImageFieldModelController(new Uri("ms-appx://Dash/Assets/cat2.jpeg")), true);
            Document.SetField(AnnotatedFieldKey, new DocumentFieldModelController(new AnnotatedImage(new Uri("ms-appx://Dash/Assets/cat2.jpeg"), "Yowling").Document), true);
            Document.SetField(TextFieldKey, new TextFieldModelController("Hello World!"), true);
            Document.SetField(RichTextKey, new RichTextFieldModelController(), true);

            var docLayout = _prototypeLayout.MakeDelegate();
            docLayout.SetField(KeyStore.PositionFieldKey, new PointFieldModelController(new Point(0, 0)), true);
            docLayout.SetField(new KeyControllerBase("opacity", "opacity"), new NumberFieldModelController(0.8), true);
            SetLayoutForDocument(Document, docLayout, forceMask: true, addToLayoutList: true);

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