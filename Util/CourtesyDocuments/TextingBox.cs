using System;
using Windows.Foundation;
using Windows.UI;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Data;
using Dash.Converters;
using DashShared;

namespace Dash
{
    /// <summary>
    /// A generic document type containing a single text element.
    /// </summary>
    public class TextingBox : CourtesyDocument
    {
        public static KeyController FontWeightKey = new KeyController("03FC5C4B-6A5A-40BA-A262-578159E2D5F7", "FontWeight");
        public static KeyController FontSizeKey = new KeyController("75902765-7F0E-4AA6-A98B-3C8790DBF7CE", "FontSize");
        public static KeyController TextAlignmentKey = new KeyController("3BD4572A-C6C9-4710-8E74-831204D2C17D", "Font Alignment");
        public static KeyController BackgroundColorKey = new KeyController("CBD8E5E1-6E5A-48C5-AFEA-8A4515FC3DFE", "Background Color");
        public static DocumentType  DocumentType = new DocumentType("181D19B4-7DEC-42C0-B1AB-365B28D8EA42", "Texting Box");

        public static string DefaultText = "Default Text";
        public static string DefaultFontWeight = "Normal"; // 100;
        public static double DefaultTextAlignment = (int)TextAlignment.Center;
        public static double DefaultFontSize = (Double)App.Instance.Resources["DefaultFontSize"];
        private static string PrototypeId = "F917C90C-14E8-45E0-A524-94C8958DDC4F";

        public TextingBox(FieldControllerBase refToText, double x = 0, double y = 0, double w = 200, double h = 40, FontWeight weight = null, Color? backgroundColor = null)
        {
            var fields = DefaultLayoutFields(new Point(x, y), new Size(w, h), refToText);
            if (w != 0 && !double.IsNaN(w))
                (fields[KeyStore.HorizontalAlignmentKey] as TextController).Data = HorizontalAlignment.Left.ToString();
            if (h != 0 && !double.IsNaN(h))
                (fields[KeyStore.VerticalAlignmentKey] as TextController).Data = VerticalAlignment.Top.ToString();
            Document = GetLayoutPrototype().MakeDelegate();
            Document.SetFields(fields, true);
            SetFontWeightField(Document, weight == null ? DefaultFontWeight : weight.ToString(), true, null);
            SetFontSizeField(Document, DefaultFontSize, true, null);
            SetTextAlignmentField(Document, DefaultTextAlignment, true, null);
            if (backgroundColor != null)
                SetBackgroundColorField(Document, (Color)backgroundColor, true, null);
        }

        protected override DocumentController GetLayoutPrototype()
        {
            var prototype = ContentController<FieldModel>.GetController<DocumentController>(PrototypeId);
            if (prototype == null)
            {
                prototype = InstantiatePrototypeLayout();
            }
            return prototype;
        }

        protected override DocumentController InstantiatePrototypeLayout()
        {
            var textController = new TextController(DefaultText);
            var fields = DefaultLayoutFields(new Point(), new Size(double.NaN, double.NaN), textController);
            var prototypeDocument = new DocumentController(fields, DocumentType, PrototypeId);

            return prototypeDocument;
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

        public override FrameworkElement makeView(DocumentController docController,Context context)
        {
            return MakeView(docController, context);
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
            // create the textblock
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

        protected static void BindTextAlignment(EditableTextBlock element, DocumentController docController, Context context)
        {
            var alignmentBinding = new FieldBinding<NumberController>()
            {
                Key = TextAlignmentKey,
                Document = docController,
                Converter = new IntToTextAlignmentConverter(),
                Mode = BindingMode.TwoWay,
                Context = context
            };
            element.AddFieldBinding(EditableTextBlock.TextAlignmentProperty, alignmentBinding);
        }

        protected static void BindBackgroundColor(EditableTextBlock element, DocumentController docController, Context context)
        {
            var backgroundBinding = new FieldBinding<TextController>()
            {
                Key = BackgroundColorKey,
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
                Key = FontWeightKey,
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
                Key = FontSizeKey,
                Document = docController,
                Mode = BindingMode.TwoWay,
                Context = context
            };
            element.AddFieldBinding(Control.FontSizeProperty, fontSizeBinding);
        }

        #endregion


        #region GettersAndSetters

        private void SetTextAlignmentField(DocumentController docController, double textAlignment, bool forceMask, Context context)
        {
            docController.SetField(TextAlignmentKey, new NumberController(textAlignment), forceMask); // set the field here so that forceMask is respected
        }

        private void SetBackgroundColorField(DocumentController docController, Color color, bool forceMask, Context context)
        {
            docController.SetField(BackgroundColorKey, new TextController(color.ToString()), forceMask); // set the field here so that forceMask is respected
        }

        private void SetFontSizeField(DocumentController docController, double fontSize, bool forceMask, Context context)
        {
            docController.SetField(FontSizeKey, new NumberController(fontSize), forceMask); // set the field here so that forceMask is respected
        }

        private void SetFontWeightField(DocumentController docController, string fontWeight, bool forceMask, Context context)
        {
            docController.SetField(FontWeightKey, new TextController(fontWeight), forceMask); // set the field here so that forceMask is respected
        }

        #endregion
    }
}