using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Collections.ObjectModel;
using System.Linq;
using Windows.Foundation;
using Windows.UI.Input.Inking;
using Windows.UI.Input.Inking.Analysis;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Shapes;
using DashShared;

namespace Dash
{
    public sealed class InkRecognitionHelper
    {
        private List<IInkAnalysisNode> _shapeRegions;
        public FreeformInkControl FreeformInkControl { get; set; }
        public Dictionary<Rect, Tuple<string, IEnumerable<uint>>> TextBoundsDictionary { get; set; }
        public InkAnalyzer Analyzer { get; set; }
        public bool DoubleTapped { get; set; }
        public List<Point> DoubleTappedPoints { get; set; }
        public List<InkStroke> NewStrokes { get; set; }
        public List<InkStroke> StrokesToRemove { get; set; }

        public InkRecognitionHelper(FreeformInkControl freeformInkControl)
        {
            Analyzer = new InkAnalyzer();
            DoubleTappedPoints = new List<Point>();
            NewStrokes = new List<InkStroke>();
            StrokesToRemove = new List<InkStroke>();
            FreeformInkControl = freeformInkControl;
        }

        /// <summary>
        ///     Tries to find documents or collections at the double tap point, or delete using the newest stroke.
        /// </summary>
        /// <param name="recognitionFromSelectedStrokes"></param>
        public async void RecognizeInk(bool recognitionFromSelectedStrokes = false)
        {
            if (NewStrokes.Count > 0 && !recognitionFromSelectedStrokes)
            {
                StrokesToRemove.AddRange(NewStrokes);
                foreach (var newStroke in NewStrokes)
                    //Done separately because it doesn't require ink analyzer
                    TryDeleteWithStroke(newStroke);
            }
            var result = await Analyzer.AnalyzeAsync();
            if (result.Status == InkAnalysisStatus.Updated)
            {
                TextBoundsDictionary = GetTextBoundsDictionary();
                _shapeRegions = new List<IInkAnalysisNode>(Analyzer.AnalysisRoot.FindNodes(InkAnalysisNodeKind.InkDrawing).OrderBy(stroke => stroke.Id).Reverse());
                foreach (var inkAnalysisNode in new List<IInkAnalysisNode>(_shapeRegions))
                {
                    var region = (InkAnalysisInkDrawing)inkAnalysisNode;
                    //Only recognize shapes if the region was just drawn and contains a new stroke
                    if (RegionContainsNewStroke(region))
                    {
                        bool recognized = false;
                        //ellipses ==> collections
                        if (region.DrawingKind == InkAnalysisDrawingKind.Circle ||
                            region.DrawingKind == InkAnalysisDrawingKind.Ellipse)
                        {
                            AddCollectionFromShapeRegion(region);
                            recognized = true;
                        }
                        if (!recognitionFromSelectedStrokes && recognized) RemoveStrokeReferences(region.GetStrokeIds().ToImmutableHashSet());
                    }
                }
                foreach (var inkAnalysisNode in _shapeRegions)
                {
                    var region = (InkAnalysisInkDrawing) inkAnalysisNode;
                    //Only recognize shapes if the region was just drawn and contains a new stroke
                    if (RegionContainsNewStroke(region))
                    {
                        bool recognized = false;
                        //rectangles ==> documents
                        if (region.DrawingKind == InkAnalysisDrawingKind.Rectangle ||
                            region.DrawingKind == InkAnalysisDrawingKind.Square)
                        {
                            AddDocumentFromShapeRegion(region);
                            recognized = true;
                        }
                        //triangles ==> operator menu
                        if (region.DrawingKind == InkAnalysisDrawingKind.Triangle &&
                            region.DrawingKind == InkAnalysisDrawingKind.EquilateralTriangle &&
                            region.DrawingKind == InkAnalysisDrawingKind.IsoscelesTriangle &&
                            region.DrawingKind == InkAnalysisDrawingKind.RightTriangle)
                        {
                            AddOperatorFromRegion(region);
                            recognized = true;
                        }
                        if (!recognitionFromSelectedStrokes && recognized) RemoveStrokeReferences(region.GetStrokeIds().ToImmutableHashSet());
                    }
                }
                //All of the unused text gets re-added to the InkAnalyzer
                foreach (var key in TextBoundsDictionary.Keys)
                {
                    var ids = TextBoundsDictionary[key]?.Item2
                        ?.Select(id => FreeformInkControl.TargetCanvas.InkPresenter.StrokeContainer.GetStrokeById(id))
                        .Where(id => id != null);
                    if (ids != null) Analyzer.AddDataForStrokes(ids);
                }
            }
            if (recognitionFromSelectedStrokes) Analyzer.ClearDataForAllStrokes();
            FreeformInkControl.UpdateInkFieldModelController();
            NewStrokes.Clear();
        }

        

