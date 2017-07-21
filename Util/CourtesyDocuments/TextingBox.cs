﻿using System;
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

        protected new static void SetupBindings(FrameworkElement element, DocumentController docController, Context context)
        {
            CourtesyDocument.SetupBindings(element, docController, context);

            AddBinding(element, docController, FontWeightKey, context, BindFontWeight);
            AddBinding(element, docController, FontSizeKey, context, BindFontSize);
            AddBinding(element, docController, DashConstants.KeyStore.DataKey, context, BindTextSource);
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
            CourtesyDocument.SetupBindings(tb.Container, docController, context);
            // use the reference to the text to get the text field model controller
            //if (textField is TextFieldModelController)
            //{
            //    TextBox textBox = new TextBox();
            //    SetupBindings(textBox, docController, context);
            //    tb = textBox;
            //    textBox.GotFocus += (s, e) => textBox.ManipulationMode = ManipulationModes.None;
            //    textBox.LostFocus += (s, e) => textBox.ManipulationMode = ManipulationModes.All;
            //    //textBox.TextWrapping = Windows.UI.Xaml.TextWrapping.Wrap;
            //}
            //else if (textField is NumberFieldModelController)
            //{
            //    TextBox textBox = new TextBox();
            //    SetupBindings(textBox, docController, context);
            //    tb = textBox;
            //    textBox.GotFocus += (s, e) => textBox.ManipulationMode = ManipulationModes.None;
            //    textBox.LostFocus += (s, e) => textBox.ManipulationMode = ManipulationModes.All;
            //    textBox.BorderThickness = new Thickness(5);
            //    textBox.BorderBrush = new SolidColorBrush(Colors.Gray);
            //    textBox.AcceptsReturn = true;
            //}

            //docController.AddFieldUpdatedListener(DashConstants.KeyStore.DataKey,
            //    delegate (DocumentController sender, DocumentController.DocumentFieldUpdatedEventArgs args)
            //    {
            //        var textField2 = GetTextField(sender, args.Context);
            //        if (textField2 is TextFieldModelController)
            //        {
            //            BindTextBoxSource(tb, textField2 as TextFieldModelController);
            //        }
            //        if (textField2 is NumberFieldModelController)
            //        {
            //            BindTextBoxSource(tb, textField2 as NumberFieldModelController);
            //        }
            //        else if (textField is RichTextFieldModelController)
            //        {
            //            BindTextBlockSource(tb, textField2 as RichTextFieldModelController);
            //        }
            //    });

            // add bindings to work with operators
            var referenceToText = GetTextReference(docController);
            if (referenceToText != null) // only bind operation interactions if text is a reference
            {
                BindOperationInteractions(tb.Block, referenceToText);
            }

            //var doc = referenceToText.GetDocumentController(context);
            //doc.AddFieldUpdatedListener(referenceToText.FieldKey, delegate (DocumentController sender, DocumentController.DocumentFieldUpdatedEventArgs args)
            //{
            //    Debug.Assert(args.Reference.FieldKey.Equals(referenceToText.FieldKey));
            //    string id = args.Context.GetDeepestDelegateOf(args.Reference.DocId);
            //    Debug.Assert(id.Equals(referenceToText.GetDocumentController(context).GetId()));
            //    if (args.Action != DocumentController.FieldUpdatedAction.Update)
            //    {
            //        var field = GetTextField(docController, args.Context);
            //        Debug.Assert(field != null);
            //        if (field is TextFieldModelController)
            //        {
            //            var textFieldModelController = field as TextFieldModelController;
            //            BindTextBoxSource(tb, textFieldModelController);
            //        }
            //        else if (field is NumberFieldModelController)
            //        {
            //            var numFieldModelController = field as NumberFieldModelController;
            //            BindTextBoxSource(tb, numFieldModelController);
            //        }
            //        else if (field is RichTextFieldModelController)
            //        {
            //            var richTextFieldModelController = field as RichTextFieldModelController;
            //            BindTextBlockSource(tb, richTextFieldModelController);
            //        }
            //    }
            //});

            if (isInterfaceBuilderLayout)
            {
                var selectableContainer = new SelectableContainer(tb.Container, docController);
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
            }
        }

        protected static void BindTextSource(FrameworkElement element, DocumentController docController, Context context)
        {
            var data = docController.GetDereferencedField(DashConstants.KeyStore.DataKey, context);
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
            BindProperty(element, sourceBinding, TextBox.TextProperty, TextBlock.TextProperty);
        }

        protected static void BindTextAllignment(FrameworkElement element, DocumentController docController, Context context)
        {
            var textAlignmentData = docController.GetDereferencedField(TextAlignmentKey, context) as TextFieldModelController;
            if (textAlignmentData == null)
            {
                return;
            }
            var alignmentBinding = new Binding
            {
                Source = textAlignmentData,
                Path = new PropertyPath(nameof(textAlignmentData.Data)),
                Mode = BindingMode.TwoWay,
                Converter = new StringToEnumConverter<TextAlignment>()
            };
            BindProperty(element, alignmentBinding, TextBox.TextAlignmentProperty, TextBlock.TextAlignmentProperty);
        }

        protected static void BindFontWeight(FrameworkElement element, DocumentController docController, Context context)
        {
            var fontWeightData = docController.GetDereferencedField(FontWeightKey, context) as NumberFieldModelController;
            if (fontWeightData == null)
            {
                return;
            }
            var fontWeightBinding = new Binding
            {
                Source = fontWeightData,
                Path = new PropertyPath(nameof(fontWeightData.Data)),
                Mode = BindingMode.TwoWay,
                Converter = new DoubleToFontWeightConverter()
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
            return docController.GetField(DashConstants.KeyStore.DataKey)?
                .DereferenceToRoot<FieldModelController>(context);
        }

        private static ReferenceFieldModelController GetTextReference(DocumentController docController)
        {
            return docController.GetField(DashConstants.KeyStore.DataKey) as ReferenceFieldModelController;
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
    }
}