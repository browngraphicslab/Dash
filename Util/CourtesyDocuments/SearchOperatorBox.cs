using Windows.Foundation;
using Windows.UI.Xaml;
using DashShared;

namespace Dash
{
    public class SearchOperatorBox : CourtesyDocument
    {
        public static DocumentType DocumentType = DashConstants.TypeStore.SearchOperatorType;
        private static readonly string PrototypeId = "C6962017-49A1-4257-9E02-635BAB84F346";

        public SearchOperatorBox(ReferenceController refToOp)
        {
            var fields = DefaultLayoutFields(new Point(), new Size(550, 80), refToOp);
            SetupDocument(DocumentType, PrototypeId, "SearchOperatorBox Prototype Layout", fields);
        }

        public static FrameworkElement MakeView(DocumentController documentController, Context context)
        {
            return OperatorBox.MakeOperatorView(documentController, context, () => new SearchOperatorView());
        }
    }
}
