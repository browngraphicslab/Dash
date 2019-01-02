using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.ApplicationModel.DataTransfer;
using Windows.Foundation;
using Windows.UI;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media.Imaging;
using Visibility = Windows.UI.Xaml.Visibility;
using Windows.Graphics.Display;
using Windows.Graphics.Imaging;
using Windows.UI.Input;
using MyToolkit.Mathematics;
using System.Threading.Tasks;
using Windows.Storage;
using Windows.Storage.Streams;

namespace Dash
{
    public class DocumentSelectionChangedEventArgs
    {
        public List<DocumentView> DeselectedViews, SelectedViews;

        public DocumentSelectionChangedEventArgs()
        {
            DeselectedViews = new List<DocumentView>();
            SelectedViews = new List<DocumentView>();
        }

        public DocumentSelectionChangedEventArgs(List<DocumentView> deselectedViews, List<DocumentView> selectedViews)
        {
            DeselectedViews = deselectedViews;
            SelectedViews = selectedViews;
        }
    }

    public static class SelectionManager
    {
        public delegate void SelectionChangedHandler(DocumentSelectionChangedEventArgs args);
        public static event SelectionChangedHandler SelectionChanged;
        public static event EventHandler DragManipulationStarted;
        public static event EventHandler DragManipulationCompleted;

        private static List<DocumentView> _dragViews;
        private static List<DocumentView> _selectedDocViews = new List<DocumentView>();

        public static IEnumerable<DocumentView>        SelectedDocViews => _selectedDocViews.Where(s => s.IsInVisualTree() && s.ViewModel != null);
        public static IEnumerable<DocumentViewModel>   SelectedDocViewModels => _selectedDocViews.Select(dv => dv?.ViewModel).Where(dvm => dvm != null);

        public static bool IsSelected(DocumentViewModel docViewModel) { return SelectedDocViewModels.Contains(docViewModel); }

        /// <summary>
        /// Selects the given document
        /// </summary>
        /// <param name="doc">The document to select</param>
        /// <param name="toggle">Whether or not to toggle the selection of the given document.
        /// This is roughly equivalent to whether Shift is pressed when selecting.</param>
        public static void Select(DocumentView doc, bool toggle)
        {
            if (!toggle)
            {
                bool alreadySelected = false;
                var deselected = new List<DocumentView>();
                foreach (var documentView in _selectedDocViews)
                {
                    if (documentView == doc)
                    {
                        alreadySelected = true;
                    }
                    else
                    {
                        //deselect
                        DeselectHelper(documentView);
                        deselected.Add(documentView);
                    }
                }

                var args = new DocumentSelectionChangedEventArgs(deselected, alreadySelected ? new List<DocumentView>() : new List<DocumentView> { doc });

                _selectedDocViews = new List<DocumentView> { doc };
                if (!alreadySelected)
                {
                    SelectHelper(doc);
                }

                OnSelectionChanged(args);
            }
            else
            {
                if (_selectedDocViews.Contains(doc))
                {
                    DeselectHelper(doc);
                    _selectedDocViews.Remove(doc);
                    OnSelectionChanged(new DocumentSelectionChangedEventArgs(new List<DocumentView> { doc }, new List<DocumentView>()));
                }
                else
                {
                    SelectHelper(doc);
                    _selectedDocViews.Add(doc);
                    OnSelectionChanged(new DocumentSelectionChangedEventArgs(new List<DocumentView>(), new List<DocumentView> { doc }));
                }
            }
        }

        /// <summary>
        /// Selects the given documents
        /// </summary>
        /// <param name="views">The documents to select</param>
        /// <param name="keepPrevious">Whether or not to deselect the previously selected documents. 
        /// False to deselect previous documents, true to keep them selected. 
        /// This will often be roughly equivalent to whether Shift is pressed</param>
        public static void SelectDocuments(IEnumerable<DocumentView> views, bool keepPrevious)
        {
            var selectedDocs = new List<DocumentView>();
            var documentViews = views.ToList();
            foreach (var documentView in documentViews)
            {
                if (_selectedDocViews.Contains(documentView))
                {
                    continue;
                }

                SelectHelper(documentView);
                selectedDocs.Add(documentView);
                if (keepPrevious)
                {
                    _selectedDocViews.Add(documentView);
                }
            }

            var deselectedDocs = new List<DocumentView>();
            if (!keepPrevious)
            {
                foreach (var documentView in _selectedDocViews)
                {
                    if (!documentViews.Contains(documentView))
                    {
                        DeselectHelper(documentView);
                        deselectedDocs.Add(documentView);
                    }
                }

                _selectedDocViews = documentViews;
            }

            OnSelectionChanged(new DocumentSelectionChangedEventArgs(deselectedDocs, selectedDocs));
        }

