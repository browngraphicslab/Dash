using System;
using Windows.Foundation;
using Windows.UI.Xaml;
using Dash;
using DashShared;

namespace Dash
{
    /// <summary>
    /// Given a reference to an operator field model, constructs a document type that displays that operator.
    /// </summary>
    public class OperatorBox : CourtesyDocument
    {
        public static DocumentType DocumentType =
            new DocumentType("53FC9C82-F32C-4704-AF6B-E55AC805C84F", "Operator Box");

        public OperatorBox(ReferenceFieldModelController refToOp)
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

        public override FrameworkElement makeView(DocumentController docController,
            Context context, bool isInterfaceBuilderLayout = false)
        {
            return MakeView(docController, context, isInterfaceBuilderLayout);
        }

        public static FrameworkElement MakeView(DocumentController docController,
            Context context, bool isInterfaceBuilderLayout = false)
        {
            var data = docController.GetField(DashConstants.KeyStore.DataKey) ?? null;
            var opfmc = (data as ReferenceFieldModelController);
            OperatorView opView = new OperatorView { DataContext = opfmc };
            if (isInterfaceBuilderLayout) return opView;
            return new SelectableContainer(opView, docController);
        }
    }
}