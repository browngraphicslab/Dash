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
            SetTextAlignmentField(Document, DefaultTextAlignment, true, null);
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
            CourtesyDocument.SetupBindings(element, docController, context);
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
            FrameworkElement element;
            TextBlock tb;
            
            if (isEditable)
            {
                var editableTB = new EditableTextBlock(referenceToText, context);
                tb = editableTB.Block;
                CourtesyDocument.SetupBindings(editableTB, docController, context);
                element = editableTB;
            }
            else
            {
                tb = new TextBlock();
                element = tb;
            }
            SetupBindings(tb, docController, context);

            // add bindings to work with operators
            if (referenceToText != null) // only bind operation interactions if text is a reference
            {
                var fmController = docController.GetDereferencedField(KeyStore.DataKey, context);
                if (fmController is TextFieldModelController)
                    fmController = fmController as TextFieldModelController;
                else if (fmController is NumberFieldModelController)
                    fmController = fmController as NumberFieldModelController;
                var reference = docController.GetField(KeyStore.DataKey) as ReferenceFieldModelController;
                BindOperationInteractions(tb, referenceToText.FieldReference.Resolve(context), reference.FieldKey, fmController);
            }

            if (isInterfaceBuilderLayout)
            {
                var selectableContainer = new SelectableContainer(element, docController);
                //SetupBindings(selectableContainer, docController, context);
                return selectableContainer;
            }

            return element;
        }
        #region Bindings

        private static void TextingBox_KeyDown(object sender, KeyRoutedEventArgs e)
        {
            DBTest.ResetCycleDetection();
        }

        protected static void SetupTextBinding(TextBlock element, DocumentController docController, Context context)
        {
            var data = docController.GetDereferencedField(KeyStore.DataKey, context);
            if (data != null)
            {
                var binding = new FieldBinding<FieldModelController>()
                {
                    Document = docController,
                    Key = KeyStore.DataKey,
                    GetHandler = TextGetHandler,
                    SetHandler = TextSetHandler,
                    Mode = BindingMode.TwoWay,
                    Context = context,
                    GetConverter = GetFieldConverter
                };
                element.AddFieldBinding(TextBlock.TextProperty, binding);

                // we need to update the TextBlock's binding whenever it contains one or more documents and their primary key(s) change.
                // Although the binding target technically hasn't changed (it's still the same document(s)), the way it should be displayed (ie, the value of its primary keys) has.
                // The current binding mechanism doesn't promote document field changes to signal changes on DocumentFieldModels or DocumentCollectionFieldModels.
                if (data is DocumentCollectionFieldModelController)
                {
                    ContainedDocumentFieldUpdatedHandler hdlr = (collection, doc, subArgs) =>
                    {
                        if ((doc.GetDereferencedField(KeyStore.PrimaryKeyKey, subArgs.Context) as ListFieldModelController<TextFieldModelController>).Data.Where((d) => (d as TextFieldModelController).Data == subArgs.Reference.FieldKey.Id).Count() > 0)
                        {
                            element.AddFieldBinding(TextBlock.TextProperty, binding);
                        }
                    };
                    element.Loaded += (sender, args) => (data as DocumentCollectionFieldModelController).ContainedDocumentFieldUpdatedEvent += hdlr;
                    element.Unloaded += (sender, args) => (data as DocumentCollectionFieldModelController).ContainedDocumentFieldUpdatedEvent -= hdlr;
                }
                if (data is DocumentFieldModelController)
                {
                    OnDocumentFieldUpdatedHandler hdlr = ((sender, ctxt) =>
                    {
                        if (((data as DocumentFieldModelController).Data.GetDereferencedField(KeyStore.PrimaryKeyKey, ctxt.Context) as ListFieldModelController<TextFieldModelController>).Data.Where((d) => (d as TextFieldModelController).Data == ctxt.Reference.FieldKey.Id).Count() > 0)
                        {
                            element.AddFieldBinding(TextBlock.TextProperty, binding);
                        }
                    });
                    element.Loaded += (sender, args) => (data as DocumentFieldModelController).Data.DocumentFieldUpdated += hdlr;
                    element.Unloaded += (sender, args) => (data as DocumentFieldModelController).Data.DocumentFieldUpdated -= hdlr;
                }
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

        protected static object TextGetHandler(FieldModelController fieldModelController)
        {
            if (fieldModelController is TextFieldModelController)
            {
                return ((TextFieldModelController)fieldModelController).Data;
            }
            if (fieldModelController is NumberFieldModelController)
            {
                return ((NumberFieldModelController)fieldModelController).Data;
            }
            if (fieldModelController is DocumentFieldModelController)
            {
                return ((DocumentFieldModelController)fieldModelController).Data;
            }
            if (fieldModelController is DocumentCollectionFieldModelController)
            {
                return ((DocumentCollectionFieldModelController)fieldModelController).GetDocuments();
            }
            return null;
        }

        private static void TextSetHandler(object binder, FieldModelController fieldModelController, object value)
        {
            var binding = binder as FieldBinding<FieldModelController>;
            var refField = binding.Document.GetField(binding.Key) as ReferenceFieldModelController;
            if (value is string && refField != null)
            {
                refField.GetDocumentController(binding.Context).ParseDocField(refField.FieldKey,
                         value as string, binding.Document.GetDereferencedField<FieldModelController>(binding.Key, binding.Context));
            }
            else if (fieldModelController is TextFieldModelController)
            {
                var data = value as string;
                if (data != null)
                {
                    ((TextFieldModelController)fieldModelController).Data = data;
                }
            }
            else if (fieldModelController is NumberFieldModelController)
            {
                var data = value as double?;
                if (data != null)
                {
                    ((NumberFieldModelController)fieldModelController).Data = data.Value;
                }
            }
            else if (fieldModelController is DocumentFieldModelController)
            {
                var doc = value as DocumentController;
                if (doc != null)
                {
                    ((DocumentFieldModelController)fieldModelController).Data = doc;
                }
            }
            else if (fieldModelController is DocumentCollectionFieldModelController)
            {
                var list = value as List<DocumentController>;
                if (list != null)
                {
                    ((DocumentCollectionFieldModelController)fieldModelController).SetDocuments(
                        list);
                }
            }
        }

        private static void DocData_FieldModelUpdated(FieldModelController sender, Context context)
        {
            throw new NotImplementedException();
        }

        protected static void BindTextAllignment(TextBlock element, DocumentController docController, Context context)
        {
            var textAlignmentData = docController.GetDereferencedField(TextAlignmentKey, context) as NumberFieldModelController;
            if (textAlignmentData == null)
            {
                return;
            }
            //var alignmentBinding = new Binding
            //{
            //    Source = textAlignmentData,
            //    Path = new PropertyPath(nameof(textAlignmentData.Data)),
            //    Mode = BindingMode.TwoWay,
            //    Converter = new IntToTextAlignmentConverter()
            //};
            var alignmentBinding = new FieldBinding<NumberFieldModelController>()
            {
                Key = TextAlignmentKey,
                Document = docController,
                Converter = new IntToTextAlignmentConverter(),
                Mode = BindingMode.TwoWay,
                Context = context,
                GetHandler = (NumberFieldModelController field) => field.Data,
                SetHandler = delegate (object binder, NumberFieldModelController field, object value)
                {
                    var s = value as double?;
                    if (s != null)
                    {
                        field.Data = s.Value;
                    }
                }
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
                Context = context,
                GetHandler = (NumberFieldModelController field) => field.Data,
                SetHandler = delegate (object binder, NumberFieldModelController field, object value)
                {
                    var s = value as double?;
                    if (s != null)
                    {
                        field.Data = s.Value;
                    }
                }
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
                Context = context,
                GetHandler = (NumberFieldModelController field) => field.Data,
                SetHandler = delegate (object binder, NumberFieldModelController field, object value)
                {
                    var s = value as double?;
                    if (s != null)
                    {
                        field.Data = s.Value;
                    }
                }
            };
            element.AddFieldBinding(TextBlock.FontSizeProperty, fontSizeBinding);
        }

        #endregion


        #region GettersAndSetters

        private static FieldModelController GetTextField(DocumentController docController, Context context)
        {
            context = Context.SafeInitAndAddDocument(context, docController);
            return docController.GetField(KeyStore.DataKey)?
                .DereferenceToRoot<FieldModelController>(context);
        }

        private static ReferenceFieldModelController GetTextReference(DocumentController docController)
        {
            return docController.GetField(KeyStore.DataKey) as ReferenceFieldModelController;
        }

        private void SetTextAlignmentField(DocumentController docController, double textAlignment, bool forceMask, Context context)
        {
            var currentTextAlignmentField = new NumberFieldModelController(textAlignment);
            docController.SetField(TextAlignmentKey, currentTextAlignmentField, forceMask); // set the field here so that forceMask is respected
        }

        private void SetFontSizeField(DocumentController docController, double fontSize, bool forceMask, Context context)
        {
            var currentFontSizeField = new NumberFieldModelController(fontSize);
            docController.SetField(FontSizeKey, currentFontSizeField, forceMask); // set the field here so that forceMask is respected
        }

        private void SetFontWeightField(DocumentController docController, string fontWeight, bool forceMask, Context context)
        {
            var currentFontWeightField = new TextFieldModelController(fontWeight);
            docController.SetField(FontWeightKey, currentFontWeightField, forceMask); // set the field here so that forceMask is respected
        }

        #endregion
    }
}