        public static void Deselect(DocumentView view)
        {
            if (_selectedDocViews.Contains(view))
            {
                _selectedDocViews.Remove(view);
                DeselectHelper(view);
                OnSelectionChanged(new DocumentSelectionChangedEventArgs(new List<DocumentView> { view }, new List<DocumentView>()));
            }
        }

        public static void DeselectAll()
        {
            foreach (var documentView in _selectedDocViews)
            {
                DeselectHelper(documentView);
            }
            var args = new DocumentSelectionChangedEventArgs(new List<DocumentView>(_selectedDocViews), new List<DocumentView>());
            _selectedDocViews.Clear();
            OnSelectionChanged(args);
        }

        public static void DeleteSelected()
        {
            using (UndoManager.GetBatchHandle())
            {
                _selectedDocViews.ToList().ForEach(dv => dv.DeleteDocument());
            }
        }

        private static void SelectHelper(DocumentView view)
        {
            view.GetDescendantsOfType<RichEditView>().Where((rv) => rv.GetDocumentView() == view).ToList().ForEach(rv =>
            {
                rv.IsEnabled = true;
                rv.Focus(FocusState.Programmatic);
            });
            view.GetDescendantsOfType<MediaPlayerElement>().Where((rv) => rv.GetDocumentView() == view).ToList().ForEach(rv =>
            {
                rv.TransportControls.Visibility = Visibility.Visible;
                rv.Focus(FocusState.Programmatic);
            });
        }

        private static void DeselectHelper(DocumentView view)
        {
            view.GetDescendantsOfType<RichEditView>().Where((rv) => rv.GetDocumentView() == view).ToList().ForEach(rv => rv.IsEnabled = false);
            view.GetDescendantsOfType<MediaPlayerElement>().Where((rv) => rv.GetDocumentView() == view).ToList().ForEach(rv => 
                rv.TransportControls.Visibility = Visibility.Collapsed);
        }

        public static IEnumerable<DocumentView> GetSelectedDocumentsInCollection(CollectionFreeformView collection)
        {
            return _selectedDocViews.Where(doc => Equals(doc.ParentCollection?.CurrentView, collection));
        }

        /*
         * Returns itself if nothing is selected.
         */
        public static List<DocumentView> GetSelectedSiblings(DocumentView view)
        {
            if (view.ParentCollection != null && view.ParentCollection.CurrentView is CollectionFreeformView cfb)
            {
                var marqueeDocs = GetSelectedDocumentsInCollection(cfb).ToList();
                if (marqueeDocs.Contains(view))
                    return marqueeDocs;
            }
            return new List<DocumentView>(new[] { view });
        }

        private static void OnSelectionChanged(DocumentSelectionChangedEventArgs args)
        {
            if (args.DeselectedViews.Count != 0 || args.SelectedViews.Count != 0)
                SelectionChanged?.Invoke(args);
        }

        #region Drag Manipulation Methods
        public static void InitiateDragDrop(DocumentView draggedView, PointerRoutedEventArgs pe)
        {
            var parents = draggedView.ViewModel.IsSelected ? new DocumentView[]{ } : draggedView.GetAncestorsOfType<DocumentView>();
            foreach (var parent in parents)
            {
                var parentIsFreeformCollection = parent.ViewModel.DataDocument.DocumentType.Equals(CollectionNote.CollectionNoteDocumentType) &&
                    parent.GetFirstDescendantOfType<CollectionView>().CurrentView is CollectionFreeformView;
                var parentIsAnnotationLayer = parent.GetFirstDescendantOfType<AnnotationOverlayEmbeddings>()?.EmbeddedDocsList.Contains(draggedView.ViewModel.DocumentController) == true;
                if ((parentIsFreeformCollection || parentIsAnnotationLayer) &&
                    (_selectedDocViews.Contains(parent) || parent == parents.Last()))
                {
                    break;
                }
                draggedView = parent;
            }

            _dragViews = _selectedDocViews.Contains(draggedView) ? _selectedDocViews.ToList() : new DocumentView[] { draggedView }.ToList();

            if (draggedView.ViewModel.DocumentController.GetIsAdornment() && !SelectedDocViewModels.Contains(draggedView.ViewModel)) // if dragging an adornment, drag all siblings that overlap it 
            {
                var rect         = draggedView.ViewModel.LayoutDocument.GetBounds();
                var siblingViews = draggedView.GetFirstAncestorOfType<Canvas>()?.Children.Select(c => c.GetFirstDescendantOfType<DocumentView>());
                _dragViews.AddRange(siblingViews.Where(dv => dv != null && rect.Intersects(dv.ViewModel.LayoutDocument.GetBounds())));
            }
            draggedView.StartDragAsync(pe?.GetCurrentPoint(draggedView) ?? MainPage.PointerRoutedArgsHack.GetCurrentPoint(draggedView));
        }

