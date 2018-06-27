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

        protected static void SetupBindings(DishReplView element, DocumentController docController, Context context)
        {
            CourtesyDocument.SetupBindings(element, docController, context);

        }



        public static FrameworkElement MakeView(DocumentController docController, Context context)
        {
            var textController = docController.GetField(KeyStore.DataKey);
            var size = docController.GetField(KeyStore.ActualSizeKey).DereferenceToRoot<PointController>(null).Data;


            var tb = new DishReplView
            {
                TargetFieldController = textController,
                TargetDocContext = context,
               TargetSize = size
            };
            SetupBindings(tb, docController, context);

            return tb;
        }

    }
}
