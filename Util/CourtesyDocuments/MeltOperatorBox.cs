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
    class MeltOperatorBox : CourtesyDocument
    {
        public MeltOperatorBox(ReferenceController refToOp)
        {
            var fields = DefaultLayoutFields(new Point(), new Size(200, 100), refToOp);
            Document = new DocumentController(fields, DashConstants.TypeStore.MeltOperatorBoxDocumentType);
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
            return MakeView(docController, context, null);
        }

        public static FrameworkElement MakeView(DocumentController docController, Context context,
            Dictionary<KeyController, FrameworkElement> keysToFrameworkElementsIn = null)
        {
            return OperatorBox.MakeOperatorView(docController, context, keysToFrameworkElementsIn,
                () => new MeltOperatorView());
        }
    }

}
