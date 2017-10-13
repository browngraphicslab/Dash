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
    class FilterOperatorBox : CourtesyDocument
    {
        public static DocumentType DocumentType = new DocumentType("30651C8A-C3EC-4CF6-999B-F8F1ED094D65", "Filter Operator Box");

        public FilterOperatorBox(ReferenceFieldModelController refToOp)
        {
            var fields = DefaultLayoutFields(new Point(), new Size(200, 100), refToOp);
            Document = new DocumentController(fields, DocumentType);
        }

        protected override DocumentController GetLayoutPrototype()
        {
            throw new NotImplementedException();
        }

        protected override DocumentController InstantiatePrototypeLayout()
        {
            throw new NotImplementedException();
        }

        public override FrameworkElement makeView(DocumentController docController, Context context, bool isInterfaceBuilderLayout = false)
        {
            return MakeView(docController, context, null, isInterfaceBuilderLayout);
        }

        public static FrameworkElement MakeView(DocumentController docController, Context context, Dictionary<KeyController, FrameworkElement> keysToFrameworkElementsIn = null,
            bool isInterfaceBuilderLayout = false)
        {

            return OperatorBox.MakeOperatorView(docController, context, keysToFrameworkElementsIn, isInterfaceBuilderLayout,
                () => new FilterView());
        }
    }

}
