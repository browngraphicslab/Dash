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
    public class SearchOperatorBox : CourtesyDocument
    {
        public SearchOperatorBox(ReferenceController refToOp)
        {
            var fields = DefaultLayoutFields(new Point(), new Size(550, 80), refToOp);
            Document = new DocumentController(fields, DashConstants.TypeStore.SearchOperatorType);
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

        public static FrameworkElement MakeView(DocumentController documentController, Context context,
            Dictionary<KeyController, FrameworkElement> keysToFrameworkElementsIn)
        {
            return OperatorBox.MakeOperatorView(documentController, context, keysToFrameworkElementsIn,
                () => new SearchOperatorView());
        }


    }
}
