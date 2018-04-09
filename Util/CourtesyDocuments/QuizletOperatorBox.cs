using System;
using Windows.Foundation;
using Windows.UI.Xaml;
using DashShared;

namespace Dash
{
    class QuizletOperatorBox : CourtesyDocument
    {

        public QuizletOperatorBox(ReferenceController refToOp)
        {
            // TODO change height and width to be what you want
            var fields = DefaultLayoutFields(new Point(), new Size(305, 165), refToOp);
            Document = new DocumentController(fields, DashConstants.TypeStore.QuizletOperatorType);
        }

        protected override DocumentController GetLayoutPrototype()
        {
            throw new NotImplementedException();
        }

        protected override DocumentController InstantiatePrototypeLayout()
        {
            throw new NotImplementedException();
        }

        public override FrameworkElement makeView(DocumentController docController, Context context)
        {
            return MakeView(docController, context);
        }

        public static FrameworkElement MakeView(DocumentController documentController, Context context)
        {
            return OperatorBox.MakeOperatorView(documentController, context, () => new QuizletOperatorView());
        }
    }
}