        #region Delete with line

        /// <summary>
        /// Tries to delete documents and field connections intersected by a stroke, so long as the stroke is linear.
        /// </summary>
        /// <param name="newStroke"></param>
        private void TryDeleteWithStroke(InkStroke newStroke)
        {
            var inkPoints = new List<Point>(newStroke.GetInkPoints().Select(p => Util.PointTransformFromVisual(
                p.Position, FreeformInkControl.SelectionCanvas,
                FreeformInkControl.FreeformView.xItemsControl.ItemsPanelRoot)));
            if (!IsLinear(inkPoints)) return;
            var point1 = inkPoints[0];
            var point2 = inkPoints.Last();
            var rectToDocView = GetDocViewRects();
            var docsRemoved = false;
            foreach (var rect in rectToDocView.Keys)
            {
                if (point1.X < rect.X && point2.X > rect.X + rect.Width ||
                    point1.X > rect.X + rect.Width && point2.X < rect.X || point1.Y < rect.Y && point2.Y > rect.Y + rect.Height ||
                    point1.Y > rect.Y + rect.Height && point2.Y < rect.Y)
                    if (PathIntersectsRect(
                        inkPoints, rect))
                    {
                        docsRemoved = true;
                        FreeformInkControl.FreeformView.DeleteConnections(rectToDocView[rect]);
                        FreeformInkControl.FreeformView.ViewModel.RemoveDocument(rectToDocView[rect].ViewModel
                            .DocumentController);
                    }
            }
            if (DeleteIntersectingConnections(point1, point2))
                docsRemoved = true;
            if (docsRemoved)
            {
                FreeformInkControl.ClearSelection();
                newStroke.Selected = true;
                FreeformInkControl.TargetCanvas.InkPresenter.StrokeContainer.DeleteSelected();
                Analyzer.RemoveDataForStroke(newStroke.Id);
                StrokesToRemove.Remove(newStroke);
            }
        }

