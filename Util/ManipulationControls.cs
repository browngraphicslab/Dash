using Microsoft.ProjectOxford.Vision.Contract;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Windows.Devices.Input;
using Windows.Foundation;
using Windows.System;
using Windows.UI;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using NewControls.Geometry;
using static Dash.NoteDocuments;
using Point = Windows.Foundation.Point;
using System.Collections.ObjectModel;

namespace Dash
{

    /// <summary>
    /// Instantiations of this class in a UserControl element will give that
    /// control's selected UIElement the ability to be moved and zoomed based on
    /// interactions with its given handleControl grid.
    /// </summary>
    public class ManipulationControls : IDisposable
    {
        public double MinScale { get; set; } = .2;
        public double MaxScale { get; set; } = 5.0;
        public DocumentView ParentDocument { get; set; }
        public double ElementScale { get; set; } = 1.0;
        public List<DocumentViewModel> Grouping { get; set; }

        public delegate void OnManipulatorTranslatedHandler(TransformGroupData transformationDelta);
        public event OnManipulatorTranslatedHandler OnManipulatorTranslatedOrScaled;

        /// <summary>
        /// Created a manipulation control to move element
        /// NOTE: bounds checking is done relative to element.Parent so the element must be in an element with the proper size for bounds checking
        /// </summary>
        /// <param name="element">The element to add manipulation to</param>
        /// <param name="doesRespondToManipulationDelta"></param>
        /// <param name="doesRespondToPointerWheel"></param>
        /// <param name="borderRegions"></param>
        public ManipulationControls(DocumentView element, List<FrameworkElement> borderRegions = null)
        {
            ParentDocument = element;
            
            element.ManipulationDelta += ElementOnManipulationDelta;
            element.PointerWheelChanged += ElementOnPointerWheelChanged;
            if (borderRegions != null)
            {
                foreach (var borderRegion in borderRegions)
                {
                    borderRegion.ManipulationMode = ManipulationModes.All;
                    borderRegion.ManipulationDelta += ElementOnManipulationDelta;
                    borderRegion.ManipulationStarted += BorderOnManipulationStarted;
                    borderRegion.AddHandler(UIElement.ManipulationCompletedEvent, new ManipulationCompletedEventHandler(ElementOnManipulationCompleted), true);
                }
            }
            element.ManipulationMode = ManipulationModes.All;
            element.ManipulationStarted += ElementOnManipulationStarted;
            element.ManipulationInertiaStarting += (sender, args) => args.TranslationBehavior.DesiredDeceleration = 0.02;
            element.AddHandler(UIElement.ManipulationCompletedEvent, new ManipulationCompletedEventHandler(ElementOnManipulationCompleted), true);
        }

        #region Snapping Layouts


        /// <summary>
        /// Enum used for snapping.
        /// TODO: Move this to the top of the class definition.
        /// </summary>
        private enum Side
        {
            Top = 1,
            Bottom = ~Top,
            Left = 2,
            Right = ~Left,
        };

        /// <summary>
        /// TODO: Move this to the top of the class definition.
        /// </summary>
        private const double ALIGNING_RECTANGLE_SENSITIVITY = 15.0;

        /// <summary>
        /// TODO: Move this to the top of the class definition.
        /// </summary>
        private const double ALIGNMENT_THRESHOLD = .2;


        /// <summary>
        /// Previews the new location/position of the element
        /// </summary>
        public void Snap(bool preview)
        {
            var docRoot = ParentDocument;
            var parent = ParentDocument.GetFirstAncestorOfType<CollectionView>()?.CurrentView as CollectionFreeformView;

            if (parent == null || ParentDocument.Equals(parent))
            {
                return;
            }

            MainPage.Instance.TemporaryRectangle.Width = MainPage.Instance.TemporaryRectangle.Height = 0;

            var currentBoundingBox = docRoot.ViewModel.Bounds;
            var closest = GetClosestDocumentView(currentBoundingBox);
            if (preview)
                PreviewSnap(currentBoundingBox, closest);
            else
                SnapToDocumentView(docRoot, closest);
        }

