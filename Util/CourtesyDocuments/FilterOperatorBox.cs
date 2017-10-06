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
            var fields = DefaultLayoutFields(new Point(), new Size(double.NaN, double.NaN), refToOp);
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
            return MakeView(docController, context, isInterfaceBuilderLayout);
        }

        public static FrameworkElement MakeView(DocumentController docController, Context context,
            bool isInterfaceBuilderLayout = false)
        {
            var data = docController.GetField(KeyStore.DataKey);
            var opfmc = (data as ReferenceFieldModelController);
            OperatorView opView = new OperatorView {DataContext = opfmc};
            var filterView = new FilterView();
            opView.OperatorContent = filterView;
            if (isInterfaceBuilderLayout) return new SelectableContainer(opView, docController);
            return opView;
        }
    }
}
