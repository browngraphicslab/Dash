using System.Collections.Generic;
using Windows.Foundation;
using Windows.UI.Xaml;
using DashShared;

namespace Dash
{
    class DishScriptBox : CourtesyDocument
    {
        public static DocumentType DocumentType = new DocumentType("6C91F9CB-C2D9-4C89-8112-39B15D799396", "Dish Script Box");
        private static readonly string PrototypeId = "6C91F9CB-C2D9-4C89-8112-39B15D799396";

        public DishScriptBox(double x = 0, double y = 0, double w = 300, double h = 400, string text = "")
        {
            var fields = DefaultLayoutFields(new Point(x, y), new Size(w, h));
            SetupDocument(DocumentType, PrototypeId, "DishScriptBox Prototype Layout", fields);
            var dataDoc = new DocumentController(new Dictionary<KeyController, FieldControllerBase>
            {
                [KeyStore.ScriptTextKey] = new TextController(text)
            }, DocumentType.DefaultType);
            Document.SetField(KeyStore.DocumentContextKey, dataDoc, true);
        }

        protected static void SetupBindings(DishReplView element, DocumentController docController)
        {
        }



        public static FrameworkElement MakeView(DocumentController docController)
        {
            return new DishScriptEditView(docController);
        }
    }
}