        public static void DropCompleted(DocumentView docView, UIElement sender, DropCompletedEventArgs args)
        {
            LocalSqliteEndpoint.SuspendTimer = false;
            _dragViews?.ForEach(dv =>
            {
                dv.Visibility = Visibility.Visible;
                dv.IsHitTestVisible = true;
            });
            _dragViews = null;
            DragManipulationCompleted?.Invoke(sender, null);
        }
        
        private static void       CollectionDragOver(object sender, DragEventArgs e)
        {
            if (e.DragUIOverride != null)
            {
                e.DragUIOverride.IsContentVisible = true;
            }
            _dragViews?.ForEach(dv => dv.Visibility = Visibility.Collapsed);
            MainPage.Instance.XDocumentDecorations.Visibility = Visibility.Collapsed;
            (sender as FrameworkElement).DragOver -= CollectionDragOver;
        }
        public static async void  DragStarting(DocumentView docView, UIElement sender, DragStartingEventArgs args)
        {
            if (SplitManager.IsRoot(docView.ViewModel) && !docView.IsShiftPressed() && !docView.IsCtrlPressed() && !docView.IsAltPressed())
            {
                args.Cancel = true;
                return;
            }
            DragManipulationStarted?.Invoke(sender, null);
            LocalSqliteEndpoint.SuspendTimer = true;

            // setup the DragModel
            var dragDocOffset  = args.GetPosition(docView);
            var relDocOffsets  = _dragViews.Select(args.GetPosition).Select(ro => new Point(ro.X - dragDocOffset.X, ro.Y - dragDocOffset.Y)).ToList();
            var parCollections = _dragViews.Select(dv => dv.GetFirstAncestorOfType<AnnotationOverlayEmbeddings>() == null ? dv.ParentViewModel : null).ToList();
            args.Data.SetDragModel(new DragDocumentModel(_dragViews, parCollections, relDocOffsets, dragDocOffset) { DraggedWithLeftButton = docView.IsLeftBtnPressed() });

            if (_dragViews.Count == 1)  // bcz: Ugh!  if more than one doc is selected and we're dragging the image, then this
                                        //      seems to override our DragUI override and only the dragged image is shown.
            {
                var type = docView.ViewModel.DocumentController.GetDocType();
                switch (type)
                {
                    case "Image Box":
                    case "Video Box":
                        try
                        {
                            var uri = type == "Image Box" ? docView.ViewModel.DataDocument.GetField<ImageController>(KeyStore.DataKey).Data :
                                                            docView.ViewModel.DataDocument.GetField<VideoController>(KeyStore.DataKey).Data;
                            var sf = (uri.AbsoluteUri.StartsWith("ms-appx://") || uri.AbsoluteUri.StartsWith("ms-appdata://"))
                                        ? await StorageFile.GetFileFromApplicationUriAsync(uri)
                                        : await StorageFile.GetFileFromPathAsync(uri.LocalPath);
                            args.Data.SetStorageItems(new HashSet<StorageFile>(new StorageFile[] { sf }));
                        }
                        catch (Exception ex) { }

                        break;
                    default:
                        break;
                }
            }
            args.AllowedOperations = DataPackageOperation.Link | DataPackageOperation.Move| DataPackageOperation.Copy;

            // compute the drag bounds rectangle and make all dragged documents not hit test visible (so we don't drop on them).
            // then get the bitmap that contains all the documents being dragged.
            var dragBounds = Rect.Empty;
            foreach (var dv in _dragViews)
            {
                dragBounds.Union(dv.TransformToVisual(MainPage.Instance.xOuterGrid).TransformBounds(new Rect(0, 0, dv.ActualWidth, dv.ActualHeight)));
                dv.IsHitTestVisible = false;
            }
            var def = args.GetDeferral();
            try
            {
                await CreateDragDropBitmap(docView, _dragViews.Count == 1 ? (FrameworkElement)docView : MainPage.Instance.xOuterGrid, args, dragBounds);
            }
            catch (Exception) { }
            def.Complete();

            // When moving a document, collapse the original so that only the Drag feedback is visible.
            if (!docView.IsShiftPressed() && !docView.IsCtrlPressed() && !docView.IsAltPressed())
            {
                // unfortunately, there is no synchronization between when the dragDrop feedback begins and
                // when the document is made (in)visible.
                // To avoid the jarring temporary artifact of the document appearing to be deleted, we 
                // wait until we start getting DragOver events and then collapse the dragged document.
                MainPage.Instance.xOuterGrid.AllowDrop = true;
                MainPage.Instance.xOuterGrid.DragOver -= CollectionDragOver;
                MainPage.Instance.xOuterGrid.DragOver += CollectionDragOver; // bcz: we rely on no one Handle'ing DragOver events
            }
        }

