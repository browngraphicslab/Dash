using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Collections.ObjectModel;
using System.Linq;
using System.Numerics;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Media.Devices;
using Windows.UI.Input.Inking;
using Windows.UI.Input.Inking.Analysis;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Automation;
using Windows.UI.Xaml.Controls;
using Dash.Controllers;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Shapes;
using DashShared;
using NewControls.Geometry;
using static Dash.NoteDocuments;

namespace Dash
{
    public sealed class InkRecognitionHelper
    {
        private FreeformInkControl _freeformInkControl;
        private readonly InkAnalyzer _analyzer;
        private InkStroke _newStroke;
        private RecognizedShapeTree _recognizedShapeTree;
        private List<InkStroke> _remainingStrokes;
        private List<LineNode> _recognizedLines;

        public InkRecognitionHelper()
        {
            _analyzer = new InkAnalyzer();
        }

        /// <summary>
        /// Tries to find documents or collections at the double tap point, or delete using the newest stroke.
        /// Sets the state to keep track of the ink control we're recognizing for and the newest stroke added to it.
        /// </summary>
        /// <param name="onlySelectedStrokes"></param>
        public async void RecognizeInk(InkStroke newStroke, FreeformInkControl inkControl, bool onlySelectedStrokes)
        {
            _remainingStrokes = new List<InkStroke>();
            _recognizedLines = new List<LineNode>();
            _freeformInkControl = inkControl;
            _newStroke = newStroke;
            //Tries to delete with a line stroke, so long as we're not doing forced recognition from selected strokes.
            if (newStroke != null && !onlySelectedStrokes && TryDeleteWithStroke(newStroke))
                return;
            //if we're not only analyzing selected strokes, we want to analyze all strokes within the range of the most recent stroke
            //else only analyze selected strokes
            _analyzer.ClearDataForAllStrokes();
            List<DocumentController> recognizedDocs;
            if (!onlySelectedStrokes)
            {
                recognizedDocs = await GetDocsFromNewStroke();
            }
            else
            {
                recognizedDocs = await GetDocsFromSelection();
            }
            _freeformInkControl.FreeformView.ViewModel.AddDocuments(recognizedDocs, null);
            _freeformInkControl.UpdateInkFieldModelController();
        }

        private async Task<List<DocumentController>> GetDocsFromNewStroke()
        {
            var docs = new List<DocumentController>();
            foreach (var inkStroke in _freeformInkControl.TargetInkCanvas.InkPresenter.StrokeContainer.GetStrokes().Where(inkStroke => _newStroke.BoundingRect.Contains(inkStroke.BoundingRect)))
            {
                _analyzer.AddDataForStroke(inkStroke);
                _remainingStrokes.Add(inkStroke);
            }
            _analyzer.AddDataForStroke(_newStroke);
            _remainingStrokes.Add(_newStroke);
            var result = await _analyzer.AnalyzeAsync();
            if (result.Status == InkAnalysisStatus.Updated)
            {
                var root = _analyzer.AnalysisRoot;
                var orderedShapes = await OrderedShapeList();
                _analyzer.ClearDataForAllStrokes();
                _analyzer.AddDataForStrokes(_remainingStrokes);
                await GetRecognizedLines();
                _recognizedShapeTree = new RecognizedShapeTree(root, orderedShapes);
                foreach (var child in _recognizedShapeTree.Root.Children)
                {
                    var doc = GetDocFromRecognizedNode(child);
                    if (doc != null) docs.Add(doc);
                }
            }
            return docs;
        }

        private async Task<List<DocumentController>> GetDocsFromSelection()
        {
            var docs = new List<DocumentController>();
            AddDataForSelectedStrokes();
            var result = await _analyzer.AnalyzeAsync();
            if (result.Status == InkAnalysisStatus.Updated)
            {
                var root = _analyzer.AnalysisRoot;
                var orderedShapes = await OrderedShapeList();
                _analyzer.ClearDataForAllStrokes();
                _analyzer.AddDataForStrokes(_remainingStrokes);
                await GetRecognizedLines();
                _recognizedShapeTree = new RecognizedShapeTree(root, orderedShapes);
                foreach (var child in _recognizedShapeTree.Root.Children)
                {
                    var doc = GetDocFromRecognizedNode(child);
                    if (doc != null) docs.Add(doc);
                }
            }
            return docs;
        }
        

