using Windows.Foundation;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using DashShared;

namespace Dash
{

    public class KeyValueDocumentBox : CourtesyDocument
    {
        public static DocumentType DocumentType = new DocumentType("737BB31D-52B4-4C57-AD33-D519F40B57DC", "Key Value Document Box");
        private static readonly string PrototypeId = "342B88D5-6B7D-43D4-BCC5-F0E8BF228AF3";
        public KeyValueDocumentBox(FieldControllerBase refToDoc, double x = 0, double y = 0, double w = 650, double h = 800)
        {
            var fields = DefaultLayoutFields(new Point(x, y), new Size(w, h), refToDoc);
            SetupDocument(DocumentType, PrototypeId, "KeyValueDocumentBox Prototype Layout", fields);
        }
        public static FrameworkElement MakeView(DocumentController docController, Context context)
        {
            var border = new Border();

            var keyValuePane = new KeyValuePane
            {
                TypeColumnWidth = new GridLength(0),
                DataContext = docController?.GetDereferencedField<DocumentController>(KeyStore.DocumentContextKey, null) ?? docController
            };
            border.Child = keyValuePane;
            SetupBindings(border, docController, context);

            return border;
        }
    }
}
