using Windows.UI.Xaml;
using DashShared;
using Windows.UI.Xaml.Data;
using Windows.Foundation;

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
            var binding = new FieldBinding<FieldControllerBase>()
            {
                Document = docController,
                Key = KeyStore.DataKey,
                Mode = BindingMode.TwoWay,
                Context = context,
                FallbackValue = "<empty>",
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
            var textController = docController.GetField(KeyStore.DataKey);
            // create the textblock
            //TODO Make TargetFieldController be a FieldReference to the field instead of just the field
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
