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

        public delegate void OnManipulationCompletedHandler();
        public delegate void OnManipulationStartedHandler();
        public delegate void OnManipulatorTranslatedHandler(TransformGroupData transformationDelta);
        public event OnManipulatorTranslatedHandler OnManipulatorTranslatedOrScaled;
        public event OnManipulationCompletedHandler OnManipulatorCompleted;
        public event OnManipulationStartedHandler OnManipulatorStarted;

        /// <summary>
        /// Created a manipulation control to move element
        /// NOTE: bounds checking is done relative to element.Parent so the element must be in an element with the proper size for bounds checking
        /// </summary>
        /// <param name="element">The element to add manipulation to</param>
        /// <param name="doesRespondToManipulationDelta"></param>
        /// <param name="doesRespondToPointerWheel"></param>
        /// <param name="borderRegions"></param>
        public ManipulationControls(DocumentView element)
        {
            ParentDocument = element;

            element.ManipulationDelta += ElementOnManipulationDelta;
            element.PointerWheelChanged += ElementOnPointerWheelChanged;
            element.ManipulationMode = ManipulationModes.All;
            element.ManipulationStarted += ElementOnManipulationStarted;
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
        /// copmutes the Bounds of the ViewModel during interaction.  This uses the cached position and scale values 
        /// that bypass the ViewModel's persistent data. When not manipulating the document, use DocumentViewModel's Bounds property.
        /// </summary>
        /// <param name="dvm"></param>
        /// <returns></returns>
        public static Rect InteractiveBounds(DocumentViewModel dvm)
        {
            return new TranslateTransform
            {
                X = dvm.InteractiveManipulationPosition.X,
                Y = dvm.InteractiveManipulationPosition.Y
            }.TransformBounds(new Rect(0, 0, dvm.ActualWidth * dvm.InteractiveManipulationScale.X, dvm.ActualHeight * dvm.InteractiveManipulationScale.Y));
        }

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


            var currentBoundingBox = InteractiveBounds(docRoot.ViewModel);

            var closest = GetClosestDocumentView(currentBoundingBox);
            if (preview)
                PreviewSnap(currentBoundingBox, closest);
            else
                SnapToDocumentView(docRoot.ViewModel, closest);
        }

        /// <summary>
        /// Snaps location of this DocumentView to the DocumentView passed in, also inheriting its width or height dimensions.
        /// </summary>
        /// <param name="closestDocumentView"></param>
        private void SnapToDocumentView(DocumentViewModel currrentDocModel, Tuple<DocumentViewModel, Side, double> closestDocumentView)
        {
            if (closestDocumentView == null)
            {
                return;
            }

            var documentViewModel = closestDocumentView.Item1;
            var side = closestDocumentView.Item2;

            var topLeftPoint = documentViewModel.Position;
            var bottomRightPoint = new Point(documentViewModel.XPos + documentViewModel.ActualWidth * documentViewModel.Scale.X,
                documentViewModel.YPos + documentViewModel.ActualHeight * documentViewModel.Scale.Y);

            var newBoundingBox = CalculateAligningRectangleForSide(~side, topLeftPoint, bottomRightPoint, currrentDocModel.ActualWidth * currrentDocModel.Scale.X, currrentDocModel.ActualHeight * currrentDocModel.Scale.Y);

            var translate = new Point(newBoundingBox.X, newBoundingBox.Y);

            currrentDocModel.Position = translate;
            currrentDocModel.Width = newBoundingBox.Width / currrentDocModel.Scale.X;
            currrentDocModel.Height = newBoundingBox.Height / currrentDocModel.Scale.Y;
        }


        /// <summary>
        /// Places the TemporaryRectangle in the location where the document view being manipulation would be dragged.
        /// </summary>
        /// <param name="currentBoundingBox"></param>
        /// <param name="closestDocumentView"></param>
        private void PreviewSnap(Rect currentBoundingBox, Tuple<DocumentViewModel, Side, double> closestDocumentView)
        {
            if (closestDocumentView == null) return;

            var docRoot = ParentDocument;

            var parent = ParentDocument.GetFirstAncestorOfType<CollectionView>()?.CurrentView as CollectionFreeformView;

            var documentViewModel = closestDocumentView.Item1;
            var side = closestDocumentView.Item2;

            var closestBoundsInCollectionSpace = InteractiveBounds(documentViewModel);
            var boundingBoxCollectionSpace = CalculateAligningRectangleForSide(~side, closestBoundsInCollectionSpace, currentBoundingBox.Width, currentBoundingBox.Height);

            //Transform the rect from xCollectionCanvas (which is equivalent to xItemsControl.ItemsPanelRoot) space to screen space
            var boundingBoxScreenSpace = Util.RectTransformFromVisual(boundingBoxCollectionSpace, parent?.xItemsControl.ItemsPanelRoot);
            MainPage.Instance.TemporaryRectangle.Width = boundingBoxScreenSpace.Width;
            MainPage.Instance.TemporaryRectangle.Height = boundingBoxScreenSpace.Height;

            Canvas.SetLeft(MainPage.Instance.TemporaryRectangle, boundingBoxScreenSpace.X);
            Canvas.SetTop(MainPage.Instance.TemporaryRectangle, boundingBoxScreenSpace.Y);
        }


        private Tuple<DocumentViewModel, Side, double> GetClosestDocumentView(Rect currentBoundingBox)
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
        private List<Tuple<DocumentViewModel, Side, double>> HitTestFromSides(Rect currentBoundingBox)
        {

            var documentViewsAboveThreshold = new List<Tuple<DocumentViewModel, Side, double>>();
            var parent = ParentDocument.GetFirstAncestorOfType<CollectionView>()?.CurrentView as CollectionFreeformView;
            Debug.Assert(parent != null);

            var docRoot = ParentDocument;

            var listOfSiblings = parent.ViewModel.DocumentViewModels;
            Side[] sides = { Side.Top, Side.Bottom, Side.Left, Side.Right };

            foreach (var side in sides)
            {
                var sideRect = CalculateAligningRectangleForSide(side, currentBoundingBox, ALIGNING_RECTANGLE_SENSITIVITY, ALIGNING_RECTANGLE_SENSITIVITY);
                foreach (var sibling in listOfSiblings)
                {
                    Rect intersection = sideRect;
                    intersection.Intersect(InteractiveBounds(sibling)); //Mutates intersection

                    var confidence = CalculateSnappingConfidence(side, sideRect, sibling);
                    if (!intersection.IsEmpty && confidence >= ALIGNMENT_THRESHOLD)
                    {
                        documentViewsAboveThreshold.Add(new Tuple<DocumentViewModel, Side, double>(sibling, side, confidence));
                    }
                }
            }

            return documentViewsAboveThreshold;
        }

        private double CalculateSnappingConfidence(Side side, Rect hitTestRect, DocumentViewModel otherDocumentView)
        {
            Rect otherDocumentViewBoundingBox = InteractiveBounds(otherDocumentView); // otherDocumentView.GetBoundingBoxScreenSpace();

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
                {
                    OnManipulatorStarted.Invoke();  // have to update the cached values before calling translate or scale (which uses the caches)
                    OnManipulatorTranslatedOrScaled?.Invoke(new TransformGroupData(new Point(), new Point(scaleAmount, scaleAmount), point.Position));
                    OnManipulatorCompleted.Invoke(); // then have to flush the caches to the viewmodel since we have to assume this is the end of the interaction.
                }
            }
        }

        public void ElementOnManipulationStarted(object sender, ManipulationStartedRoutedEventArgs e)
        {
            if (e != null && ParentDocument.ManipulationMode == ManipulationModes.None)
            {
                e.Complete();
                return;
            }

            OnManipulatorStarted?.Invoke();

            if (e != null)
            {
                e.Handled = true;
            }
        }
        /// <summary>
        /// Applies manipulation controls (zoom, translate) in the grid manipulation event.
        /// </summary>
        void ElementOnManipulationDelta(object sender, ManipulationDeltaRoutedEventArgs e)
        {
            if (e != null && ParentDocument.ManipulationMode == ManipulationModes.None)
            {
                e.Complete();
                return;
            }
            if (ParentDocument.IsRightBtnPressed() || ParentDocument.IsLeftBtnPressed())
            {
                var pointerPosition = MainPage.Instance.TransformToVisual(ParentDocument.GetFirstAncestorOfType<ContentPresenter>()).TransformPoint(new Point());
                var pointerPosition2 = MainPage.Instance.TransformToVisual(ParentDocument.GetFirstAncestorOfType<ContentPresenter>()).TransformPoint(e.Delta.Translation);
                var delta = new Point(pointerPosition2.X - pointerPosition.X, pointerPosition2.Y - pointerPosition.Y);

                TranslateAndScale(e.Position, delta, e.Delta.Scale);
                //DetectShake(sender, e);

                Snap(true);
                e.Handled = true;
            }
        }
        /// <summary>
        /// Applies manipulation controls (zoom, translate) in the grid manipulation event.
        /// </summary>
        /// <param name="e">passed in frm routed event args</param>
        /// <param name="grouped"></param>
        public void TranslateAndScale(Point position, Point translate, double scaleFactor)
        {
            ElementScale *= scaleFactor;

            //Clamp the scale factor 
            if (!ClampScale(scaleFactor))
            {
                OnManipulatorTranslatedOrScaled?.Invoke(new TransformGroupData(translate, new Point(scaleFactor, scaleFactor), position));
            }
        }

        public void ElementOnManipulationCompleted(object sender, ManipulationCompletedRoutedEventArgs e)
        {
            if (e == null || !e.Handled)
            {
                Snap(false); //Snap if you're dragging the element body and it's not a part of the group

                MainPage.Instance.TemporaryRectangle.Width = MainPage.Instance.TemporaryRectangle.Height = 0;

                var docRoot = ParentDocument;
                
                var pos = docRoot.RootPointerPos();
                var overlappedViews = VisualTreeHelper.FindElementsInHostCoordinates(pos, MainPage.Instance).OfType<DocumentView>().ToList();
                
                docRoot?.Dispatcher?.RunAsync(CoreDispatcherPriority.High, new DispatchedHandler(
                        () => docRoot.MoveToContainingCollection(overlappedViews)));

                OnManipulatorCompleted?.Invoke();

                if (e != null)
                {
                    e.Handled = true;
                }
            }
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
