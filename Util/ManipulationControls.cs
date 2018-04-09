using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Windows.Foundation;
using Windows.System;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Point = Windows.Foundation.Point;
using DashShared;

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


        private List<DocumentController> _documentsToRemoveAfterManipulation = new List<DocumentController>();


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

        enum AlignmentAxis
        {
            XMin, XMid, XMax, YMin, YMid, YMax
        }

        double[] AlignmentAxes(Rect bounds)
        {
            double[] axes = new double[6];
            axes[(int)AlignmentAxis.XMin] = bounds.Left;
            axes[(int)AlignmentAxis.XMid] = bounds.Left + bounds.Width/2.0;
            axes[(int)AlignmentAxis.XMax] = bounds.Right;
            axes[(int)AlignmentAxis.YMin] = bounds.Top;
            axes[(int)AlignmentAxis.YMid] = bounds.Top + bounds.Height / 2.0;
            axes[(int)AlignmentAxis.YMax] = bounds.Bottom;
            return axes;
        }


        //START OF NEW SNAPPING

        private static AlignmentAxis[] getAlignableAxis(AlignmentAxis axis)
        {
            switch (axis)
            {
                case AlignmentAxis.XMin:
                case AlignmentAxis.XMid:
                case AlignmentAxis.XMax:
                    return new AlignmentAxis[] { AlignmentAxis.XMin, AlignmentAxis.XMid, AlignmentAxis.XMax };
                case AlignmentAxis.YMin:
                case AlignmentAxis.YMid:
                case AlignmentAxis.YMax:
                    return new AlignmentAxis[] { AlignmentAxis.YMin, AlignmentAxis.YMid, AlignmentAxis.YMax };
                default:
                    throw new ArgumentOutOfRangeException(nameof(axis), axis, null);
            }
        }
        
        public Rect ResizeAlign(Point translate, Point sizeChange, bool shiftTop, bool shiftLeft)
        {
            MainPage.Instance.AlignmentLine.Visibility = Windows.UI.Xaml.Visibility.Collapsed;

            var collectionFreeformView = ParentDocument.GetFirstAncestorOfType<CollectionView>()?.CurrentView as CollectionFreeformView;
            if (collectionFreeformView == null || ParentDocument.Equals(collectionFreeformView))
                return ParentDocument.ViewModel.Bounds;

            var parentDocumentBoundsBefore = ParentDocument.ViewModel.Bounds;
            var parentDocumentBoundsAfter = new Rect(parentDocumentBoundsBefore.X + translate.X, parentDocumentBoundsBefore.Y + translate.Y,
                                                                                        Math.Max(ParentDocument.MinWidth, parentDocumentBoundsBefore.Width + sizeChange.X), 
                                                                                        Math.Max(ParentDocument.MinHeight, parentDocumentBoundsBefore.Height + sizeChange.Y));
            var listOfSiblings = collectionFreeformView.ViewModel.DocumentViewModels; //.Where(vm => vm != ParentDocument.ViewModel);
            var parentAxesBefore = AlignmentAxes(ParentDocument.ViewModel.Bounds);
            var parentAxesAfter = AlignmentAxes(parentDocumentBoundsAfter);
            double thresh = 2; //TODO: Refactor this to be extensible (probably dependent on zoom level)

            //Find the document we will snap to when resized - optionally resize 
            foreach (var document in listOfSiblings)
            {
                if (document == ParentDocument.ViewModel) continue;
                var documentBounds = InteractiveBounds(document);
                var documentAxes = AlignmentAxes(documentBounds);
                //Check four sides of the document view (hopefully, we can resize from multiple places one day!) 
                AlignmentAxis[] relevantAxes = ReleventAxes(shiftTop, shiftLeft);

                foreach (var parentDocumentAxis in relevantAxes)
                {
                    bool axisPos = parentAxesAfter[(int)parentDocumentAxis] >= parentAxesBefore[(int)parentDocumentAxis];
                    foreach (var otherDocumentAxis in getAlignableAxis(parentDocumentAxis))
                    {
                        var deltaBefore = documentAxes[(int)otherDocumentAxis] - parentAxesBefore[(int)parentDocumentAxis];
                        var distance = Math.Abs(deltaBefore);

                        if ((distance > 15) ||
                            (!axisPos && parentAxesAfter[(int) parentDocumentAxis] <= documentAxes[(int) otherDocumentAxis] - thresh) || 
                            ((axisPos && parentAxesAfter[(int) parentDocumentAxis] >= documentAxes[(int) otherDocumentAxis] + thresh)))
                            continue;


                        ShowPreviewLine(parentDocumentBoundsBefore, documentAxes, parentDocumentAxis, otherDocumentAxis, new Point(deltaBefore, translate.Y));
                        return BoundsAfterResizeAligningAxis(parentDocumentAxis, deltaBefore);
                        

                    }
                }
            }
            return parentDocumentBoundsAfter;
        }

        private AlignmentAxis[] ReleventAxes(bool shiftTop, bool shiftLeft)
        {
            List<AlignmentAxis> axes = new List<AlignmentAxis>();
            if (shiftTop)
                axes.Add(AlignmentAxis.YMin);
            else
                axes.Add(AlignmentAxis.YMax);

            if (shiftLeft)
                axes.Add(AlignmentAxis.XMin);
            else
                axes.Add(AlignmentAxis.XMax);

            return axes.ToArray();
        }

        private Rect BoundsAfterResizeAligningAxis(AlignmentAxis axisBeingAligned, double deltaBefore)
        {
            var parentDocumentBoundsBefore = ParentDocument.ViewModel.Bounds;
            switch (axisBeingAligned)
            {
                case AlignmentAxis.XMin:
                    return new Rect(parentDocumentBoundsBefore.X + deltaBefore, parentDocumentBoundsBefore.Y, parentDocumentBoundsBefore.Width + deltaBefore, parentDocumentBoundsBefore.Height);
                case AlignmentAxis.XMax:
                    return new Rect(parentDocumentBoundsBefore.X, parentDocumentBoundsBefore.Y, parentDocumentBoundsBefore.Width + deltaBefore, parentDocumentBoundsBefore.Height);
                case AlignmentAxis.YMin:
                    return new Rect(parentDocumentBoundsBefore.X, parentDocumentBoundsBefore.Y + deltaBefore, parentDocumentBoundsBefore.Width, parentDocumentBoundsBefore.Height + deltaBefore);
                case AlignmentAxis.YMax:
                    return new Rect(parentDocumentBoundsBefore.X, parentDocumentBoundsBefore.Y, parentDocumentBoundsBefore.Width, parentDocumentBoundsBefore.Height + deltaBefore);
                default:
                    return parentDocumentBoundsBefore;
            }

        }
        public Point SimpleAlign(Point translate)
        {
            MainPage.Instance.AlignmentLine.Visibility = Windows.UI.Xaml.Visibility.Collapsed;

            //Don't do any alignment if simply panning the collection
            var collectionFreeformView = ParentDocument.GetFirstAncestorOfType<CollectionView>()?.CurrentView as CollectionFreeformView;
            if (collectionFreeformView == null || ParentDocument.Equals(collectionFreeformView))
                return translate;

            var boundsBeforeTranslation = InteractiveBounds(ParentDocument.ViewModel);
            var parentDocumentAxesBefore = AlignmentAxes(boundsBeforeTranslation);

            var parentDocumentBounds = new Rect(boundsBeforeTranslation.X + translate.X, boundsBeforeTranslation.Y + translate.Y, boundsBeforeTranslation.Width, boundsBeforeTranslation.Height);
            var listOfSiblings = collectionFreeformView.ViewModel.DocumentViewModels.Where(vm => vm != ParentDocument.ViewModel);
            var parentDocumentAxesAfter = AlignmentAxes(parentDocumentBounds);

            double distanceThreshold = 1000;
            double thresh = 2; //TODO: Refactor this to be extensible (probably dependent on zoom level)
            foreach(var documentView in listOfSiblings)
            {
                var documentBounds = InteractiveBounds(documentView);
                var documentAxes = AlignmentAxes(documentBounds);
                //To avoid the visual clutter of aligning to document views in a large workspace, we currently ignore any document views that are further than some threshold

                if (Math.Abs(documentBounds.X - parentDocumentBounds.X) > distanceThreshold || Math.Abs(documentBounds.Y - parentDocumentBounds.Y) > distanceThreshold)
                    continue;

                //For every axis in the ParentDocument
                for(int parentAxis = 0; parentAxis < 6; parentAxis++)
                {
                    for(int otherAxis = 3 * (parentAxis/3); otherAxis < 3* (parentAxis/3) + 3; otherAxis++)
                    {

                        var delta = documentAxes[otherAxis] - parentDocumentAxesBefore[parentAxis];
                        var distance = Math.Abs(delta);
                        
                        //If X axis
                        if(parentAxis < 3 && distance < 15)
                        {
                            if((translate.X <= 0 && parentDocumentAxesAfter[parentAxis] <= documentAxes[otherAxis] - thresh) || ((translate.X >= 0 && parentDocumentAxesAfter[parentAxis] >= documentAxes[otherAxis] + thresh)))
                                continue;

                            ShowPreviewLine(boundsBeforeTranslation, documentAxes, (AlignmentAxis)parentAxis, (AlignmentAxis)otherAxis, new Point(delta, translate.Y));
                            return new Point(delta, translate.Y);
                        }
                        if(parentAxis >=3 && distance < 15)
                        {

                            if ((translate.Y <= 0 && parentDocumentAxesAfter[parentAxis] <= documentAxes[otherAxis] - thresh) || ((translate.Y >= 0 && parentDocumentAxesAfter[parentAxis] >= documentAxes[otherAxis] + thresh)))
                                continue;

                            ShowPreviewLine(boundsBeforeTranslation, documentAxes, (AlignmentAxis)parentAxis, (AlignmentAxis)otherAxis, new Point(translate.X, delta));
                            return new Point(translate.X, delta);

                        }

                    }
                    
                }
            }

            return translate ;
            
        }

        private void ShowPreviewLine(Rect boundsBeforeAlignment, double[] otherDocumentAxes, AlignmentAxis parentAxis,  AlignmentAxis otherAxis, Point alignmentTranslation)
        {
            double[] axesAfterAlignment = AlignmentAxes(new Rect(boundsBeforeAlignment.X + alignmentTranslation.X, boundsBeforeAlignment.Y + alignmentTranslation.Y, boundsBeforeAlignment.Width, boundsBeforeAlignment.Height));
            ShowPreviewLine(axesAfterAlignment, otherDocumentAxes, parentAxis, otherAxis, alignmentTranslation);
        }
        private void ShowPreviewLine(double[] parentDocumentAxes, double[] otherDocumentAxes, AlignmentAxis parentAxis, AlignmentAxis otherAxis, Point point)
        {
            MainPage.Instance.AlignmentLine.Visibility = Windows.UI.Xaml.Visibility.Visible;

            Point p1, p2;
            //If X axis
            if((int) parentAxis < 3)
            {
                p1.X = otherDocumentAxes[(int)otherAxis];
                p2.X = otherDocumentAxes[(int)otherAxis];
                p1.Y = Math.Min(parentDocumentAxes[(int)AlignmentAxis.YMin], otherDocumentAxes[(int)AlignmentAxis.YMin]);
                p2.Y = Math.Max(parentDocumentAxes[(int)AlignmentAxis.YMax], otherDocumentAxes[(int)AlignmentAxis.YMax]);

            }
            else
            {
                p1.Y = otherDocumentAxes[(int)otherAxis];
                p2.Y = otherDocumentAxes[(int)otherAxis];
                p1.X = Math.Min(parentDocumentAxes[(int)AlignmentAxis.XMin], otherDocumentAxes[(int)AlignmentAxis.XMin]);
                p2.X = Math.Max(parentDocumentAxes[(int)AlignmentAxis.XMax], otherDocumentAxes[(int)AlignmentAxis.XMax]);

            }
            var currentCollection = ParentDocument.GetFirstAncestorOfType<CollectionView>()?.CurrentView as CollectionFreeformView;


            var screenPoint1 = Util.PointTransformFromVisual(p1, currentCollection?.xItemsControl.ItemsPanelRoot);
            var screenPoint2 = Util.PointTransformFromVisual(p2, currentCollection?.xItemsControl.ItemsPanelRoot);
            MainPage.Instance.AlignmentLine.X1 = screenPoint1.X;
            MainPage.Instance.AlignmentLine.Y1 = screenPoint1.Y;
            MainPage.Instance.AlignmentLine.X2 = screenPoint2.X;
            MainPage.Instance.AlignmentLine.Y2 = screenPoint2.Y;

        }


        private (Point p1, Point p2) PreviewLine(Rect snappingTo, Side snappingToSide)
        {
            Rect parentDocumentBounds = ParentDocument.ViewModel.Bounds;

            Point point1 = new Point();
            Point point2 = new Point();

            switch (snappingToSide)
            {
                case Side.Top:
                    point1.Y = point2.Y = snappingTo.Top;
                    point1.X = Math.Min(parentDocumentBounds.Left, snappingTo.Left);
                    point2.X = Math.Max(parentDocumentBounds.Right, snappingTo.Right);
                    break;
                case Side.Bottom:
                    point1.Y = point2.Y = snappingTo.Bottom;
                    point1.X = Math.Min(parentDocumentBounds.Left, snappingTo.Left);
                    point2.X = Math.Max(parentDocumentBounds.Right, snappingTo.Right);
                    break;
                case Side.Left:
                    point1.X = point2.X = snappingTo.Left;
                    point1.Y = Math.Min(parentDocumentBounds.Top, snappingTo.Top);
                    point2.Y = Math.Max(parentDocumentBounds.Bottom, snappingTo.Bottom);
                    break;
                case Side.Right:
                    point1.X = point2.X = snappingTo.Right;
                    point1.Y = Math.Min(parentDocumentBounds.Top, snappingTo.Top);
                    point2.Y = Math.Max(parentDocumentBounds.Bottom, snappingTo.Bottom);
                    break;
            }

            var currentCollection = ParentDocument.GetFirstAncestorOfType<CollectionView>()?.CurrentView as CollectionFreeformView;

            var screenPoint1 = Util.PointTransformFromVisual(point1, currentCollection?.xItemsControl.ItemsPanelRoot);
            var screenPoint2 = Util.PointTransformFromVisual(point2, currentCollection?.xItemsControl.ItemsPanelRoot);

            return (screenPoint1, screenPoint2);

        }

        private Point SimpleSnapPoint(Rect snappingTo, Side snappingToSide)
        {
            Rect parentDocumentBounds = ParentDocument.ViewModel.Bounds;

            Point newTopLeftPoint;
            switch (snappingToSide)
            {
                case Side.Top:
                    newTopLeftPoint = new Point(parentDocumentBounds.X, snappingTo.Top - parentDocumentBounds.Height);
                    break;
                case Side.Bottom:
                    newTopLeftPoint = new Point(parentDocumentBounds.X, snappingTo.Bottom);
                    break;
                case Side.Left:
                    newTopLeftPoint = new Point(snappingTo.Left - parentDocumentBounds.Width, parentDocumentBounds.Y);
                    break;
                case Side.Right:
                    newTopLeftPoint = new Point(snappingTo.Right, parentDocumentBounds.Y);
                    break;
            }

            return newTopLeftPoint;
        }


        private void SnapToCollection(Tuple<DocumentView, Side, double> closest)
        {
            var collection = closest.Item1;
            var side = closest.Item2;

            var newCollectionBoundingBoxNullable = BoundingBox(ParentDocument.ViewModel, collection.ViewModel);
            if (!newCollectionBoundingBoxNullable.HasValue)
                return;
            var newCollectionBoundingBox = newCollectionBoundingBoxNullable.Value;

            //Translate and resize the collection using bounding box
            collection.ViewModel.Position = new Point(newCollectionBoundingBox.X, newCollectionBoundingBox.Y);
            collection.ViewModel.Width = newCollectionBoundingBox.Width;
            collection.ViewModel.Height = newCollectionBoundingBox.Height;

            //Add ParentDocument to collection
            if (collection.ViewModel.DocumentController.DocumentType.Equals(DashConstants.TypeStore.CollectionBoxType))
            {
                collection.GetFirstDescendantOfType<CollectionView>().ViewModel.AddDocument(ParentDocument.ViewModel.DocumentController, null);
            }

            _documentsToRemoveAfterManipulation = new List<DocumentController>()
            {
                ParentDocument.ViewModel.DocumentController
            };


            //Readjust the translates so that they are relative to the bounding box
            ParentDocument.ViewModel.Position = new Point(ParentDocument.ViewModel.Bounds.X - newCollectionBoundingBox.X, ParentDocument.ViewModel.Bounds.Y - newCollectionBoundingBox.Y);

        }
        private Tuple<DocumentViewModel, Side, double> GetClosestDocumentView(Rect bounds)
        {
            //Get a list of all DocumentViews hittested using the ParentDocument's bounds + some threshold
            var allDocumentViewsHit = HitTestFromSides(bounds);

            //Return closest DocumentView (using the double that represents the confidence) or null
            return allDocumentViewsHit.FirstOrDefault(item => item.Item3 == allDocumentViewsHit.Max(i2 => i2.Item3)); //Sadly no better argmax one-liner 
        }

        private List<Tuple<DocumentViewModel, Side, double>> HitTestFromSides(Rect currentBoundingBox)
        {
            var documentViewsAboveThreshold = new List<Tuple<DocumentViewModel, Side, double>>();
            var containingCollection = ParentDocument.GetFirstAncestorOfType<CollectionView>()?.CurrentView as CollectionFreeformView;
            Debug.Assert(containingCollection != null);

            var listOfSiblings = containingCollection.ViewModel.DocumentViewModels.Where(vm => vm != ParentDocument.ViewModel);
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

        //END OF NEW SNAPPING





        private Rect? BoundingBox(DocumentViewModel doc1, DocumentViewModel doc2, double padding = 0)
        {
            if (doc1 == null || doc2 == null)
            {
                return null;
            }
            var minX = Math.Min(doc1.Bounds.X, doc2.Bounds.X);
            var minY = Math.Min(doc1.Bounds.Y, doc2.Bounds.Y);

            var maxX = Math.Max(doc1.Bounds.Right, doc2.Bounds.Right);
            var maxY = Math.Max(doc1.Bounds.Bottom, doc2.Bounds.Bottom);
            return new Rect(new Point(minX - padding, minY - padding), new Point(maxX + padding, maxY + padding));
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

            var currentCollection = ParentDocument.GetFirstAncestorOfType<CollectionView>()?.CurrentView as CollectionFreeformView;

            var documentView = closestDocumentView.Item1;
            var side = closestDocumentView.Item2;

            var closestBoundsInCollectionSpace = documentView.ViewModel.Bounds;
            var boundingBoxCollectionSpace = CalculateAligningRectangleForSide(~side, closestBoundsInCollectionSpace, currentBoundingBox.Width, currentBoundingBox.Height);

            //Transform the rect from xCollectionCanvas (which is equivalent to xItemsControl.ItemsPanelRoot) space to screen space
            var boundingBoxScreenSpace = Util.RectTransformFromVisual(boundingBoxCollectionSpace, currentCollection?.xItemsControl.ItemsPanelRoot);

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
                var deltaAfterAlignment = SimpleAlign(delta);

                TranslateAndScale(e.Position, deltaAfterAlignment, e.Delta.Scale);
                //DetectShake(sender, e);

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
                MainPage.Instance.AlignmentLine.Visibility = Windows.UI.Xaml.Visibility.Collapsed;

                var docRoot = ParentDocument;
                
                var pos = docRoot.RootPointerPos();
                var overlappedViews = VisualTreeHelper.FindElementsInHostCoordinates(pos, MainPage.Instance).OfType<DocumentView>().ToList();
                
                docRoot?.Dispatcher?.RunAsync(CoreDispatcherPriority.High, new DispatchedHandler(
                    () =>
                    {
                        if (_documentsToRemoveAfterManipulation.Any())
                        {
                            var currentCollection = ParentDocument.GetFirstAncestorOfType<CollectionView>()?.CurrentView as CollectionFreeformView;
                            currentCollection?.ViewModel.RemoveDocuments(_documentsToRemoveAfterManipulation);
                            _documentsToRemoveAfterManipulation.Clear();
                        }

                        docRoot.MoveToContainingCollection(overlappedViews);
                    }));

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
