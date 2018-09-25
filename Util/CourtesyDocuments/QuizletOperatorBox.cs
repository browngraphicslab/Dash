using Windows.Foundation;
using Windows.UI.Xaml;
using DashShared;

namespace Dash
{
    class QuizletOperatorBox : CourtesyDocument
    {
        public static DocumentType DocumentType = DashConstants.TypeStore.QuizletOperatorType;
        private static readonly string PrototypeId = "30C41ACD-B223-455B-B316-F09F2DD31C27";



        public QuizletOperatorBox(ReferenceController refToOp)
        {
            // TODO change height and width to be what you want
            var fields = DefaultLayoutFields(new Point(), new Size(305, 165), refToOp);
            SetupDocument(DocumentType, PrototypeId, "QuizletOperatorBox Prototype Layout", fields);
        }

        public static FrameworkElement MakeView(DocumentController documentController, Context context)
        {
            return OperatorBox.MakeOperatorView(documentController, context, () => new QuizletOperatorView());
        }
    }
}
