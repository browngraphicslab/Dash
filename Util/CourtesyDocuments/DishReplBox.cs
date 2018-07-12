using System.Collections.Generic;
using DashShared;
using Windows.UI.Xaml;
using Windows.Foundation;

namespace Dash
{
    class DishReplBox : CourtesyDocument
    {
        public static DocumentType DocumentType = new DocumentType("BD5890ED-EC33-4FDB-AA16-E633FA3BCEC5", "Dish Repl Box");
        private static readonly string PrototypeId = "BD5890ED-EC33-4FDB-AA16-E633FA3BCEC5";

        public DishReplBox(double x = 0, double y = 0, double w = 200, double h = 200)
        {
            var fields = DefaultLayoutFields(new Point(x, y), new Size(w, h));
            SetupDocument(DocumentType, PrototypeId, "DishReplBox Prototype Layout", fields);
            var dataDoc = new DocumentController(new Dictionary<KeyController, FieldControllerBase>
            {   [KeyStore.TitleKey] = new TextController("Dish Repl Box"),
                [KeyStore.ReplLineTextKey] = new ListController<TextController>(),
                [KeyStore.ReplValuesKey] = new ListController<FieldControllerBase>(),
                [KeyStore.ReplCurrentIndentKey] = new ListController<NumberController>(),
                [KeyStore.ReplScopeKey] = new DocumentController(),
            }, DocumentType.DefaultType);
            Document.SetField(KeyStore.DocumentContextKey, dataDoc, true);
        }

        protected static void SetupBindings(DishReplView element, DocumentController docController, Context context)
        {
            CourtesyDocument.SetupBindings(element, docController, context);
        }



        public static FrameworkElement MakeView(DocumentController docController, Context context)
        {
            var tb = new DishReplView(docController.GetDataDocument());
            SetupBindings(tb, docController, context);

            return tb;
        }

    }
}
