using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Windows.Foundation;
using Windows.UI.Input.Inking;
using Windows.UI.Input.Inking.Analysis;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
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

        public InkRecognitionHelper(FreeformInkControl freeformInkControl)
        {
            Analyzer = new InkAnalyzer();
            DoubleTappedPoints = new List<Point>();
            NewStrokes = new List<InkStroke>();
            _freeformInkControl = freeformInkControl;
            _timer = new DispatcherTimer {Interval = TimeSpan.FromMilliseconds(600)};
            _timer.Tick += TimerOnTick;
            _timer.Start();
            GlobalInkSettings.RecognitionChanged += GlobalInkSettingsOnRecognitionChanged;
        }

        private void GlobalInkSettingsOnRecognitionChanged(bool newValue)
        {
            if (!newValue) Analyzer.ClearDataForAllStrokes();
        }

        private void TimerOnTick(object sender, object o1)
        {
            if (GlobalInkSettings.IsRecognitionEnabled) RecognizeInk();
        }

        /// <summary>
        ///     Tries to find documents or collections at the double tap point, or delete using the newest stroke.
        /// </summary>
        /// <param name="doubleTapped"></param>
        /// <param name="newStroke"></param>
        public async void RecognizeInk()
        {
            if (NewStrokes.Count > 0)
            {
                foreach (var newStroke in NewStrokes)
                    //Done separately because it doesn't require ink analyzer
                    TryDeleteWithStroke(newStroke);
                NewStrokes.Clear();
            }
            var result = await Analyzer.AnalyzeAsync();
            if (result.Status == InkAnalysisStatus.Updated)
            {
                //Dictionary<Rect, Tuple<string, IEnumerable<uint>>> listBoundsDictionary = GetListBoundsDictionary();
                //_paragraphBoundsDictionary = GetParagraphBoundsDictionary();
                _textBoundsDictionary = GetTextBoundsDictionary();
                // Find circles and rectangles
                var shapeRegions = Analyzer.AnalysisRoot.FindNodes(InkAnalysisNodeKind.InkDrawing);
                foreach (InkAnalysisInkDrawing region in shapeRegions)
                {
                    //If the region is a rectangle and was double tapped, add the document
                    if (region.DrawingKind != InkAnalysisDrawingKind.Rectangle &&
                        region.DrawingKind != InkAnalysisDrawingKind.Square) continue;
                    //if (!ContainsDoubleTapPoint(region.BoundingRect, out List<Point> containedPoints)) continue;
                    AddDocumentFromShapeRegion(region);
                    Analyzer.RemoveDataForStrokes(region.GetStrokeIds());
                    DoubleTapped = false;
                    //foreach (var point in containedPoints)
                    //{
                    //    DoubleTappedPoints.Remove(point);
                    //}
                }
                //If the region is an ellipse, add a collection
                foreach (InkAnalysisInkDrawing region in shapeRegions)
                {
                    if (region.DrawingKind != InkAnalysisDrawingKind.Circle &&
                        region.DrawingKind != InkAnalysisDrawingKind.Ellipse) continue;
                    AddCollectionFromShapeRegion(region);
                    Analyzer.RemoveDataForStrokes(region.GetStrokeIds());
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

            _freeformInkControl.UpdateInkFieldModelController();
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
            var inkPoints = new List<Point>(newStroke.GetInkPoints().Select(p => p.Position));
            if (!IsLinear(inkPoints)) return;
            var transformedLine = new Polyline();
            foreach (var point in inkPoints)
                transformedLine.Points.Add(Util.PointTransformFromVisual(point, _freeformInkControl.SelectionCanvas,
                    _freeformInkControl.FreeformView));
            var point1 = transformedLine.Points[0];
            var point2 = transformedLine.Points.Last();
            var rectToDocView = GetDocViewRects();
            var docsRemoved = false;
            foreach (var rect in rectToDocView.Keys)
                if (point1.X < rect.X && point2.X > rect.X + rect.Width ||
                    point1.X > rect.X + rect.Width && point2.X < rect.X)
                    if (PathIntersectsRect(
                        transformedLine.Points, rect))
                    {
                        docsRemoved = true;
                        _freeformInkControl.FreeformView.DeleteConnections(rectToDocView[rect]);
                        _freeformInkControl.FreeformView.ViewModel.RemoveDocument(rectToDocView[rect].ViewModel
                            .DocumentController);
                    }
            //TODO swipe to remove connections
            //foreach (var value in _freeformInkControl.FreeformView.LineDict.Values)
            //{

            //}
            if (docsRemoved)
            {
                _freeformInkControl.ClearSelection();
                newStroke.Selected = true;
                _freeformInkControl.TargetCanvas.InkPresenter.StrokeContainer.DeleteSelected();
                Analyzer.RemoveDataForStroke(newStroke.Id);
            }
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
            var textFields = new List<DocumentController>();
            var keysToRemove = new List<Rect>();
            //TODO: need better differentiation between paragraphs and lines before we can to rtf for paragraphs.
            foreach (var rect in _textBoundsDictionary.Keys)
                if (RectContainsRect(region.BoundingRect, rect))
                {
                    DeleteStrokesByID(_textBoundsDictionary[rect].Item2.ToImmutableHashSet());
                    var str = _textBoundsDictionary[rect].Item1;
                    var key = TryGetKey(str);
                    var text = TryGetText(str);
                    var relativePosition = new Point(rect.X - topLeft.X, rect.Y - topLeft.Y);
                    doc.SetField(key, new TextFieldModelController(text), true);
                    var textBox = new TextingBox(new ReferenceFieldModelController(doc.GetId(), key),
                        relativePosition.X, relativePosition.Y, rect.Width * 1.5, rect.Height);
                    (textBox.Document.GetField(TextingBox.FontSizeKey) as NumberFieldModelController).Data =
                        rect.Height / 1.3;
                    textFields.Add(textBox.Document);
                    keysToRemove.Add(rect);
                }
            foreach (var key in keysToRemove) _textBoundsDictionary.Remove(key);
            var layout = new FreeFormDocument(textFields,
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
            NewStrokes.AddRange(strokes);
        }

        public void RemoveStrokeData(List<InkStroke> strokes)
        {
            Analyzer.RemoveDataForStrokes(strokes.Select(s => s.Id));
            NewStrokes.RemoveAll(strokes.Contains);
        }
    }
}