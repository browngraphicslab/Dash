using System;
using System.Collections.Generic;
using System.Collections.Immutable;
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
        private readonly FreeformInkControl _freeformInkControl;
        private Dictionary<Rect, Tuple<string, IEnumerable<uint>>> _textBoundsDictionary;
        private readonly DispatcherTimer _timer;
        public InkAnalyzer Analyzer;
        public bool DoubleTapped;
        public List<Point> DoubleTappedPoints;
        public List<InkStroke> NewStrokes;
        public List<InkStroke> StrokesToRemove;

        public InkRecognitionHelper(FreeformInkControl freeformInkControl)
        {
            Analyzer = new InkAnalyzer();
            DoubleTappedPoints = new List<Point>();
            NewStrokes = new List<InkStroke>();
            StrokesToRemove = new List<InkStroke>();
            _freeformInkControl = freeformInkControl;
            //_timer = new DispatcherTimer {Interval = TimeSpan.FromMilliseconds(600)};
            //_timer.Tick += TimerOnTick;
            //_timer.Start();
            //GlobalInkSettings.RecognitionChanged += GlobalInkSettingsOnRecognitionChanged;
        }

        private void GlobalInkSettingsOnRecognitionChanged(bool newValue)
        {
            if (!newValue)
            {
                Analyzer.ClearDataForAllStrokes();
                _freeformInkControl.ClearSelection();
                foreach (var stroke in StrokesToRemove)
                {
                    stroke.Selected = true;
                }
                _freeformInkControl.TargetCanvas.InkPresenter.StrokeContainer.DeleteSelected();
            }
        }

        private void TimerOnTick(object sender, object o1)
        {
            RecognizeInk();
        }

        /// <summary>
        ///     Tries to find documents or collections at the double tap point, or delete using the newest stroke.
        /// </summary>
        /// <param name="doubleTapped"></param>
        /// <param name="newStroke"></param>
        public async void RecognizeInk(bool addToRemoveList=true)
        {
            if (NewStrokes.Count > 0)
            {
                StrokesToRemove.AddRange(NewStrokes);
                foreach (var newStroke in NewStrokes)
                    //Done separately because it doesn't require ink analyzer
                    TryDeleteWithStroke(newStroke);
                
            }
            var result = await Analyzer.AnalyzeAsync();
            if (result.Status == InkAnalysisStatus.Updated)
            {
                _textBoundsDictionary = GetTextBoundsDictionary();
                // Find circles and rectangles
                var shapeRegions = Analyzer.AnalysisRoot.FindNodes(InkAnalysisNodeKind.InkDrawing);
                foreach (InkAnalysisInkDrawing region in shapeRegions)
                {
                    //If the region is a rectangle and was double tapped, add the document
                    if (region.DrawingKind != InkAnalysisDrawingKind.Rectangle &&
                        region.DrawingKind != InkAnalysisDrawingKind.Square) continue;
                    if (RegionContainsNewStroke(region)) AddDocumentFromShapeRegion(region);
                    if(addToRemoveList) RemoveStrokeReferences(region.GetStrokeIds().ToImmutableHashSet());
                }
                //If the region is an ellipse, add a collection
                foreach (InkAnalysisInkDrawing region in shapeRegions)
                {
                    if (region.DrawingKind != InkAnalysisDrawingKind.Circle &&
                        region.DrawingKind != InkAnalysisDrawingKind.Ellipse) continue;
                    if(RegionContainsNewStroke(region)) AddCollectionFromShapeRegion(region);
                    if (addToRemoveList) RemoveStrokeReferences(region.GetStrokeIds().ToImmutableHashSet());
                }
                //All of the unused text gets re-added to the InkAnalyzer
                foreach (var key in _textBoundsDictionary.Keys)
                {
                    var ids = _textBoundsDictionary[key]?.Item2
                        ?.Select(id => _freeformInkControl.TargetCanvas.InkPresenter.StrokeContainer.GetStrokeById(id))
                        .Where(id => id != null);
                    if (ids != null) Analyzer.AddDataForStrokes(ids);
                }
            }
            if (!addToRemoveList) Analyzer.ClearDataForAllStrokes();
            _freeformInkControl.UpdateInkFieldModelController();
            NewStrokes.Clear();
        }

        private bool RegionContainsNewStroke(IInkAnalysisNode region)
        {
            return region.GetStrokeIds()
                .Select(id => _freeformInkControl.TargetCanvas.InkPresenter.StrokeContainer.GetStrokeById(id)).Any(stroke => NewStrokes.Contains(stroke));
        }

        private void RemoveStrokeReferences(ICollection<uint> ids)
        {
            AccountForRemovedStrokes(ids.ToImmutableHashSet());
            Analyzer.RemoveDataForStrokes(ids);
        }

        public void AccountForRemovedStrokes(ICollection<uint> ids)
        {
            var strokes = _freeformInkControl.TargetCanvas.InkPresenter.StrokeContainer.GetStrokes()
                .Where(s => ids.Contains(s.Id));
            foreach (var stroke in strokes) StrokesToRemove.Remove(stroke);
        }

        private bool ContainsDoubleTapPoint(Rect rect, out List<Point> containedPoints)
        {
            containedPoints = new List<Point>();
            var ret = false;
            foreach (var point in DoubleTappedPoints)
                if (rect.Contains(point))
                {
                    containedPoints.Add(point);
                    ret = true;
                }
            return ret;
        }

        private void TryDeleteWithStroke(InkStroke newStroke)
        {
            var inkPoints = new List<Point>(newStroke.GetInkPoints().Select(p => Util.PointTransformFromVisual(p.Position, _freeformInkControl.SelectionCanvas,
                _freeformInkControl.FreeformView.xItemsControl.ItemsPanelRoot)));
            if (!IsLinear(inkPoints)) return;
            var point1 = inkPoints[0];
            var point2 = inkPoints.Last();
            var rectToDocView = GetDocViewRects();
            var docsRemoved = false;
            foreach (var rect in rectToDocView.Keys)
            {
                if (point1.X < rect.X && point2.X > rect.X + rect.Width ||
                    point1.X > rect.X + rect.Width && point2.X < rect.X)
                    if (PathIntersectsRect(
                        inkPoints, rect))
                    {
                        docsRemoved = true;
                        _freeformInkControl.FreeformView.DeleteConnections(rectToDocView[rect]);
                        _freeformInkControl.FreeformView.ViewModel.RemoveDocument(rectToDocView[rect].ViewModel
                            .DocumentController);
                    }
            }
            if (DeleteIntersectingConnections(point1, point2))
                docsRemoved = true;
            if (docsRemoved)
            {
                _freeformInkControl.ClearSelection();
                newStroke.Selected = true;
                _freeformInkControl.TargetCanvas.InkPresenter.StrokeContainer.DeleteSelected();
                Analyzer.RemoveDataForStroke(newStroke.Id);
                StrokesToRemove.Remove(newStroke);
            }
        }

        public bool DeleteIntersectingConnections(Point point1, Point point2)
        {
            bool lineDeleted = false;
            var toBeDeleted = new List<FieldReference>();
            //Calculate line 1
            var slope1 = (point2.Y - point1.Y) / (point2.X - point1.X);
            var yInt1 = point1.Y - point1.X * slope1;
            var view = _freeformInkControl.FreeformView;

            foreach (var pair in view.LineDict)
            {
                //Calculate line 2
                var line = pair.Value;
                var converter = line.Converter;
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
                    view.xItemsControl.ItemsPanelRoot.Children.Remove(pair.Value.Line);
                    toBeDeleted.Add(pair.Key);
                    var view2 = converter.Element2.GetFirstAncestorOfType<DocumentView>();
                    var doc2 = view2.ViewModel.DocumentController;
                    var fields = doc2.EnumFields().ToImmutableList();
                    foreach (var field in fields)
                    {
                        var referenceFieldModelController = (field.Value as ReferenceFieldModelController);
                        if (referenceFieldModelController != null)
                        {
                            var referencesEqual = referenceFieldModelController.DereferenceToRoot(null).Equals(pair.Key.DereferenceToRoot(null));
                            if (referencesEqual)
                            {
                                doc2.SetField(field.Key,
                                    referenceFieldModelController.DereferenceToRoot(null).Copy(), true);
                            }
                        }
                    }
                    lineDeleted = true;
                }
            }
            foreach (var key in toBeDeleted) view.LineDict.Remove(key);
            return lineDeleted;
        }

        private bool PointBetween(Point testPoint, Point a, Point b)
        {
            var rect = new Rect(new Point(Math.Min(a.X, b.X), Math.Min(a.Y, b.Y)), new Size(Math.Abs(a.X - b.X), Math.Abs(a.Y - b.Y)));
            return rect.Contains(testPoint);
        }

        private bool IsLinear(IEnumerable<Point> points)
        {
            var xVals = points.Select(p => p.X).ToArray();
            var yVals = points.Select(p => p.Y).ToArray();
            Util.LinearRegression(xVals, yVals, 0, points.Count(), out double rsquared, out double yInt,
                out double slope);
            return rsquared > 0.85;
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
            var dict = new Dictionary<Rect, DocumentView>();
            var parameters = _freeformInkControl.FreeformView.xItemsControl.Items.OfType<DocumentViewModelParameters>();
            foreach (var param in parameters)
            {
                var doc = param.Controller;
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
                if (_freeformInkControl.FreeformView.xItemsControl.ItemContainerGenerator != null && _freeformInkControl
                        .FreeformView.xItemsControl
                        .ContainerFromItem(param) is ContentPresenter contentPresenter)
                    dict[rect] =
                        contentPresenter.GetFirstDescendantOfType<DocumentView>();
            }
            return dict;
        }

        private void AddCollectionFromShapeRegion(InkAnalysisInkDrawing region)
        {
            var selectionPoints = new List<Point>(GetPointsFromStrokeIDs(region.GetStrokeIds())
                .Select(p => Util.PointTransformFromVisual(p, _freeformInkControl.SelectionCanvas,
                    _freeformInkControl.FreeformView.xItemsControl.ItemsPanelRoot as Canvas)));
            var selectedDocuments = _freeformInkControl.LassoHelper.GetSelectedDocuments(selectionPoints);
            var docControllers = new List<DocumentController>();
            var topLeft = new Point(region.BoundingRect.X, region.BoundingRect.Y);
            var size = new Size(region.BoundingRect.Width, region.BoundingRect.Height);
            var position = Util.PointTransformFromVisual(topLeft, _freeformInkControl.SelectionCanvas,
                _freeformInkControl.FreeformView.xItemsControl.ItemsPanelRoot as Canvas);
            foreach (var view in selectedDocuments)
            {
                var doc = (view.DataContext as DocumentViewModel).DocumentController;
                var ogPos = doc.GetPositionField().Data;
                var newPos = Util.PointTransformFromVisual(ogPos,
                    _freeformInkControl.FreeformView.xItemsControl.ItemsPanelRoot, _freeformInkControl.SelectionCanvas);
                var relativePos = new Point(newPos.X - topLeft.X, newPos.Y - topLeft.Y);
                doc.GetPositionField().Data = relativePos;
                _freeformInkControl.FreeformView.ViewModel.RemoveDocument(doc);
                docControllers.Add(doc);
            }
            var fields = new Dictionary<KeyController, FieldModelController>
            {
                [DocumentCollectionFieldModelController.CollectionKey] =
                new DocumentCollectionFieldModelController(docControllers)
            };
            var documentController = new DocumentController(fields, DocumentType.DefaultType);
            documentController.SetActiveLayout(
                new CollectionBox(
                    new ReferenceFieldModelController(documentController.GetId(),
                        DocumentCollectionFieldModelController.CollectionKey), position.X, position.Y, size.Width,
                    size.Height).Document, true, true);
            _freeformInkControl.FreeformView.ViewModel.AddDocument(documentController, null);
            DeleteStrokesByID(region.GetStrokeIds().ToImmutableHashSet());
        }

        private List<Point> GetPointsFromStrokeIDs(IEnumerable<uint> ids)
        {
            var points = new List<Point>();
            foreach (var id in ids)
                points.AddRange(_freeformInkControl.TargetCanvas.InkPresenter.StrokeContainer.GetStrokeById(id)
                    .GetInkPoints().Select(inkPoint => inkPoint.Position));
            return points;
        }

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

        private Dictionary<Rect, Tuple<string, IEnumerable<uint>>> GetListBoundsDictionary()
        {
            var textLineRegions = new List<InkAnalysisListItem>(Analyzer.AnalysisRoot
                .FindNodes(InkAnalysisNodeKind.ListItem).Select(o => o as InkAnalysisListItem));
            var textBoundsDictionary = new Dictionary<Rect, Tuple<string, IEnumerable<uint>>>();
            foreach (var textLine in textLineRegions)
            {
                textBoundsDictionary[textLine.BoundingRect] =
                    new Tuple<string, IEnumerable<uint>>(textLine.RecognizedText, textLine.GetStrokeIds());
                Analyzer.RemoveDataForStrokes(textLine.GetStrokeIds());
            }

            return textBoundsDictionary;
        }

        private Dictionary<Rect, Tuple<string, IEnumerable<uint>>> GetParagraphBoundsDictionary()
        {
            var textLineRegions = new List<InkAnalysisParagraph>(Analyzer.AnalysisRoot
                .FindNodes(InkAnalysisNodeKind.Paragraph).Select(o => o as InkAnalysisParagraph));
            var textBoundsDictionary = new Dictionary<Rect, Tuple<string, IEnumerable<uint>>>();
            foreach (var textLine in textLineRegions)
            {
                textBoundsDictionary[textLine.BoundingRect] =
                    new Tuple<string, IEnumerable<uint>>(textLine.RecognizedText, textLine.GetStrokeIds());
                Analyzer.RemoveDataForStrokes(textLine.GetStrokeIds());
            }

            return textBoundsDictionary;
        }

        private void AddDocumentFromShapeRegion(InkAnalysisInkDrawing region)
        {
            DeleteStrokesByID(region.GetStrokeIds().ToImmutableHashSet());
            Analyzer.RemoveDataForStrokes(region.GetStrokeIds());
            var topLeft = new Point(region.BoundingRect.X, region.BoundingRect.Y);
            var size = new Size(region.BoundingRect.Width, region.BoundingRect.Height);
            var position = Util.PointTransformFromVisual(topLeft, _freeformInkControl.SelectionCanvas,
                _freeformInkControl.FreeformView.xItemsControl.ItemsPanelRoot as Canvas);
            var fields = new Dictionary<KeyController, FieldModelController>();
            var doc = new DocumentController(fields, DocumentType.DefaultType);
            var layoutDocs = new List<DocumentController>();
            var keysToRemove = new List<Rect>();
            foreach (var rect in _textBoundsDictionary.Keys)
                if (RectContainsRect(region.BoundingRect, rect))
                {
                    DeleteStrokesByID(_textBoundsDictionary[rect].Item2.ToImmutableHashSet());
                    var str = _textBoundsDictionary[rect].Item1;
                    var key = TryGetKey(str);
                    var text = TryGetText(str);
                    var relativePosition = new Point(rect.X - topLeft.X, rect.Y - topLeft.Y);
                    double n;
                    bool isNumeric = double.TryParse(text, out n);
                    if (isNumeric)
                    {
                        doc.SetField(key, new NumberFieldModelController(n), true);
                    }
                    else
                    {
                        doc.SetField(key, new TextFieldModelController(text), true);
                    }
                    var textBox = new TextingBox(new ReferenceFieldModelController(doc.GetId(), key),
                        relativePosition.X, relativePosition.Y, rect.Width, rect.Height);
                    (textBox.Document.GetField(TextingBox.FontSizeKey) as NumberFieldModelController).Data =
                        rect.Height / 1.5;
                    layoutDocs.Add(textBox.Document);
                    keysToRemove.Add(rect);
                }
            foreach (var key in keysToRemove) _textBoundsDictionary.Remove(key);
            var layout = new FreeFormDocument(layoutDocs,
                position, size).Document;
            doc.SetActiveLayout(layout, true, true);
            _freeformInkControl.FreeformView.ViewModel.AddDocument(doc, null);
        }

        private string TryGetText(string str)
        {
            if (str.Contains(':'))
            {
                var splitstring = str.Split(':');
                var text = splitstring[1].TrimEnd(' ').TrimStart(' ');
                return text;
            }
            return str;
        }

        private KeyController TryGetKey(string text)
        {
            if (text.Contains(':'))
            {
                var splitstring = text.Split(':');
                var key = splitstring[0].TrimEnd(' ').TrimStart(' ');
                return new KeyController(Guid.NewGuid().ToString(), key);
            }
            return new KeyController(Guid.NewGuid().ToString(), text);
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

        private void DeleteStrokesByID(ICollection<uint> IDs)
        {
            _freeformInkControl.ClearSelection();
            foreach (var stroke in _freeformInkControl.TargetCanvas.InkPresenter.StrokeContainer.GetStrokes())
                if (IDs.Contains(stroke.Id))
                    stroke.Selected = true;
            _freeformInkControl.TargetCanvas.InkPresenter.StrokeContainer.DeleteSelected();
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
            RecognizeInk(false);
        }
    }
}