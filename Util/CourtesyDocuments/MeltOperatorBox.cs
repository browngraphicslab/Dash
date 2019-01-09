using Windows.Foundation;
using Windows.UI.Xaml;
using DashShared;

namespace Dash
{
    class MeltOperatorBox : CourtesyDocument
    {
        public static DocumentType DocumentType = DashConstants.TypeStore.MeltOperatorBoxDocumentType;
        private static readonly string PrototypeId = "C1CBA8C9-9B50-415D-89B3-352C27440836";

        public MeltOperatorBox(ReferenceController refToOp)
        {
            var fields = DefaultLayoutFields(new Point(), new Size(200, 100), refToOp);
            SetupDocument(DocumentType, PrototypeId, "MeltOperatorBox Prototype Layout", fields);
        }

        public static FrameworkElement MakeView(DocumentController docController)
        {
            return OperatorBox.MakeOperatorView(docController, () => new MeltOperatorView());
        }
    }

}
