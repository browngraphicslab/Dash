using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Dash.Controllers;
using DashShared;
using Windows.Foundation;

namespace Dash
{
    class XampleText : CourtesyDocument
    {
        public static DocumentType NumbersType =
            new DocumentType("2E92492D-67FA-46DB-9140-33BBEB90B192", "XampleText");

        public static KeyController Text1FieldKey = new KeyController("AB81901C-0001-4653-A2FC-749B398CD26C", "Text1");
        public static KeyController Text2FieldKey = new KeyController("E14AE421-3534-4588-B69B-C4ED3E234E0F", "Text2");
        public static KeyController Text3FieldKey = new KeyController("0B9AEBB0-42B9-4E54-BE0D-71CDEA2BCB32", "Text3");


        public XampleText()
        {
            // create a document with two images
            var fields = DefaultLayoutFields(new Point(), new Size(double.NaN, double.NaN), null);
            fields.Add(Text1FieldKey, new TextFieldModelController("Test1"));
            fields.Add(Text2FieldKey, new TextFieldModelController("Test2"));
            fields.Add(Text3FieldKey, new TextFieldModelController("Test3"));

            Document = new DocumentController(fields, NumbersType);

            var tBox1 = new TextingBox(new DocumentReferenceFieldController(Document.GetId(), Text1FieldKey), 0,
                0, 60, 35).Document;
            var tBox2 = new TextingBox(new DocumentReferenceFieldController(Document.GetId(), Text2FieldKey), 0,
                0, 60, 35).Document;
            var tBox3 = new TextingBox(new DocumentReferenceFieldController(Document.GetId(), Text3FieldKey), 0,
                0, 60, 35).Document;

            var gridPanel = new GridLayout().Document;
            gridPanel.SetGridColumnDefinitions(new List<ColumnDefinition>
            {
                new ColumnDefinition{Width = new GridLength(1, GridUnitType.Star)},
            });
            gridPanel.SetGridRowDefinitions(new List<RowDefinition>
            {
                new RowDefinition{Height = new GridLength(1, GridUnitType.Star)},
                new RowDefinition{Height = new GridLength(1, GridUnitType.Star)},
                new RowDefinition{Height = new GridLength(1, GridUnitType.Star)},
            });
            gridPanel.SetHorizontalAlignment(HorizontalAlignment.Right);
            gridPanel.SetField(KeyStore.WidthFieldKey, new NumberFieldModelController(200), true);
            gridPanel.SetField(KeyStore.HeightFieldKey, new NumberFieldModelController(200), true);
            tBox1.SetGridRow(0);
            gridPanel.AddChild(tBox1);
            tBox2.SetGridRow(1);
            gridPanel.AddChild(tBox2);
            tBox3.SetGridRow(2);
            gridPanel.AddChild(tBox3);

            SetLayoutForDocument(Document, gridPanel, true, true);
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
