using Windows.Foundation;
using Windows.UI.Xaml;
using DashShared;

namespace Dash
{
    public class ExtractSentencesOperatorBox : CourtesyDocument
    {
        public static DocumentType DocumentType = DashConstants.TypeStore.ExtractSentencesDocumentType;
        private static readonly string PrototypeId = "414C8F91-2162-42FC-8C04-FEF37D0882CE";

        public ExtractSentencesOperatorBox(ReferenceController refToOp)
        {
            var fields = DefaultLayoutFields(new Point(), new Size(470, 120), refToOp);
            SetupDocument(DocumentType, PrototypeId, "ExtractSentencesOperatorBox Prototype Layout", fields);
        }

        public static FrameworkElement MakeView(DocumentController documentController)
        {
            return OperatorBox.MakeOperatorView(documentController, () => new ExtractSentencesOperatorView());
        }
    }
}