        private DocumentController GetDocFromRecognizedNode(RecognizedShapeNode node)
        {
            var drawing = node.InkRegion as InkAnalysisInkDrawing;
            if (drawing != null)
            {
                if (drawing.DrawingKind == InkAnalysisDrawingKind.Circle ||
                    drawing.DrawingKind == InkAnalysisDrawingKind.Ellipse)
                {
                    return GetCollectionFromShapeRegion(node);
                }
                if (drawing.DrawingKind == InkAnalysisDrawingKind.Square ||
                    drawing.DrawingKind == InkAnalysisDrawingKind.Rectangle)
                {
                    return GetDocumentFromShapeRegion(node);
                }
            }
            return null;
        }

        private async Task GetRecognizedLines()
        {
            await _analyzer.AnalyzeAsync();
            var heightCutoff = 75;
            //Get all InkLines within permitted size bounds and remove their strokes from RemainingStrokes
            List<InkAnalysisLine> smallLines = _analyzer.AnalysisRoot.FindNodes(InkAnalysisNodeKind.Line)
                .OfType<InkAnalysisLine>().ToList();
            foreach (var smallLine in smallLines)
            {
                List<WordNode> children = new List<WordNode>();
                foreach (var child in smallLine.Children.OfType<InkAnalysisInkWord>())
                {
                    WordNode word = new WordNode
                    {
                        BoundingRect = child.BoundingRect,
                        Text = child.RecognizedText,
                        Strokes = child.GetStrokeIds()
                            .Select(id => _freeformInkControl.TargetInkCanvas.InkPresenter.StrokeContainer
                                .GetStrokeById(id)).ToList()
                    };
                    children.Add(word);
                }
                LineNode line = new LineNode
                {
                    BoundingRect = smallLine.BoundingRect,
                    Text = smallLine.RecognizedText,
                    Children = children,
                    Strokes = smallLine.GetStrokeIds()
                        .Select(id => _freeformInkControl.TargetInkCanvas.InkPresenter.StrokeContainer
                            .GetStrokeById(id)).ToList()
                };
                _recognizedLines.Add(line);
                var ids = smallLine.GetStrokeIds();
                _remainingStrokes.RemoveAll(stroke => ids.Contains(stroke.Id));
                _analyzer.RemoveDataForStrokes(ids);
            }
            if (_remainingStrokes.Count == 0) return;
            double maxHeight = _remainingStrokes.Max(stroke => stroke.BoundingRect.Height);
            double y = _remainingStrokes.Min(stroke => stroke.BoundingRect.Y);
            double x = _remainingStrokes.Min(stroke => stroke.BoundingRect.X);
            double scaleFactor = heightCutoff / maxHeight;
            Matrix3x2 scaleMatrix = Matrix3x2.CreateScale((float)scaleFactor,
                new Vector2((float)x, (float)y));
            Matrix3x2 reverseScale = Matrix3x2.CreateScale((float)(1/scaleFactor),
                new Vector2((float)x, (float)y));
            List<InkStroke> scaledStrokes = new List<InkStroke>(_remainingStrokes);
            Dictionary<InkStroke, Rect> originalSizes = new Dictionary<InkStroke, Rect>();
            foreach (var stroke in _remainingStrokes)
            {
                _analyzer.RemoveDataForStroke(stroke.Id);
                originalSizes[stroke] = stroke.BoundingRect;
                stroke.PointTransform = Matrix3x2.Multiply(stroke.PointTransform, scaleMatrix);
            }
            _analyzer.AddDataForStrokes(_remainingStrokes);
            var result = await _analyzer.AnalyzeAsync();
            if (result.Status == InkAnalysisStatus.Updated)
            {
                var inkLines = _analyzer.AnalysisRoot.FindNodes(InkAnalysisNodeKind.Line).OfType<InkAnalysisLine>();
                foreach (var inkLine in inkLines)
                {
                    var ids = inkLine.GetStrokeIds();
                    _remainingStrokes.RemoveAll(stroke => ids.Contains(stroke.Id));
                    List<WordNode> children = new List<WordNode>();
                    foreach (var child in inkLine.Children.OfType<InkAnalysisInkWord>())
                    {
                        WordNode word = new WordNode
                        {
                            Text = child.RecognizedText,
                            Strokes = child.GetStrokeIds()
                                .Select(id => _freeformInkControl.TargetInkCanvas.InkPresenter.StrokeContainer
                                    .GetStrokeById(id)).ToList()
                        };
                        Rect wordRect = word.Strokes[0].BoundingRect;
                        word.Strokes.ForEach(stroke => wordRect.Union(originalSizes[stroke]));
                        word.BoundingRect = wordRect;
                        children.Add(word);
                    }
                    LineNode line = new LineNode
                    {
                        Text = inkLine.RecognizedText,
                        Children = children,
                        Strokes = inkLine.GetStrokeIds()
                            .Select(id => _freeformInkControl.TargetInkCanvas.InkPresenter.StrokeContainer
                                .GetStrokeById(id)).ToList()
                    };
                    Rect lineRect = line.Strokes[0].BoundingRect;
                    line.Strokes.ForEach(stroke =>
                    {
                        lineRect.Union(originalSizes[stroke]);
                    });
                    line.BoundingRect = lineRect;
                    _recognizedLines.Add(line);
                }
            }
            foreach (var stroke in scaledStrokes)
            {
                stroke.PointTransform = Matrix3x2.Multiply(stroke.PointTransform, reverseScale);
            }
        }