        /// <summary>
        /// Snaps location of this DocumentView to the DocumentView passed in, also inheriting its width or height dimensions.
        /// </summary>
        /// <param name="closestDocumentView"></param>
        private void SnapToDocumentView(DocumentView currrentDoc, Tuple<DocumentView, Side, double> closestDocumentView)
        {
            if (closestDocumentView == null)
            {
                return;
            }

            var documentView = closestDocumentView.Item1;
            var side = closestDocumentView.Item2;
            var currentScaleAmount = currrentDoc.ViewModel.GroupTransform.ScaleAmount;

            var topLeftPoint = new Point(documentView.ViewModel.GroupTransform.Translate.X,
                documentView.ViewModel.GroupTransform.Translate.Y);
            var bottomRightPoint = new Point(documentView.ViewModel.GroupTransform.Translate.X + documentView.ActualWidth,
                documentView.ViewModel.GroupTransform.Translate.Y + documentView.ActualHeight);

            var newBoundingBox = CalculateAligningRectangleForSide(~side, topLeftPoint, bottomRightPoint, currrentDoc.ActualWidth, currrentDoc.ActualHeight);

            var translate = new Point(newBoundingBox.X, newBoundingBox.Y);

            currrentDoc.ViewModel.GroupTransform = new TransformGroupData(translate, currentScaleAmount);

            currrentDoc.ViewModel.Width = newBoundingBox.Width;
            currrentDoc.ViewModel.Height = newBoundingBox.Height;
        }


        /// <summary>
        /// Places the TemporaryRectangle in the location where the document view being manipulation would be dragged.
        /// </summary>
        /// <param name="currentBoundingBox"></param>
        /// <param name="closestDocumentView"></param>
        private void PreviewSnap(Rect currentBoundingBox, Tuple<DocumentView, Side, double> closestDocumentView)
        {
            if (closestDocumentView == null) return;

            var docRoot = ParentDocument;

            var parent = ParentDocument.GetFirstAncestorOfType<CollectionView>()?.CurrentView as CollectionFreeformView;

            var documentView = closestDocumentView.Item1;
            var side = closestDocumentView.Item2;

            var closestBoundsInCollectionSpace = documentView.ViewModel.Bounds;
            var boundingBoxCollectionSpace = CalculateAligningRectangleForSide(~side, closestBoundsInCollectionSpace, currentBoundingBox.Width, currentBoundingBox.Height);

            //Transform the rect from xCollectionCanvas (which is equivalent to xItemsControl.ItemsPanelRoot) space to screen space
            var boundingBoxScreenSpace = Util.RectTransformFromVisual(boundingBoxCollectionSpace, parent?.xItemsControl.ItemsPanelRoot);
            MainPage.Instance.TemporaryRectangle.Width = boundingBoxScreenSpace.Width;
            MainPage.Instance.TemporaryRectangle.Height = boundingBoxScreenSpace.Height;

            Canvas.SetLeft(MainPage.Instance.TemporaryRectangle, boundingBoxScreenSpace.X);
            Canvas.SetTop(MainPage.Instance.TemporaryRectangle, boundingBoxScreenSpace.Y);
        }


        private Tuple<DocumentView, Side, double> GetClosestDocumentView(Rect currentBoundingBox)
        {
            //List of all DocumentViews hit, along with a double representing how close they are
            var allDocumentViewsHit = HitTestFromSides(currentBoundingBox);

            //Return closest DocumentView (using the double that represents the confidence)
            return allDocumentViewsHit.FirstOrDefault(item => item.Item3 == allDocumentViewsHit.Max(i2 => i2.Item3)); //Sadly no better argmax one-liner 
        }

