using System.Collections.Generic;
using Windows.Foundation;
using Windows.UI.Xaml;
using Dash.Views;
using DashShared;

namespace Dash
{
    public class CollectionMapOperatorBox : CourtesyDocument
    {
        public static readonly DocumentType DocumentType = new DocumentType("AC7E7026-0522-4E8C-8F05-83AE7AB4000C", "Collection Map Box");

        public CollectionMapOperatorBox(ReferenceFieldModelController refToOp)
        {
            var fields = DefaultLayoutFields(new Point(), new Size(double.NaN, double.NaN), refToOp);
            Document = new DocumentController(fields, DocumentType);
        }

        protected override DocumentController GetLayoutPrototype()
        {
            throw new System.NotImplementedException();
        }

        protected override DocumentController InstantiatePrototypeLayout()
        {
            throw new System.NotImplementedException();
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
            OperatorView opView = new OperatorView { DataContext = opfmc.GetFieldReference() };
            var mapView = new CollectionMapView();
            opView.OperatorContent = mapView;
            if (isInterfaceBuilderLayout) return new SelectableContainer(opView, docController);
            return opView;
        }
    }
}