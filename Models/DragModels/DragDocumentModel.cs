using System.Collections.Generic;
using System.Linq;
using Windows.Foundation;
using Windows.UI.Xaml;

// ReSharper disable once CheckNamespace
namespace Dash
{
    public class DragDocumentModel : DragModelBase
    {
        /*
         * if the drag is going to result in a view copy, false if the drag will result in a key value pane
         */
        public bool ShowViewCopy;

        /*
        * The XAML view that originated the drag operation - not required
        */
        public DocumentView LinkSourceView;

        public string LinkType = null;

        public DragDocumentModel(List<DocumentController> draggedDocuments, bool showView, DocumentView sourceView = null) : base(draggedDocuments)
        {
            DraggedDocuments = draggedDocuments;
            ShowViewCopy = showView;
            LinkSourceView = sourceView;
        }
        
        /*
         * Tests whether dropping the document would create a cycle and, if so, returns false
         */
        public bool CanDrop(FrameworkElement sender)
        {
            if (sender is CollectionView cview && (MainPage.Instance.IsShiftPressed() || ShowViewCopy || MainPage.Instance.IsCtrlPressed()))
                return !cview.ViewModel.CreatesCycle(DraggedDocuments);
            return true;
        }

        /*
         * Gets the document which will be dropped based on the current state of the syste
         */
        public override List<DocumentController> GetDropDocuments(Point where, bool forceShowViewCopy = false)
        {
            // For each dragged document...

            // ...if CTRL pressed, create a key value pane
            if (MainPage.Instance.IsCtrlPressed()) return DraggedDocuments.Select(d => d.GetDataInstance(where)).ToList();

            // ...if ALT pressed, create a data instance
            if (MainPage.Instance.IsAltPressed()) return DraggedDocuments.Select(d => d.GetKeyValueAlias(where)).ToList();

            // ...otherwise, create a view copy
            return DraggedDocuments.Select(d =>
            {
                DocumentController vcopy = d.GetViewCopy(where);

                // when we drop a something that had no bounds (e.g., a workspace or a docked document), then we create
                // an arbitrary size for it and zero out its pan position so that it will FitToParent
                if (vcopy.DocumentType.Equals(RichTextBox.DocumentType) || 
                    !double.IsNaN(vcopy.GetWidthField().Data) ||
                    !double.IsNaN(vcopy.GetHeightField().Data))
                    return vcopy;

                vcopy.SetWidth(500);
                vcopy.SetHeight(300);
                vcopy.SetFitToParent(true);
                return vcopy;
            }).ToList();
        }
    }
}