        public bool DeleteIntersectingConnections(Point point1, Point point2)
        {
            bool lineDeleted = false;
            //Calculate line 1
            var slope1 = (point2.Y - point1.Y) / (point2.X - point1.X);
            var yInt1 = point1.Y - point1.X * slope1;
            var view = FreeformInkControl.FreeformView;
            var refsToLines = new Dictionary<FieldReference, Path>();
            foreach (var pair in view.RefToLine)
            {
                refsToLines[pair.Key] = pair.Value;
            }
            foreach (var pair in refsToLines)
            {
                //Calculate line 2
                var line = pair.Value;
                var converter = view.LineToConverter[line];
                var curvePoint1 = converter.Element1.TransformToVisual(view.xItemsControl.ItemsPanelRoot)
                    .TransformPoint(new Point(converter.Element1.ActualWidth / 2, converter.Element1.ActualHeight / 2));
                var curvePoint2 = converter.Element2.TransformToVisual(view.xItemsControl.ItemsPanelRoot)
                    .TransformPoint(new Point(converter.Element2.ActualWidth / 2, converter.Element2.ActualHeight / 2));
                var slope2 = (curvePoint2.Y - curvePoint1.Y) / (curvePoint2.X - curvePoint1.X);
                var yInt2 = curvePoint1.Y - curvePoint1.X * slope2;

                //Calculate intersection
                var intersectionX = (yInt2 - yInt1) / (slope1 - slope2);
                var intersectionY = slope1 * intersectionX + yInt1;
                var intersectionPoint = new Point(intersectionX, intersectionY);

                //if the intersection is on the two line segments, remove the path and the reference
                if (PointBetween(intersectionPoint, point1, point2) &&
                    PointBetween(intersectionPoint, curvePoint1, curvePoint2))
                {
                    var view2 = converter.Element2.GetFirstAncestorOfType<DocumentView>();
                    var doc2 = view2.ViewModel.DocumentController;
                    var fields = doc2.EnumFields().ToImmutableList();
                    foreach (var field in fields)
                    {
                        var referenceFieldModelController = (field.Value as ReferenceFieldModelController);
                        if (referenceFieldModelController != null)
                        {
                            var referencesEqual = referenceFieldModelController.DereferenceToRoot(null)
                                .Equals(pair.Key.DereferenceToRoot(null));
                            if (referencesEqual)
                            {
                                view.DeleteLine(pair.Key, view.RefToLine[pair.Key]);
                                doc2.SetField(field.Key,
                                    referenceFieldModelController.DereferenceToRoot(null).Copy(), true);
                            }
                        }
                    }
                    lineDeleted = true;
                }
            }
            return lineDeleted;
        }

        private bool PointBetween(Point testPoint, Point a, Point b)
        {
            var rect = new Rect(new Point(Math.Min(a.X, b.X), Math.Min(a.Y, b.Y)),
                new Size(Math.Abs(a.X - b.X), Math.Abs(a.Y - b.Y)));
            return rect.Contains(testPoint);
        }

        private bool IsLinear(IEnumerable<Point> points)
        {
            var pList = points.ToList();
            var r1 = GetRSquared(pList);
            var r2 = GetRSquared(RotatePoints(pList));
            return r1 > 0.9 || r2 > 0.9;
        }

        private double GetRSquared(List<Point> pList)
        {
            var xVals = pList.Select(p => p.X).ToArray();
            var yVals = pList.Select(p => p.Y).ToArray();
            Util.LinearRegression(xVals, yVals, 0, pList.Count, out double rsquared, out double yInt,
                out double slope);
            return rsquared;
        }

        private double GetRange(IEnumerable<double> vals)
        {
            var min = double.PositiveInfinity;
            var max = double.NegativeInfinity;
            foreach (var val in vals)
            {
                if (val > max) max = val;
                if (val < min) min = val;
            }
            return max - min;
        }

        private List<Point> RotatePoints(List<Point> points)
        {
            List<Point> newPoints = new List<Point>();
            var ctrIndex = points.Count / 2;
            var ctrPoint = points[ctrIndex];
            foreach (var point in points)
            {
                var dx = point.X - ctrPoint.X;
                var dy = point.Y - ctrPoint.Y;
                var dist = Math.Sqrt(Math.Pow(dx, 2) + Math.Pow(dy, 2));
                var currAngle = Math.Atan2(dy, dx);
                if (dx < 0) currAngle += Math.PI;
                var newPoint = new Point(Math.Cos(currAngle + Math.PI/4) * dist + ctrPoint.X, Math.Sin(currAngle + Math.PI/4) * dist + ctrPoint.Y);
                newPoints.Add(newPoint);
            }
            return newPoints;
        }

        private bool PathIntersectsRect(IEnumerable<Point> path, Rect rect)
        {
            var innerRect = new Rect
            {
                X = rect.X + rect.Width / 8,
                Y = rect.Y + rect.Height / 8,
                Width = rect.Width / 4 * 3,
                Height = rect.Height / 4 * 3
            };
            foreach (var point in path)
                if (innerRect.Contains(point)) return true;
            return false;
        }

