using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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

        public override FrameworkElement makeView(DocumentController docController, Context context,
            bool isInterfaceBuilderLayout = false)
        {
            return MakeView(docController, context, null, isInterfaceBuilderLayout);
        }

        public static FrameworkElement MakeView(DocumentController documentController, Context context,
            Dictionary<KeyController, FrameworkElement> keysToFrameworkElementsIn, bool isInterfaceBuilder)
        {
            return OperatorBox.MakeOperatorView(documentController, context, keysToFrameworkElementsIn,
                isInterfaceBuilder, () => new QuizletOperatorView());
        }
    }
}
