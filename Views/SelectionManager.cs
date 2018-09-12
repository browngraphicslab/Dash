using DashShared;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Windows.Andy.Code4App.Extension.CommonObjectEx;
using Windows.ApplicationModel.DataTransfer;
using Windows.Foundation;
using Windows.System;
using Windows.UI;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Imaging;
using Windows.UI.Xaml.Shapes;
using Dash.Converters;
using Visibility = Windows.UI.Xaml.Visibility;
using Windows.ApplicationModel.DataTransfer.DragDrop.Core;
using Windows.Graphics.Display;
using Windows.Storage.Streams;
using Windows.Graphics.Imaging;
using Windows.UI.Input;
using Windows.UI.Xaml.Media.Animation;
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
        public static void InitiateDragDrop(DocumentView draggedView, PointerPoint p, ManipulationStartedRoutedEventArgs e)
        {
            if (e != null)
            {
                e.Handled = true;
                e.Complete();
            }
            _dragViews = SelectedDocs.Contains(draggedView) ? SelectedDocs : new List<DocumentView>(new[] { draggedView });

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
            _dragViews?.ForEach((dv) => dv.Visibility = Visibility.Visible);
            _dragViews?.ForEach((dv) => dv.IsHitTestVisible = true);
            _dragViews = null;
            DragManipulationCompleted?.Invoke(sender, null);
        }

        public static async void DragStarting(DocumentView docView, UIElement sender, DragStartingEventArgs args)
        {
            DragManipulationStarted?.Invoke(sender, null);

           
            double scaling = DisplayInformation.GetForCurrentView().RawPixelsPerViewPixel;

            var rawOffsets = _dragViews.Select(args.GetPosition);
            var offsets = rawOffsets.Select(ro => new Point((ro.X - args.GetPosition(docView).X), (ro.Y - args.GetPosition(docView).Y)));

            args.Data.AddDragModel(new DragDocumentModel(_dragViews,
                _dragViews.Select(dv => dv.GetFirstAncestorOfType<AnnotationOverlay>() == null ? dv.ParentCollection : null).ToList(),
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
            }

            var width = (br.X - tl.X);
            var height = (br.Y - tl.Y);
            var s1 = new Point(width, height);

            // create an empty parent bitmap large enough for all selected elements that we can
            // blip a bitmap for each element onto
            var parentBitmap = new WriteableBitmap((int)s1.X, (int)s1.Y);
            var thisOffset = new Point();
            
            var def = args.GetDeferral();
            foreach (var doc in _dragViews)
            {
                // renders a bitmap for each selected document and blits it onto the parent bitmap at the correct position
                var rtb = new RenderTargetBitmap();
                var s = new Point(Math.Ceiling(doc.ActualWidth), Math.Ceiling(doc.ActualHeight));
                var transformToVisual = doc.TransformToVisual(Window.Current.Content);
                var rect = transformToVisual.TransformBounds(new Rect(0, 0, s.X, s.Y));
                s = new Point(rect.Width, rect.Height);
                await rtb.RenderAsync(doc, (int)Math.Floor(s.X), (int)Math.Floor(s.Y));
                var buf = await rtb.GetPixelsAsync();
                var miniBitmap = new WriteableBitmap(rtb.PixelWidth, rtb.PixelHeight);
                var miniSBitmap = SoftwareBitmap.CreateCopyFromBuffer(buf, BitmapPixelFormat.Bgra8, rtb.PixelWidth, rtb.PixelHeight);
                miniSBitmap.CopyToBuffer(miniBitmap.PixelBuffer);
                var pos = new Point(rect.Left * scaling - tl.X, rect.Top * scaling - tl.Y);
                parentBitmap.Blit(pos, miniBitmap, new Rect(0, 0, miniBitmap.PixelWidth, miniBitmap.PixelHeight),
                    Colors.White, WriteableBitmapExtensions.BlendMode.Additive);

                if (doc == docView)
                {
                    thisOffset.X = rect.X - tl.X / scaling;
                    thisOffset.Y = rect.Y - tl.Y / scaling;
                }
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