        private Dictionary<Rect, DocumentView> GetDocViewRects()
        {
            Dictionary<Rect, DocumentView> dict = new Dictionary<Rect, DocumentView>();
            IEnumerable<DocumentViewModel> parameters = FreeformInkControl.FreeformView.xItemsControl.Items.OfType<DocumentViewModel>();
            foreach (var param in parameters)
            {
                var doc = param.DocumentController;
                var position = doc.GetPositionField().Data;
                var width = doc.GetWidthField().Data;
                var height = doc.GetHeightField().Data;
                var rect = new Rect
                {
                    X = position.X,
                    Y = position.Y,
                    Width = width,
                    Height = height
                };
                if (FreeformInkControl.FreeformView.xItemsControl.ItemContainerGenerator != null && FreeformInkControl
                        .FreeformView.xItemsControl
                        .ContainerFromItem(param) is ContentPresenter contentPresenter)
                    dict[rect] =
                        contentPresenter.GetFirstDescendantOfType<DocumentView>();
            }
            return dict;
        }

        #endregion

        #region Add collection, doc, operator

        private void AddCollectionFromShapeRegion(InkAnalysisInkDrawing region)
        {
            var regions = new List<IInkAnalysisNode>(_shapeRegions);
            List<DocumentController> recognizedDocuments = new List<DocumentController>();
            //Look for rectangles inside ellipse and add them as docs to collection
            foreach (var child in regions.OfType<InkAnalysisInkDrawing>().Where(
                r => RectContainsRect(region.BoundingRect, r.BoundingRect)))
            {
                if (child.DrawingKind == InkAnalysisDrawingKind.Rectangle ||
                    child.DrawingKind == InkAnalysisDrawingKind.Square)
                {
                    recognizedDocuments.Add(AddDocumentFromShapeRegion(child, false));
                    RemoveStrokeReferences(child.GetStrokeIds().ToImmutableHashSet());
                    _shapeRegions.Remove(child);
                }
            }
            var lassoPoints = new List<Point>(GetPointsFromStrokeIDs(region.GetStrokeIds())
                .Select(p => Util.PointTransformFromVisual(p, FreeformInkControl.SelectionCanvas,
                    FreeformInkControl.FreeformView.xItemsControl.ItemsPanelRoot as Canvas)));
            var selectedDocuments = FreeformInkControl.LassoHelper.GetSelectedDocuments(lassoPoints);
            var topLeft = new Point(region.BoundingRect.X, region.BoundingRect.Y);
            var position = Util.PointTransformFromVisual(topLeft, FreeformInkControl.SelectionCanvas,
                FreeformInkControl.FreeformView.xItemsControl.ItemsPanelRoot as Canvas);
            recognizedDocuments.AddRange(selectedDocuments.Select(view => (view.DataContext as DocumentViewModel).DocumentController));
            foreach (var doc in recognizedDocuments)
            {
                var ogPos = doc.GetPositionField().Data;
                var newPos = Util.PointTransformFromVisual(ogPos,
                    FreeformInkControl.FreeformView.xItemsControl.ItemsPanelRoot, FreeformInkControl.SelectionCanvas);
                var relativePos = new Point(newPos.X - topLeft.X, newPos.Y - topLeft.Y);
                doc.GetPositionField().Data = relativePos;
                FreeformInkControl.FreeformView.ViewModel.RemoveDocument(doc);
            }
            var fields = new Dictionary<KeyController, FieldModelController>
            {
                [DocumentCollectionFieldModelController.CollectionKey] =
                new DocumentCollectionFieldModelController(recognizedDocuments)
            };
            var documentController = new DocumentController(fields, DocumentType.DefaultType);
            documentController.SetActiveLayout(
                new CollectionBox(
                    new ReferenceFieldModelController(documentController.GetId(),
                        DocumentCollectionFieldModelController.CollectionKey), position.X, position.Y, region.BoundingRect.Width,
                    region.BoundingRect.Height).Document, true, true);
            FreeformInkControl.FreeformView.ViewModel.AddDocument(documentController, null);
            DeleteStrokesByID(region.GetStrokeIds().ToImmutableHashSet());
        }