        #region Delete with line

        /// <summary>
        /// Tries to delete documents and field connections intersected by a stroke, so long as the stroke is linear.
        /// </summary>
        /// <param name="newStroke"></param>
        private bool TryDeleteWithStroke(InkStroke newStroke)
        {
            var inkPoints = new List<Point>(newStroke.GetInkPoints().Select(p => Util.PointTransformFromVisual(
                p.Position, _freeformInkControl.SelectionCanvas,
                _freeformInkControl.FreeformView.xItemsControl.ItemsPanelRoot)));
            if (!IsLinear(inkPoints)) return false;
            var point1 = inkPoints[0];
            var point2 = inkPoints.Last();
            //Try using the new line to delete documents or links
            var deleted = DeleteIntersectingDocuments(inkPoints, point1, point2) ||
                          DeleteIntersectingConnections(point1, point2);
            //If they were deleted, remove the line.
            if (deleted)
            {
                _freeformInkControl.UndoSelection();
                newStroke.Selected = true;
                _freeformInkControl.TargetInkCanvas.InkPresenter.StrokeContainer.DeleteSelected();
                _analyzer.RemoveDataForStroke(newStroke.Id);
                _freeformInkControl.UpdateInkFieldModelController();
            }
            return deleted;
        }

        private bool DeleteIntersectingDocuments(List<Point> inkPoints, Point point1, Point point2)
        {
            bool deleted = false;
            Dictionary<Rect, DocumentView> rectToDocView = GetDocViewRects();
            foreach (var rect in rectToDocView.Keys)
            {
                if (point1.X < rect.X && point2.X > rect.X + rect.Width ||
                    point1.X > rect.X + rect.Width && point2.X < rect.X || point1.Y < rect.Y && point2.Y > rect.Y + rect.Height ||
                    point1.Y > rect.Y + rect.Height && point2.Y < rect.Y)
                    if (PathIntersectsRect(
                        inkPoints, rect))
                    {
                        _freeformInkControl.FreeformView.DeleteConnections(rectToDocView[rect]);
                        _freeformInkControl.FreeformView.ViewModel.RemoveDocument(rectToDocView[rect].ViewModel
                            .DocumentController);
                        deleted = true;
                    }
            }
            return deleted;
        }

