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

        private static List<DocumentController> _dragDocs;
        public  static event EventHandler DragManipulationStarted;
        public  static event EventHandler DragManipulationCompleted;

        private static IList<DocumentView> SelectedDocs { get; set; } = new List<DocumentView>();

        public static IList<DocumentView> GetSelectedDocs()
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
            if (args.DeselectedViews.Count == 0 && args.SelectedViews.Count == 0)
            {
                return;
            }
            SelectionChanged?.Invoke(args);
        }

        #region Drag Manipulation Methods
        public static async void InitiateDragDrop(DocumentView draggedDoc, PointerPoint p, ManipulationStartedRoutedEventArgs e)
        {
            if (e != null)
            {
                e.Handled = true;
                e.Complete();
            }
            await draggedDoc.StartDragAsync(p ?? PointerPoint.GetCurrentPoint(MainPage.PointerCaptureHack?.PointerId ?? 1));
        }

        public static void DropCompleted(DocumentView docView, UIElement sender, DropCompletedEventArgs args)
        {
            if (args.DropResult == DataPackageOperation.None)
                _dragDocs?.ForEach(d => d.SetHidden(false));
            _dragDocs = null;
            DragManipulationCompleted?.Invoke(sender, null);
        }
        public static async void DragStarting(DocumentView docView, UIElement sender, DragStartingEventArgs args)
        {
            DragManipulationStarted?.Invoke(sender, null);
            MainPage.Instance.XDocumentDecorations.VisibilityState = Visibility.Collapsed;
            MainPage.Instance.XDocumentDecorations.ResizerVisibilityState = Visibility.Collapsed;
            docView.ToFront();

            var dragSelectionViews = SelectionManager.GetSelectedDocs().Contains(docView) ? SelectionManager.GetSelectedDocs() : new List<DocumentView>(new DocumentView[] { docView });
            _dragDocs = dragSelectionViews.Select(dv => dv.ViewModel.DocumentController).ToList();
            double scaling = DisplayInformation.GetForCurrentView().RawPixelsPerViewPixel;

            var rawOffsets = dragSelectionViews.Select(args.GetPosition);
            var offsets = rawOffsets.Select(ro => new Point((ro.X - args.GetPosition(docView).X), (ro.Y - args.GetPosition(docView).Y)));

            args.Data.AddDragModel(new DragDocumentModel(_dragDocs, true, off: offsets.ToList())
            {
                Offset = args.GetPosition(docView),
                SourceCollectionViews = dragSelectionViews.Select(dv => dv.ParentCollection).ToList()
            });

            args.AllowedOperations =
                DataPackageOperation.Link | DataPackageOperation.Move | DataPackageOperation.Copy;

            //combine all selected docs into an image to display on drag
            //use size of each doc to get size of combined image
            var tl = new Point(double.PositiveInfinity, double.PositiveInfinity);
            var br = new Point(double.NegativeInfinity, double.NegativeInfinity);
            foreach (var doc in dragSelectionViews)
            {
                var bounds = new Rect(0, 0, doc.ActualWidth, doc.ActualHeight);
                bounds = doc.TransformToVisual(Window.Current.Content).TransformBounds(bounds);
                tl.X = Math.Min(tl.X, bounds.Left * scaling);
                tl.Y = Math.Min(tl.Y, bounds.Top * scaling);
                br.X = Math.Max(br.X, bounds.Right * scaling);
                br.Y = Math.Max(br.Y, bounds.Bottom * scaling);
            }

            var width = (br.X - tl.X);
            var height = (br.Y - tl.Y);
            var s1 = new Point(width, height);

            var bp = new WriteableBitmap((int)s1.X, (int)s1.Y);
            var thisOffset = new Point();

            var def = args.GetDeferral();
            foreach (var doc in dragSelectionViews)
            {
                var rtb = new RenderTargetBitmap();
                var s = new Point(Math.Floor(doc.ActualWidth), Math.Floor(doc.ActualHeight));
                var transformToVisual = doc.TransformToVisual(Window.Current.Content);
                var rect = transformToVisual.TransformBounds(new Rect(0, 0, s.X, s.Y));
                s = new Point(rect.Width, rect.Height);
                await rtb.RenderAsync(doc, (int)Math.Floor(s.X), (int)Math.Floor(s.Y));
                var buf = await rtb.GetPixelsAsync();
                var additionalBp = new WriteableBitmap(rtb.PixelWidth, rtb.PixelHeight);
                var sb = SoftwareBitmap.CreateCopyFromBuffer(buf, BitmapPixelFormat.Bgra8, rtb.PixelWidth, rtb.PixelHeight);
                sb.CopyToBuffer(additionalBp.PixelBuffer);
                var c = additionalBp.GetPixel(40, 0);
                var pos = new Point(rect.Left * scaling - tl.X, rect.Top * scaling - tl.Y);
                bp.Blit(pos, additionalBp, new Rect(0, 0, additionalBp.PixelWidth, additionalBp.PixelHeight),
                    Colors.White, WriteableBitmapExtensions.BlendMode.None);
                //bp.BlitRender(additionalBp, false, 1F,
                //    new TranslateTransform {X = pos.X, Y = pos.Y});

                if (doc == docView)
                {
                    thisOffset.X = rect.X - tl.X / scaling;
                    thisOffset.Y = rect.Y - tl.Y / scaling;
                }
            }

            var p = args.GetPosition(Window.Current.Content);
            p.X = p.X - tl.X / scaling + thisOffset.X;
            p.Y = p.Y - tl.Y / scaling + thisOffset.Y;
            var sb2 = SoftwareBitmap.CreateCopyFromBuffer(bp.PixelBuffer, BitmapPixelFormat.Bgra8, bp.PixelWidth,
                bp.PixelHeight, BitmapAlphaMode.Premultiplied);
            args.DragUI.SetContentFromSoftwareBitmap(sb2, p);

            if (!docView.IsShiftPressed())
            {
                dragSelectionViews.ForEach((dv) => dv.ViewModel.DocumentController.SetHidden(true));
            }
            def.Complete();
        }

        #endregion
    }
}