        private void AddOperatorFromRegion(InkAnalysisInkDrawing region)
        {
            TabMenu.AddsToThisCollection = FreeformInkControl.FreeformView;
            if (MainPage.Instance.xCanvas.Children.Contains(TabMenu.Instance)) return;
            MainPage.Instance.xCanvas.Children.Add(TabMenu.Instance);
            Point absPos =
                Util.PointTransformFromVisual(new Point(region.BoundingRect.X, region.BoundingRect.Y),
                    FreeformInkControl.TargetCanvas, MainPage.Instance);
            Canvas.SetLeft(TabMenu.Instance, absPos.X);
            Canvas.SetTop(TabMenu.Instance, absPos.Y);
            DeleteStrokesByID(region.GetStrokeIds().ToImmutableHashSet());
            Analyzer.RemoveDataForStrokes(region.GetStrokeIds());
        }

        private DocumentController AddDocumentFromShapeRegion(InkAnalysisInkDrawing region, bool addToFreeformView = true)
        {
            DeleteStrokesByID(region.GetStrokeIds().ToImmutableHashSet());
            Analyzer.RemoveDataForStrokes(region.GetStrokeIds());
            var topLeft = new Point(region.BoundingRect.X, region.BoundingRect.Y);
            var size = new Size(region.BoundingRect.Width, region.BoundingRect.Height);
            var position = Util.PointTransformFromVisual(topLeft, FreeformInkControl.SelectionCanvas,
                FreeformInkControl.FreeformView.xItemsControl.ItemsPanelRoot as Canvas);
            var fields = new Dictionary<KeyController, FieldModelController>();
            var doc = new DocumentController(fields, DocumentType.DefaultType);
            var layoutDocs = new List<DocumentController>();
            var keysToRemove = new List<Rect>();
            foreach (var rect in TextBoundsDictionary.Keys.Where(r => RectContainsRect(region.BoundingRect, r)))
                {
                    DeleteStrokesByID(TextBoundsDictionary[rect].Item2.ToImmutableHashSet());
                    var str = TextBoundsDictionary[rect].Item1;
                    TryGetText(str, out string text, out KeyController key);
                    var relativePosition = new Point(rect.X - topLeft.X, rect.Y - topLeft.Y);
                    //bool isNumbers = double.TryParse(text, out double n);
                    //if (isNumbers)
                    //{
                    //    doc.SetField(key, new NumberFieldModelController(n), true);
                    //}
                    //else
                    //{
                    //    doc.SetField(key, new TextFieldModelController(text), true);
                    //}
                    doc.ParseDocField(key, text);
                    var textBox = new TextingBox(new ReferenceFieldModelController(doc.GetId(), key),
                        relativePosition.X, relativePosition.Y, rect.Width, rect.Height);
                    (textBox.Document.GetField(TextingBox.FontSizeKey) as NumberFieldModelController).Data =
                        rect.Height / 1.5;
                    layoutDocs.Add(textBox.Document);
                    keysToRemove.Add(rect);
                }
            foreach (var key in keysToRemove) TextBoundsDictionary.Remove(key);
            var layout = new FreeFormDocument(layoutDocs,
                position, size).Document;
            doc.SetActiveLayout(layout, true, true);
            if(addToFreeformView) FreeformInkControl.FreeformView.ViewModel.AddDocument(doc, null);
            return doc;
        }

        #endregion

        #region InkStroke data helpers

