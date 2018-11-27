using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DashShared;

namespace Dash
{
    public static class SelectionFunctions
    {
        [OperatorReturnName("SelectedDocs")]
        public static ListController<DocumentController> GetSelectedDocs()
        {
            var docKey = KeyController.Get("Document");
            var parentKey = KeyController.Get("Parent");
            var selected = SelectionManager.GetSelectedDocs();
            var parents = selected.Select(dv => dv.GetFirstAncestorOfTypeFast<DocumentView>());
            var result = selected.Zip(parents, (doc, parent) => new DocumentController(
                new Dictionary<KeyController, FieldControllerBase>
                {
                    [docKey] = doc.ViewModel.DocumentController,
                    [parentKey] = parent?.ViewModel.DocumentController
                }, DocumentType.DefaultType));
            return result.ToListController();
        }

        public static DocumentController ActiveDocument()
        {
            return SplitFrame.ActiveFrame.DocumentController;
        }
    }
}
