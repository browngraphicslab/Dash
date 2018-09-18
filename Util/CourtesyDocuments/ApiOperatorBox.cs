using Windows.Foundation;
using Windows.UI.Xaml;
using Dash.Views;
using DashShared;

namespace Dash
{
    class ApiOperatorBox : CourtesyDocument
    {
        public static DocumentType DocumentType = new DocumentType("62F7196C-BC2B-4822-AA24-EF2D63866D58", "Api Operator Box");
        private static readonly string PrototypeId = "6730AAEC-D0AC-4A12-910E-9D8052C9F605";

        public ApiOperatorBox(ReferenceController refToOp)
        {
            var fields = DefaultLayoutFields(new Point(), new Size(200, 100), refToOp);
            SetupDocument(DocumentType, PrototypeId, "API Operator Box Prototype Layout", fields);
        }

        public static FrameworkElement MakeView(DocumentController docController, Context context)
        {
            return OperatorBox.MakeOperatorView(docController, context, () => new ApiOpView());
        }
    }
}
