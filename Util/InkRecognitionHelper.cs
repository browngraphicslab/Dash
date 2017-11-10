using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Media.Devices;
using Windows.UI.Input.Inking;
using Windows.UI.Input.Inking.Analysis;
using Windows.UI.Xaml;
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
        public FreeformInkControl FreeformInkControl { get; set; }
        public Dictionary<Rect, InkAnalysisLine> TextBoundsDictionary { get; set; }
        public InkAnalyzer Analyzer { get; set; }
        public List<Point> DoubleTappedPoints { get; set; }
        private InkStroke NewStroke;

        private RecognizedInkTree RecognizedInkTree;

        public InkRecognitionHelper()
        {
            Analyzer = new InkAnalyzer();
        }

        /// <summary>
        /// Tries to find documents or collections at the double tap point, or delete using the newest stroke.
        /// Sets the state to keep track of the ink control we're recognizing for and the newest stroke added to it.
        /// </summary>
        /// <param name="onlySelectedStrokes"></param>
        public async void RecognizeInk(InkStroke newStroke, FreeformInkControl inkControl, bool onlySelectedStrokes)
        {
            FreeformInkControl = inkControl;
            NewStroke = newStroke;
            //Tries to delete with a line stroke, so long as we're not doing forced recognition from selected strokes.
            if (newStroke != null && !onlySelectedStrokes && TryDeleteWithStroke(newStroke))
                return;
            //if we're not only analyzing selected strokes, we want to analyze all strokes within the range of the most recent stroke
            //else only analyze selected strokes
            Analyzer.ClearDataForAllStrokes();
            List<DocumentController> recognizedDocs;
            if (!onlySelectedStrokes)
            {
                recognizedDocs = await GetDocsFromNewStroke();
            }
            else
            {
                recognizedDocs = await GetDocsFromSelection();
            }
            FreeformInkControl.FreeformView.ViewModel.AddDocuments(recognizedDocs, null);
            FreeformInkControl.UpdateInkFieldModelController();
        }

        private async Task<List<DocumentController>> GetDocsFromNewStroke()
        {
            var docs = new List<DocumentController>();
            foreach (var inkStroke in FreeformInkControl.TargetInkCanvas.InkPresenter.StrokeContainer.GetStrokes().Where(inkStroke => NewStroke.BoundingRect.Contains(inkStroke.BoundingRect)))
            {
                Analyzer.AddDataForStroke(inkStroke);
            }
            var result = await Analyzer.AnalyzeAsync();
            if (result.Status == InkAnalysisStatus.Updated)
            {
                var root = Analyzer.AnalysisRoot;
                RecognizedInkTree = new RecognizedInkTree(root);
                foreach (var child in RecognizedInkTree.Root.Children)
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
            var result = await Analyzer.AnalyzeAsync();
            if (result.Status == InkAnalysisStatus.Updated)
            {
                var root = Analyzer.AnalysisRoot;
                RecognizedInkTree = new RecognizedInkTree(root);
                foreach (var child in RecognizedInkTree.Root.Children)
                {
                    var doc = GetDocFromRecognizedNode(child);
                    if (doc != null) docs.Add(doc);
                }
            }
            return docs;
        }

        private DocumentController GetDocFromRecognizedNode(RecognizedInkNode node)
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

        #region Delete with line

        /// <summary>
        /// Tries to delete documents and field connections intersected by a stroke, so long as the stroke is linear.
        /// </summary>
        /// <param name="newStroke"></param>
        private bool TryDeleteWithStroke(InkStroke newStroke)
        {
            var inkPoints = new List<Point>(newStroke.GetInkPoints().Select(p => Util.PointTransformFromVisual(
                p.Position, FreeformInkControl.SelectionCanvas,
                FreeformInkControl.FreeformView.xItemsControl.ItemsPanelRoot)));
            if (!IsLinear(inkPoints)) return false;
            var point1 = inkPoints[0];
            var point2 = inkPoints.Last();
            //Try using the new line to delete documents or links
            var deleted = DeleteIntersectingDocuments(inkPoints, point1, point2) ||
                          DeleteIntersectingConnections(point1, point2);
            //If they were deleted, remove the line.
            if (deleted)
            {
                FreeformInkControl.UndoSelection();
                newStroke.Selected = true;
                FreeformInkControl.TargetInkCanvas.InkPresenter.StrokeContainer.DeleteSelected();
                Analyzer.RemoveDataForStroke(newStroke.Id);
                FreeformInkControl.UpdateInkFieldModelController();
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
                        FreeformInkControl.FreeformView.DeleteConnections(rectToDocView[rect]);
                        FreeformInkControl.FreeformView.ViewModel.RemoveDocument(rectToDocView[rect].ViewModel
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
            var view = FreeformInkControl.FreeformView;
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

        private Dictionary<Rect, InkAnalysisLine> GetTextBoundsAndRemoveData()
        {
            var textLineRegions = new List<InkAnalysisLine>(Analyzer.AnalysisRoot.FindNodes(InkAnalysisNodeKind.Line)
                .Select(o => o as InkAnalysisLine));
            var textBoundsDictionary = new Dictionary<Rect, InkAnalysisLine>();
            foreach (var textLine in textLineRegions)
            {
                textBoundsDictionary[textLine.BoundingRect] = textLine;
            }
            return textBoundsDictionary;
        }

        private DocumentController GetCollectionFromShapeRegion(RecognizedInkNode node)
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
            var position = Util.PointTransformFromVisual(topLeft, FreeformInkControl.SelectionCanvas,
                FreeformInkControl.FreeformView.xItemsControl.ItemsPanelRoot as Canvas);
            foreach (var doc in recognizedDocuments)
            {
                var ogPos = doc.GetPositionField().Data;
                var newPos = Util.PointTransformFromVisual(ogPos,
                    FreeformInkControl.FreeformView.xItemsControl.ItemsPanelRoot, FreeformInkControl.SelectionCanvas);
                var relativePos = new Point(newPos.X - topLeft.X, newPos.Y - topLeft.Y);
                doc.GetPositionField().Data = relativePos;
                DocumentView documentView = FreeformInkControl.FreeformView.GetDocView(doc);
                if (documentView != null)
                {
                    FreeformInkControl.FreeformView.DeleteConnections(documentView);
                }
                FreeformInkControl.FreeformView.ViewModel.RemoveDocument(doc);
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
            var selectedDocuments = FreeformInkControl.LassoHelper.GetSelectedDocuments(lassoPoints).Select(view => view.ViewModel.DocumentController).ToList();
            return selectedDocuments;
        }

        private DocumentController GetDocumentFromShapeRegion(RecognizedInkNode node)
        {
            var region = node.InkRegion as InkAnalysisInkDrawing;
            if (region == null) return null;
            DeleteStrokesByID(region.GetStrokeIds().ToImmutableHashSet());
            Analyzer.RemoveDataForStrokes(region.GetStrokeIds());
            var topLeft = new Point(region.BoundingRect.X, region.BoundingRect.Y);
            var size = new Size(region.BoundingRect.Width, region.BoundingRect.Height);
            var position = Util.PointTransformFromVisual(topLeft, FreeformInkControl.SelectionCanvas,
                FreeformInkControl.FreeformView.xItemsControl.ItemsPanelRoot as Canvas);
            var fields = new Dictionary<KeyController, FieldControllerBase>();
            var doc = new DocumentController(fields, DocumentType.DefaultType);
            var layoutDocs = new List<DocumentController>();
            int fieldIndex = 0;
            ListFieldModelController<TextFieldModelController> list = null;
            // try making fields from at least partially intersected lines
            var intersectionEnumerable = new List<InkAnalysisLine>();
            foreach (var writing in node.Children.Where(child => child.IsWriting).Select(child => child.InkRegion).OfType<InkAnalysisWritingRegion>())
            {
                foreach (var paragraph in writing.Children.OfType<InkAnalysisParagraph>())
                {
                    foreach (var line in paragraph.Children.OfType<InkAnalysisLine>())
                    {
                        if (node.InkRegion.BoundingRect.IntersectsWith(line.BoundingRect))
                        {
                            intersectionEnumerable.Add(line);   
                        }
                    }
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
                        containedLine += " " + word.RecognizedText;
                        DeleteStrokesByID(word.GetStrokeIds().ToImmutableHashSet());
                        Analyzer.RemoveDataForStrokes(word.GetStrokeIds());
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

        private List<InkAnalysisInkWord> GetContainedWords(InkAnalysisLine writing, Rect docBounds)
        {
            var containedWords = new List<InkAnalysisInkWord>();
            containedWords.AddRange(Analyzer.AnalysisRoot.FindNodes(InkAnalysisNodeKind.InkWord).OfType<InkAnalysisInkWord>()
                .Where(word => docBounds.Contains(word.BoundingRect) && word.Parent.Id == writing.Id).ToList());
            return containedWords;
        }

        #endregion

        #region InkStroke data helpers

        private List<Point> GetPointsFromStrokeIDs(IEnumerable<uint> ids)
        {
            var points = new List<Point>();
            foreach (var id in ids)
                points.AddRange(FreeformInkControl.TargetInkCanvas.InkPresenter.StrokeContainer.GetStrokeById(id)
                    .GetInkPoints().Select(inkPoint => inkPoint.Position));
            return points;
        }
        private void DeleteStrokesByID(ICollection<uint> IDs)
        {
            FreeformInkControl.UndoSelection();
            foreach (var stroke in FreeformInkControl.TargetInkCanvas.InkPresenter.StrokeContainer.GetStrokes())
                if (IDs.Contains(stroke.Id))
                    stroke.Selected = true;
            FreeformInkControl.TargetInkCanvas.InkPresenter.StrokeContainer.DeleteSelected();
        }

        private void AddDataForSelectedStrokes()
        {
            Analyzer.AddDataForStrokes(FreeformInkControl.TargetInkCanvas.InkPresenter.StrokeContainer.GetStrokes().Where(stroke => stroke.Selected));
        }

        private bool RegionContainsNewStroke(IInkAnalysisNode region)
        {
            return region.GetStrokeIds()
                .Any(id => NewStroke.Id == id);
        }
        

        #endregion

        #region Helper methods

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
    public class RecognizedInkTree
    {
        public RecognizedInkNode Root;
        public RecognizedInkTree(InkAnalysisRoot root)
        {
            var nodeDict = new Dictionary<IInkAnalysisNode, RecognizedInkNode>();
            var orderedNodes = root.Children.OrderBy(node => node.BoundingRect.Width * node.BoundingRect.Height).ToList();
            for (int i = 0; i < orderedNodes.Count; i++)
            {
                var node = orderedNodes[i];
                var recognizedNode = new RecognizedInkNode(node);
                nodeDict[node] = recognizedNode;
                if (recognizedNode.IsEllipse || recognizedNode.IsRectangle)
                {
                    foreach (var smallerNode in orderedNodes.GetRange(0, i))
                    {
                        if (nodeDict[smallerNode].Parent != null) continue;
                        if (recognizedNode.IsRectangle && nodeDict[smallerNode].IsWriting && node.BoundingRect.IntersectsWith(smallerNode.BoundingRect))
                        {
                            recognizedNode.Children.Add(nodeDict[smallerNode]);
                        }
                        if(recognizedNode.IsEllipse && (nodeDict[smallerNode].IsRectangle || nodeDict[smallerNode].IsEllipse) && node.BoundingRect.Contains(smallerNode.BoundingRect))
                        {
                            recognizedNode.Children.Add(nodeDict[smallerNode]);
                            nodeDict[smallerNode].Parent = recognizedNode;
                        }
                    }
                }
            }
            Root = new RecognizedInkNode(root);
            Root.Children.AddRange(nodeDict.Values.Where(n => n.Parent == null && !n.IsWriting));
        }


    }

    public class RecognizedInkNode
    {
        public List<RecognizedInkNode> Children;
        public IInkAnalysisNode InkRegion;
        public RecognizedInkNode Parent;

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

        public bool IsWriting => InkRegion is InkAnalysisInkWord || InkRegion is InkAnalysisLine ||
                                 InkRegion is InkAnalysisParagraph || InkRegion is InkAnalysisWritingRegion;

        public RecognizedInkNode(IInkAnalysisNode inkRegion)
        {
            InkRegion = inkRegion;
            Children = new List<RecognizedInkNode>();
        }


    }
}