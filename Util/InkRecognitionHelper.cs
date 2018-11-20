using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Windows.Foundation;
using Windows.UI.Input.Inking;
using Windows.UI.Input.Inking.Analysis;
using Windows.UI.Xaml.Controls;
using DashShared;
using NewControls.Geometry;

namespace Dash
{
    /// <summary>
    /// This class manages recognition of ink strokes, which are converted into documents with text fields or collections.
    /// - There is one InkRecognitionHelper associated with each FreeformInkControl
    /// TODO: this class is in need of a major refactor. Ideally should only recognize inkstrokes within the recognition bounds, and allow for better nesting
    /// TODO: do we still need NewStrokes?
    /// </summary>
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
        /// - Highest-level method, called at each new recognition event
        /// - Tries to find documents or collections at the double tap point, or delete using the newest stroke.
        /// TODO: is commented-out code still needed?
        /// TODO: do we need the textboundsdictionary? 
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
            FreeformInkControl.UpdateInkController();
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
                FreeformInkControl.FreeformView.GetItemsControl().ItemsPanelRoot)));
            //check for linearity
            if (!IsLinear(inkPoints)) return false;
            var point1 = inkPoints[0];
            var point2 = inkPoints.Last();
            var rectToDocView = GetDocViewRects();
            //Try using the new line to delete documents or links
            var deleted = DeleteIntersectingDocuments(inkPoints, point1, point2);
            //If they were deleted, remove the line.
            if (deleted)
            {
                FreeformInkControl.UndoSelection();
                newStroke.Selected = true;
                FreeformInkControl.TargetInkCanvas.InkPresenter.StrokeContainer.DeleteSelected();
                Analyzer.RemoveDataForStroke(newStroke.Id);
                FreeformInkControl.UpdateInkController();

            }
            return deleted;
        }

        //Removes all documents from the Collection associated with this InkRecognitionHelper's FreeformInkControl that are intersected by the line
        //passed in.
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
                        FreeformInkControl.FreeformView.ViewModel.RemoveDocument(rectToDocView[rect].ViewModel
                            .DocumentController);
                        deleted = true;
                    }
            }
            return deleted;
        }

        #endregion

        #region Add collection, doc, operator

        /// <summary>
        /// Finds the bounds of all ink strokes recognized as text. Creates a dictionary of bounds->InkAnalysisLine and removes 
        /// the recognized ink data from the analyzer.
        /// TODO: is this still needed if we aren't removing the ink stroke data?
        /// </summary>
        /// <returns></returns>
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

        /// <summary>
        /// Generates a collection from a circular or elliptical ink drawing and captures all of the drawn or preexisting documents within the ink drawing, 
        /// adding them to the collection.
        /// </summary>
        /// <param name="region"></param>
        private void AddCollectionFromShapeRegion(InkAnalysisInkDrawing region)
        {
            var regions = new List<IInkAnalysisNode>(_shapeRegions);
            List<DocumentController> recognizedDocuments = new List<DocumentController>();
            //Look for rectangles inside ellipse and add them as docs to collection
            foreach (var child in regions.OfType<InkAnalysisInkDrawing>().Where(
                r => region.BoundingRect.Contains(r.BoundingRect)))
            {
                if (child.DrawingKind == InkAnalysisDrawingKind.Rectangle ||
                    child.DrawingKind == InkAnalysisDrawingKind.Square)
                {
                    recognizedDocuments.Add(AddDocumentFromShapeRegion(child, false));
                    Analyzer.RemoveDataForStrokes(child.GetStrokeIds().ToImmutableHashSet());
                    _shapeRegions.Remove(child);
                }
            }
            //Get preexisting documents within circle by transforming points to collection space and using LassoHelper to get contained docs.
            var lassoPoints = new List<Point>(GetPointsFromStrokeIDs(region.GetStrokeIds())
                .Select(p => Util.PointTransformFromVisual(p, FreeformInkControl.SelectionCanvas,
                    FreeformInkControl.FreeformView.GetItemsControl().ItemsPanelRoot as Canvas)));
            var selectedDocuments = FreeformInkControl.LassoHelper.GetSelectedDocuments(lassoPoints);
            var topLeft = new Point(region.BoundingRect.X, region.BoundingRect.Y);
            var position = Util.PointTransformFromVisual(topLeft, FreeformInkControl.SelectionCanvas,
                FreeformInkControl.FreeformView.GetItemsControl().ItemsPanelRoot as Canvas);
            recognizedDocuments.AddRange(selectedDocuments.Select(view => (view.DataContext as DocumentViewModel).DocumentController));
            foreach (var doc in recognizedDocuments)
            {
                var ogPos = doc.GetPositionField().Data;
                var newPos = Util.PointTransformFromVisual(ogPos,
                    FreeformInkControl.FreeformView.GetItemsControl().ItemsPanelRoot, FreeformInkControl.SelectionCanvas);
                var relativePos = new Point(newPos.X - topLeft.X, newPos.Y - topLeft.Y);
                doc.GetPositionField().Data = relativePos;
                FreeformInkControl.FreeformView.ViewModel.RemoveDocument(doc);
            }
            //Construct the new collection
            var cnote  = new CollectionNote(position, CollectionViewType.Freeform);
            cnote.SetDocuments(recognizedDocuments);
            var documentController = cnote.Document;
            documentController.SetLayoutDimensions(region.BoundingRect.Width,
                region.BoundingRect.Height);
            FreeformInkControl.FreeformView.ViewModel.AddDocument(documentController);
            DeleteStrokesByID(region.GetStrokeIds().ToImmutableHashSet());
        }

        /// <summary>
        /// Constructs a new document with the bounds of the rectangular region passed in, and tries to recognize text and add contained
        /// recognized words as text fields on the document.
        /// </summary>
        /// <param name="region"></param>
        /// <param name="addToFreeformView"></param>
        /// <returns></returns>
        private DocumentController AddDocumentFromShapeRegion(InkAnalysisInkDrawing region, bool addToFreeformView = true)
        {
            DeleteStrokesByID(region.GetStrokeIds().ToImmutableHashSet());
            Analyzer.RemoveDataForStrokes(region.GetStrokeIds());
            var topLeft = new Point(region.BoundingRect.X, region.BoundingRect.Y);
            var size = new Size(region.BoundingRect.Width, region.BoundingRect.Height);
            var position = Util.PointTransformFromVisual(topLeft, FreeformInkControl.SelectionCanvas,
                FreeformInkControl.FreeformView.GetItemsControl().ItemsPanelRoot as Canvas);
            var fields = new Dictionary<KeyController, FieldControllerBase>();
            var doc = new DocumentController(fields, DocumentType.DefaultType);
            var layoutDocs = new List<DocumentController>();
            var keysToRemove = new List<Rect>();
            int fieldIndex = 0;
            //try making fields from only partially intersected lines
            var intersectedLines = TextBoundsDictionary.Keys.Where(r => r.IntersectsWith(region.BoundingRect) && !keysToRemove.Contains(r));
            var intersectionEnumerable = intersectedLines as IList<Rect> ?? intersectedLines.ToList();
            ListController<TextController> list = intersectionEnumerable.Count == 0 ? null : new ListController<TextController>();
            if (list == null && intersectionEnumerable.Count > 0) list = new ListController<TextController>();
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
                            intersectionEnumerable.Count() > 1 ? (++fieldIndex).ToString() : "");
                        var relativePosition = new Point(containedRect.X - topLeft.X, containedRect.Y - topLeft.Y);
                        //TODO
                        //doc.ParseDocField(key, "="+text);
                        var field = doc.GetField(key);
                        if (field != null)
                        {
                            list.AddBase(field);
                        }
                        var textBox = new TextingBox(new DocumentReferenceController(doc, key),
                            relativePosition.X, relativePosition.Y, containedRect.Width, containedRect.Height);
                        (textBox.Document.GetField(KeyStore.FontSizeKey) as NumberController).Data =
                            containedRect.Height / 1.5;
                        layoutDocs.Add(textBox.Document);
                    }
                }
            }
            foreach (var key in keysToRemove) TextBoundsDictionary.Remove(key);
            if (list != null)
            {
                doc.SetField(KeyStore.ParsedFieldsKey, list, true);
            }
            var layout = new FreeFormDocument(layoutDocs, position, size).Document;

            throw new Exception("ActiveLayout code has not been updated yet");
            // doc.SetActiveLayout(layout, true, true);
            if (addToFreeformView) FreeformInkControl.FreeformView.ViewModel.AddDocument(doc);
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

        /// <summary>
        /// Checks if a list of points is linear by assessing whether the R^2 value is greater than 0.9
        /// - rotates the line and checks R^2 value again because R^2 = 0 for horizontal line.
        /// </summary>
        /// <param name="points"></param>
        /// <returns></returns>
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
            IEnumerable<DocumentViewModel> parameters = FreeformInkControl.FreeformView.GetItemsControl().Items.OfType<DocumentViewModel>();
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
                if (FreeformInkControl.FreeformView.GetItemsControl().ItemContainerGenerator != null && FreeformInkControl
                        .FreeformView.GetItemsControl()
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
                key = KeyController.Get(keystring);
            }
            else
            {
                value = str;
                key = KeyController.Get($"Document Field {suffix}");
            }

        }

        #endregion

    }
}