        /// <summary>
        /// Returns a list of DocumentViews hit by the side, as well as a double representing how close they are
        /// </summary>
        /// <param name="side"></param>
        /// <param name="topLeftScreenPoint"></param>
        /// <param name="bottomRightScreenPoint"></param>
        /// <returns></returns>
        private List<Tuple<DocumentView, Side, double>> HitTestFromSides(Rect currentBoundingBox)
        {

            var documentViewsAboveThreshold = new List<Tuple<DocumentView, Side, double>>();
            var parent = ParentDocument.GetFirstAncestorOfType<CollectionView>()?.CurrentView as CollectionFreeformView;
            Debug.Assert(parent != null);

            var docRoot = ParentDocument;

            var listOfSiblings = parent.DocumentViews.Where(docView => docView != docRoot);
            Side[] sides = { Side.Top, Side.Bottom, Side.Left, Side.Right };

            foreach (var side in sides)
            {
                var sideRect = CalculateAligningRectangleForSide(side, currentBoundingBox, ALIGNING_RECTANGLE_SENSITIVITY, ALIGNING_RECTANGLE_SENSITIVITY);
                foreach (var sibling in listOfSiblings)
                {
                    Rect intersection = sideRect;
                    intersection.Intersect(sibling.ViewModel.Bounds); //Mutates intersection

                    var confidence = CalculateSnappingConfidence(side, sideRect, sibling);
                    if (!intersection.IsEmpty && confidence >= ALIGNMENT_THRESHOLD)
                    {
                        documentViewsAboveThreshold.Add(new Tuple<DocumentView, Side, double>(sibling, side, confidence));
                    }
                }
            }

            return documentViewsAboveThreshold;
        }

        private double CalculateSnappingConfidence(Side side, Rect hitTestRect, DocumentView otherDocumentView)
        {
            Rect otherDocumentViewBoundingBox = otherDocumentView.ViewModel.Bounds; // otherDocumentView.GetBoundingBoxScreenSpace();

            var midX = hitTestRect.X + hitTestRect.Width / 2;
            var midY = hitTestRect.Y + hitTestRect.Height / 2;

            double distanceToMid = -1;

            //Get normalized x or y distance from the complementary edge of the other DocumentView and the midpoint of the hitTestRect
            switch (side)
            {
                case Side.Top:
                    distanceToMid = Math.Abs(midY - (otherDocumentViewBoundingBox.Y + otherDocumentViewBoundingBox.Height));
                    distanceToMid = 1.0f - Math.Min(1.0, distanceToMid / hitTestRect.Height);
                    return distanceToMid * GetSharedRectWidthProportion(hitTestRect, otherDocumentViewBoundingBox);
                case Side.Bottom:
                    distanceToMid = Math.Abs(otherDocumentViewBoundingBox.Y - midY);
                    distanceToMid = 1.0f - Math.Min(1.0, distanceToMid / hitTestRect.Height);
                    return distanceToMid * GetSharedRectWidthProportion(hitTestRect, otherDocumentViewBoundingBox);
                case Side.Left:
                    distanceToMid = Math.Abs(midX - (otherDocumentViewBoundingBox.X + otherDocumentViewBoundingBox.Width));
                    distanceToMid = 1.0f - Math.Min(1.0, distanceToMid / hitTestRect.Width);
                    return distanceToMid * GetSharedRectHeightProportion(hitTestRect, otherDocumentViewBoundingBox);
                case Side.Right:
                    distanceToMid = Math.Abs(otherDocumentViewBoundingBox.X - midX);
                    distanceToMid = 1.0f - Math.Min(1.0, distanceToMid / hitTestRect.Width);
                    return distanceToMid * GetSharedRectHeightProportion(hitTestRect, otherDocumentViewBoundingBox);
            }
            return distanceToMid;
        }


        private Rect CalculateAligningRectangleForSide(Side side, Point topLeftPoint, Point bottomRightPoint, double w, double h)
        {
            Point newTopLeft, newBottomRight;

            switch (side)
            {
                case Side.Top:
                    newTopLeft = new Point(topLeftPoint.X, topLeftPoint.Y - h);
                    newBottomRight = new Point(bottomRightPoint.X, topLeftPoint.Y);
                    break;
                case Side.Bottom:
                    newTopLeft = new Point(topLeftPoint.X, bottomRightPoint.Y);
                    newBottomRight = new Point(bottomRightPoint.X, bottomRightPoint.Y + h);
                    break;
                case Side.Left:
                    newTopLeft = new Point(topLeftPoint.X - w, topLeftPoint.Y);
                    newBottomRight = new Point(topLeftPoint.X, bottomRightPoint.Y);
                    break;
                case Side.Right:
                    newTopLeft = new Point(bottomRightPoint.X, topLeftPoint.Y);
                    newBottomRight = new Point(bottomRightPoint.X + w, bottomRightPoint.Y);
                    break;
            }
            return new Rect(newTopLeft, newBottomRight);
        }

