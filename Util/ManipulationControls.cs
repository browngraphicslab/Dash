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
using Dash;
using Dash.Views;
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

        // for docking
        public double ManipulationStartX { get; set; }
        public double ManipulationStartY { get; set; }

        public delegate void OnManipulationCompletedHandler();
        public delegate void OnManipulationStartedHandler();
        public delegate void OnManipulatorTranslatedHandler(TransformGroupData transformationDelta);
        public event OnManipulatorTranslatedHandler OnManipulatorTranslatedOrScaled;
        public event OnManipulationCompletedHandler OnManipulatorCompleted;
        public event OnManipulationStartedHandler OnManipulatorStarted;


        private CollectionView _previouslyHighlightedCollectionView = null;

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
            lines[(int)AlignmentLine.XMid] = bounds.Left + bounds.Width / 2.0;
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

			var collectionFreeformView = ParentDocument.GetFirstAncestorOfType<CollectionView>()?.CurrentView as CollectionFreeformBase;
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
                            (!axisPos && parentAxesAfter[(int)parentDocumentAxis] <= documentAxes[(int)otherDocumentAxis] - thresh) ||
                            ((axisPos && parentAxesAfter[(int)parentDocumentAxis] >= documentAxes[(int)otherDocumentAxis] + thresh)))
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

			//Don't do any alignment if simply panning the collection or
			var collectionFreeformView =
				ParentDocument.GetFirstAncestorOfType<CollectionView>()?.CurrentView as CollectionFreeformBase;
            var collectionStandaradView = ParentDocument.GetFirstAncestorOfType<CollectionView>()?.CurrentView as CollectionStandardView;
            if (collectionFreeformView == null || ParentDocument.Equals(collectionFreeformView) || collectionStandaradView?.ViewModel.ViewLevel != CollectionViewModel.StandardViewLevel.Detail)
				return originalTranslate;
            var boundsBeforeTranslation = InteractiveBounds(ParentDocument.ViewModel);
            var parentDocumentLinesBefore = AlignmentLinesFromRect(boundsBeforeTranslation);

            var parentDocumentBounds = new Rect(boundsBeforeTranslation.X + originalTranslate.X,
                boundsBeforeTranslation.Y + originalTranslate.Y, boundsBeforeTranslation.Width,
                boundsBeforeTranslation.Height);
            var listOfSiblings = collectionFreeformView.ViewModel.DocumentViewModels.Where(vm =>
                vm != ParentDocument.ViewModel && !SelectionManager.GetSelectedDocumentsInCollection(collectionFreeformView).Select(dv => dv.ViewModel)
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
                    var cfw = ParentDocument.GetFirstAncestorOfType<CollectionView>()?.CurrentView as CollectionFreeformBase;
                    var scale = cfw.ViewModel.TransformGroup.ScaleAmount;
                    double alignmentX = (translateAfterSecondAlignment.X + offsetX - originalTranslate.X) * scale.X;
                    double alignmentY = (translateAfterSecondAlignment.Y + offsetY - originalTranslate.Y) * scale.Y;
                    //Move mouse by the alignment offset
                    var old = Window.Current.CoreWindow.PointerPosition;
                    Window.Current.CoreWindow.PointerPosition = new Point(old.X + alignmentX, old.Y + alignmentY);
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
            if (Math.Abs(parentLine - targetLine) < 0.001) //Was running into bugs cause
            {
                if (Math.Abs(parentLineAfter + acc - targetLine) > accumulatedDistanceThreshold) //Break away from alignment
                {
                    mouseOffset += acc;
                    acc = 0;
                    return false;
                }
                acc += delta;
                return true; //Snap and accumulate
            }

            //If we're under the snap distance threshold...
            else if (Math.Abs(parentLineAfter - targetLine) < distanceThreshold)
            {
                //If moving away from the target line, we shouldn't snap
                if ((parentLine < targetLine && delta < 0) || (parentLine > targetLine && delta > 0))
                {
                    //acc += delta;
                    return false;
                }
                //Snapping for the first time.
                acc = 0;
                return true;
            }
            return false;
        }
        private void ShowPreviewLine(Rect boundsBeforeAlignment, double[] otherDocumentAxes, AlignmentLine parentAxis, AlignmentLine otherAxis, Point alignmentTranslation)
        {
            double[] axesAfterAlignment = AlignmentLinesFromRect(new Rect(boundsBeforeAlignment.X + alignmentTranslation.X, boundsBeforeAlignment.Y + alignmentTranslation.Y, boundsBeforeAlignment.Width, boundsBeforeAlignment.Height));
            ShowPreviewLine(axesAfterAlignment, otherDocumentAxes, parentAxis, otherAxis, alignmentTranslation);
        }
        private void ShowPreviewLine(double[] parentDocumentAxes, double[] otherDocumentAxes, AlignmentLine parentAxis, AlignmentLine otherAxis, Point point)
        {
            Point p1, p2;
            Line line = null;
            //If X line
            if ((int)parentAxis < 3)
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
			var currentCollection = ParentDocument.GetFirstAncestorOfType<CollectionView>()?.CurrentView as CollectionFreeformBase;

			line.Visibility = Windows.UI.Xaml.Visibility.Visible;
			var screenPoint1 = Util.PointTransformFromVisual(p1, currentCollection?.GetItemsControl().ItemsPanelRoot);
			var screenPoint2 = Util.PointTransformFromVisual(p2, currentCollection?.GetItemsControl().ItemsPanelRoot);
			line.X1 = screenPoint1.X;
			line.Y1 = screenPoint1.Y;
			line.X2 = screenPoint2.X;
			line.Y2 = screenPoint2.Y;

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

			var currentCollection = ParentDocument.GetFirstAncestorOfType<CollectionView>()?.CurrentView as CollectionFreeformBase;

			var screenPoint1 = Util.PointTransformFromVisual(point1, currentCollection?.GetItemsControl().ItemsPanelRoot);
			var screenPoint2 = Util.PointTransformFromVisual(point2, currentCollection?.GetItemsControl().ItemsPanelRoot);

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

        private void Dock(bool preview)
        {
            if (!Window.Current.CoreWindow.GetKeyState(VirtualKey.Control).HasFlag(CoreVirtualKeyStates.Down))
            {
                MainPage.Instance.DockManager.UnhighlightDock();
                return;
            }

            DockDirection overlappedDirection = GetDockIntersection();

            if (overlappedDirection != DockDirection.None)
            {
                if (preview)
                {
                    MainPage.Instance.DockManager.HighlightDock(overlappedDirection);
                }
                else
                {
                    ParentDocument.ViewModel.XPos = ManipulationStartX;
                    ParentDocument.ViewModel.YPos = ManipulationStartY;
                    MainPage.Instance.DockManager.UnhighlightDock();
                    MainPage.Instance.DockManager.Dock(ParentDocument.ViewModel.DocumentController, overlappedDirection);
                }
            }
            else
            {
                MainPage.Instance.DockManager.UnhighlightDock();
            }
        }

        private DockDirection GetDockIntersection()
        {
            var actualX = ParentDocument.ViewModel.ActualSize.X * ParentDocument.ViewModel.Scale.X *
                          (MainPage.Instance.xMainDocView.ViewModel.DocumentController
                              .GetField<PointController>(KeyStore.PanZoomKey)?.Data.X ?? 1);
            var actualY = ParentDocument.ViewModel.ActualSize.Y * ParentDocument.ViewModel.Scale.Y *
                          (MainPage.Instance.xMainDocView.ViewModel.DocumentController
                               .GetField<PointController>(KeyStore.PanZoomKey)?.Data.Y ?? 1);

            var currentBoundingBox = new Rect(ParentDocument.TransformToVisual(MainPage.Instance.xMainDocView).TransformPoint(new Point(0, 0)),
                new Size(actualX, actualY));

            return MainPage.Instance.DockManager.GetDockIntersection(currentBoundingBox);

        }


        //END OF NEW SNAPPING



        #endregion

        void ElementOnPointerWheelChanged(object sender, PointerRoutedEventArgs e)
        {
            var viewlevel = ParentDocument.ViewModel.ViewLevel;
            // pointer wheel changed on document when collection is in standardview falls through to the collection and zooms the collection in/out to a different standard zoom level
            if (!viewlevel.Equals(CollectionViewModel.StandardViewLevel.None) && !viewlevel.Equals(CollectionViewModel.StandardViewLevel.Detail))
                return;
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

        // DO NOT ADD NEW CODE INTO THIS METHOD (see overloaded method with no parameters below). This one is ONLY for dealing with unique eventargs-related stuff.
        private void ElementOnManipulationStarted(object sender, ManipulationStartedRoutedEventArgs e)
        {
            if (e != null && ParentDocument.ManipulationMode == ManipulationModes.None)
            {
                e.Complete();
                return;
            }

            ElementOnManipulationStarted();

            if (e != null)
            {
                e.Handled = true;
            }
        }

        // If you want to add new code into the ElementOnManipulationStarted handler, use this one. It will always be called.
        public void ElementOnManipulationStarted()
        {
            ManipulationStartX = ParentDocument.ViewModel.XPos;
            ManipulationStartY = ParentDocument.ViewModel.YPos;

            OnManipulatorStarted?.Invoke();
        }

        /// <summary>
        /// Applies manipulation controls (zoom, translate) in the grid manipulation event. Note that this event does NOT always fire: TranslateAndScale should be the
        /// method to add code in so ALL documents will have access to the code.
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
        /// Applies manipulation controls (zoom, translate) in the grid manipulation event. This is the only "manipulationdelta"-esque method
        /// that ALL documents use.
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

            var nestedCollection = ParentDocument.GetCollectionToMoveTo(GetOverlappedViews());
            if ((nestedCollection == null && _previouslyHighlightedCollectionView != null) || nestedCollection != null && !nestedCollection.Equals(_previouslyHighlightedCollectionView))
            {
                _previouslyHighlightedCollectionView?.Unhighlight();
                nestedCollection?.Highlight();
                _previouslyHighlightedCollectionView = nestedCollection;
            }
            Dock(true);
        }

        // DO NOT ADD CODE INTO THIS METHOD: add into the overloaded ElementOnManipulationCompleted method below. Not all documents will
        // fire this method.
        private void ElementOnManipulationCompleted(object sender, ManipulationCompletedRoutedEventArgs e)
        {
            if (e == null || !e.Handled)
            {
                ElementOnManipulationCompleted();

                if (e != null)
                {
                    e.Handled = true;
                }
            }
        }

        // If you want to add code that runs after ANY document's manipulation is completed, use this method.
        public void ElementOnManipulationCompleted()
        {
            MainPage.Instance.HorizontalAlignmentLine.Visibility = Windows.UI.Xaml.Visibility.Collapsed;
            MainPage.Instance.VerticalAlignmentLine.Visibility = Windows.UI.Xaml.Visibility.Collapsed;
            _previouslyHighlightedCollectionView?.Unhighlight();

            var docRoot = ParentDocument;

            var pos = docRoot.RootPointerPos();
            var overlappedViews = VisualTreeHelper.FindElementsInHostCoordinates(pos, MainPage.Instance).OfType<DocumentView>().ToList();

            docRoot?.Dispatcher?.RunAsync(CoreDispatcherPriority.Normal, () =>
            {
                docRoot.MoveToContainingCollection(overlappedViews);
            });

            OnManipulatorCompleted?.Invoke();
            Dock(false);

            _accumulatedTranslateAfterSnappingX = _accumulatedTranslateAfterSnappingY = 0;


        }

        private List<DocumentView> GetOverlappedViews()
        {
            var pos = ParentDocument.RootPointerPos();
            return VisualTreeHelper.FindElementsInHostCoordinates(pos, MainPage.Instance).OfType<DocumentView>().ToList();
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
