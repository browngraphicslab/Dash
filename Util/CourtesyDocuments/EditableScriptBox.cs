using Windows.Foundation;
using Windows.UI.Xaml;
using DashShared;
using System;

namespace Dash
{
    public class EditableScriptBox : CourtesyDocument
    {
        public static DocumentType DocumentType = new DocumentType("8F81DFF7-AFAE-4C93-B508-21B8D670E5B6", "Editable Script Box");
        public static readonly string PrototypeId = "8761FDB8-2397-4088-B3E7-9EEAC9BB63FD";
        public EditableScriptBox(FieldControllerBase refToData, double x = 0, double y = 0, double w = 200, double h = 200)
        {
            var fields = DefaultLayoutFields(new Point(x, y), new Size(double.NaN, double.NaN), refToData);
            SetupDocument(DocumentType, PrototypeId, "EditableScriptBox Prototype Layout", fields);
        }

        public static FrameworkElement MakeView(DocumentController documentController)
        {
            var fref = documentController.GetField(KeyStore.DataKey) as DocumentReferenceController;
            var ebox = new EditableScriptView();
            ebox.ViewModel = new EditableScriptViewModel(fref.GetFieldReference());
            ebox.Tag = "Auto TextBox " + DateTime.Now.Second + "." + DateTime.Now.Millisecond;
            ebox.HorizontalAlignment = HorizontalAlignment.Stretch;
            ebox.VerticalAlignment = VerticalAlignment.Stretch;
            return ebox;
        }
    }
}