        private Rect CalculateAligningRectangleForSide(Side side, Rect boundingBox, double w, double h)
        {
            Point topLeftPoint = new Point(boundingBox.X, boundingBox.Y);
            Point bottomRightPoint = new Point(boundingBox.X + boundingBox.Width, boundingBox.Y + boundingBox.Height);
            return CalculateAligningRectangleForSide(side, topLeftPoint, bottomRightPoint, w, h);
        }

        private double GetSharedRectWidthProportion(Rect source, Rect target)
        {
            var targetMin = target.X;
            var targetMax = target.X + target.Width;

            var sourceStart = Math.Max(targetMin, source.X);
            var sourceEnd = Math.Min(targetMax, source.X + source.Width);
            return (sourceEnd - sourceStart) / source.Width;
        }

        private double GetSharedRectHeightProportion(Rect source, Rect target)
        {
            var targetMin = target.Y;
            var targetMax = target.Y + target.Height;

            var sourceStart = Math.Max(targetMin, source.Y);
            var sourceEnd = Math.Min(targetMax, source.Y + source.Height);

            return (sourceEnd - sourceStart) / source.Height;
        }
        /*
        /// <summary>
        /// Returns the bounding box in screen space of a FrameworkElement.
        /// 
        /// TODO: move this to a more logical place, such as a Util class or an extension class
        /// </summary>
        /// <returns></returns>
        private Rect GetBoundingBox(FrameworkElement element)
        {
            Point topLeftObjectPoint = new Point(0, 0);
            Point bottomRightObjectPoint = new Point(element.ActualWidth, element.ActualHeight);

            var topLeftPoint = Util.PointTransformFromVisual(topLeftObjectPoint, element);
            var bottomRightPoint = Util.PointTransformFromVisual(bottomRightObjectPoint, element);

            return new Rect(topLeftPoint, bottomRightPoint);
        }
        */

        #endregion

