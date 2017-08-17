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

        protected new static void SetupBindings(FrameworkElement element, DocumentController docController, Context context)
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
            // create the textblock
            var editableTB = new EditableTextBlock();
            TextBlock tb = isEditable ? editableTB.Block : new TextBlock(); 

            SetupBindings(tb, docController, context);
            if (isEditable) {
                SetupBindings(editableTB.Box, docController, context);
                CourtesyDocument.SetupBindings(editableTB, docController, context); 
            }

            // add bindings to work with operators
            var referenceToText = GetTextReference(docController);
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
                var selectableContainer = new SelectableContainer(tb, docController);
                //SetupBindings(selectableContainer, docController, context);
                return selectableContainer;
            }

            if (isEditable) return editableTB; 
            return tb;
        }
        #region Bindings

        protected static void BindProperty<T>(FrameworkElement element, FieldBinding<T> binding,
            DependencyProperty textBoxProperty, DependencyProperty textBlockProperty) where T : FieldModelController
        {
            if (element is TextBlock)
            {
                element.AddFieldBinding(textBlockProperty, binding);
            }
            else if (element is TextBox)
            {
                element.AddFieldBinding(textBoxProperty, binding);
                (element as TextBox).KeyDown += TextingBox_KeyDown;
            }
        }

        private static void TextingBox_KeyDown(object sender, KeyRoutedEventArgs e)
        {
            DBTest.ResetCycleDetection();
        }

        protected static void SetupTextBinding(FrameworkElement element, DocumentController controller, Context context)
        {
            var data = controller.GetField(KeyStore.DataKey);
            if (data is ReferenceFieldModelController)
            {
                var reference = data as ReferenceFieldModelController;
                var dataDoc = reference.GetDocumentController(context);
                //dataDoc.AddFieldUpdatedListener(reference.FieldKey,
                DocumentController.OnDocumentFieldUpdatedHandler handler = delegate (DocumentController sender, DocumentController.DocumentFieldUpdatedEventArgs args)
                {
                    if (args.Action == DocumentController.FieldUpdatedAction.Update || args.FromDelegate)
                    {
                        return;
                    }
                    BindTextSource(element, sender, args.Context, reference.FieldKey);
                };
                element.Loaded += delegate
                {
                    dataDoc.AddFieldUpdatedListener(reference.FieldKey, handler);
                };
                element.Unloaded += delegate
                {
                    dataDoc.RemoveFieldUpdatedListener(reference.FieldKey, handler);
                };
            }
            BindTextSource(element, controller, context, KeyStore.DataKey);
        }

        protected static void BindTextSource(FrameworkElement element, DocumentController docController, Context context, KeyController key)
        {
            var data = docController.GetDereferencedField(key, context);
            if (data == null)
            {
                return;
            }
            IValueConverter converter = null;
            SetHandler<FieldModelController> setHandler = TextSetHandler;
            GetHandler<FieldModelController> getHandler = TextGetHandler;
            if (data is TextFieldModelController)
            {

            }
            else if (data is NumberFieldModelController)
            {
                converter = new StringToDoubleConverter(0);
            }
            else if (data is DocumentFieldModelController)
            {
                converter = new DocumentControllerToStringConverter();
                var docData = data as DocumentFieldModelController;

                //sourceBinding = new Binding
                //{
                //    Source = docData,
                //    Mode = BindingMode.TwoWay,
                //    Converter = new DocumentFieldModelToStringConverter(),
                //    UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged
                //};
            }
            else if (data is DocumentCollectionFieldModelController)
            {
                converter = new DocumentCollectionToStringConverter();
                //var collectionDoc = data as DocumentCollectionFieldModelController;
                //collectionDoc.ContainedDocumentFieldUpdatedEvent += (collection, doc, args) =>
                //{
                //    var bndng = new FieldBinding<FieldModelController>()
                //    {
                //        Document = docController,
                //        Key = key,
                //        GetHandler = getHandler,
                //        SetHandler = setHandler,
                //        Mode = BindingMode.TwoWay,
                //        Context = context,
                //        Converter = converter
                //    };
                //    BindProperty(element, bndng, TextBox.TextProperty, TextBlock.TextProperty);
                //};
            }
            var binding = new FieldBinding<FieldModelController>()
            {
                Document = docController,
                Key = key,
                GetHandler = getHandler,
                SetHandler = setHandler,
                Mode = BindingMode.TwoWay,
                Context = context,
                Converter = converter
            };
            BindProperty(element, binding, TextBox.TextProperty, TextBlock.TextProperty);
        }

        private static object TextGetHandler(FieldModelController fieldModelController)
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

        private static void TextSetHandler(FieldModelController fieldModelController, object value)
        {
            if (fieldModelController is TextFieldModelController)
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

        protected static void BindTextAllignment(FrameworkElement element, DocumentController docController, Context context)
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
                SetHandler = delegate (NumberFieldModelController field, object value)
                {
                    var s = value as double?;
                    if (s != null)
                    {
                        field.Data = s.Value;
                    }
                }
            };
            BindProperty(element, alignmentBinding, TextBox.TextAlignmentProperty, TextBlock.TextAlignmentProperty);
        }

        protected static void BindFontWeight(FrameworkElement element, DocumentController docController, Context context)
        {
            var fontWeightBinding = new FieldBinding<NumberFieldModelController>()
            {
                Key = FontWeightKey,
                Document = docController,
                Converter = new DoubleToFontWeightConverter(),
                Mode = BindingMode.TwoWay,
                Context = context,
                GetHandler = (NumberFieldModelController field) => field.Data,
                SetHandler = delegate (NumberFieldModelController field, object value)
                {
                    var s = value as double?;
                    if (s != null)
                    {
                        field.Data = s.Value;
                    }
                }
            };
            BindProperty(element, fontWeightBinding, TextBox.FontWeightProperty, TextBlock.FontWeightProperty);
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
                SetHandler = delegate (NumberFieldModelController field, object value)
                {
                    var s = value as double?;
                    if (s != null)
                    {
                        field.Data = s.Value;
                    }
                }
            };
            BindProperty(element, fontSizeBinding, TextBox.FontSizeProperty, TextBlock.FontSizeProperty);
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