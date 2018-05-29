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
using Windows.UI.Xaml.Shapes;
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


        /// <summary>
        /// At every ManipulationDelta, store the translate. 
        /// </summary>
        private Point _translateLastManipulationDelta;


        private double _accumulatedTranslateAfterSnappingX;
        private double _accumulatedTranslateAfterSnappingY;

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
            }.TransformBounds(new Rect(0, 0, dvm.ActualSize.X * dvm.InteractiveManipulationScale.X, dvm.ActualSize.Y * dvm.InteractiveManipulationScale.Y));
        }

        enum AlignmentLine
        {
            XMin, XMid, XMax, YMin, YMid, YMax
        }

        double[] AlignmentLinesFromRect(Rect bounds)
        {
            double[] lines = new double[6];
            lines[(int)AlignmentLine.XMin] = bounds.Left;
            lines[(int)AlignmentLine.XMid] = bounds.Left + bounds.Width/2.0;
            lines[(int)AlignmentLine.XMax] = bounds.Right;
            lines[(int)AlignmentLine.YMin] = bounds.Top;
            lines[(int)AlignmentLine.YMid] = bounds.Top + bounds.Height / 2.0;
            lines[(int)AlignmentLine.YMax] = bounds.Bottom;
            return lines;
        }


        //START OF NEW SNAPPING

        private static AlignmentLine[] GetAlignableLines(AlignmentLine line)
        {
            switch (line)
            {
                case AlignmentLine.XMin:
                case AlignmentLine.XMid:
                case AlignmentLine.XMax:
                    return new AlignmentLine[] { AlignmentLine.XMin, AlignmentLine.XMid, AlignmentLine.XMax };
                case AlignmentLine.YMin:
                case AlignmentLine.YMid:
                case AlignmentLine.YMax:
                    return new AlignmentLine[] { AlignmentLine.YMin, AlignmentLine.YMid, AlignmentLine.YMax };
                default:
                    throw new ArgumentOutOfRangeException(nameof(line), line, null);
            }
        }
        
        /// <summary>
        /// Old code to align when a DocumentView is being resized. This should be put back in soon.
        /// </summary>
        /// <param name="translate"></param>
        /// <param name="sizeChange"></param>
        /// <param name="shiftTop"></param>
        /// <param name="shiftLeft"></param>
        /// <returns></returns>
        public Rect ResizeAlign(Point translate, Point sizeChange, bool shiftTop, bool shiftLeft)
        {
            MainPage.Instance.HorizontalAlignmentLine.Visibility = Windows.UI.Xaml.Visibility.Collapsed;
            MainPage.Instance.VerticalAlignmentLine.Visibility = Windows.UI.Xaml.Visibility.Collapsed;

            var collectionFreeformView = ParentDocument.GetFirstAncestorOfType<CollectionView>()?.CurrentView as CollectionFreeformView;
            if (collectionFreeformView == null || ParentDocument.Equals(collectionFreeformView))
                return ParentDocument.ViewModel.Bounds;

            var parentDocumentBoundsBefore = ParentDocument.ViewModel.Bounds;
            var parentDocumentBoundsAfter = new Rect(parentDocumentBoundsBefore.X + translate.X, parentDocumentBoundsBefore.Y + translate.Y,
                                                                                        Math.Max(ParentDocument.MinWidth, parentDocumentBoundsBefore.Width + sizeChange.X), 
                                                                                        Math.Max(ParentDocument.MinHeight, parentDocumentBoundsBefore.Height + sizeChange.Y));
            var listOfSiblings = collectionFreeformView.ViewModel.DocumentViewModels; //.Where(vm => vm != ParentDocument.ViewModel);
            var parentAxesBefore = AlignmentLinesFromRect(ParentDocument.ViewModel.Bounds);
            var parentAxesAfter = AlignmentLinesFromRect(parentDocumentBoundsAfter);
            double thresh = 2; //TODO: Refactor this to be extensible (probably dependent on zoom level)

            //Find the document we will snap to when resized - optionally resize 
            foreach (var document in listOfSiblings)
            {
                if (document == ParentDocument.ViewModel) continue;
                var documentBounds = InteractiveBounds(document);
                var documentAxes = AlignmentLinesFromRect(documentBounds);
                //Check four sides of the document view (hopefully, we can resize from multiple places one day!) 
                AlignmentLine[] relevantAxes = ReleventAxes(shiftTop, shiftLeft);

                foreach (var parentDocumentAxis in relevantAxes)
                {
                    bool axisPos = parentAxesAfter[(int)parentDocumentAxis] >= parentAxesBefore[(int)parentDocumentAxis];
                    foreach (var otherDocumentAxis in GetAlignableLines(parentDocumentAxis))
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

        private AlignmentLine[] ReleventAxes(bool shiftTop, bool shiftLeft)
        {
            List<AlignmentLine> lines = new List<AlignmentLine>();
            if (shiftTop)
                lines.Add(AlignmentLine.YMin);
            else
                lines.Add(AlignmentLine.YMax);

            if (shiftLeft)
                lines.Add(AlignmentLine.XMin);
            else
                lines.Add(AlignmentLine.XMax);

            return lines.ToArray();
        }

        private Rect BoundsAfterResizeAligningAxis(AlignmentLine axisBeingAligned, double deltaBefore)
        {
            var parentDocumentBoundsBefore = ParentDocument.ViewModel.Bounds;
            switch (axisBeingAligned)
            {
                case AlignmentLine.XMin:
                    return new Rect(parentDocumentBoundsBefore.X + deltaBefore, parentDocumentBoundsBefore.Y, parentDocumentBoundsBefore.Width + deltaBefore, parentDocumentBoundsBefore.Height);
                case AlignmentLine.XMax:
                    return new Rect(parentDocumentBoundsBefore.X, parentDocumentBoundsBefore.Y, parentDocumentBoundsBefore.Width + deltaBefore, parentDocumentBoundsBefore.Height);
                case AlignmentLine.YMin:
                    return new Rect(parentDocumentBoundsBefore.X, parentDocumentBoundsBefore.Y + deltaBefore, parentDocumentBoundsBefore.Width, parentDocumentBoundsBefore.Height + deltaBefore);
                case AlignmentLine.YMax:
                    return new Rect(parentDocumentBoundsBefore.X, parentDocumentBoundsBefore.Y, parentDocumentBoundsBefore.Width, parentDocumentBoundsBefore.Height + deltaBefore);
                default:
                    return parentDocumentBoundsBefore;
            }

        }

        public Point SimpleAlign(Point originalTranslate)
        {
            MainPage.Instance.HorizontalAlignmentLine.Visibility = Windows.UI.Xaml.Visibility.Collapsed;
            MainPage.Instance.VerticalAlignmentLine.Visibility = Windows.UI.Xaml.Visibility.Collapsed;

            //Don't do any alignment if simply panning the collection
            var collectionFreeformView =
                ParentDocument.GetFirstAncestorOfType<CollectionView>()?.CurrentView as CollectionFreeformView;
            if (collectionFreeformView == null || ParentDocument.Equals(collectionFreeformView))
                return originalTranslate;

            var boundsBeforeTranslation = InteractiveBounds(ParentDocument.ViewModel);
            var parentDocumentLinesBefore = AlignmentLinesFromRect(boundsBeforeTranslation);

            var parentDocumentBounds = new Rect(boundsBeforeTranslation.X + originalTranslate.X,
                boundsBeforeTranslation.Y + originalTranslate.Y, boundsBeforeTranslation.Width,
                boundsBeforeTranslation.Height);
            var listOfSiblings = collectionFreeformView.ViewModel.DocumentViewModels.Where(vm =>
                vm != ParentDocument.ViewModel && !collectionFreeformView.SelectedDocs.Select((dv) => dv.ViewModel)
                    .ToList().Contains(vm));
            var parentDocumentLinesAfter = AlignmentLinesFromRect(parentDocumentBounds);

            double offsetX = 0;
            double offsetY = 0;

            double distanceThreshold = 1000; //How close the X and Y positions have to be for alignment to happen.
            foreach (var documentView in listOfSiblings)
            {
                var documentBounds = InteractiveBounds(documentView);
                var documentLines = AlignmentLinesFromRect(documentBounds);
                //To avoid the visual clutter of aligning to document views in a large workspace, we currently ignore any document views that are further than some threshold
                if (Math.Abs(documentBounds.X - parentDocumentBounds.X) > distanceThreshold ||
                    Math.Abs(documentBounds.Y - parentDocumentBounds.Y) > distanceThreshold)
                    continue;


                var translateAfterFirstAlignment = originalTranslate;
                bool alignedToX = false;
                //Check every x-aligned line (xmin, xmid, xmax), align to the first available one
                for (int parentLine = 0; parentLine < 3 && (!alignedToX); parentLine++)
                {
                    for (int targetLine = 0; targetLine < 3; targetLine++)
                    {
                        if (SnapTriggered(parentDocumentLinesBefore[parentLine], documentLines[targetLine], originalTranslate.X, ref _accumulatedTranslateAfterSnappingX, ref offsetX))
                        {
                            translateAfterFirstAlignment.X = documentLines[targetLine] - parentDocumentLinesBefore[parentLine];
                            ShowPreviewLine(boundsBeforeTranslation, documentLines, (AlignmentLine)parentLine, (AlignmentLine)targetLine, translateAfterFirstAlignment);
                            alignedToX = true;
                            break;
                        }
                    }
                }

                var translateAfterSecondAlignment = translateAfterFirstAlignment;
                bool alignedToY = false;
                //Check every y-aligned line, align to the first available one, while also maintaining the alignment to any x-aligned line found above
                for (int parentLine = 3; parentLine < 6 && (!alignedToY); parentLine++)
                {
                    for (int otherLine = 3; otherLine < 6; otherLine++)
                    {
                        if (SnapTriggered(parentDocumentLinesBefore[parentLine], documentLines[otherLine], originalTranslate.Y, ref _accumulatedTranslateAfterSnappingY, ref offsetY))
                        {
                            translateAfterSecondAlignment.Y = documentLines[otherLine] - parentDocumentLinesBefore[parentLine];
                            ShowPreviewLine(boundsBeforeTranslation, documentLines, (AlignmentLine)parentLine, (AlignmentLine)otherLine, translateAfterSecondAlignment);
                            alignedToY = true;
                            break;
                        }
                    }
                }

                if (alignedToX || alignedToY)
                {

                    return new Point(translateAfterSecondAlignment.X + offsetX, translateAfterSecondAlignment.Y + offsetY);
                }

            }
            //return originalTranslate ;
            return new Point(originalTranslate.X + offsetX, originalTranslate.Y + offsetY);


        }

        private bool SnapTriggered(double parentLine, double targetLine, double delta, ref double acc, ref double mouseOffset)
        {
            double parentLineAfter = parentLine + delta;
            double distanceThreshold = 1.0; //TODO: Refactor this to be dependent on zoom level
            double accumulatedDistanceThreshold = 4.0; // How much distance the node must be moved to get out of alignment TODO: Refactor this to be dependent on zoom level

            //If already snapped
            if (parentLine == targetLine)
            {
                if (Math.Abs(parentLineAfter + acc - targetLine) > accumulatedDistanceThreshold) //Break away from alignment
                {
                    Debug.WriteLine(mouseOffset);
                    mouseOffset += acc;
                    return false;
                }
                Debug.WriteLine("Accumulating... " + delta.ToString());
                acc += delta;
                return true; //Snap and accumulate
            }
            
            //If we're under the snap distance threshold...
            else if (Math.Abs(parentLineAfter - targetLine) < distanceThreshold)
            {
                Debug.WriteLine("Delta " + delta.ToString());
                Debug.WriteLine("Parent " + parentLine.ToString());
                Debug.WriteLine("Target " + targetLine.ToString());
                Debug.WriteLine("Parent After" + parentLineAfter.ToString());


                //If moving away from the target line, we shouldn't snap
                if ((parentLine < targetLine && delta < 0) || (parentLine > targetLine && delta > 0))
                {
                    //acc += delta;
                    Debug.WriteLine("Moving away... ");
                    return false;
                }
                Debug.WriteLine("Snapping 1st time ");
                acc = 0;

                //Snapping for the first time.
                return true;
            }
            return false;
        }
        private void ShowPreviewLine(Rect boundsBeforeAlignment, double[] otherDocumentAxes, AlignmentLine parentAxis,  AlignmentLine otherAxis, Point alignmentTranslation)
        {
            double[] axesAfterAlignment = AlignmentLinesFromRect(new Rect(boundsBeforeAlignment.X + alignmentTranslation.X, boundsBeforeAlignment.Y + alignmentTranslation.Y, boundsBeforeAlignment.Width, boundsBeforeAlignment.Height));
            ShowPreviewLine(axesAfterAlignment, otherDocumentAxes, parentAxis, otherAxis, alignmentTranslation);
        }
        private void ShowPreviewLine(double[] parentDocumentAxes, double[] otherDocumentAxes, AlignmentLine parentAxis, AlignmentLine otherAxis, Point point)
        {
            Point p1, p2;
            Line line = null;
            //If X line
            if((int) parentAxis < 3)
            {
                p1.X = otherDocumentAxes[(int)otherAxis];
                p2.X = otherDocumentAxes[(int)otherAxis];
                p1.Y = Math.Min(parentDocumentAxes[(int)AlignmentLine.YMin], otherDocumentAxes[(int)AlignmentLine.YMin]);
                p2.Y = Math.Max(parentDocumentAxes[(int)AlignmentLine.YMax], otherDocumentAxes[(int)AlignmentLine.YMax]);
                line = MainPage.Instance.VerticalAlignmentLine;

            }
            else
            {
                p1.Y = otherDocumentAxes[(int)otherAxis];
                p2.Y = otherDocumentAxes[(int)otherAxis];
                p1.X = Math.Min(parentDocumentAxes[(int)AlignmentLine.XMin], otherDocumentAxes[(int)AlignmentLine.XMin]);
                p2.X = Math.Max(parentDocumentAxes[(int)AlignmentLine.XMax], otherDocumentAxes[(int)AlignmentLine.XMax]);
                line = MainPage.Instance.HorizontalAlignmentLine;

            }
            var currentCollection = ParentDocument.GetFirstAncestorOfType<CollectionView>()?.CurrentView as CollectionFreeformView;

            line.Visibility = Windows.UI.Xaml.Visibility.Visible;
            var screenPoint1 = Util.PointTransformFromVisual(p1, currentCollection?.xItemsControl.ItemsPanelRoot);
            var screenPoint2 = Util.PointTransformFromVisual(p2, currentCollection?.xItemsControl.ItemsPanelRoot);
            line.X1 = screenPoint1.X;
            line.Y1 = screenPoint1.Y;
            line.X2 = screenPoint2.X;
            line.Y2 = screenPoint2.Y;

        }

        //END OF NEW SNAPPING



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
                //_translateLastManipulationDelta = delta;

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
                MainPage.Instance.HorizontalAlignmentLine.Visibility = Windows.UI.Xaml.Visibility.Collapsed;
                MainPage.Instance.VerticalAlignmentLine.Visibility = Windows.UI.Xaml.Visibility.Collapsed;

                var docRoot = ParentDocument;
                
                var pos = docRoot.RootPointerPos();
                var overlappedViews = VisualTreeHelper.FindElementsInHostCoordinates(pos, MainPage.Instance).OfType<DocumentView>().ToList();
                

                OnManipulatorCompleted?.Invoke();
                _accumulatedTranslateAfterSnappingX = _accumulatedTranslateAfterSnappingY = 0;
                //_translateLastManipulationDelta = new Point();
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
