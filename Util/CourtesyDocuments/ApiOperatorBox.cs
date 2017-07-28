using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.UI.Xaml;
using Dash.Views;
using DashShared;

namespace Dash
{
    class ApiOperatorBox : CourtesyDocument
    {
        public static DocumentType DocumentType = new DocumentType("62F7196C-BC2B-4822-AA24-EF2D63866D58", "Api Operator Box");

        public ApiOperatorBox(ReferenceFieldModelController refToOp)
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
            var data = docController.GetField(DashConstants.KeyStore.DataKey);
            var opfmc = (data as ReferenceFieldModelController);
            OperatorView opView = new OperatorView { DataContext = opfmc.FieldReference };
            var apiView = new ApiOpView();
            opView.OperatorContent = apiView;
            if (isInterfaceBuilderLayout) return new SelectableContainer(opView, docController);
            return opView;
        }
    }
}
