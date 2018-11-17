using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Graphics.Display;
using Windows.UI.Xaml;

// ReSharper disable once CheckNamespace
namespace Dash
{
    public class DragDocumentModel : DragModelBase
    {
        /// <summary>
        /// Flags whether documents or their link buttons are being dragged.
        /// </summary>
        public bool                     DraggingLinkButton = false;
        public string                   DraggedLinkType = null; // type of link to be created

        /// <summary>
        /// When DraggingLinkButton is false, this stores the collection views that contained each of the documents
        /// at the start of the drag.  When the documents are dropped, this allows us to remove the documents
        /// from where they were (in the case of a Move operation)
        /// </summary>
        public List<CollectionViewModel>     DraggedDocCollectionViews;

        public List<DocumentView>       DraggedDocumentViews;   // The Document views being dragged
        public List<DocumentController> DraggedDocuments; // The Documents being dragged (they correspond to the DraggedDocumentViews when specified)
        public List<Point>              DocOffsets; // offsets of documents from set of dragged documents
        public Point                    Offset; // offset of dragged document from pointer
        /// <summary>
        /// Flags whether the dropped set of documents should be wrapped in a collection
        /// </summary>
        public bool MakeCollection { get; set; } = false;

        public CollectionViewType ViewType { get; set; } = CollectionViewType.Freeform;
        public bool DraggingJoinButton { get; set; } = false;

        public DragDocumentModel(DocumentController draggedDocument)
        {
            DraggedDocuments = new List<DocumentController> { draggedDocument };
        }
        public DragDocumentModel(DocumentView draggedDocumentView)
        {
            DraggedDocuments = new List<DocumentController> { draggedDocumentView.ViewModel.DocumentController };
            DraggedDocumentViews = new List<DocumentView> { draggedDocumentView };
        }

        public DragDocumentModel(List<DocumentView> draggedDocumentViews, List<CollectionViewModel> draggedDocCollectionViews, List<Point> off, Point offset)
        {
            DraggedDocuments = draggedDocumentViews.Select((dv) => dv.ViewModel.DocumentController).ToList();
            DraggedDocumentViews = draggedDocumentViews;
            DraggedDocCollectionViews = draggedDocCollectionViews;
            DocOffsets = off;
            Offset = offset;
            Debug.Assert(draggedDocCollectionViews.Count == draggedDocumentViews.Count);
        }

        public DragDocumentModel(List<DocumentController> draggedDocuments, CollectionViewType viewType)
        {
            DraggedDocuments = draggedDocuments;
            ViewType = viewType;
            MakeCollection = true;
        }

        /*
         * Tests whether dropping the document would create a cycle and, if so, returns false
         */
        public override bool CanDrop(FrameworkElement sender)
        {
            if (sender.DataContext is CollectionViewModel cvm && (MainPage.Instance.IsShiftPressed() || MainPage.Instance.IsCtrlPressed()))
                return !cvm.CreatesCycle(DraggedDocuments);
            return true;
        }

        /*
         * Gets the document which will be dropped based on the current state of the syste
         */
        public override async Task<List<DocumentController>> GetDropDocuments(Point? where, FrameworkElement target)
        {
            // For each dragged document...
            var docs = new List<DocumentController>();

            double scaling = DisplayInformation.GetForCurrentView().RawPixelsPerViewPixel;
            Point? GetPosition(int i)
            {
                return where == null ? where:
                        new Point(where.Value.X - Offset.X / scaling - (DocOffsets?[i] ?? new Point()).X,
                                  where.Value.Y - Offset.Y / scaling - (DocOffsets?[i] ?? new Point()).Y);
            }
            // ...if CTRL pressed, create a key value pane
            if ( MainPage.Instance.IsCtrlPressed())
            {
                for (int i = 0; i < DraggedDocuments.Count; i++)
                {
                    docs.Add(DraggedDocuments[i].GetDataInstance(GetPosition(i)));
                }
            }
            // ...if ALT pressed, create a data instance
            else if (MainPage.Instance.IsAltPressed())
            {
                Debug.Assert(where.HasValue);
                docs = GetLinkDocuments((Point)where);
            }
            else if (MainPage.Instance.IsShiftPressed())
            {
                // ...otherwise, create a view copy
                for (int i = 0; i < DraggedDocuments.Count; i++)
                {
                   docs.Add(DraggedDocuments[i].GetViewCopy(GetPosition(i)));
                }
            }
            else if (target?.GetFirstAncestorOfType<AnnotationOverlayEmbeddings>() == null && DraggingLinkButton) // don't want to create a link when dropping a link button onto an overlay
            {
                for (int i = 0; i < DraggedDocuments.Count; i++)
                {
                    docs.Add(DraggedDocuments[i].GetKeyValueAlias(GetPosition(i)));
                }
            }
            else
            {
                for (int i = 0; i < DraggedDocuments.Count; i++)
                {
                    var draggedDoc = DraggedDocuments[i];

                    var pos = GetPosition(i);
                    if (pos.HasValue && !dontMove)
                    {
                        draggedDoc.SetPosition(pos.Value);
                    }

                    docs.Add(draggedDoc);
                }
            }

            return MakeCollection ? new List<DocumentController> { new CollectionNote(where ?? new Point(),  ViewType, double.NaN, double.NaN, collectedDocuments: docs).Document } : docs;
        }

        //TODO do we want to create link here?
        //TODO Add back ability to drag off collection of links/link targets if we want that.
        //TODO: this doesn't account for offsets
        private async Task<List<DocumentController>> GetLinkDocuments(Point where)
        {
            var anno = new RichTextNote(where: where).Document;

            for (var i = 0; i < DraggedDocuments.Count; i++)
            {
                DocumentController dragDoc = DraggedDocuments[i];
                var view = DraggedDocumentViews[i];

                if (KeyStore.RegionCreator[dragDoc.DocumentType] != null)
                {
                    // if RegionCreator exists, then dragDoc becomes the region document
                    dragDoc = await KeyStore.RegionCreator[dragDoc.DocumentType](view);
                }

                dragDoc?.Link(anno, LinkBehavior.Annotate, DraggedLinkType);
            }
            return new List<DocumentController> { anno };
        }
    }
}
