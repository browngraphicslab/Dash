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

            AddBinding(element, docController, FontWeightKey, context, BindFontWeight);
            AddBinding(element, docController, FontSizeKey, context, BindFontSize);
            //AddBinding(element, docController, KeyStore.DataKey, context, BindTextSource);
            SetupTextBinding(element, docController, context);
            AddBinding(element, docController, TextAlignmentKey, context, BindTextAllignment);
        }

        public override FrameworkElement makeView(DocumentController docController,
            Context context, bool isInterfaceBuilderLayout = false)
        {
            return MakeView(docController, context);
        }

        public static FrameworkElement MakeView(DocumentController docController, Context context, bool isInterfaceBuilderLayout = false)
        {
            // the text field model controller provides us with the DATA
            // the Document on this courtesty document provides us with the parameters to display the DATA.
            // X, Y, Width, and Height etc....

            // create the textblock
            EditableTextBlock tb = new EditableTextBlock();

            SetupBindings(tb.Block, docController, context);
            SetupBindings(tb.Box, docController, context);
            tb.Box.AcceptsReturn = true;
            CourtesyDocument.SetupBindings(tb.Container, docController, context);

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
                BindOperationInteractions(tb.Block, referenceToText.FieldReference.Resolve(context), reference.FieldKey, fmController);
            }

            if (isInterfaceBuilderLayout)
            {
                var selectableContainer = new SelectableContainer(tb.Container, docController);
                //SetupBindings(selectableContainer, docController, context);
                return selectableContainer;
            }

            return tb.Container;
        }
        #region Bindings

        protected static void BindProperty(FrameworkElement element, Binding binding,
            DependencyProperty textBoxProperty, DependencyProperty textBlockProperty)
        {
            if (element is TextBlock)
            {
                element.SetBinding(textBlockProperty, binding);
            }
            else if (element is TextBox)
            {
                element.SetBinding(textBoxProperty, binding);
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
            Binding sourceBinding = null;
            if (data is TextFieldModelController)
            {
                var textData = data as TextFieldModelController;
                sourceBinding = new Binding
                {
                    Source = textData,
                    Path = new PropertyPath(nameof(textData.Data)),
                    Mode = BindingMode.TwoWay,
                    UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged
                };
            }
            else if (data is NumberFieldModelController)
            {
                var numberData = data as NumberFieldModelController;
                sourceBinding = new Binding
                {
                    Source = numberData,
                    Path = new PropertyPath(nameof(numberData.Data)),
                    Mode = BindingMode.TwoWay,
                    Converter = new StringToDoubleConverter(0),
                    UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged
                };
            }
            else if (data is DocumentFieldModelController)
            {
                var docData = data as DocumentFieldModelController;

                sourceBinding = new Binding
                {
                    Source = docData,
                    Path = new PropertyPath(nameof(docData.Data)),
                    Mode = BindingMode.TwoWay,
                    Converter = new DocumentControllerToStringConverter(),
                    UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged
                };
                if (docData.Data != null)
                {
                    docData.Data.DocumentFieldUpdated += ((sender, ctxt) =>
                    {
                        sourceBinding = new Binding
                        {
                            Source = docData,
                            Path = new PropertyPath(nameof(docData.Data)),
                            Mode = BindingMode.TwoWay,
                            Converter = new DocumentControllerToStringConverter(),
                            UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged
                        };
                        BindProperty(element, sourceBinding, TextBox.TextProperty, TextBlock.TextProperty);
                    });
                }
            }
            else if (data is DocumentCollectionFieldModelController)
            {

                var docData = data as DocumentCollectionFieldModelController;
                sourceBinding = new Binding
                {
                    Source = docData,
                    Path = new PropertyPath(nameof(docData.Data)),
                    Mode = BindingMode.TwoWay,
                    Converter = new DocumentCollectionToStringConverter(),
                    UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged
                };
                //foreach (var ldoc in docData.Data)
                //    ldoc.DocumentFieldUpdated += ((sender, ctxt) =>
                //    {
                //        sourceBinding = new Binding
                //        {
                //            Source = docData,
                //            Path = new PropertyPath(nameof(docData.Data)),
                //            Mode = BindingMode.TwoWay,
                //            Converter = new DocumentCollectionToStringConverter(),
                //            UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged
                //        };
                //        BindProperty(element, sourceBinding, TextBox.TextProperty, TextBlock.TextProperty);
                //    });
            }
            if (sourceBinding != null)
                BindProperty(element, sourceBinding, TextBox.TextProperty, TextBlock.TextProperty);
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
            var alignmentBinding = new Binding
            {
                Source = textAlignmentData,
                Path = new PropertyPath(nameof(textAlignmentData.Data)),
                Mode = BindingMode.TwoWay,
                Converter = new IntToTextAlignmentConverter()
            };
            BindProperty(element, alignmentBinding, TextBox.TextAlignmentProperty, TextBlock.TextAlignmentProperty);
        }

        protected static void BindFontWeight(FrameworkElement element, DocumentController docController, Context context)
        {
            var fontWeightData = docController.GetDereferencedField(FontWeightKey, context) as TextFieldModelController;
            if (fontWeightData == null)
            {
                return;
            }
            var fontWeightBinding = new Binding
            {
                Source = fontWeightData,
                Path = new PropertyPath(nameof(fontWeightData.Data)),
                Mode = BindingMode.TwoWay,
                Converter = new StringToFontweightConverter()
            };
            BindProperty(element, fontWeightBinding, TextBox.FontWeightProperty, TextBlock.FontWeightProperty);
        }

        protected static void BindFontSize(FrameworkElement element, DocumentController docController, Context context)
        {
            var fontSizeData = docController.GetDereferencedField(FontSizeKey, context) as NumberFieldModelController;
            if (fontSizeData == null)
            {
                return;
            }
            var fontSizeBinding = new Binding
            {
                Source = fontSizeData,
                Path = new PropertyPath(nameof(fontSizeData.Data)),
                Mode = BindingMode.TwoWay,
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