        public bool DeleteIntersectingConnections(Point point1, Point point2)
        {
            bool lineDeleted = false;
            //Calculate line 1: the line of the ink stroke
            var slope1 = (point2.Y - point1.Y) / (point2.X - point1.X);
            var yInt1 = point1.Y - point1.X * slope1;
            var view = _freeformInkControl.FreeformView;
            var refsToLines = new Dictionary<FieldReference, Path>();
            foreach (var pair in view.RefToLine)
            {
                refsToLines[pair.Key] = pair.Value;
            }
            //iterate through reference:link/path pairs stored by collection
            foreach (var pair in refsToLines)
            {
                //Calculate line 2: an approximation of the bezier curve of the link
                var line = pair.Value;
                var converter = view.LineToConverter[line];
                var curvePoint1 = converter.Element1.TransformToVisual(view.xItemsControl.ItemsPanelRoot)
                    .TransformPoint(new Point(converter.Element1.ActualWidth / 2, converter.Element1.ActualHeight / 2));
                var curvePoint2 = converter.Element2.TransformToVisual(view.xItemsControl.ItemsPanelRoot)
                    .TransformPoint(new Point(converter.Element2.ActualWidth / 2, converter.Element2.ActualHeight / 2));
                var slope2 = (curvePoint2.Y - curvePoint1.Y) / (curvePoint2.X - curvePoint1.X);
                var yInt2 = curvePoint1.Y - curvePoint1.X * slope2;

                //Calculate intersection of the lines
                var intersectionX = (yInt2 - yInt1) / (slope1 - slope2);
                var intersectionY = slope1 * intersectionX + yInt1;
                var intersectionPoint = new Point(intersectionX, intersectionY);

                //if the intersection is on the two line segments, remove the path and the reference
                if (PointBetween(intersectionPoint, point1, point2) &&
                    PointBetween(intersectionPoint, curvePoint1, curvePoint2))
                {
                    var view2 = converter.Element2.GetFirstAncestorOfType<DocumentView>();
                    var view1 = converter.Element1.GetFirstAncestorOfType<DocumentView>();
                    var layoutDoc2 = view2.ViewModel.DocumentController;
                    var fields = layoutDoc2.EnumFields().ToImmutableList();
                    var key1 = view.LineToElementKeysDictionary[pair.Value].Item1;
                    var key2 = view.LineToElementKeysDictionary[pair.Value].Item2;
                    var dataRef = layoutDoc2.GetField(key2) as ReferenceFieldModelController;
                    var referencesEqual =
                        view1.ViewModel.KeysToFrameworkElements[key1].Equals(converter.Element1) && view2
                            .ViewModel.KeysToFrameworkElements[key2].Equals(converter.Element2);
                    if (referencesEqual && view.RefToLine.ContainsKey(pair.Key) && dataRef != null)
                    {
                        //Case where we have layout document and need to get dataDoc;
                        view.DeleteLine(pair.Key, view.RefToLine[pair.Key]);
                        var dataDoc = (layoutDoc2.GetField(KeyStore.DataKey) as ReferenceFieldModelController)?.GetDocumentController(new Context(layoutDoc2.GetDataDocument(null)));
                        if (dataDoc != null)
                        {
                            dataDoc.SetField(key2, dataRef
                                .DereferenceToRoot(new Context(dataDoc))
                                ?.GetCopy(), true);
                        }
                        else
                        {
                            //Case where what we thought was a layout doc is actually a data document with an active layout
                            layoutDoc2.SetField(key2, dataRef.DereferenceToRoot(new Context(layoutDoc2))?.GetCopy(),
                                true);
                        }
                        lineDeleted = true;
                    }
                }
            }
            return lineDeleted;
        }




        #endregion

        #region Add collection, doc, operator
        