        private static async Task CreateDragDropBitmap(DocumentView docView, FrameworkElement renderTarget, DragStartingEventArgs args, Rect dragBounds)
        {
            var (finalBitmap, scaling) = await ExtractDocumentBitmap(renderTarget, dragBounds);
            var cursorPt = args.GetPosition(MainPage.Instance.xOuterGrid);
            var topLeft = docView.TransformToVisual(MainPage.Instance.xOuterGrid).TransformPoint(new Point());
            args.DragUI.SetContentFromSoftwareBitmap(finalBitmap, new Point(cursorPt.X - topLeft.X - (dragBounds.X - topLeft.X) * scaling,
                                                                            cursorPt.Y - topLeft.Y - (dragBounds.Y - topLeft.Y) * scaling));
        }
        public static async Task<(SoftwareBitmap, double)> ExtractDocumentBitmap(FrameworkElement renderTarget, Rect dragBounds)
        {
            var rtb = new RenderTargetBitmap();
            var screenscaling = renderTarget.GetBoundingRect(MainPage.Instance.xOuterGrid).Width / renderTarget.ActualWidth;
            var deviceScaling = DisplayInformation.GetForCurrentView().RawPixelsPerViewPixel;
            await rtb.RenderAsync(renderTarget, (int)(screenscaling * renderTarget.ActualWidth), (int)(screenscaling * renderTarget.ActualHeight));
            var buf = (await rtb.GetPixelsAsync()).ToArray();
            var miniBitmap = new WriteableBitmap(rtb.PixelWidth, rtb.PixelHeight);
            miniBitmap.PixelBuffer.AsStream().Write(buf, 0, buf.Length);

            // copy out the bitmap rectangle that contains all the documents being dragged
            var renderBounds = renderTarget.GetBoundingRect(MainPage.Instance.xOuterGrid);
            var scaling = rtb.PixelWidth / renderTarget.ActualWidth;
            var parentBitmap = new WriteableBitmap((int)(dragBounds.Width * deviceScaling), (int)(dragBounds.Height * deviceScaling));
            parentBitmap.Blit(new Point(renderBounds.Left * scaling - dragBounds.X * scaling, renderBounds.Top * scaling - dragBounds.Y * scaling),
                                miniBitmap,
                                new Rect(0, 0, miniBitmap.PixelWidth, miniBitmap.PixelHeight),
                                Colors.White, WriteableBitmapExtensions.BlendMode.None);

            // Convert the dragged documents' bitmap into a software bitmap that can be used for the Drag/Drop UI
            // and offset it to pick correlate properly with the cursor.
            return (SoftwareBitmap.CreateCopyFromBuffer(parentBitmap.PixelBuffer, BitmapPixelFormat.Bgra8, parentBitmap.PixelWidth,
                                                       parentBitmap.PixelHeight, BitmapAlphaMode.Premultiplied),
                    scaling);
        }

        #endregion
    }
}
