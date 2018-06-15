using System;
using Windows.UI.Xaml;
using DashShared;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.Foundation;
using Dash.Converters;
using Windows.UI.Xaml.Controls;

namespace Dash
{
    class MarkdownBox : CourtesyDocument
    {
        public static DocumentType DocumentType = new DocumentType("C2093D8E-0D4D-4616-A359-B3BEF816E6DA", "Markdown Box");
        private static readonly string PrototypeId = "23C2120A-735C-4C90-83C0-492816C17A8F";

        public MarkdownBox(FieldControllerBase refToRichText, double x = 0, double y = 0, double w = 200, double h = 20)
        {
            var fields = DefaultLayoutFields(new Point(x, y), new Size(w, h), refToRichText);
            SetupDocument(DocumentType, PrototypeId, "MarkdownBox Prototype Layout", fields);
        }
        protected static void SetupTextBinding(EditableMarkdownBlock element, DocumentController docController, Context context)
        {
            /*
            var data = docController.GetDereferencedField(KeyStore.DataKey, context);
            if (data != null)
            {
                var binding = new FieldBinding<RichTextController>()
                {
                    Document = docController,
                    Key = KeyStore.DataKey,
                    Mode = BindingMode.TwoWay,
                    Context = context,
                    Tag = "Markdown Box Text Binding"
                };
                element.AddFieldBinding(RichTextView.TextProperty, binding);

                var wrapBinding = new FieldBinding<TextController>()
                {
                    Document = docController,
                    Key = KeyStore.TextWrappingKey,
                    Mode = BindingMode.OneWay,
                    Converter = new StringToTextWrappingConverter(),
                    Context = context,
                    Tag = "Markdown Box Text Wrapping Binding"
                };
                element.XMarkdownBox.AddFieldBinding(RichEditBox.TextWrappingProperty, wrapBinding);
            }*/

            var binding = new FieldBinding<FieldControllerBase>()
            {
                Document = docController,
                Key = KeyStore.DataKey,
                Mode = BindingMode.TwoWay,
                Context = context,
                GetConverter = FieldConversion.GetFieldtoStringConverter,
                FallbackValue = "<null>",
                Tag = "MarkdownBox SetupTextBinding"
            };
            element.AddFieldBinding(EditableMarkdownBlock.TextProperty, binding);
        }

        protected static void SetupBindings(EditableMarkdownBlock element, DocumentController docController, Context context)
        {
            CourtesyDocument.SetupBindings(element, docController, context);
            
            SetupTextBinding(element, docController, context);
        }
        /*
        public static DocumentController MakeRegionDocument(DocumentView richTextBox)
        {
            var rtv = richTextBox.GetFirstDescendantOfType<EditableMarkdownBlock>();
            return rtv.GetRegionDocument();
        }*/
        public static FrameworkElement MakeView(DocumentController docController, Context context)
        {
            /*
            EditableMarkdownBlock rtv = null;
            var dataField = docController.GetField(KeyStore.DataKey);
            var refToRichText = dataField as ReferenceController;
            var fieldModelController = (refToRichText?.DereferenceToRoot(context) ?? dataField) as RichTextController;
            if (fieldModelController != null)
            {
                rtv = new EditableMarkdownBlock()
                {
                   // LayoutDocument = docController.GetActiveLayout() ?? docController,
                   // DataDocument = refToRichText?.GetDocumentController(context) ?? docController.GetDataDocument()
                };
                rtv.ManipulationMode = ManipulationModes.All;
                rtv.PointerEntered += (sender, args) => rtv.ManipulationMode = ManipulationModes.None;
                rtv.GotFocus += (sender, args) => rtv.ManipulationMode = ManipulationModes.None;
                rtv.LostFocus += (sender, args) => rtv.ManipulationMode = ManipulationModes.All;
                //TODO: lose focus when you drag the rich text view so that text doesn't select at the same time
                rtv.HorizontalAlignment = HorizontalAlignment.Stretch;
                rtv.VerticalAlignment = VerticalAlignment.Stretch;
                SetupTextBinding(rtv, docController, context);
                SetupBindings(rtv, docController, context);
            }

            return rtv;*/

            var textController = docController.GetField(KeyStore.DataKey);
            // create the textblock
            var tb = new EditableMarkdownBlock
            {
                TargetFieldController = textController,
                TargetDocContext = context
            };
            SetupBindings(tb, docController, context);

            return tb;
        }

        /*
        private static ReferenceController GetTextReference(DocumentController docController)
        {
            return docController.GetField(KeyStore.DataKey) as ReferenceController;
        }*/
    }
}