        private DocumentController GetCollectionFromShapeRegion(RecognizedShapeNode node)
        {
            List<DocumentController> recognizedDocuments = new List<DocumentController>();
            //Look for rectangles inside ellipse and add them as docs to collection
            var region = node.InkRegion as InkAnalysisInkDrawing;
            if (region == null) return null;
            if (node.Children != null)
            {
                foreach (var child in node.Children)
                {
                    var doc = GetDocFromRecognizedNode(child);
                    if(doc != null) recognizedDocuments.Add(doc);
                }
            }

            recognizedDocuments.AddRange(GetContainedDocuments(region));
            var topLeft = new Point(region.BoundingRect.X, region.BoundingRect.Y);
            var position = Util.PointTransformFromVisual(topLeft, _freeformInkControl.SelectionCanvas,
                _freeformInkControl.FreeformView.xItemsControl.ItemsPanelRoot as Canvas);
            foreach (var doc in recognizedDocuments)
            {
                var ogPos = doc.GetPositionField().Data;
                var newPos = Util.PointTransformFromVisual(ogPos,
                    _freeformInkControl.FreeformView.xItemsControl.ItemsPanelRoot, _freeformInkControl.SelectionCanvas);
                var relativePos = new Point(newPos.X - topLeft.X, newPos.Y - topLeft.Y);
                doc.GetPositionField().Data = relativePos;
                DocumentView documentView = _freeformInkControl.FreeformView.GetDocView(doc);
                if (documentView != null)
                {
                    _freeformInkControl.FreeformView.DeleteConnections(documentView);
                }
                _freeformInkControl.FreeformView.ViewModel.RemoveDocument(doc);
            }

            var documentController = Util.BlankCollection();
            documentController.SetField(KeyStore.CollectionKey,
                new DocumentCollectionFieldModelController(recognizedDocuments), true);
            documentController.SetActiveLayout(
                new CollectionBox(
                    new DocumentReferenceFieldController(documentController.GetId(),
                        KeyStore.CollectionKey), position.X, position.Y, region.BoundingRect.Width,
                    region.BoundingRect.Height).Document, true, true);
            DeleteStrokesByID(region.GetStrokeIds().ToImmutableHashSet());
            return documentController;
        }

        private List<DocumentController> GetContainedDocuments(InkAnalysisInkDrawing region)
        {
            //var lassoPoints = new List<Point>(GetPointsFromStrokeIDs(region.GetStrokeIds())
            //                .Select(p => Util.PointTransformFromVisual(p, FreeformInkControl.SelectionCanvas,
            //                    FreeformInkControl.FreeformView.xItemsControl.ItemsPanelRoot as Canvas)));
            var lassoPoints = region.Points.ToList();
            var selectedDocuments = _freeformInkControl.LassoHelper.GetSelectedDocuments(lassoPoints).Select(view => view.ViewModel.DocumentController).ToList();
            return selectedDocuments;
        }

        private DocumentController GetDocumentFromShapeRegion(RecognizedShapeNode node)
        {
            var region = node.InkRegion as InkAnalysisInkDrawing;
            if (region == null) return null;
            DeleteStrokesByID(region.GetStrokeIds().ToImmutableHashSet());
            //Analyzer.RemoveDataForStrokes(region.GetStrokeIds());
            var topLeft = new Point(region.BoundingRect.X, region.BoundingRect.Y);
            var size = new Size(region.BoundingRect.Width, region.BoundingRect.Height);
            var position = Util.PointTransformFromVisual(topLeft, _freeformInkControl.SelectionCanvas,
                _freeformInkControl.FreeformView.xItemsControl.ItemsPanelRoot as Canvas);
            var fields = new Dictionary<KeyController, FieldControllerBase>();
            var doc = new DocumentController(fields, DocumentType.DefaultType);
            var layoutDocs = new List<DocumentController>();
            int fieldIndex = 0;
            ListFieldModelController<TextFieldModelController> list = null;
            // try making fields from at least partially intersected lines
            var intersectionEnumerable = new List<LineNode>();
            foreach (var line in _recognizedLines)
            {
                if (node.InkRegion.BoundingRect.IntersectsWith(line.BoundingRect))
                {
                    intersectionEnumerable.Add(line);
                }
            }
            if (intersectionEnumerable.Count > 0) list = new ListFieldModelController<TextFieldModelController>();
            foreach (var inkLine in intersectionEnumerable)
            {
                var containedWords = GetContainedWords(inkLine, region.BoundingRect);
                if (containedWords.Count > 0)
                {
                    string containedLine = "";
                    Rect containedRect = containedWords[0].BoundingRect;
                    foreach (var word in containedWords)
                    {
                        containedLine += " " + word.Text;
                        DeleteStrokesByID(word.Strokes.Select(stroke => stroke.Id).ToImmutableHashSet());
                        containedRect.Union(word.BoundingRect);
                        containedRect.X = Math.Min(containedRect.X, word.BoundingRect.X);
                        containedRect.Y = Math.Min(containedRect.Y, word.BoundingRect.Y);
                    }
                    if (containedLine != "")
                    {
                        TryGetText(containedLine, out string text, out KeyController key,
                            intersectionEnumerable.Count() > 1 ? (++fieldIndex).ToString() : "");
                        var relativePosition = new Point(containedRect.X - topLeft.X, containedRect.Y - topLeft.Y);
                        doc.ParseDocField(key, text);
                        var field = doc.GetField(key);
                        if (field != null)
                        {
                            list.Add(field);
                        }
                        var textBox = new TextingBox(new DocumentReferenceFieldController(doc.GetId(), key),
                            relativePosition.X, relativePosition.Y, containedRect.Width, containedRect.Height);
                        (textBox.Document.GetField(TextingBox.FontSizeKey) as NumberFieldModelController).Data =
                            containedRect.Height / 1.5;
                        layoutDocs.Add(textBox.Document);
                    }
                }
            }
            if (list != null)
            {
                doc.SetField(KeyStore.ParsedFieldKey, list, true);
            }
            var layout = new FreeFormDocument(layoutDocs,
                position, size).Document;
            doc.SetActiveLayout(layout, true, true);
            //if (addToFreeformView) FreeformInkControl.FreeformView.ViewModel.AddDocument(doc, null);
            return doc;
        }

