using System.Collections.Generic;
using System.Diagnostics;
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
        public List<DocumentView> LinkSourceViews;

        public List<DocumentController> DraggedDocuments;

        public string LinkType = null;

        public bool MakeCollection { get; set; }

        public CollectionView.CollectionViewType ViewType { get; set; } = CollectionView.CollectionViewType.Freeform;

        public DragDocumentModel(DocumentController draggedDocument, bool showView, DocumentView sourceView = null)
        {
            DraggedDocuments = new List<DocumentController> { draggedDocument };
            ShowViewCopy = showView;
            if (sourceView != null) LinkSourceViews = new List<DocumentView>() { sourceView };
            MakeCollection = false;
        }

        public DragDocumentModel(List<DocumentController> draggedDocuments, bool showView, List<DocumentView> sourceViews = null)
        {
            DraggedDocuments = draggedDocuments;
            ShowViewCopy = showView;
            if (sourceViews != null) LinkSourceViews = sourceViews;
            MakeCollection = false;

            Debug.Assert(LinkSourceViews == null || DraggedDocuments.Count == LinkSourceViews.Count);
        }

        public DragDocumentModel(List<DocumentController> draggedDocuments, CollectionView.CollectionViewType viewType)
        {
            DraggedDocuments = draggedDocuments;
            ViewType = viewType;
            MakeCollection = true;

            Debug.Assert(LinkSourceViews == null || DraggedDocuments.Count == LinkSourceViews.Count);
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
            List<DocumentController> docs;

            // ...if CTRL pressed, create a key value pane
            if (MainPage.Instance.IsCtrlPressed())
            {
                docs = DraggedDocuments.Select(d => d.GetDataInstance(where)).ToList();
            } 
            // ...if ALT pressed, create a data instance
            else if (MainPage.Instance.IsAltPressed())
            {
                docs = DraggedDocuments.Select(d => d.GetKeyValueAlias(where)).ToList();
            }
            else if (LinkSourceViews != null)
            {
                docs = GetLinkDocuments(where);
            }
            else
            {
                // ...otherwise, create a view copy
                docs = DraggedDocuments.Select(d =>
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

            return MakeCollection ? new List<DocumentController>{ new CollectionNote(where, ViewType, collectedDocuments: docs).Document } : docs;
        }

        //TODO do we want to create link here?
        //TODO Add back ability to drag off collection of links/link targets if we want that.
        private List<DocumentController> GetLinkDocuments(Point where)
        {
            DocumentController anno = new RichTextNote(where: where).Document;

            for (var i = 0; i < DraggedDocuments.Count; i++)
            {
                DocumentController dragDoc = DraggedDocuments[i];
                DocumentView view = LinkSourceViews[i];

                if (KeyStore.RegionCreator[dragDoc.DocumentType] != null)
                {
                    // if RegionCreator exists, then dragDoc becomes the region document
                    dragDoc = KeyStore.RegionCreator[dragDoc.DocumentType](view);
                }

                dragDoc.Link(anno, LinkTargetPlacement.Default, annotation: true);
            }
            return new List<DocumentController>{ anno };
        }
    }
}