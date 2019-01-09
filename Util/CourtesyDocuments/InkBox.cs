using Windows.Foundation;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using DashShared;

namespace Dash
{
    class InkBox : CourtesyDocument
    {
        public static DocumentType DocumentType = new DocumentType("ACDF5539-656B-44B5-AC0A-BA6E1875A4C2", "Ink Box");

        private static string PrototypeId = "29BD18A0-8236-4305-B063-B77BA4192C59";

        public InkBox(FieldControllerBase refToInk, double x = 0, double y = 0, double w = 200, double h = 200)
        {
            var fields = DefaultLayoutFields(new Point(x, y), new Size(w, h), refToInk);
            SetupDocument(DocumentType, PrototypeId, "InkBox Prototype Layout", fields);
            //Document.SetField(InkDataKey, new InkFieldModelController(), true);
            //SetLayoutForDocument(Document, Document, true, true);
        }

        public static FrameworkElement MakeView(DocumentController docController)
        {
            var fmController = docController.GetDereferencedField(KeyStore.DataKey, null) as InkController;
            return fmController != null ? (FrameworkElement)new InkCanvasControl(fmController) : new Grid();
        }

        private static ReferenceController GetInkReference(DocumentController docController)
        {
            return docController.GetField(KeyStore.DataKey) as ReferenceController;
        }
    }
}
