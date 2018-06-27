using DashShared;
using Windows.UI.Xaml;
using Windows.Foundation;

namespace Dash
{
    class DishReplBox : CourtesyDocument
    {
        public static DocumentType DocumentType = new DocumentType("BD5890ED-EC33-4FDB-AA16-E633FA3BCEC5", "Dish Repl Box");
        private static readonly string PrototypeId = "BD5890ED-EC33-4FDB-AA16-E633FA3BCEC5";

        public DishReplBox(FieldControllerBase refToRichText, double x = 0, double y = 0, double w = 200, double h = 20)
        {
            var fields = DefaultLayoutFields(new Point(x, y), new Size(w, h), refToRichText);
            SetupDocument(DocumentType, PrototypeId, "DishReplBox Prototype Layout", fields);
        }
        protected static void SetupTextBinding(DishReplView element, DocumentController docController, Context context)
        {
            //var binding = new FieldBinding<FieldControllerBase>()
            //{
            //    Document = docController,
            //    Key = KeyStore.DataKey,
            //    Mode = Windows.UI.Xaml.Data.BindingMode.TwoWay,
            //    Context = context,
            //    FallbackValue = "<empty>",
            //    Tag = "DishReplBox SetupTextBinding"
            //};
            //element.AddFieldBinding(EditableMarkdownBlock.TextProperty, binding);
        }

        protected static void SetupBindings(DishReplView element, DocumentController docController, Context context)
        {
            CourtesyDocument.SetupBindings(element, docController, context);

            //SetupTextBinding(element, docController, context);
        }



        public static FrameworkElement MakeView(DocumentController docController, Context context)
        {
            var textController = docController.GetField(KeyStore.DataKey);
            // create the textblock
            //TODO Make TargetFieldController be a FieldReference to the field instead of just the field
            var tb = new DishReplView
            {
                TargetFieldController = textController,
                TargetDocContext = context
            };
            SetupBindings(tb, docController, context);

            return tb;
        }

    }
}
