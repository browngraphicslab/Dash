using System;
using System.Collections.Generic;
using Windows.Foundation;
using Dash;
using Dash.Controllers;
using DashShared;

namespace Dash
{
    public class TwoImages : CourtesyDocument
    {
        public static DocumentType TwoImagesType = new DocumentType("FC8EF5EB-1A0B-433C-85B6-6929B974A4B7", "Two Images");
        public static KeyController Image1FieldKey = new KeyController("827F581B-6ECB-49E6-8EB3-B8949DE0FE21", "ImageField1");
        public static KeyController Image2FieldKey = new KeyController("BCB1109C-0C55-47B7-B1E3-34CA9C66627E", "ImageField2");
        public static KeyController AnnotatedFieldKey = new KeyController("F370A8F6-22D9-4442-A528-A7FEEC29E306", "AnnotatedImage");
        public static KeyController TextFieldKey = new KeyController("73A8E9AB-A798-4FA0-941E-4C4A5A2BF9CE", "TextField");
        public static KeyController RichTextKey = new KeyController("1C46E96E-F3CB-4DEE-8799-AD71DB1FB4D1", "RichTextField");
        static DocumentController _prototypeTwoImages = CreatePrototype2Images();
        static DocumentController _prototypeLayout = CreatePrototypeLayout();

        static DocumentController CreatePrototype2Images()
        {
            // bcz: default values for data fields can be added, but should not be needed
            Dictionary<KeyController, FieldControllerBase> fields = new Dictionary<KeyController, FieldControllerBase>();
            fields.Add(TextFieldKey, new TextController("Prototype Text"));
            fields.Add(Image1FieldKey, new ImageController(new Uri("ms-appx://Dash/Assets/cat.jpg")));
            fields.Add(Image2FieldKey, new ImageController(new Uri("ms-appx://Dash/Assets/cat2.jpeg")));
            fields.Add(AnnotatedFieldKey, new ImageController(new Uri("ms-appx://Dash/Assets/cat2.jpeg")));
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
            var prototypeImage1Layout = new ImageBox(new DocumentReferenceController(_prototypeTwoImages.GetId(), Image1FieldKey), 0, 0, 200, 200);
            var prototypeImage2Layout = new ImageBox(new DocumentReferenceController(_prototypeTwoImages.GetId(), Image2FieldKey), 0, 0, 200, 200);
            var prototypeAnnotatedLayout = new DocumentBox(new DocumentReferenceController(_prototypeTwoImages.GetId(), AnnotatedFieldKey), 0, 0, 200, 250);
            var prototypeTextLayout = new TextingBox(new DocumentReferenceController(_prototypeTwoImages.GetId(), TextFieldKey), 0, 0, 200, 50);
            var prototypeLayout = new StackLayout(new[] { prototypeTextLayout.Document, prototypeImage1Layout.Document, prototypeImage2Layout.Document, });
            prototypeLayout.Document.SetField(KeyStore.HeightFieldKey, new NumberController(700), true);
            prototypeLayout.Document.SetField(KeyStore.WidthFieldKey, new NumberController(200), true);

            return prototypeLayout.Document;
        }

        public TwoImages(bool displayFieldsAsDocuments)
        {
            Document = _prototypeTwoImages.MakeDelegate();
            Document.SetField(Image1FieldKey, new ImageController(new Uri("ms-appx://Dash/Assets/cat.jpg")), true);
            Document.SetField(Image2FieldKey, new ImageController(new Uri("ms-appx://Dash/Assets/cat2.jpeg")), true);
            Document.SetField(AnnotatedFieldKey, new AnnotatedImage(new Uri("ms-appx://Dash/Assets/cat2.jpeg"), "Yowling").Document, true);
            Document.SetField(TextFieldKey, new TextController("Hello World!"), true);
            Document.SetField(RichTextKey, new RichTextController(), true);

            var docLayout = _prototypeLayout.MakeDelegate();
            docLayout.SetField(KeyStore.PositionFieldKey, new PointController(new Point(0, 0)), true);
            docLayout.SetField(new KeyController("opacity", "opacity"), new NumberController(0.8), true);
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