        private List<Point> GetPointsFromStrokeIDs(IEnumerable<uint> ids)
        {
            var points = new List<Point>();
            foreach (var id in ids)
                points.AddRange(FreeformInkControl.TargetCanvas.InkPresenter.StrokeContainer.GetStrokeById(id)
                    .GetInkPoints().Select(inkPoint => inkPoint.Position));
            return points;
        }
        private void DeleteStrokesByID(ICollection<uint> IDs)
        {
            FreeformInkControl.ClearSelection();
            foreach (var stroke in FreeformInkControl.TargetCanvas.InkPresenter.StrokeContainer.GetStrokes())
                if (IDs.Contains(stroke.Id))
                    stroke.Selected = true;
            FreeformInkControl.TargetCanvas.InkPresenter.StrokeContainer.DeleteSelected();
        }

        public void AddStrokeData(List<InkStroke> strokes)
        {
            Analyzer.AddDataForStrokes(strokes);
            NewStrokes = strokes;
        }

        public void RemoveStrokeData(List<InkStroke> strokes)
        {
            Analyzer.RemoveDataForStrokes(strokes.Select(s => s.Id));
            NewStrokes.RemoveAll(strokes.Contains);
        }

        public void RecognizeAndForgetStrokes(IEnumerable<InkStroke> strokes)
        {
            AddStrokeData(new List<InkStroke>(strokes));
            RecognizeInk(true);
        }

        private bool RegionContainsNewStroke(IInkAnalysisNode region)
        {
            return region.GetStrokeIds()
                .Select(id => FreeformInkControl.TargetCanvas.InkPresenter.StrokeContainer.GetStrokeById(id))
                .Any(stroke => NewStrokes.Contains(stroke));
        }

        private void RemoveStrokeReferences(ICollection<uint> ids)
        {
            var strokes = FreeformInkControl.TargetCanvas.InkPresenter.StrokeContainer.GetStrokes()
                .Where(s => ids.Contains(s.Id));
            foreach (var stroke in strokes) StrokesToRemove.Remove(stroke);
            Analyzer.RemoveDataForStrokes(ids);
        }

        #endregion

        private Dictionary<Rect, Tuple<string, IEnumerable<uint>>> GetTextBoundsDictionary()
        {
            var textLineRegions = new List<InkAnalysisLine>(Analyzer.AnalysisRoot.FindNodes(InkAnalysisNodeKind.Line)
                .Select(o => o as InkAnalysisLine));
            var textBoundsDictionary = new Dictionary<Rect, Tuple<string, IEnumerable<uint>>>();
            foreach (var textLine in textLineRegions)
            {
                textBoundsDictionary[textLine.BoundingRect] =
                    new Tuple<string, IEnumerable<uint>>(textLine.RecognizedText, textLine.GetStrokeIds());
                Analyzer.RemoveDataForStrokes(textLine.GetStrokeIds());
            }

            return textBoundsDictionary;
        }

        private void TryGetText(string str, out string value, out KeyController key)
        {
            value = str;
            key = new KeyController(Guid.NewGuid().ToString(), str);
            if (str.Contains(':'))
            {
                var splitstring = str.Split(':');
                value = splitstring[1].TrimEnd(' ').TrimStart(' ');
                string keystring = splitstring[0].TrimEnd(' ').TrimStart(' ');
                key = new KeyController(Guid.NewGuid().ToString(), keystring);
            }
            
        }

        private bool RectContainsRect(Rect outer, Rect inner)
        {
            var topLeft = new Point(inner.X, inner.Y);
            var topRight = new Point(inner.X + inner.Width, inner.Y);
            var bottomLeft = new Point(inner.X, inner.Y + inner.Height);
            var bottomRight = new Point(inner.X + inner.Width, inner.Y + inner.Height);
            var points = new List<Point>
            {
                topLeft,
                topRight,
                bottomLeft,
                bottomRight
            };
            foreach (var point in points)
                if (!outer.Contains(point)) return false;
            return true;
        }

    }
}