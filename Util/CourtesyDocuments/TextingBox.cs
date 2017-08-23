using System;
using System.Diagnostics;
using Windows.Foundation;
using Windows.UI;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Dash;
using Dash.Converters;
using DashShared;
using TextWrapping = DashShared.TextWrapping;
using System.Collections.Generic;
using System.Linq;
using static Dash.DocumentCollectionFieldModelController;
using static Dash.DocumentController;

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
        public static DocumentType DocumentType =
            new DocumentType("181D19B4-7DEC-42C0-B1AB-365B28D8EA42", "Texting Box");

        public static string DefaultText = "Default Text";
        public static string DefaultFontWeight = "Normal"; // 100;
        public static double DefaultTextAlignment = (int)TextAlignment.Left;
        public static double DefaultFontSize = 15;
        private static string PrototypeId = "F917C90C-14E8-45E0-A524-94C8958DDC4F";

        public TextingBox(FieldModelController refToText, double x = 0, double y = 0, double w = 200, double h = 20, FontWeight weight = null)
        {
            var fields = DefaultLayoutFields(new Point(x, y), new Size(w, h), refToText);
            Document = GetLayoutPrototype().MakeDelegate();
            Document.SetFields(fields, true);
            SetFontWeightField(Document, weight == null ? DefaultFontWeight : weight.ToString(), true, null);
            SetFontSizeField(Document, DefaultFontSize, true, null);
           // SetTextAlignmentField(Document, DefaultTextAlignment, true, null);
        }

        protected override DocumentController GetLayoutPrototype()
        {
            var prototype = ContentController.GetController<DocumentController>(PrototypeId);
            if (prototype == null)
            {
                prototype = InstantiatePrototypeLayout();
            }
            return prototype;
        }

        protected override DocumentController InstantiatePrototypeLayout()
        {
            var textFieldModelController = new TextFieldModelController(DefaultText);
            var fields = DefaultLayoutFields(new Point(), new Size(double.NaN, double.NaN), textFieldModelController);
            var prototypeDocument = new DocumentController(fields, DocumentType, PrototypeId);

            return prototypeDocument;
        }

        protected static void SetupBindings(TextBlock element, DocumentController docController, Context context)
        {
            BindFontWeight(element, docController, context);
            BindFontSize(element, docController, context);
            BindTextAllignment(element, docController, context);
            SetupTextBinding(element, docController, context);
        }

        public override FrameworkElement makeView(DocumentController docController,
            Context context, bool isInterfaceBuilderLayout = false)
        {
            return MakeView(docController, context);
        }

        /// <summary>
        /// Makes the view 
        /// </summary>
        /// <param name="isEditable"> Parameter used to determine if the textingbox will be editable upon double click, or just read-only </param>
        /// <returns></returns>
        public static FrameworkElement MakeView(DocumentController docController, Context context, bool isInterfaceBuilderLayout = false, bool isEditable = false)
        {
            var referenceToText = GetTextReference(docController);
            
            var editableTB = new EditableTextBlock()
            {
                TargetFieldReference = referenceToText,
                TargetDocContext = context,
                IsEditable = isEditable
            };

            CourtesyDocument.SetupBindings(editableTB, docController, context);
            SetupBindings(editableTB.xTextBlock, docController, context);

            // add bindings to work with operators (only if text is a reference)
            if (referenceToText != null) 
            {
                var fmController = docController.GetDereferencedField(KeyStore.DataKey, context);
                //BindOperationInteractions(editableTB.xTextBlock, referenceToText.FieldReference.Resolve(context), referenceToText.FieldKey, fmController);
                BindOperationInteractions(editableTB, referenceToText.FieldReference.Resolve(context), referenceToText.FieldKey, fmController);
            }

            return isInterfaceBuilderLayout ? (FrameworkElement)new SelectableContainer(editableTB, docController) : editableTB;
        }
        #region Bindings

        protected static void SetupTextBinding(TextBlock element, DocumentController docController, Context context)
        {
            var data = docController.GetDereferencedField(KeyStore.DataKey, context);
            if (data != null)
            {
                var binding = new FieldBinding<FieldModelController>()
                {
                    Document = docController,
                    Key = KeyStore.DataKey,
                    Mode = BindingMode.TwoWay,
                    Context = context,
                    GetConverter = GetFieldConverter
                };
                element.AddFieldBinding(TextBlock.TextProperty, binding);
            }
        }

        static protected IValueConverter GetFieldConverter(FieldModelController fieldModelController)
        {
            if (fieldModelController is TextFieldModelController)
            {
                return new StringToStringConverter();
            }
            else if (fieldModelController is NumberFieldModelController)
            {
               return new StringToDoubleConverter(0);
            }
            else if (fieldModelController is DocumentFieldModelController)
            {
                return new DocumentControllerToStringConverter();
            }
            else if (fieldModelController is DocumentCollectionFieldModelController)
            {
                return new DocumentCollectionToStringConverter();
            }
            return null;
        }

        protected static void BindTextAllignment(TextBlock element, DocumentController docController, Context context)
        {
            var alignmentBinding = new FieldBinding<NumberFieldModelController>()
            {
                Key = TextAlignmentKey,
                Document = docController,
                Converter = new IntToTextAlignmentConverter(),
                Mode = BindingMode.TwoWay,
                Context = context
            };
            element.AddFieldBinding(TextBlock.TextAlignmentProperty, alignmentBinding);
        }

        protected static void BindFontWeight(TextBlock element, DocumentController docController, Context context)
        {
            var fontWeightBinding = new FieldBinding<NumberFieldModelController>()
            {
                Key = FontWeightKey,
                Document = docController,
                Converter = new DoubleToFontWeightConverter(),
                Mode = BindingMode.TwoWay,
                Context = context
            };
            element.AddFieldBinding(TextBlock.FontWeightProperty, fontWeightBinding);
        }

        protected static void BindFontSize(FrameworkElement element, DocumentController docController, Context context)
        {
            var fontSizeBinding = new FieldBinding<NumberFieldModelController>()
            {
                Key = FontSizeKey,
                Document = docController,
                Mode = BindingMode.TwoWay,
                Context = context
            };
            element.AddFieldBinding(TextBlock.FontSizeProperty, fontSizeBinding);
        }

        #endregion


        #region GettersAndSetters
        

        private static ReferenceFieldModelController GetTextReference(DocumentController docController)
        {
            return docController.GetField(KeyStore.DataKey) as ReferenceFieldModelController;
        }

        private void SetTextAlignmentField(DocumentController docController, double textAlignment, bool forceMask, Context context)
        {
            docController.SetField(TextAlignmentKey, new NumberFieldModelController(textAlignment), forceMask); // set the field here so that forceMask is respected
        }

        private void SetFontSizeField(DocumentController docController, double fontSize, bool forceMask, Context context)
        {
            docController.SetField(FontSizeKey, new NumberFieldModelController(fontSize), forceMask); // set the field here so that forceMask is respected
        }

        private void SetFontWeightField(DocumentController docController, string fontWeight, bool forceMask, Context context)
        {
            docController.SetField(FontWeightKey, new TextFieldModelController(fontWeight), forceMask); // set the field here so that forceMask is respected
        }

        #endregion
    }
}