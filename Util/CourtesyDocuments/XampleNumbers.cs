using System;
using System.Collections.Generic;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Dash;
using DashShared;

namespace Dash
{
    public class Numbers : CourtesyDocument
    {
        public static DocumentType NumbersType =
            new DocumentType("8FC422AB-015E-4B72-A28B-16271808C888", "Numbers");

        public static KeyController Number1FieldKey = new KeyController("0D3B939F-1E74-4577-8ACC-0685111E451C", "Number1");
        public static KeyController Number2FieldKey = new KeyController("56162B53-B02D-4880-912F-9D66B5F1F15B", "Number2");
        public static KeyController Number3FieldKey = new KeyController("61C34393-7DF7-4F26-9FDF-E0B138532F39", "Number3");
        public static KeyController Number4FieldKey = new KeyController("953D09E5-5770-4ED3-BC3F-76DFB22619E8", "Number4");
        public static KeyController Number5FieldKey = new KeyController("F59AAEC1-FCB6-4543-89CB-13ED5C5FD893", "Number5");

        private static Random r = new Random();

        public Numbers()
        {
            // create a document with two images
            var fields = DefaultLayoutFields(0, 0, double.NaN, double.NaN, null);
            fields.Add(Number1FieldKey, new NumberFieldModelController(789));
            fields.Add(Number2FieldKey, new NumberFieldModelController(23));
            fields.Add(Number3FieldKey, new NumberFieldModelController(8));
            fields.Add(Number4FieldKey, new NumberFieldModelController((r.NextDouble() - 0.5) * 600));
            fields.Add(Number5FieldKey, new NumberFieldModelController((r.NextDouble() - 0.5) * 600));

            Document = new DocumentController(fields, NumbersType);

            var tBox1 = new TextingBox(new ReferenceFieldModelController(Document.GetId(), Number1FieldKey), 0,
                0, 60, 35).Document;
            var tBox2 = new TextingBox(new ReferenceFieldModelController(Document.GetId(), Number2FieldKey), 0,
                0, 60, 35).Document;
            var tBox3 = new TextingBox(new ReferenceFieldModelController(Document.GetId(), Number3FieldKey), 0,
                0, 60, 35).Document;
            var tBox4 = new TextingBox(new ReferenceFieldModelController(Document.GetId(), Number4FieldKey), 0,
                0, 60, 35).Document;
            var tBox5 = new TextingBox(new ReferenceFieldModelController(Document.GetId(), Number5FieldKey), 0,
                0, 60, 35).Document;
            var tBox6 = new TextingBox(new ReferenceFieldModelController(Document.GetId(), Number3FieldKey), 0,
                0, 60, 35).Document;

            var gridPanel = new GridLayout().Document;
            gridPanel.SetGridColumnDefinitions(new List<ColumnDefinition>
        {
            new ColumnDefinition{Width = new GridLength(1, GridUnitType.Star)},
            new ColumnDefinition{Width = new GridLength(1, GridUnitType.Star)}
        });
            gridPanel.SetGridRowDefinitions(new List<RowDefinition>
        {
            new RowDefinition{Height = new GridLength(1, GridUnitType.Star)},
            new RowDefinition{Height = new GridLength(1, GridUnitType.Star)},
            new RowDefinition{Height = new GridLength(1, GridUnitType.Star)},
            new RowDefinition{Height = new GridLength(1, GridUnitType.Star)},
            new RowDefinition{Height = new GridLength(1, GridUnitType.Star)},
            new RowDefinition{Height = new GridLength(1, GridUnitType.Star)}
        });
            gridPanel.SetHorizontalAlignment(HorizontalAlignment.Right);
            gridPanel.SetField(KeyStore.WidthFieldKey, new NumberFieldModelController(200), true);
            gridPanel.SetField(KeyStore.HeightFieldKey, new NumberFieldModelController(200), true);
            tBox1.SetGridRow(0);
            gridPanel.AddChild(tBox1);
            tBox2.SetGridRow(1);
            tBox2.SetGridColumn(1);
            tBox2.SetHorizontalAlignment(HorizontalAlignment.Right);
            gridPanel.AddChild(tBox2);
            tBox3.SetGridRow(2);
            gridPanel.AddChild(tBox3);
            tBox4.SetGridRow(3);
            tBox6.SetGridColumn(1);
            gridPanel.AddChild(tBox4);
            tBox5.SetGridRow(4);
            tBox5.SetHorizontalAlignment(HorizontalAlignment.Left);
            gridPanel.AddChild(tBox5);
            tBox6.SetGridRow(5);
            tBox6.SetGridColumn(1);
            tBox6.SetVerticalAlignment(VerticalAlignment.Stretch);
            tBox6.SetHorizontalAlignment(HorizontalAlignment.Stretch);
            gridPanel.AddChild(tBox6);
            //var stackPan = new StackingPanel(new[] { tBox1, tBox2, tBox3, tBox4, tBox5, tBox6 }, false).Document;

            SetLayoutForDocument(Document, gridPanel, forceMask: true, addToLayoutList: true);
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