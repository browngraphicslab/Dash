using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dash
{
    public static class SelectionFunctions
    {
        [OperatorReturnName("SelectedDocs")]
        public static ListController<DocumentController> GetSelectedDocs()
        {
            return SelectionManager.GetSelectedDocs().Select(dv => dv.ViewModel.DocumentController).ToListController();
        }
    }
}