        void ElementOnPointerWheelChanged(object sender, PointerRoutedEventArgs e)
        {
            if (e.KeyModifiers.HasFlag(VirtualKeyModifiers.Control))
            {
                e.Handled = true;

                var point = e.GetCurrentPoint(ParentDocument);

                // get the scale amount from the mousepoint in canvas space
                float scaleAmount = e.GetCurrentPoint(ParentDocument).Properties.MouseWheelDelta > 0 ? 1.07f : 1 / 1.07f;

                //Clamp the scale factor 
                ElementScale *= scaleAmount;

                if (!ClampScale(scaleAmount))
                    OnManipulatorTranslatedOrScaled?.Invoke(new TransformGroupData(new Point(), new Point(scaleAmount, scaleAmount), point.Position));
            }
        }
        void BorderOnManipulationStarted(object sender, ManipulationStartedRoutedEventArgs e)
        {
            ElementOnManipulationStarted(sender, e);
            Grouping = null;
        }
        public void ElementOnManipulationStarted(object sender, ManipulationStartedRoutedEventArgs e)
        {
            if (e != null && ParentDocument.ManipulationMode == ManipulationModes.None)
            {
                e.Complete();
                return;
            }
            Grouping = GroupManager.SetupGroupings(ParentDocument.ViewModel, ParentDocument.ParentCollection, false);
            //var groupViews = GroupViews(_grouping);
            //foreach (var gv in groupViews)
            //    gv.ToFront();

            if (e != null)
                e.Handled = true;
        }
        /// <summary>
        /// Applies manipulation controls (zoom, translate) in the grid manipulation event.
        /// </summary>
        void ElementOnManipulationDelta(object sender, ManipulationDeltaRoutedEventArgs e)
        {
            if (Window.Current.CoreWindow.GetKeyState(VirtualKey.RightButton).HasFlag(CoreVirtualKeyStates.Down) ||
                Window.Current.CoreWindow.GetKeyState(VirtualKey.LeftButton).HasFlag(CoreVirtualKeyStates.Down))
            {
                var pointerPosition = MainPage.Instance.TransformToVisual(ParentDocument.GetFirstAncestorOfType<ContentPresenter>()).TransformPoint(new Point());
                var pointerPosition2 = MainPage.Instance.TransformToVisual(ParentDocument.GetFirstAncestorOfType<ContentPresenter>()).TransformPoint(e.Delta.Translation);
                var delta = new Point(e.Delta.Translation.X == 0 ? 0 : (pointerPosition2.X - pointerPosition.X), e.Delta.Translation.Y == 0 ? 0 : (pointerPosition2.Y - pointerPosition.Y) );

                TranslateAndScale(e.Position, delta, e.Delta.Scale, Grouping);
                //DetectShake(sender, e);

                if (Grouping == null || Grouping.Count < 2)
                    Snap(true);
                e.Handled = true;
            }
        }
        /// <summary>
        /// Applies manipulation controls (zoom, translate) in the grid manipulation event.
        /// </summary>
        /// <param name="e">passed in frm routed event args</param>
        /// <param name="grouped"></param>
        public void TranslateAndScale(Point position, Point translate, double scaleFactor, List<DocumentViewModel> grouped = null)
        {
            ElementScale *= scaleFactor;

            //Clamp the scale factor 
            if (!ClampScale(scaleFactor))
            {
                // translate the entire group except for
                var transformGroup = new TransformGroupData(translate, new Point(scaleFactor, scaleFactor), position);
                if (grouped != null && grouped.Any())
                {
                    foreach (var g in grouped.Except(new List<DocumentViewModel> { ParentDocument.ViewModel }))
                    {
                        g?.TransformDelta(transformGroup);
                    }
                }

                OnManipulatorTranslatedOrScaled?.Invoke(transformGroup);
            }
        }
        public void ElementOnManipulationCompleted(object sender, ManipulationCompletedRoutedEventArgs manipulationCompletedRoutedEventArgs)
        {
            if (manipulationCompletedRoutedEventArgs == null || !manipulationCompletedRoutedEventArgs.Handled)
            {
                if (Grouping == null || Grouping.Count < 2)
                    Snap(false); //Snap if you're dragging the element body and it's not a part of the group

                MainPage.Instance.TemporaryRectangle.Width = MainPage.Instance.TemporaryRectangle.Height = 0;

                var docRoot = ParentDocument;

                var groupViews = GroupViews(Grouping);

                var pointerPosition2 = Windows.UI.Core.CoreWindow.GetForCurrentThread().PointerPosition;
                var x = pointerPosition2.X - Window.Current.Bounds.X;
                var y = pointerPosition2.Y - Window.Current.Bounds.Y;
                var pos = new Point(x, y);
                var overlappedViews = VisualTreeHelper.FindElementsInHostCoordinates(pos, MainPage.Instance).OfType<DocumentView>().ToList();

                var pc = docRoot.GetFirstAncestorOfType<CollectionView>();
                docRoot?.Dispatcher?.RunAsync(CoreDispatcherPriority.High, new DispatchedHandler(
                        () => {
                            var group = pc?.GetDocumentGroup(docRoot.ViewModel.DocumentController) ?? docRoot?.ViewModel?.DocumentController;
                            if (docRoot.MoveToContainingCollection(overlappedViews, groupViews))
                                GroupManager.RemoveGroup(pc, group);
                        }));

                if (manipulationCompletedRoutedEventArgs != null)
                {
                    manipulationCompletedRoutedEventArgs.Handled = true;
                }
            }
        }
        
        List<DocumentView> GroupViews(List<DocumentViewModel> groups)
        {
            var collectionFreeFormChildren = (ParentDocument.GetFirstAncestorOfType<CollectionView>()?.CurrentView as CollectionFreeformView)?.xItemsControl?.ItemsPanelRoot?.Children;
            // TODO why is _grouping null at the end of this line.. null check to save demo but probably a real bug
            var groupings = collectionFreeFormChildren?.Select((c) => (c as ContentPresenter).GetFirstDescendantOfType<DocumentView>())?.Where((dv) => Grouping != null && Grouping.Contains(dv?.ViewModel));
            return groupings?.ToList() ?? new List<DocumentView>();
        }
        public void Dispose()
        {
            ParentDocument.ManipulationDelta -= ElementOnManipulationDelta;
            ParentDocument.PointerWheelChanged -= ElementOnPointerWheelChanged;
        }
        private bool ClampScale(double scaleFactor)
        {
            if (ElementScale > MaxScale)
            {
                ElementScale = MaxScale;
                return scaleFactor > 1;
            }

            if (ElementScale < MinScale)
            {
                ElementScale = MinScale;
                return scaleFactor < 1;
            }
            return false;
        }
    }
}