        private List<WordNode> GetContainedWords(LineNode writing, Rect docBounds)
        {
            var containedWords = new List<WordNode>();
            containedWords.AddRange(writing.Children.Where(word => docBounds.Contains(word.BoundingRect)));
            return containedWords;
        }

        #endregion

        #region InkStroke data helpers
        
        private void DeleteStrokesByID(ICollection<uint> IDs)
        {
            _freeformInkControl.UndoSelection();
            foreach (var stroke in _freeformInkControl.TargetInkCanvas.InkPresenter.StrokeContainer.GetStrokes())
                if (IDs.Contains(stroke.Id))
                    stroke.Selected = true;
            _freeformInkControl.TargetInkCanvas.InkPresenter.StrokeContainer.DeleteSelected();
        }

        private void AddDataForSelectedStrokes()
        {
            var selectedStrokes = _freeformInkControl.TargetInkCanvas.InkPresenter.StrokeContainer.GetStrokes()
                .Where(stroke => stroke.Selected).ToList();
            _analyzer.AddDataForStrokes(selectedStrokes);
            _remainingStrokes.AddRange(selectedStrokes);
        }


        #endregion

        #region Helper methods
        private async Task<List<InkAnalysisInkDrawing>> OrderedShapeList()
        {
            HashSet<InkAnalysisInkDrawing> unorderedShapes = new HashSet<InkAnalysisInkDrawing>();
            for (int i = 0; i < 3; i++)
            {
                await _analyzer.AnalyzeAsync();
                var shapes = _analyzer.AnalysisRoot.FindNodes(InkAnalysisNodeKind.InkDrawing)
                    .OfType<InkAnalysisInkDrawing>()
                    .Where(drawing => drawing.DrawingKind == InkAnalysisDrawingKind.Circle ||
                                      drawing.DrawingKind == InkAnalysisDrawingKind.Ellipse ||
                                      drawing.DrawingKind == InkAnalysisDrawingKind.Square ||
                                      drawing.DrawingKind == InkAnalysisDrawingKind.Rectangle);
                foreach (var shape in shapes)
                {
                    var ids = shape.GetStrokeIds();
                    //Remove data from analyzer *after* using stroke ids from region otherwise stuff gets messed up
                    DeleteStrokesByID(ids.ToImmutableHashSet());
                    _remainingStrokes.RemoveAll(stroke => ids.Contains(stroke.Id));
                    _analyzer.RemoveDataForStrokes(shape.GetStrokeIds());
                    unorderedShapes.Add(shape);
                }
            }
            var orderedShapes = unorderedShapes.OrderBy(shape => shape.BoundingRect.Width * shape.BoundingRect.Height).ToList();
            return orderedShapes;
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
                var newPoint = new Point(Math.Cos(currAngle + Math.PI / 4) * dist + ctrPoint.X, Math.Sin(currAngle + Math.PI / 4) * dist + ctrPoint.Y);
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
            IEnumerable<DocumentViewModel> parameters = _freeformInkControl.FreeformView.xItemsControl.Items.OfType<DocumentViewModel>();
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
                if (_freeformInkControl.FreeformView.xItemsControl.ItemContainerGenerator != null && _freeformInkControl
                        .FreeformView.xItemsControl
                        .ContainerFromItem(param) is ContentPresenter contentPresenter)
                    dict[rect] =
                        contentPresenter.GetFirstDescendantOfType<DocumentView>();
            }
            return dict;
        }

