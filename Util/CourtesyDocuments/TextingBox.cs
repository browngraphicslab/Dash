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

namespace Dash
{
    /// <summary>
    /// A generic document type containing a single text element.
    /// </summary>
    public class TextingBox : CourtesyDocument
    {
        public static Key FontWeightKey = new Key("03FC5C4B-6A5A-40BA-A262-578159E2D5F7", "FontWeight");
        public static Key FontSizeKey = new Key("75902765-7F0E-4AA6-A98B-3C8790DBF7CE", "FontSize");
        public static Key TextAlignmentKey = new Key("3BD4572A-C6C9-4710-8E74-831204D2C17D", "Font Alignment");
        public static DocumentType DocumentType =
            new DocumentType("181D19B4-7DEC-42C0-B1AB-365B28D8EA42", "Texting Box");

        public static string DefaultText = "Default Text";
        public static double DefaultFontWeight = 100;
        public static double DefaultTextAlignment = (int)TextAlignment.Left;
        public static double DefaultFontSize = 12;
        private static string PrototypeId = "F917C90C-14E8-45E0-A524-94C8958DDC4F";

        public TextingBox(FieldModelController refToText, double x = 0, double y = 0, double w = 200, double h = 20)
        {
            var fields = DefaultLayoutFields(new Point(x, y), new Size(w, h), refToText);
            Document = GetLayoutPrototype().MakeDelegate();
            Document.SetFields(fields, true);
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

            SetFontWeightField(prototypeDocument, DefaultFontWeight, true, null);
            SetFontSizeField(prototypeDocument, DefaultFontSize, true, null);
            SetTextAlignmentField(prototypeDocument, DefaultTextAlignment, true, null);
            return prototypeDocument;
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
            FrameworkElement tb = null;

            // use the reference to the text to get the text field model controller
            var textField = GetTextField(docController, context);
            Debug.Assert(textField != null);
            if (textField is TextFieldModelController)
            {
                var textBox = new TextBox();
                textBox.GotFocus += (s, e) => textBox.ManipulationMode = ManipulationModes.None;
                textBox.LostFocus += (s, e) => textBox.ManipulationMode = ManipulationModes.All;
                tb = textBox;
                tb.HorizontalAlignment = HorizontalAlignment.Stretch;
                tb.VerticalAlignment = VerticalAlignment.Stretch;
                textBox.TextWrapping = Windows.UI.Xaml.TextWrapping.Wrap;
                var textFieldModelController = textField as TextFieldModelController;
                BindTextBoxSource(tb, textFieldModelController);
            }
            else if (textField is NumberFieldModelController)
            {
                var textBox = new TextBox();
                textBox.BorderThickness = new Thickness(5);
                textBox.BorderBrush = new SolidColorBrush(Colors.Gray);
                textBox.AcceptsReturn = true;
                tb = textBox;
                var numFieldModelController = textField as NumberFieldModelController;
                BindTextBoxSource(tb, numFieldModelController);
            }
            else if (textField is RichTextFieldModelController)
            {
                tb = new TextBlock();
                var richTextFieldModelController = textField as RichTextFieldModelController;
                BindTextBlockSource(tb, richTextFieldModelController);
            }
            docController.AddFieldUpdatedListener(DashConstants.KeyStore.DataKey,
                delegate (DocumentController sender, DocumentController.DocumentFieldUpdatedEventArgs args)
                {
                    var textField2 = GetTextField(sender, args.Context);
                    if (textField2 is TextFieldModelController)
                    {
                        BindTextBoxSource(tb, textField2 as TextFieldModelController);
                    }
                    if (textField2 is NumberFieldModelController)
                    {
                        BindTextBoxSource(tb, textField2 as NumberFieldModelController);
                    }
                    else if (textField is RichTextFieldModelController)
                    {
                        BindTextBlockSource(tb, textField2 as RichTextFieldModelController);
                    }
                });

            // bind the text height
            var heightController = GetHeightField(docController, context);
            BindHeight(tb, heightController);

            // bind the text width
            var widthController = GetWidthField(docController, context);
            BindWidth(tb, widthController);

            var fontWeightController = GetFontWeightField(docController, context);
            BindFontWeight(tb, fontWeightController);

            var fontSizeController = GetFontSizeField(docController, context);
            BindFontSize(tb, fontSizeController);

            var textAlignmentController = GetTextAlignmentField(docController, context);
            BindTextAlignment(tb, textAlignmentController);

            // add bindings to work with operators
            var referenceToText = GetTextReference(docController);
            if (referenceToText != null) // only bind operation interactions if text is a reference
            {
                BindOperationInteractions(tb, referenceToText);
            }

            var doc = referenceToText.GetDocumentController(context);
            doc.AddFieldUpdatedListener(referenceToText.FieldKey, delegate (DocumentController sender, DocumentController.DocumentFieldUpdatedEventArgs args)
            {
                Debug.Assert(args.Reference.FieldKey.Equals(referenceToText.FieldKey));
                string id = args.Context.GetDeepestDelegateOf(args.Reference.DocId);
                Debug.Assert(id.Equals(referenceToText.GetDocumentController(context).GetId()));
                if (args.Action != DocumentController.FieldUpdatedAction.Update)
                {
                    var field = GetTextField(docController, args.Context);
                    Debug.Assert(field != null);
                    if (field is TextFieldModelController)
                    {
                        var textFieldModelController = field as TextFieldModelController;
                        BindTextBoxSource(tb, textFieldModelController);
                    }
                    else if (field is NumberFieldModelController)
                    {
                        var numFieldModelController = field as NumberFieldModelController;
                        BindTextBoxSource(tb, numFieldModelController);
                    }
                    else if (field is RichTextFieldModelController)
                    {
                        var richTextFieldModelController = field as RichTextFieldModelController;
                        BindTextBlockSource(tb, richTextFieldModelController);
                    }
                }
            });

            if (isInterfaceBuilderLayout)
            {
                var selectableContainer = new SelectableContainer(tb, docController);
                return selectableContainer;
            }

            return tb;
        }


