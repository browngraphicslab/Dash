﻿using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Windows.Foundation;
using Windows.Graphics.Display;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

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
        public List<CollectionView> DraggedDocCollectionView;

        public List<DocumentView> DraggedDocumentViews;
        public List<DocumentController> DraggedDocuments;
        public List<Point> OffsetsDocs;

        public string LinkType = null;

        public Point Offset;

        public bool MakeCollection { get; set; }

        public CollectionView.CollectionViewType ViewType { get; set; } = CollectionView.CollectionViewType.Freeform;

        public DragDocumentModel(DocumentView draggedDocumentView, bool showView, DocumentView sourceView = null)
        {
            DraggedDocuments = new List<DocumentController> { draggedDocumentView.ViewModel.DocumentController };
            DraggedDocumentViews = new List<DocumentView>(new DocumentView[] { draggedDocumentView } );
            ShowViewCopy = showView;
            if (sourceView != null) LinkSourceViews = new List<DocumentView>() { sourceView };
            MakeCollection = false;
        }
        public DragDocumentModel(DocumentController draggedDocument, bool showView, DocumentView sourceView = null)
        {
            DraggedDocuments = new List<DocumentController> { draggedDocument };
            ShowViewCopy = showView;
            if (sourceView != null) LinkSourceViews = new List<DocumentView>() { sourceView };
            MakeCollection = false;
        }

        public DragDocumentModel(List<DocumentView> draggedDocumentViews, bool showView,
            List<DocumentView> sourceViews = null, List<Point> off = null)
        {
            DraggedDocuments = draggedDocumentViews.Select((dv) => dv.ViewModel.DocumentController).ToList();
            DraggedDocumentViews = draggedDocumentViews;
            ShowViewCopy = showView;
            OffsetsDocs = off;
            if (sourceViews != null) LinkSourceViews = sourceViews;
            MakeCollection = false;

            Debug.Assert(LinkSourceViews == null || DraggedDocuments.Count == LinkSourceViews.Count);
        }
        public DragDocumentModel(List<DocumentController> draggedDocuments, bool showView, 
            List<DocumentView> sourceViews = null, List<Point> off = null)
        {
            DraggedDocuments = draggedDocuments;
            ShowViewCopy = showView;
            OffsetsDocs = off;
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
        public override List<DocumentController> GetDropDocuments(Point where, FrameworkElement target)
        {
            // For each dragged document...
            var docs = new List<DocumentController>();

            double scaling = DisplayInformation.GetForCurrentView().RawPixelsPerViewPixel;
            // ...if CTRL pressed, create a key value pane
            if (MainPage.Instance.IsCtrlPressed())
            {
                for (int i = 0; i < DraggedDocuments.Count; i++)
                {
                    docs.Add(DraggedDocuments[i].GetDataInstance(new Point(where.X - Offset.X / scaling - (OffsetsDocs?[i] ?? new Point()).X,
                        where.Y - Offset.Y / scaling - (OffsetsDocs?[i] ?? new Point()).Y)));
                }
            }
            // ...if ALT pressed, create a data instance
            else if (MainPage.Instance.IsAltPressed())
            {
                for (int i = 0; i < DraggedDocuments.Count; i++)
                {
                    docs.Add(DraggedDocuments[i].GetKeyValueAlias(new Point(where.X - Offset.X / scaling - (OffsetsDocs?[i] ?? new Point()).X,
                        where.Y - Offset.Y / scaling - (OffsetsDocs?[i] ?? new Point()).Y)));
                }
            }
            else if (MainPage.Instance.IsShiftPressed())
            {
                // ...otherwise, create a view copy
                for (int i = 0; i < DraggedDocuments.Count; i++)
                {
                    DocumentController vcopy = DraggedDocuments[i]
                        .GetViewCopy(new Point(where.X - Offset.X / scaling - (OffsetsDocs?[i] ?? new Point()).X,
                            where.Y - Offset.Y / scaling - (OffsetsDocs?[i] ?? new Point()).Y));

                    // when we drop a something that had no bounds (e.g., a workspace or a docked document), then we create
                    // an arbitrary size for it and zero out its pan position so that it will FitToParent
                    if (vcopy.DocumentType.Equals(RichTextBox.DocumentType) ||
                        !double.IsNaN(vcopy.GetWidthField().Data) ||
                        !double.IsNaN(vcopy.GetHeightField().Data))
                        docs.Add(vcopy);
                    else
                    {
                        vcopy.SetWidth(500);
                        vcopy.SetHeight(300);
                        vcopy.SetFitToParent(true);
                        docs.Add(vcopy);
                    }
                }
            }
            else if (LinkSourceViews != null)
            {
                if (target?.GetFirstAncestorOfType<NewAnnotationOverlay>() == null) // don't want to create a link when dropping a link button onto an overlay
                {
                    docs = GetLinkDocuments(where);
                }
            }
            else
            {
                for (int i = 0; i < DraggedDocuments.Count; i++)
                {
                    if (DraggedDocumentViews == null ||
                        DraggedDocumentViews[i].GetFirstAncestorOfType<NewAnnotationOverlay>() == // bcz: this is hacky -- better to make NewAnnotationOverlay's be Collections?  
                        target?.GetFirstAncestorOfType<NewAnnotationOverlay>())  //Without this, dropping onto an annotation overlay sets the position of the document based on the overlay, but the document isn't added to the overlay so it jumps
                    {
                        DraggedDocuments[i].SetPosition(new Point(where.X - Offset.X / scaling - (OffsetsDocs?[i] ?? new Point()).X,
                            where.Y - Offset.Y / scaling - (OffsetsDocs?[i] ?? new Point()).Y));
                        docs.Add(DraggedDocuments[i]);
                    }
                }
            }

            return MakeCollection ? new List<DocumentController>{ new CollectionNote(where, ViewType, collectedDocuments: docs).Document } : docs;
        }

        //TODO do we want to create link here?
        //TODO Add back ability to drag off collection of links/link targets if we want that.
        //TODO: this doesn't account for offsets
        private List<DocumentController> GetLinkDocuments(Point where)
        {
            var anno = new RichTextNote(where: where).Document;

            for (var i = 0; i < DraggedDocuments.Count; i++)
            {
                var dragDoc = DraggedDocuments[i];
                var view = LinkSourceViews[i];

                if (KeyStore.RegionCreator[dragDoc.DocumentType] != null)
                {
                    // if RegionCreator exists, then dragDoc becomes the region document
                    dragDoc = KeyStore.RegionCreator[dragDoc.DocumentType](view);
                }

                dragDoc.Link(anno, LinkBehavior.Annotate);
            }
            return new List<DocumentController>{ anno };
        }
    }
}