        private void TryGetText(string str, out string value, out KeyController key, string suffix)
        {
            if (str.Contains(':'))
            {
                var splitstring = str.Split(':');
                value = splitstring[1].TrimEnd(' ').TrimStart(' ');
                string keystring = splitstring[0].TrimEnd(' ').TrimStart(' ');
                key = new KeyController(Guid.NewGuid().ToString(), keystring);
            }
            else
            {
                value = str;
                key = new KeyController(Guid.NewGuid().ToString(), $"Document Field {suffix}");
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

        #endregion
        
    }
    public class RecognizedShapeTree
    {
        public RecognizedShapeNode Root;
        public HashSet<uint> UsedStrokeIDs;
        public RecognizedShapeTree(InkAnalysisRoot root, List<InkAnalysisInkDrawing> orderedNodes)
        {
            UsedStrokeIDs = new HashSet<uint>();
            var nodeDict = new Dictionary<IInkAnalysisNode, RecognizedShapeNode>();
            for (int i = 0; i < orderedNodes.Count; i++)
            {
                var node = orderedNodes[i];
                foreach (var strokeId in node.GetStrokeIds()) UsedStrokeIDs.Add(strokeId);
                var recognizedNode = new RecognizedShapeNode(node);
                nodeDict[node] = recognizedNode;
                if(recognizedNode.IsEllipse)
                foreach (var smallerNode in orderedNodes.GetRange(0, i))
                {
                    if (nodeDict[smallerNode].Parent != null || !node.BoundingRect.Contains(smallerNode.BoundingRect)) continue;
                    recognizedNode.Children.Add(nodeDict[smallerNode]);
                    nodeDict[smallerNode].Parent = recognizedNode;
                }
            }
            Root = new RecognizedShapeNode(root);
            Root.Children.AddRange(nodeDict.Values.Where(n => n.Parent == null));
        }


    }

    public class RecognizedShapeNode
    {
        public List<RecognizedShapeNode> Children;
        public IInkAnalysisNode InkRegion;
        public RecognizedShapeNode Parent;

        public bool IsRectangle
        {
            get
            {
                var drawing = InkRegion as InkAnalysisInkDrawing;
                return drawing != null && (drawing.DrawingKind == InkAnalysisDrawingKind.Square ||
                       drawing.DrawingKind == InkAnalysisDrawingKind.Rectangle);
            }
        }

        public bool IsEllipse
        {
            get
            {
                var drawing = InkRegion as InkAnalysisInkDrawing;
                return drawing != null && (drawing.DrawingKind == InkAnalysisDrawingKind.Circle ||
                                           drawing.DrawingKind == InkAnalysisDrawingKind.Ellipse);
            }
        }

        public RecognizedShapeNode(IInkAnalysisNode inkRegion)
        {
            InkRegion = inkRegion;
            Children = new List<RecognizedShapeNode>();
        }
    }

    public struct LineNode
    {
        public string Text;
        public Rect BoundingRect;
        public List<InkStroke> Strokes;
        public List<WordNode> Children;
    }

    public struct WordNode
    {
        public string Text;
        public Rect BoundingRect;
        public List<InkStroke> Strokes;
    }
}