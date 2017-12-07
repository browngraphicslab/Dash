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
    public sealed class InkRecognitionSubHelper
    {
        private readonly InkAnalyzer _analyzer;
        private FreeformInkControl _freeformInkControl;
        private InkStroke _newStroke;
        private RecognizedShapeTree _recognizedShapeTree;
        private List<InkStroke> _analysisTargetStrokes;
        private List<LineNode> _recognizedLines;
        public LassoSelectHelper LassoSelectHelper;

        public InkRecognitionSubHelper(InkAnalyzer analyzer, InkStroke newStroke, FreeformInkControl inkControl)
        {
            _analyzer = analyzer;
            _newStroke = newStroke;
            _freeformInkControl = inkControl;
        }

        /// <summary>
        /// Tries to find documents or collections at the double tap point, or delete using the newest stroke.
        /// Sets the state to keep track of the ink control we're recognizing for and the newest stroke added to it.
        /// </summary>
        /// <param name="onlySelectedStrokes"></param>
        public async void RecognizeInk( bool onlySelectedStrokes)
        {
            //Tries to delete with a line stroke, so long as we're not doing forced recognition from selected strokes.
            if (_newStroke != null && !onlySelectedStrokes && TryDeleteWithStroke(_newStroke))
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
            _freeformInkControl.UpdateInkController();
        }
        private async Task<List<DocumentController>> GetDocsFromNewStroke()
        {
            _analyzer.AddDataForStrokes(_analysisTargetStrokes =
                _freeformInkControl.TargetInkCanvas.InkPresenter.StrokeContainer.GetStrokes()
                    .Where(inkStroke => _newStroke.BoundingRect.Contains(inkStroke.BoundingRect))
                    .Union(new List<InkStroke>{ _newStroke }).ToList());
            return await GetDocs();
        }

        private async Task<List<DocumentController>> GetDocsFromSelection()
        {
            _analyzer.AddDataForStrokes(_analysisTargetStrokes = _freeformInkControl.TargetInkCanvas.InkPresenter
                .StrokeContainer.GetStrokes()
                .Where(stroke => stroke.Selected).ToList());
            return await GetDocs();
        }

        #region Higher-Level/Organization Methods

        private async Task<List<DocumentController>> GetDocs()
        {
            var docs = new List<DocumentController>();
            var result = await _analyzer.AnalyzeAsync();
            if (result.Status == InkAnalysisStatus.Updated)
            {
                //Get rectangles with contained text
                //Get ellipses
                //Order list of rectangles and ellipses by size
                //Assign parents to smaller regions 
                //Get docs from regions without parents
            }
            return docs;
        }

        private async Task<List<RecognizedNode>> GetDocumentsFromRectangles()
        {
            InkAnalysisResult result;
            List<RecognizedNode> documentNodes = new List<RecognizedNode>();
            for (int i = 0; i < 3; i++)
            {
                result = await _analyzer.AnalyzeAsync();
                if (result.Status == InkAnalysisStatus.Updated)
                {
                    var rectangles = _analyzer.AnalysisRoot.FindNodes(InkAnalysisNodeKind.InkDrawing)
                        .OfType<InkAnalysisInkDrawing>()
                        .Where(d => d.DrawingKind == InkAnalysisDrawingKind.Rectangle ||
                                    d.DrawingKind == InkAnalysisDrawingKind.Square).ToList();
                    foreach (var rectangle in rectangles)
                    {
                        //Make document node
                        var strokeIds = rectangle.GetStrokeIds();
                        documentNodes.Add(new RecognizedNode
                        {
                            InkRegion = rectangle,
                            Strokes = strokeIds.Select(GetStrokeByID).ToList(),
                            NodeType = RecognizedNode.RecognizedNodeType.Document,
                            Parent = null,
                            BoundingRect = rectangle.BoundingRect
                        });
                        RemoveStrokes(strokeIds);
                    }
                }
            }
            foreach (var docNode in documentNodes.OrderBy(d => d.InkRegion.BoundingRect.Height * d.InkRegion.BoundingRect.Width))
            {
                _analyzer.ClearDataForAllStrokes();
                var containedStrokes =
                    _analysisTargetStrokes.Where(stroke => docNode.InkRegion.BoundingRect.Contains(stroke.BoundingRect));
                if (containedStrokes.Count() > 0)
                {
                    _analyzer.AddDataForStrokes(containedStrokes);
                    result = await _analyzer.AnalyzeAsync();
                    if (result.Status == InkAnalysisStatus.Updated)
                    {
                        FindLinesInDocument(docNode);
                    }
                }

            }
            return documentNodes;
        }

        

        private async void FindLinesInDocument(RecognizedNode docNode)
        {
            var heightCutoff = 75;
            //Get all InkLines within permitted size bounds and remove their strokes from RemainingStrokes
            List<RecognizedNode> nodes = new List<RecognizedNode>();
            var inkLines = _analyzer.AnalysisRoot.FindNodes(InkAnalysisNodeKind.InkWord)
                .Where(word => docNode.BoundingRect.Contains(word.BoundingRect)).Select(word => word.Parent)
                .OfType<InkAnalysisLine>().ToList();
            foreach (var line in inkLines)
            {
                var containedWords = line.Children.OfType<InkAnalysisInkWord>()
                    .Where(word => docNode.BoundingRect.Contains(word.BoundingRect));
                var node = containedWords.Select(word => new RecognizedNode
                {
                    RecognizedText = word.RecognizedText,
                    Strokes = word.GetStrokeIds().Select(id => GetStrokeByID(id)).ToList(),
                    BoundingRect = word.BoundingRect,
                    Parent = docNode,
                    NodeType = RecognizedNode.RecognizedNodeType.Text,
                    InkRegion = line
                }).Aggregate((node1, node2) =>
                {
                    node1.BoundingRect.Union(node2.BoundingRect);
                    node1.Strokes.Union(node2.Strokes);
                    node1.RecognizedText += " " + node2.RecognizedText;
                    return node1;
                });
            }

            //foreach (var line in smallLines)
            //{
            //    RecognizedNode textNode = line.Children.OfType<InkAnalysisInkWord>().Where(word => docNode.BoundingRect.Contains(word.BoundingRect)).Select(word => new RecognizedNode{RecognizedText = word.RecognizedText, Strokes = word.GetStrokeIds().Select(id => GetStrokeByID(id)).ToList(), BoundingRect = word.BoundingRect, Parent = docNode, NodeType = RecognizedNode.RecognizedNodeType.Text, InkRegion = line}).Aggregate((node1, node2) => {node1.BoundingRect.Union(node2.BoundingRect); node1.Strokes.Union(node2.Strokes);node1.RecognizedText += " " + node2.RecognizedText; return node1;});

            //}
            if (_analysisTargetStrokes.Count == 0) return;
            double maxHeight = _analysisTargetStrokes.Max(stroke => stroke.BoundingRect.Height);
            double y = _analysisTargetStrokes.Min(stroke => stroke.BoundingRect.Y);
            double x = _analysisTargetStrokes.Min(stroke => stroke.BoundingRect.X);
            double scaleFactor = heightCutoff / maxHeight;
            Matrix3x2 scaleMatrix = Matrix3x2.CreateScale((float)scaleFactor,
                new Vector2((float)x, (float)y));
            Matrix3x2 reverseScale = Matrix3x2.CreateScale((float)(1 / scaleFactor),
                new Vector2((float)x, (float)y));
            List<InkStroke> scaledStrokes = new List<InkStroke>(_analysisTargetStrokes);
            Dictionary<InkStroke, Rect> originalSizes = new Dictionary<InkStroke, Rect>();
            List<InkStroke> resizedStrokes = new List<InkStroke>();
            foreach (var stroke in _analysisTargetStrokes.Where(stroke => docNode.BoundingRect.Contains(stroke.BoundingRect)))
            {
                originalSizes[stroke] = stroke.BoundingRect;
                stroke.PointTransform = Matrix3x2.Multiply(stroke.PointTransform, scaleMatrix);
                resizedStrokes.Add(stroke);
            }
            _analyzer.ClearDataForAllStrokes();
            _analyzer.AddDataForStrokes(resizedStrokes);
            var result = await _analyzer.AnalyzeAsync();
            if (result.Status == InkAnalysisStatus.Updated)
            {
                var inkLines = new List<InkAnalysisLine>();
                foreach (var inkWord in _analyzer.AnalysisRoot.FindNodes(InkAnalysisNodeKind.InkWord))
                {
                    var rect = inkWord.GetStrokeIds().Select(id => originalSizes[GetStrokeByID(id)])
                        .Aggregate((rect1, rect2) =>
                        {
                            rect1.Union(rect2);
                            return rect1;
                        });
                    inkLines.Add(inkWord.Parent as InkAnalysisLine);
                }
                MakeScaledTextNodes(originalSizes, inkLines);
            }
            foreach (var stroke in scaledStrokes)
            {
                stroke.PointTransform = Matrix3x2.Multiply(stroke.PointTransform, reverseScale);
            }
        }


        private void MakeScaledTextNodes(Dictionary<InkStroke, Rect> originalSizes, IEnumerable<InkAnalysisLine> inkLines)
        {
            foreach (var inkLine in inkLines)
            {
                if(inkLine.RecognizedText.ToLower() == "o") continue;
                var ids = inkLine.GetStrokeIds();
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
                _analysisTargetStrokes.RemoveAll(stroke => ids.Contains(stroke.Id));
                _recognizedLines.Add(line);
                DeleteStrokesById(ids);
                _analyzer.RemoveDataForStrokes(ids);
            }
        }

        private void MakeTextNodes(List<InkAnalysisLine> smallLines)
        {
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
                _analysisTargetStrokes.RemoveAll(stroke => ids.Contains(stroke.Id));
                _analyzer.RemoveDataForStrokes(ids);
            }
        }

        #endregion


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
            bool docsDeleted = DeleteIntersectingDocuments(inkPoints, point1, point2);
            bool linesDeleted = DeleteIntersectingConnections(point1, point2);
            bool deleted = docsDeleted || linesDeleted;
            //If they were deleted, remove the line.
            if (deleted)
            {
                _freeformInkControl.UndoSelection();
                newStroke.Selected = true;
                _freeformInkControl.TargetInkCanvas.InkPresenter.StrokeContainer.DeleteSelected();
                _analyzer.RemoveDataForStroke(newStroke.Id);
                _freeformInkControl.UpdateInkController();
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
            var refsToLines = new Dictionary<FieldReference, Path>(view.RefToLine);
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
                    var key1 = view.LineToElementKeysDictionary[pair.Value].Item1;
                    var key2 = view.LineToElementKeysDictionary[pair.Value].Item2;
                    var dataRef = layoutDoc2.GetField(key2) as ReferenceController;
                    var referencesEqual =
                        view1.ViewModel.KeysToFrameworkElements[key1].Equals(converter.Element1) && view2
                            .ViewModel.KeysToFrameworkElements[key2].Equals(converter.Element2);
                    if (referencesEqual && view.RefToLine.ContainsKey(pair.Key) && dataRef != null)
                    {
                        //Case where we have layout document and need to get dataDoc;
                        view.DeleteLine(pair.Key, view.RefToLine[pair.Key]);
                        var dataDoc = (layoutDoc2.GetField(KeyStore.DataKey) as ReferenceController)?.GetDocumentController(new Context(layoutDoc2.GetDataDocument(null)));
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



        #region Helper methods

        private void RemoveStrokes(IReadOnlyList<uint> strokeIds)
        {
            DeleteStrokesById(strokeIds);
            _analysisTargetStrokes.RemoveAll(stroke => strokeIds.Contains(stroke.Id));
            _analyzer.RemoveDataForStrokes(strokeIds);
        }

        private void DeleteStrokesById(IEnumerable<uint> ids)
        {
            _freeformInkControl.UndoSelection();
            foreach (var inkStroke in ids.Select(id => _freeformInkControl.TargetInkCanvas.InkPresenter.StrokeContainer.GetStrokeById(id)))
            {
                inkStroke.Selected = true;
            }
            _freeformInkControl.TargetInkCanvas.InkPresenter.StrokeContainer.DeleteSelected();
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
            var ctrPoint = points[points.Count / 2];
            foreach (var point in points)
            {
                var dx = point.X - ctrPoint.X;
                var dy = point.Y - ctrPoint.Y;
                var dist = Math.Sqrt(Math.Pow(dx, 2) + Math.Pow(dy, 2));
                var currAngle = Math.Atan2(dy, dx);
                if (dx < 0) currAngle += Math.PI;
                var newPoint = new Point(Math.Cos(currAngle + Math.PI / 4) * dist + ctrPoint.X,
                    Math.Sin(currAngle + Math.PI / 4) * dist + ctrPoint.Y);
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

        public InkStroke GetStrokeByID(uint id)
        {
            return _freeformInkControl.TargetInkCanvas.InkPresenter.StrokeContainer.GetStrokeById(id);
        }

        #endregion

    }

    public class RecognizedNode
    {
        public List<RecognizedNode> Children;
        public IInkAnalysisNode InkRegion;
        public RecognizedNode Parent;
        public RecognizedNodeType NodeType;
        public List<InkStroke> Strokes;
        public string RecognizedText;
        public Rect BoundingRect;

        public enum RecognizedNodeType
        {
            Document,
            Collection,
            Text
        }
    }

}