using System;
using Windows.Foundation;
using Windows.UI;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Data;
using Dash.Converters;
using DashShared;
using System.Collections.Generic;

namespace Dash
{
    /// <summary>
    /// A generic document type containing a single text element.
    /// </summary>
    public class TextingBox : CourtesyDocument
    {
        public static KeyController TextAlignmentKey = new KeyController("Font Alignment", "3BD4572A-C6C9-4710-8E74-831204D2C17D");
        public static DocumentType  DocumentType = new DocumentType("181D19B4-7DEC-42C0-B1AB-365B28D8EA42", "Texting Box");

        public static string DefaultText = "Default Text";
        public static string DefaultFontWeight = "Normal"; // 100;
        public static double DefaultTextAlignment = (int)TextAlignment.Center;
        public static double DefaultFontSize = (Double)App.Instance.Resources["DefaultFontSize"];
        private static string PrototypeId = "F917C90C-14E8-45E0-A524-94C8958DDC4F";

        public TextingBox(FieldControllerBase refToText, double x = 0, double y = 0, double w = 200, double h = 40, FontWeight weight = null, Color? backgroundColor = null)
        {
            var fields = DefaultLayoutFields(new Point(x, y), new Size(w, h), refToText);
            fields.Add(KeyStore.FontWeightKey, new TextController(weight == null ? DefaultFontWeight : weight.ToString()));
            fields.Add(KeyStore.FontSizeKey, new NumberController(DefaultFontSize));
          //  fields.Add(TextAlignmentKey, new NumberController((int)(refToText.RootTypeInfo == TypeInfo.Text ? TextAlignment.Left : TextAlignment.Right)));
            if (backgroundColor != null)
                fields.Add(KeyStore.BackgroundColorKey, new TextController(backgroundColor.ToString()));
            if (w != 0 && !double.IsNaN(w))
                (fields[KeyStore.HorizontalAlignmentKey] as TextController).Data = HorizontalAlignment.Left.ToString();
            if (h != 0 && !double.IsNaN(h))
                (fields[KeyStore.VerticalAlignmentKey] as TextController).Data = VerticalAlignment.Top.ToString();
            SetupDocument(DocumentType, PrototypeId, "TextingBox Prototype Layout", fields);
        }

        protected static void SetupBindings(EditableTextBlock element, DocumentController docController, Context context)
        {
            CourtesyDocument.SetupBindings(element, docController, context);

            BindFontWeight(element, docController, context);
            BindFontSize(element, docController, context);
            BindTextAlignment(element, docController, context);
            BindBackgroundColor(element, docController, context);
            SetupTextBinding(element, docController, context);
        }
        /// <summary>
        /// Makes the view 
        /// </summary>
        /// <param name="isEditable"> Parameter used to determine if the textingbox will be editable upon double click, or just read-only </param>
        /// <returns></returns>
        public static FrameworkElement MakeView(DocumentController docController, Context context)
        {
            // the text field model controller provides us with the DATA
            // the Document on this courtesty document provides us with the parameters to display the DATA.
            // X, Y, Width, and Height etc....
            var textController = docController.GetField(KeyStore.DataKey);
            var text = textController.GetValue(null);
            // create the textblock
            //TODO Make TargetFieldController be a FieldReference to the field instead of just the field
            var tb = new EditableTextBlock
            {
                TargetFieldController = textController,
                TargetDocContext = context
            };
            SetupBindings(tb, docController, context);

            return tb;
        }

        #region Bindings

        protected static void SetupTextBinding(EditableTextBlock element, DocumentController docController, Context context)
        {
            var binding = new FieldBinding<FieldControllerBase>()
            {
                Document = docController,
                Key = KeyStore.DataKey,
                Mode = BindingMode.TwoWay,
                Context = context,
                GetConverter = FieldConversion.GetFieldtoStringConverter,
                FallbackValue = "<null>",
                Tag = "TextingBox SetupTextBinding"
            };
            element.AddFieldBinding(EditableTextBlock.TextProperty, binding);
        }

        public class TypedTextAlignmentBinding : SafeDataToXamlConverter<List<object>, TextAlignment>
        {
            public override TextAlignment ConvertDataToXaml(List<object> data, object parameter = null)
            {
                bool isNumber = data[1] is double;
                if (data[0] != null && data[0] is double) 
                {
                    switch ((int)(double)data[0]) {
                        case (int)TextAlignment.Left:
                            return TextAlignment.Left;
                        case (int)TextAlignment.Right:
                            return TextAlignment.Right;
                        case (int)TextAlignment.Center:
                            return TextAlignment.Center;
                        case (int)TextAlignment.Justify:
                            return TextAlignment.Justify;
                    }
                    
                }
                return isNumber ? TextAlignment.Right : TextAlignment.Center;
            }

            public override List<object> ConvertXamlToData(TextAlignment xaml, object parameter = null)
            {
                throw new NotImplementedException();
            }
        }
        protected static void BindTextAlignment(EditableTextBlock element, DocumentController docController, Context context)
        {
            var dataRef = new DocumentFieldReference(docController, TextAlignmentKey);
            var sideCountRef = new DocumentFieldReference(docController, KeyStore.DataKey);

            var alignmentBinding = new FieldMultiBinding<TextAlignment>(dataRef, sideCountRef)
            {
                Converter = new TypedTextAlignmentBinding(),
                Mode = BindingMode.OneWay,
                Context = context,
                CanBeNull = true
            };
            element.AddFieldBinding(EditableTextBlock.TextAlignmentProperty, alignmentBinding);
        }

        protected static void BindBackgroundColor(EditableTextBlock element, DocumentController docController, Context context)
        {
            var backgroundBinding = new FieldBinding<TextController>()
            {
                Key = KeyStore.BackgroundColorKey,
                Document = docController,
                Converter = new StringToBrushConverter(),
                Mode = BindingMode.TwoWay,
                Context = context
            };
            element.TextBackground.AddFieldBinding(Grid.BackgroundProperty, backgroundBinding);
        }

        protected static void BindFontWeight(EditableTextBlock element, DocumentController docController, Context context)
        {
            var fontWeightBinding = new FieldBinding<NumberController>()
            {
                Key = KeyStore.FontWeightKey,
                Document = docController,
                Converter = new DoubleToFontWeightConverter(),
                Mode = BindingMode.TwoWay,
                Context = context
            };
            element.AddFieldBinding(Control.FontWeightProperty, fontWeightBinding);
        }

        protected static void BindFontSize(EditableTextBlock element, DocumentController docController, Context context)
        {
            var fontSizeBinding = new FieldBinding<NumberController>()
            {
                Key = KeyStore.FontSizeKey,
                Document = docController,
                Mode = BindingMode.TwoWay,
                Context = context
            };
            element.AddFieldBinding(Control.FontSizeProperty, fontSizeBinding);
        }

        #endregion
    }
}