        #region GettersAndSetters

        private static FieldModelController GetTextField(DocumentController docController, Context context)
        {
            context = Context.SafeInitAndAddDocument(context, docController);
            return docController.GetField(DashConstants.KeyStore.DataKey)?
                .DereferenceToRoot<FieldModelController>(context);
        }

        private static ReferenceFieldModelController GetTextReference(DocumentController docController)
        {
            return docController.GetField(DashConstants.KeyStore.DataKey) as ReferenceFieldModelController;
        }

        private static NumberFieldModelController GetTextAlignmentField(DocumentController docController, Context context)
        {
            context = Context.SafeInitAndAddDocument(context, docController);
            return docController.GetField(TextAlignmentKey)?
                .DereferenceToRoot<NumberFieldModelController>(context);
        }

        private static NumberFieldModelController GetFontWeightField(DocumentController docController, Context context)
        {
            context = Context.SafeInitAndAddDocument(context, docController);
            return docController.GetField(FontWeightKey)?
                .DereferenceToRoot<NumberFieldModelController>(context);
        }

        protected static NumberFieldModelController GetFontSizeField(DocumentController docController, Context context)
        {
            context = Context.SafeInitAndAddDocument(context, docController);
            return docController.GetField(FontSizeKey)?
                .DereferenceToRoot<NumberFieldModelController>(context);
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

        private void SetFontWeightField(DocumentController docController, double fontWeight, bool forceMask, Context context)
        {
            var currentFontWeightField = new NumberFieldModelController(fontWeight);
            docController.SetField(FontWeightKey, currentFontWeightField, forceMask); // set the field here so that forceMask is respected
        }

        #endregion

        #region Bindings

        protected static void BindTextAlignment(FrameworkElement renderElement,
            NumberFieldModelController textAlignmentController)
        {
            if (textAlignmentController == null) throw new ArgumentNullException(nameof(textAlignmentController));
            var alignmentBinding = new Binding
            {
                Source = textAlignmentController,
                Path = new PropertyPath(nameof(textAlignmentController.Data)),
                Mode = BindingMode.TwoWay,
                Converter = new IntToTextAlignmentConverter()
            };
            if (renderElement is TextBlock)
            {
                renderElement.SetBinding(TextBlock.TextAlignmentProperty, alignmentBinding);
            }
            else if (renderElement is TextBox)
            {
                renderElement.SetBinding(TextBox.TextAlignmentProperty, alignmentBinding);
            }
            else
            {
                Debug.Assert(false, $"we don't support alignment for elements of type {renderElement.GetType()}");
            }
        }

        protected static void BindFontWeight(FrameworkElement renderElement,
            NumberFieldModelController fontWeightController)
        {
            if (fontWeightController == null) throw new ArgumentNullException(nameof(fontWeightController));
            var fontWeightBinding = new Binding
            {
                Source = fontWeightController,
                Path = new PropertyPath(nameof(fontWeightController.Data)),
                Mode = BindingMode.TwoWay,
                Converter = new DoubleToFontWeightConverter()
            };
            if (renderElement is TextBlock)
            {
                renderElement.SetBinding(Control.FontWeightProperty, fontWeightBinding);
            }
            else if (renderElement is TextBox)
            {
                renderElement.SetBinding(Control.FontWeightProperty, fontWeightBinding);
            }
            else
            {
                Debug.Assert(false, $"we don't support fontweight for elements of type {renderElement.GetType()}");
            }
        }

        protected static void BindFontSize(FrameworkElement renderElement,
            NumberFieldModelController sizeController)
        {
            if (sizeController == null) throw new ArgumentNullException(nameof(sizeController));
            var fontSizeBinding = new Binding
            {
                Source = sizeController,
                Path = new PropertyPath(nameof(sizeController.Data)),
                Mode = BindingMode.TwoWay,
            };
            if (renderElement is TextBlock)
            {
                renderElement.SetBinding(Control.FontSizeProperty, fontSizeBinding);
            }
            else if (renderElement is TextBox)
            {
                renderElement.SetBinding(Control.FontSizeProperty, fontSizeBinding);
            }
            else
            {
                Debug.Assert(false, $"we don't support fontsize for elements of type {renderElement.GetType()}");
            }
        }

        private static void BindTextBlockSource(FrameworkElement renderElement, RichTextFieldModelController fieldModelController)
        {
            var sourceBinding = new Binding
            {
                Source = fieldModelController,
                Path = new PropertyPath(nameof(fieldModelController.RichTextData))
            };
            renderElement.SetBinding(TextBlock.TextProperty, sourceBinding);
        }

        private static void BindTextBlockSource(FrameworkElement renderElement, NumberFieldModelController fieldModelController)
        {
            //<<<<<<< HEAD
            var sourceBinding = new Binding
            {
                Source = fieldModelController,
                Path = new PropertyPath(nameof(fieldModelController.Data))
            };
            renderElement.SetBinding(TextBlock.TextProperty, sourceBinding);
            //=======
            //                // the text field model controller provides us with the DATA
            //                // the Document on this courtesty document provides us with the parameters to display the DATA.
            //                // X, Y, Width, and Height etc....

            //                // create the textblock
            //                FrameworkElement tb = null;
            //                FrameworkElement child = null;

            //                // use the reference to the text to get the text field model controller
            //                ReferenceFieldModelController refToData;
            //                var fieldModelController = GetDereferencedDataFieldModelController(docController, context, new TextFieldModelController("<default>"), out refToData);
            //                var doc = refToData.GetDocumentController(context);
            //                Debug.Assert(fieldModelController != null);
            //                if (fieldModelController is TextFieldModelController)
            //                {
            //                    var textFieldModelController = fieldModelController as TextFieldModelController;
            //                    var textBox = new TextBox();
            //                    textBox.ManipulationDelta += (s, e) => e.Handled = true;
            //                    tb = textBox;
            //                    textBox.AcceptsReturn = true;
            //                    tb.HorizontalAlignment = HorizontalAlignment.Stretch;
            //                    tb.VerticalAlignment = VerticalAlignment.Stretch;
            //                    // make text update when changed
            //                    var sourceBinding = new Binding
            //                    {
            //                        Source = textFieldModelController,
            //                        Path = new PropertyPath(nameof(textFieldModelController.Data)),
            //                        Mode = BindingMode.TwoWay
            //                    };
            //                    tb.SetBinding(TextBox.TextProperty, sourceBinding);
            //                    textBox.TextWrapping = Windows.UI.Xaml.TextWrapping.Wrap;
            //                    textBox.TextChanged += TextBox_TextChanged;
            //                    textBox.Tag = refToData.GetDocumentController(context);
            //                    child = textBox;
            //                }
            //                else if (fieldModelController is NumberFieldModelController)
            //                {
            //                    var numFieldModelController = fieldModelController as NumberFieldModelController;
            //                    var textBox = new TextBox();
            //                    textBox.ManipulationDelta += (s, e) => e.Handled = true;
            //                    tb = textBox;
            //                    textBox.AcceptsReturn = false;
            //                    tb.HorizontalAlignment = HorizontalAlignment.Stretch;
            //                    tb.VerticalAlignment = VerticalAlignment.Stretch;
            //                    // make text update when changed
            //                    var sourceBinding = new Binding
            //                    {
            //                        Source = numFieldModelController,
            //                        Converter = new StringToDoubleConverter(0),
            //                        Path = new PropertyPath(nameof(numFieldModelController.Data)),
            //                        Mode = BindingMode.TwoWay,
            //                        UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged
            //                    };
            //                    tb.SetBinding(TextBox.TextProperty, sourceBinding);
            //                    textBox.TextWrapping = Windows.UI.Xaml.TextWrapping.Wrap;
            //                    textBox.TextChanged += TextBox_NumberChanged;
            //                    textBox.Tag = numFieldModelController;
            //                    child = textBox;
            //                }
            //                else if (fieldModelController is DocumentFieldModelController)
            //                {
            //                    var documentfieldModelController = fieldModelController as DocumentFieldModelController;
            //                    return documentfieldModelController.Data.MakeViewUI(context);
            //                }

            //                doc.DocumentFieldUpdated += delegate (DocumentController.DocumentFieldUpdatedEventArgs args)
            //                {
            //                    string s = args.Context.GetDeepestDelegateOf(args.Reference.DocId);
            //                    if (args.Action != DocumentController.FieldUpdatedAction.Update && s.Equals(refToData.GetDocumentController(context).GetId()) && args.Reference.FieldKey.Equals(refToData.FieldKey))
            //                    {
            //                        var fmc = args.Reference.DereferenceToRoot(new Context(args.Context));
            //                        Binding sourceBinding = null;
            //                        if (fmc is TextFieldModelController)
            //                        {
            //                            TextFieldModelController tfmc = fmc as TextFieldModelController;
            //                            sourceBinding = new Binding
            //                            {
            //                                Source = fmc,
            //                                Path = new PropertyPath(nameof(tfmc.Data)),
            //                                Mode = BindingMode.TwoWay
            //                            };
            //                        }
            //                        else if (fmc is NumberFieldModelController)
            //                        {
            //                            NumberFieldModelController nfmc = fmc as NumberFieldModelController;
            //                            sourceBinding = new Binding
            //                            {
            //                                Source = nfmc,
            //                                Converter = new StringToDoubleConverter(0),
            //                                Path = new PropertyPath(nameof(nfmc.Data)),
            //                                Mode = BindingMode.TwoWay,
            //                                UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged
            //                            };
            //                        }
            //                        child.SetBinding(TextBox.TextProperty, sourceBinding);
            //                    }
            //                };

            //                var border = new Border();
            //                border.Child = tb;

            //                var fontWeightController = GetFontWeightFieldController(docController, context);
            //                BindFontWeight(tb, fontWeightController);

            //                var fontSizeController = GetFontSizeFieldController(docController, context);
            //                BindFontSize(tb, fontSizeController);

            //                var textAlignmentController = GetTextAlignmentFieldController(docController, context);
            //                BindTextAlignment(tb, textAlignmentController);

            //                tb = border;
            //                // bind the text height
            //                var heightController = GetHeightFieldController(docController, context);
            //                BindHeight(tb, heightController);

            //                // bind the text width
            //                var widthController = GetWidthFieldController(docController, context);
            //                BindWidth(tb, widthController);

            //                // add bindings to work with operators
            //                BindOperationInteractions(refToData.Resolve(context), tb);

            //                border.BorderThickness = new Thickness(5);
            //                border.BorderBrush = new SolidColorBrush(Windows.UI.Color.FromArgb(50, 50, 50, 50));

            //                return border;
            //>>>>>>> origin/master
        }

        private static void BindTextBoxSource(FrameworkElement renderElement, TextFieldModelController fieldModelController)
        {
            var sourceBinding = new Binding
            {
                Source = fieldModelController,
                Path = new PropertyPath(nameof(fieldModelController.Data)),
                Mode = BindingMode.TwoWay,
                UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged
            };
            renderElement.SetBinding(TextBox.TextProperty, sourceBinding);
        }

        private static void BindTextBoxSource(FrameworkElement renderElement, NumberFieldModelController fieldModelController)
        {
            var sourceBinding = new Binding
            {
                Source = fieldModelController,
                Path = new PropertyPath(nameof(fieldModelController.Data)),
                Mode = BindingMode.TwoWay,
                Converter = new StringToDoubleConverter(0),
                UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged
            };
            renderElement.SetBinding(TextBox.TextProperty, sourceBinding);
        }

        #endregion
    }
}