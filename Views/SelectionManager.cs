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

        private static List<DocumentView> _dragViews;
        public  static event EventHandler DragManipulationStarted;
        public  static event EventHandler DragManipulationCompleted;

        private static List<DocumentView> SelectedDocs { get; set; } = new List<DocumentView>();

        public static List<DocumentView> GetSelectedDocs()
        {
            return new List<DocumentView>(SelectedDocs);
        }

        public static bool IsSelected(DocumentView doc)
        {
            return SelectedDocs.Contains(doc);
        }

        public delegate void SelectionChangedHandler(DocumentSelectionChangedEventArgs args);
        public static event SelectionChangedHandler SelectionChanged;

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
                foreach (var documentView in SelectedDocs)
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

                var args = new DocumentSelectionChangedEventArgs(deselected, alreadySelected ? new List<DocumentView>() : new List<DocumentView>{doc});

                SelectedDocs = new List<DocumentView>{doc};
                if (!alreadySelected)
                {
                    SelectHelper(doc);
                }

                OnSelectionChanged(args);
            }
            else
            {
                if (SelectedDocs.Contains(doc))
                {
                    DeselectHelper(doc);
                    SelectedDocs.Remove(doc);
                    OnSelectionChanged(new DocumentSelectionChangedEventArgs(new List<DocumentView>{doc}, new List<DocumentView>()));
                }
                else
                {
                    SelectHelper(doc);
                    SelectedDocs.Add(doc);
                    OnSelectionChanged(new DocumentSelectionChangedEventArgs(new List<DocumentView>(), new List<DocumentView>{doc}));
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
                if (SelectedDocs.Contains(documentView))
                {
                    continue;
                }

                SelectHelper(documentView);
                selectedDocs.Add(documentView);
                if (keepPrevious)
                {
                    SelectedDocs.Add(documentView);
                }
            }

            var deselectedDocs = new List<DocumentView>();
            if (!keepPrevious)
            {
                foreach (var documentView in SelectedDocs)
                {
                    if (!documentViews.Contains(documentView))
                    {
                        DeselectHelper(documentView);
                        deselectedDocs.Add(documentView);
                    }
                }

                SelectedDocs = documentViews;
            }

            OnSelectionChanged(new DocumentSelectionChangedEventArgs(deselectedDocs, selectedDocs));
        }

        public static void Deselect(DocumentView view)
        {
            if (SelectedDocs.Contains(view))
            {
                SelectedDocs.Remove(view);
                DeselectHelper(view);
                OnSelectionChanged(new DocumentSelectionChangedEventArgs(new List<DocumentView>{view}, new List<DocumentView>()));
            }
        }

        public static void DeselectAll()
        {
            foreach (var documentView in SelectedDocs)
            {
                DeselectHelper(documentView);
            }
            var args = new DocumentSelectionChangedEventArgs(new List<DocumentView>(SelectedDocs), new List<DocumentView>());
            SelectedDocs.Clear();
            OnSelectionChanged(args);
        }

        private static void SelectHelper(DocumentView view)
        {
            view.OnSelected();
        }

        private static void DeselectHelper(DocumentView view)
        {
            view.OnDeselected();
        }

        public static IEnumerable<DocumentView> GetSelectedDocumentsInCollection(CollectionFreeformBase collection)
        {
            return SelectedDocs.Where(doc => Equals(doc.ParentCollection?.CurrentView, collection));
        }

        /*
         * Returns itself if nothing is selected.
         */
        public static List<DocumentView> GetSelectedSiblings(DocumentView view)
        {
            if (view.ParentCollection != null && view.ParentCollection.CurrentView is CollectionFreeformBase cfb)
            {
                var marqueeDocs = GetSelectedDocumentsInCollection(cfb).ToList();
                if (marqueeDocs.Contains(view))
                    return marqueeDocs;
            }
            return new List<DocumentView>(new[] { view });
        }

        static void OnSelectionChanged(DocumentSelectionChangedEventArgs args)
        {
            if (args.DeselectedViews.Count != 0 || args.SelectedViews.Count != 0) 
                SelectionChanged?.Invoke(args);
        }

        #region Drag Manipulation Methods

        public static bool TryInitiateDragDrop(DocumentView draggedView, PointerRoutedEventArgs pe, ManipulationStartedRoutedEventArgs e)
        {
            var parents = draggedView.GetAncestorsOfType<DocumentView>().ToList();
            if (parents.Count < 2 || SelectionManager.GetSelectedDocs().Contains(draggedView) ||
                SelectionManager.GetSelectedDocs().Contains(draggedView.GetFirstAncestorOfType<DocumentView>()))
            {
                SelectionManager.InitiateDragDrop(draggedView, pe?.GetCurrentPoint(draggedView), e);
                return true;
            }
            else
            {
                var prevParent = parents.FirstOrDefault();
                foreach (var parent in parents)// bcz: Ugh.. this is ugly.
                {
                    if (parent.ViewModel.DataDocument.DocumentType.Equals(CollectionNote.DocumentType) &&
                        parent.GetFirstDescendantOfType<CollectionView>().CurrentView is CollectionFreeformBase &&
                        (SelectionManager.GetSelectedDocs().Contains(parent) || parent == parents.Last()))
                    {
                        SelectionManager.InitiateDragDrop(prevParent, pe?.GetCurrentPoint(prevParent), e);
                        return true;
                    }
                    prevParent = parent;
                }
            }
            return false;
        }
        public static void InitiateDragDrop(DocumentView draggedView, PointerPoint p, ManipulationStartedRoutedEventArgs e)
        {
            if (e != null)
            {
                e.Handled = true;
                e.Complete();
            }

            Debug.WriteLine("DragViews in initiate");
            _dragViews = SelectedDocs.Contains(draggedView) ? SelectedDocs.ToArray().ToList() : new List<DocumentView>(new[] { draggedView });

            if (draggedView.ViewModel.DocumentController.GetIsAdornment())
            {
                var rect = new Rect(draggedView.ViewModel.XPos, draggedView.ViewModel.YPos,
                    draggedView.ViewModel.ActualSize.X, draggedView.ViewModel.ActualSize.Y);
                foreach (var cp in draggedView.GetFirstAncestorOfType<Canvas>()?.Children)
                {
                    if (cp.GetFirstDescendantOfType<DocumentView>() != null)
                    {
                        var dv = cp.GetFirstDescendantOfType<DocumentView>();
                        var dvmRect = new Rect(dv.ViewModel.XPos, dv.ViewModel.YPos, dv.ViewModel.ActualSize.X,
                            dv.ViewModel.ActualSize.Y);
                        if (rect.Intersects(dvmRect))
                        {
                            _dragViews.Add(dv);
                        }
                    }
                }
            }
            draggedView.StartDragAsync(p ?? MainPage.PointerRoutedArgsHack.GetCurrentPoint(draggedView));
        }

        public static void DropCompleted(DocumentView docView, UIElement sender, DropCompletedEventArgs args)
        {

            LocalSqliteEndpoint.SuspendTimer = false;
            _dragViews?.ForEach((dv) => dv.Visibility = Visibility.Visible);
            _dragViews?.ForEach((dv) => dv.IsHitTestVisible = true);
            _dragViews = null;
            DragManipulationCompleted?.Invoke(sender, null);
        }

        public static async void DragStarting(DocumentView docView, UIElement sender, DragStartingEventArgs args)
        {
            DragManipulationStarted?.Invoke(sender, null);
            LocalSqliteEndpoint.SuspendTimer = true;
           
            double scaling = DisplayInformation.GetForCurrentView().RawPixelsPerViewPixel;

            var rawOffsets = _dragViews.Select(args.GetPosition);
            var offsets = rawOffsets.Select(ro => new Point((ro.X - args.GetPosition(docView).X), (ro.Y - args.GetPosition(docView).Y)));

            args.Data.SetDragModel(new DragDocumentModel(_dragViews,
                _dragViews.Select(dv => dv.GetFirstAncestorOfType<AnnotationOverlay>() == null ? dv.ParentCollection?.ViewModel : null).ToList(),
                offsets.ToList(), args.GetPosition(docView)));

            args.AllowedOperations =  DataPackageOperation.Link | DataPackageOperation.Move | DataPackageOperation.Copy;

            //combine all selected docs into an image to display on drag
            //use size of each doc to get size of combined image
            var tl = new Point(double.PositiveInfinity, double.PositiveInfinity);
            var br = new Point(double.NegativeInfinity, double.NegativeInfinity);
            foreach (var doc in _dragViews)
            {
                var bounds = new Rect(0, 0, doc.ActualWidth, doc.ActualHeight);
                bounds = doc.TransformToVisual(Window.Current.Content).TransformBounds(bounds);
                tl.X = Math.Min(tl.X, bounds.Left * scaling);
                tl.Y = Math.Min(tl.Y, bounds.Top * scaling);
                br.X = Math.Max(br.X, bounds.Right * scaling);
                br.Y = Math.Max(br.Y, bounds.Bottom * scaling);
                doc.IsHitTestVisible = false;
                doc.Tag = "INVISIBLE";
            }

            var width = (br.X - tl.X);
            var height = (br.Y - tl.Y);
            var s1 = new Point(width, height);

            // create an empty parent bitmap large enough for all selected elements that we can
            // blip a bitmap for each element onto
            var parentBitmap = new WriteableBitmap((int)s1.X, (int)s1.Y);
            var thisOffset = new Point();

            var def = args.GetDeferral();
            try
            {
                var doc =  MainPage.Instance.MainSplitter;
                // renders a bitmap for each selected document and blits it onto the parent bitmap at the correct position
                var rtb = new RenderTargetBitmap();
                var rect = doc.TransformToVisual(Window.Current.Content).TransformBounds(new Rect(0, 0, doc.ActualWidth, doc.ActualHeight));
                await rtb.RenderAsync(doc);
                var buf = (await rtb.GetPixelsAsync()).ToArray();
                var miniBitmap = new WriteableBitmap(rtb.PixelWidth, rtb.PixelHeight);
                miniBitmap.PixelBuffer.AsStream().Write(buf, 0, buf.Length);
                parentBitmap.Blit(new Point(rect.Left - tl.X, rect.Top - tl.Y), miniBitmap, new Rect(0, 0, miniBitmap.PixelWidth, miniBitmap.PixelHeight),
                    Colors.White, WriteableBitmapExtensions.BlendMode.Additive);
            } catch (Exception)
            {

            }

            var p = args.GetPosition(Window.Current.Content);
            p.X = p.X - tl.X / scaling + thisOffset.X;
            p.Y = p.Y - tl.Y / scaling + thisOffset.Y;
            var finalBitmap = SoftwareBitmap.CreateCopyFromBuffer(parentBitmap.PixelBuffer, BitmapPixelFormat.Bgra8, parentBitmap.PixelWidth,
                parentBitmap.PixelHeight, BitmapAlphaMode.Premultiplied);
            args.DragUI.SetContentFromSoftwareBitmap(finalBitmap, p);
            if (!docView.IsShiftPressed())
            {
                // unfortunately, there is no synchronization between when the dragDrop feedback begins and
                // when the document is made (in)visible.
                // To avoid the jarring temporary artifact of the document appearing to be deleted, we 
                // wait until we start getting DragOver events and then collapse the dragged document.
                MainPage.Instance.xOuterGrid.RemoveHandler(UIElement.DragOverEvent, _collectionDragOverHandler);
                MainPage.Instance.xOuterGrid.AddHandler(UIElement.DragOverEvent, _collectionDragOverHandler, true); // bcz: true doesn't actually work. we rely on no one Handle'ing DragOver events
            }
            def.Complete();
            MainPage.Instance.XDocumentDecorations.VisibilityState = Visibility.Collapsed;
            MainPage.Instance.XDocumentDecorations.ResizerVisibilityState = Visibility.Collapsed;
        }

        static DragEventHandler _collectionDragOverHandler = new DragEventHandler(collectionDragOver);
        static void collectionDragOver(object sender, DragEventArgs e)
        {
            if (e.DragUIOverride != null)
                e.DragUIOverride.IsContentVisible = true;
            _dragViews?.ForEach((dv) => dv.Visibility = Visibility.Collapsed);
            (sender as FrameworkElement).RemoveHandler(UIElement.DragOverEvent, _collectionDragOverHandler);
        }

        #endregion
    }
}
