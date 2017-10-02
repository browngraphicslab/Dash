using System;
using System.Collections.Generic;
using Dash;
using Dash.Controllers;
using DashShared;

namespace Dash
{
    public class NestedDocExample : CourtesyDocument
    {
        public static DocumentType NestedDocExampleType =
            new DocumentType("700FAEE4-5520-4E5E-9AED-3C8C5C1BE58B", "Nested Doc Example");

        public static KeyController TextFieldKey = new KeyController("73A8E9AB-A798-4FA0-941E-4C4A5A2BF9CE", "TextField");
        public static KeyController TextField2Key = new KeyController("B53F1453-4C52-4302-96A3-A6B40DA7D587", "TextField2");
        public static KeyController TwoImagesKey = new KeyController("4E5C2B62-905D-4952-891D-24AADE14CA80", "TowImagesField");

        public NestedDocExample(bool displayFieldsAsDocuments)
        {
            // create a document with two images
            var twoModel = new DocumentFieldModelController(new TwoImages(displayFieldsAsDocuments).Document);
            var tModel = new TextFieldModelController("Nesting");
            var tModel2 = new TextFieldModelController("More Nesting");
            var fields = new Dictionary<KeyController, FieldControllerBase>
            {
                [TextFieldKey] = tModel,
                [TwoImagesKey] = twoModel,
                [TextField2Key] = tModel2
            };
            Document = new DocumentController(fields, NestedDocExampleType);

            var tBox = new TextingBox(new DocumentReferenceFieldController(Document.GetId(), TextFieldKey), 0, 0, double.NaN, 100).Document;
            var imBox1 = twoModel.Data;
            var tBox2 = new TextingBox(new DocumentReferenceFieldController(Document.GetId(), TextField2Key), 0, 0, double.NaN, 100).Document;

            var stackPan = new StackLayout(new DocumentController[] { tBox, imBox1, tBox2 }).Document;

            SetLayoutForDocument(Document, stackPan, forceMask: true, addToLayoutList: true);
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