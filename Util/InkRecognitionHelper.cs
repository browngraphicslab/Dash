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
        private List<IInkAnalysisNode> _shapeRegions;
        public FreeformInkControl FreeformInkControl { get; set; }
        public Dictionary<Rect, InkAnalysisLine> TextBoundsDictionary { get; set; }
        public InkAnalyzer Analyzer { get; set; }
        public List<Point> DoubleTappedPoints { get; set; }
        public List<InkStroke> NewStrokes { get; set; }

        public InkRecognitionHelper(FreeformInkControl freeformInkControl)
        {
            Analyzer = new InkAnalyzer();
            NewStrokes = new List<InkStroke>();
            FreeformInkControl = freeformInkControl;
        }

        /// <summary>
        /// Tries to find documents or collections at the double tap point, or delete using the newest stroke.
        /// </summary>
        /// <param name="onlySelectedStrokes"></param>
        public async void RecognizeInk(bool onlySelectedStrokes = false)
        {
            Analyzer.ClearDataForAllStrokes();
            //Tries to delete with a line stroke, so long as we're not doing forced recognition from selected strokes.
            if (NewStrokes.Count > 0 && !onlySelectedStrokes)
            {
                foreach (var newStroke in NewStrokes)
                    //Done separately because it doesn't require ink analyzer
                    if (TryDeleteWithStroke(newStroke)) return;
            }
            //If we're not analyzing selected strokes, we want to analyze all strokes
            if (!onlySelectedStrokes)
            {
                AddDataForAllStrokes();
            }
            //Otherwise only analyze selected strokes
            else
            {
                AddDataForSelectedStrokes();
            }
            var result = await Analyzer.AnalyzeAsync();
            if (result.Status == InkAnalysisStatus.Updated)
            {
                //Gets a dictionary mapping the rect bounds of all recognized text to lists of the corresponding stroke ids, and removes the data for those strokes.
                TextBoundsDictionary = GetTextBoundsAndRemoveData();
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
                        //if (!recognitionFromSelectedStrokes && recognized) RemoveStrokeReferences(region.GetStrokeIds().ToImmutableHashSet());
                    }
                }
                foreach (var inkAnalysisNode in _shapeRegions)
                {
                    var region = (InkAnalysisInkDrawing)inkAnalysisNode;
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
                        //if (!recognitionFromSelectedStrokes && recognized) RemoveStrokeReferences(region.GetStrokeIds().ToImmutableHashSet());
                    }
                }
                //All of the unused text gets re-added to the InkAnalyzer
                //foreach (var key in TextBoundsDictionary.Keys)
                //{
                //    var ids = TextBoundsDictionary[key]?.Item2
                //        ?.Select(id => FreeformInkControl.TargetCanvas.InkPresenter.StrokeContainer.GetStrokeById(id))
                //        .Where(id => id != null);
                //    if (ids != null) Analyzer.AddDataForStrokes(ids);
                //}
            }
            //if (recognitionFromSelectedStrokes) Analyzer.ClearDataForAllStrokes();
            
            NewStrokes.Clear();
            FreeformInkControl.UpdateInkFieldModelController();
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
            var rectToDocView = GetDocViewRects();
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
                //Analyzer.RemoveDataForStrokes(textLine.GetStrokeIds());
                var textIds = textLine.GetStrokeIds();
                NewStrokes.RemoveAll(stroke => textIds.Contains(stroke.Id));
            }

            return textBoundsDictionary;
        }

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
                    Analyzer.RemoveDataForStrokes(child.GetStrokeIds().ToImmutableHashSet());
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
                DocumentView documentView = FreeformInkControl.FreeformView.GetDocView(doc);
                if (documentView != null)
                {
                    FreeformInkControl.FreeformView.DeleteConnections(documentView);
                }
            }

            var documentController = Util.BlankCollection();
            documentController.SetField(KeyStore.CollectionKey,
                new DocumentCollectionFieldModelController(recognizedDocuments), true);
            documentController.SetActiveLayout(
                new CollectionBox(
                    new DocumentReferenceFieldController(documentController.GetId(),
                        KeyStore.CollectionKey), position.X, position.Y, region.BoundingRect.Width,
                     region.BoundingRect.Height).Document, true, true);
            FreeformInkControl.FreeformView.ViewModel.AddDocument(documentController, null);
            DeleteStrokesByID(region.GetStrokeIds().ToImmutableHashSet());
        }

        private void AddOperatorFromRegion(InkAnalysisInkDrawing region)
        {
            Point absPos =
                Util.PointTransformFromVisual(new Point(region.BoundingRect.X, region.BoundingRect.Y),
                    FreeformInkControl.TargetInkCanvas, MainPage.Instance);
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
            var fields = new Dictionary<KeyController, FieldControllerBase>();
            var doc = new DocumentController(fields, DocumentType.DefaultType);
            var layoutDocs = new List<DocumentController>();
            var keysToRemove = new List<Rect>();
            int fieldIndex = 0;
            var stringFields = TextBoundsDictionary.Keys.Where(r => RectContainsRect(region.BoundingRect, r));
            var enumerable = stringFields as IList<Rect> ?? stringFields.ToList();
            ListFieldModelController<TextFieldModelController> list = enumerable.Count == 0 ? null : new ListFieldModelController<TextFieldModelController>();
            foreach (var rect in enumerable)
            {
                DeleteStrokesByID(TextBoundsDictionary[rect].GetStrokeIds().ToImmutableHashSet());
                var str = TextBoundsDictionary[rect].RecognizedText;
                TryGetText(str, out string text, out KeyController key,
                    enumerable.Count() > 1 ? (++fieldIndex).ToString() : "");
                var relativePosition = new Point(rect.X - topLeft.X, rect.Y - topLeft.Y);
                doc.ParseDocField(key, text);
                var field = doc.GetField(key);
                if (field != null && field is TextFieldModelController)
                {
                    list.Add(field);
                }
                var textBox = new TextingBox(new DocumentReferenceFieldController(doc.GetId(), key),
                    relativePosition.X, relativePosition.Y, rect.Width, rect.Height);
                (textBox.Document.GetField(TextingBox.FontSizeKey) as NumberFieldModelController).Data =
                    rect.Height / 1.5;
                layoutDocs.Add(textBox.Document);
                keysToRemove.Add(rect);
            }
            //now try making fields from only partially intersected lines
            var intersectedLines = TextBoundsDictionary.Keys.Where(r => r.IntersectsWith(region.BoundingRect) && !keysToRemove.Contains(r));
            var intersectionEnumerable = intersectedLines as IList<Rect> ?? intersectedLines.ToList();
            if (list == null && intersectionEnumerable.Count > 0) list = new ListFieldModelController<TextFieldModelController>();
            foreach (var rect in intersectionEnumerable)
            {
                var containedWords = GetContainedWords(rect, region.BoundingRect);
                if (containedWords.Count > 0)
                {
                    string containedLine = "";
                    Rect containedRect = containedWords[0].BoundingRect;
                    foreach (var word in containedWords)
                    {
                        containedLine += " " + word.RecognizedText;
                        DeleteStrokesByID(word.GetStrokeIds().ToImmutableHashSet());
                        containedRect.Union(word.BoundingRect);
                        containedRect.X = Math.Min(containedRect.X, word.BoundingRect.X);
                        containedRect.Y = Math.Min(containedRect.Y, word.BoundingRect.Y);
                    }
                    if (containedLine != "")
                    {
                        TryGetText(containedLine, out string text, out KeyController key,
                            enumerable.Count() > 1 ? (++fieldIndex).ToString() : "");
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
            foreach (var key in keysToRemove) TextBoundsDictionary.Remove(key);
            if (list != null)
            {
                doc.SetField(KeyStore.ParsedFieldKey, list, true);
            }
            var layout = new FreeFormDocument(layoutDocs,
                position, size).Document;
            doc.SetActiveLayout(layout, true, true);
            if (addToFreeformView) FreeformInkControl.FreeformView.ViewModel.AddDocument(doc, null);
            return doc;
        }

        private List<InkAnalysisInkWord> GetContainedWords(Rect lineRect, Rect docBounds)
        {
            InkAnalysisLine line = TextBoundsDictionary[lineRect];
            List<InkAnalysisInkWord> containedWords = new List<InkAnalysisInkWord>();
            containedWords.AddRange(Analyzer.AnalysisRoot.FindNodes(InkAnalysisNodeKind.InkWord).OfType<InkAnalysisInkWord>()
                .Where(word => docBounds.Contains(word.BoundingRect) && word.Parent.Id == line.Id).ToList());
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

        /// <summary>
        /// Adds the stroke data to the Analyzer and stores the newly collected strokes in NewStrokes.
        /// </summary>
        /// <param name="strokes"></param>
        public void SetNewStrokes(List<InkStroke> strokes)
        {
            Analyzer.AddDataForStrokes(strokes);
            NewStrokes = strokes;
        }

        private void AddDataForSelectedStrokes()
        {
            Analyzer.AddDataForStrokes(FreeformInkControl.TargetInkCanvas.InkPresenter.StrokeContainer.GetStrokes().Where(stroke => stroke.Selected));
        }

        public void RemoveStrokeData(List<InkStroke> strokes)
        {
            Analyzer.RemoveDataForStrokes(strokes.Select(s => s.Id));
            NewStrokes.RemoveAll(strokes.Contains);
        }

        public void RecognizeAndForgetStrokes(IEnumerable<InkStroke> strokes)
        {
            SetNewStrokes(new List<InkStroke>(strokes));
            RecognizeInk(true);
        }

        private bool RegionContainsNewStroke(IInkAnalysisNode region)
        {
            return region.GetStrokeIds()
                .Select(id => FreeformInkControl.TargetInkCanvas.InkPresenter.StrokeContainer.GetStrokeById(id))
                .Any(stroke => NewStrokes.Contains(stroke));
        }


        private void AddDataForAllStrokes()
        {
            Analyzer.AddDataForStrokes(FreeformInkControl.TargetInkCanvas.InkPresenter.StrokeContainer.GetStrokes());
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
}