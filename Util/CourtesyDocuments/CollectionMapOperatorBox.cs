using System.Collections.Generic;
using Windows.Foundation;
using Windows.UI.Xaml;
using Dash.Views;
using DashShared;

namespace Dash
{
    public class CollectionMapOperatorBox : CourtesyDocument
    {
        public CollectionMapOperatorBox(ReferenceController refToOp)
        {
            var fields = DefaultLayoutFields(new Point(), new Size(200, 100), refToOp);
            Document = new DocumentController(fields, DashConstants.TypeStore.MapOperatorBoxType);
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
            return MakeView(docController, context, null, isInterfaceBuilderLayout);
        }

        public static FrameworkElement MakeView(DocumentController docController, Context context,
            Dictionary<KeyController, FrameworkElement> keysToFrameworkElementsIn = null, bool isInterfaceBuilderLayout = false)
        {
            return OperatorBox.MakeOperatorView(docController, context, keysToFrameworkElementsIn,
                isInterfaceBuilderLayout, () => new CollectionMapView());
        }
    }
}