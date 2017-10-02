using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Windows.Foundation;
using Windows.UI.Input.Inking;
using Windows.UI.Input.Inking.Analysis;
using Windows.UI.Xaml.Controls;
using Dash.Controllers;
using DashShared;

namespace Dash
{
    public sealed class InkRecognitionHelper
    {
        private FreeformInkControl _freeformInkControl;
        private Dictionary<Rect, Tuple<string, IEnumerable<uint>>> _textBoundsDictionary;
        public InkAnalyzer Analyzer;
        public InkRecognitionHelper(FreeformInkControl freeformInkControl)
        {
            _freeformInkControl = freeformInkControl;
            Analyzer = new InkAnalyzer();
        }

        /// <summary>
        /// Tries to find documents or collections at the double tap point, or delete using the newest stroke.
        /// </summary>
        /// <param name="doubleTapped"></param>
        /// <param name="newStroke"></param>
        public async void RecognizeInk(bool doubleTapped = false, InkStroke newStroke = null)
        {
            if (newStroke != null)
            {
                //Done separately because it doesn't require ink analyzer
                TryDeleteWithStroke(newStroke);
            }
            if (_freeformInkControl.IsPressed || doubleTapped)
            {
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
                        //If the region is a rectangle and was double tapped, add the document (not very efficient rn)
                        if (region.DrawingKind == InkAnalysisDrawingKind.Rectangle ||
                            region.DrawingKind == InkAnalysisDrawingKind.Square)
                        {
                            if (doubleTapped && region.BoundingRect.Contains(_freeformInkControl.DoubleTapPoint))
                            {
                                AddDocumentFromShapeRegion(region);
                                Analyzer.RemoveDataForStrokes(region.GetStrokeIds());
                            }
                        }
                    }
                    //If the region is an ellipse and the user is pressing near it, add a collection
                    foreach (InkAnalysisInkDrawing region in shapeRegions)
                    {
                        if (region.DrawingKind == InkAnalysisDrawingKind.Circle ||
                            region.DrawingKind == InkAnalysisDrawingKind.Ellipse)
                        {
                            var rect = region.BoundingRect;
                            var newRect = new Rect(rect.X - 200, rect.Y - 200, rect.Width + 400, rect.Width + 400);
                            if (_freeformInkControl.IsPressed && newRect.Contains(_freeformInkControl.PressedPoint))
                            {
                                AddCollectionFromShapeRegion(region);
                            }
                            Analyzer.RemoveDataForStrokes(region.GetStrokeIds());
                        }
                    }
                    //All of the unused text gets re-added to the InkAnalyzer
                    foreach (var key in _textBoundsDictionary.Keys)
                    {
                        var ids = _textBoundsDictionary[key]?.Item2
                            ?.Select(id => _freeformInkControl.TargetCanvas.InkPresenter.StrokeContainer.GetStrokeById(id)).Where(id => id != null);
                        if (ids != null) Analyzer.AddDataForStrokes(ids);
                    }
                }
            }
            _freeformInkControl.UpdateInkFieldModelController();
        }

        private void TryDeleteWithStroke(InkStroke newStroke)
        {
            var point1 = Util.PointTransformFromVisual((newStroke.GetInkPoints()[0].Position), _freeformInkControl.SelectionCanvas, _freeformInkControl.FreeformView.xItemsControl.ItemsPanelRoot);
            var point2 = Util.PointTransformFromVisual((newStroke.GetInkPoints().Last().Position), _freeformInkControl.SelectionCanvas, _freeformInkControl.FreeformView.xItemsControl.ItemsPanelRoot);
            var rectToDocView = GetDocViewRects();
            foreach (var rect in rectToDocView.Keys)
            {
                bool docsRemoved = false;
                if (IsLinear(newStroke) && (point1.X < rect.X && point2.X > rect.X + rect.Width ||
                                            point1.X > rect.X + rect.Width && point2.X < rect.X))
                {
                    if (PathIntersectsRect(
                        newStroke.GetInkPoints().Select(p => Util.PointTransformFromVisual(p.Position, _freeformInkControl.SelectionCanvas, _freeformInkControl.FreeformView.xItemsControl.ItemsPanelRoot)), rect))
                    {
                        docsRemoved = true;
                        _freeformInkControl.FreeformView.ViewModel.RemoveDocument(rectToDocView[rect].ViewModel
                            .DocumentController);
                    }
                }
                if (docsRemoved)
                {
                    _freeformInkControl.ClearSelection();
                    newStroke.Selected = true;
                    _freeformInkControl.TargetCanvas.InkPresenter.StrokeContainer.DeleteSelected();
                    Analyzer.RemoveDataForStroke(newStroke.Id);
                }
            }
        }

        private bool IsLinear(InkStroke stroke)
        {
            var points = stroke.GetInkPoints().Select(i => i.Position);
            var xVals = points.Select(p => p.X).ToArray();
            var yVals = points.Select(p => p.Y).ToArray();
            Util.LinearRegression(xVals, yVals, 0, points.Count(), out double rsquared, out double yInt, out double slope);
            return rsquared > 0.85;
        }

        private bool PathIntersectsRect(IEnumerable<Point> path, Rect rect)
        {
            var innerRect = new Rect
            {
                X = rect.X + rect.Width/8,
                Y = rect.Y + rect.Height/8,
                Width = rect.Width/4 * 3,
                Height = rect.Height/4 * 3
            };
            foreach (var point in path)
            {
                if (innerRect.Contains(point)) return true;
            }
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
                if (_freeformInkControl.FreeformView.xItemsControl.ItemContainerGenerator != null && _freeformInkControl.FreeformView.xItemsControl
                        .ContainerFromItem(param) is ContentPresenter contentPresenter)
                {
                    dict[rect] =
                        contentPresenter.GetFirstDescendantOfType<DocumentView>();
                }
            }
            return dict;
        }

        private void AddCollectionFromShapeRegion(InkAnalysisInkDrawing region)
        {
            var selectionPoints = new List<Point>(Enumerable.Select<Point, Point>(GetPointsFromStrokeIDs(region.GetStrokeIds()), p => Util.PointTransformFromVisual(p, _freeformInkControl.SelectionCanvas, _freeformInkControl.FreeformView.xItemsControl.ItemsPanelRoot as Canvas)));
            var selectedDocuments = _freeformInkControl.LassoHelper.GetSelectedDocuments(selectionPoints);
            var docControllers = new List<DocumentController>();
            var topLeft = new Point(region.BoundingRect.X, region.BoundingRect.Y);
            var size = new Size(region.BoundingRect.Width, region.BoundingRect.Height);
            var position = Util.PointTransformFromVisual(topLeft, _freeformInkControl.SelectionCanvas, _freeformInkControl.FreeformView.xItemsControl.ItemsPanelRoot as Canvas);
            foreach (var view in selectedDocuments)
            {
                var doc = (view.DataContext as DocumentViewModel).DocumentController;
                var ogPos = doc.GetPositionField().Data;
                var newPos = Util.PointTransformFromVisual(ogPos, _freeformInkControl.FreeformView.xItemsControl.ItemsPanelRoot, _freeformInkControl.SelectionCanvas);
                var relativePos = new Point(newPos.X - topLeft.X, newPos.Y - topLeft.Y);
                doc.GetPositionField().Data = relativePos;
                _freeformInkControl.FreeformView.ViewModel.RemoveDocument(doc);
                docControllers.Add(doc);
            }
            var fields = new Dictionary<KeyController, FieldControllerBase>()
            {
                [DocumentCollectionFieldModelController.CollectionKey] = new DocumentCollectionFieldModelController(docControllers),
            };
            var documentController = new DocumentController(fields, DocumentType.DefaultType);
            documentController.SetActiveLayout(new CollectionBox(new DocumentReferenceFieldController(documentController.GetId(), DocumentCollectionFieldModelController.CollectionKey), position.X, position.Y, size.Width, size.Height).Document, true, true);
            _freeformInkControl.FreeformView.ViewModel.AddDocument(documentController, null);
            DeleteStrokesByID(region.GetStrokeIds().ToImmutableHashSet());
        }

        private List<Point> GetPointsFromStrokeIDs(IEnumerable<uint> ids)
        {
            var points = new List<Point>();
            foreach (var id in ids)
            {
                points.AddRange(_freeformInkControl.TargetCanvas.InkPresenter.StrokeContainer.GetStrokeById(id).GetInkPoints().Select(inkPoint => inkPoint.Position));
            }
            return points;
        }

        private Dictionary<Rect, Tuple<string, IEnumerable<uint>>> GetTextBoundsDictionary()
        {
            List<InkAnalysisLine> textLineRegions = new List<InkAnalysisLine>(Analyzer.AnalysisRoot.FindNodes(InkAnalysisNodeKind.Line).Select(o => o as InkAnalysisLine));
            Dictionary<Rect, Tuple<string, IEnumerable<uint>>> textBoundsDictionary = new Dictionary<Rect, Tuple<string, IEnumerable<uint>>>();
            foreach (InkAnalysisLine textLine in textLineRegions)
            {
                textBoundsDictionary[textLine.BoundingRect] = new Tuple<string, IEnumerable<uint>>(textLine.RecognizedText, textLine.GetStrokeIds());
                Analyzer.RemoveDataForStrokes(textLine.GetStrokeIds());
            }

            return textBoundsDictionary;
        }

        private Dictionary<Rect, Tuple<string, IEnumerable<uint>>> GetListBoundsDictionary()
        {
            List<InkAnalysisListItem> textLineRegions = new List<InkAnalysisListItem>(Analyzer.AnalysisRoot.FindNodes(InkAnalysisNodeKind.ListItem).Select(o => o as InkAnalysisListItem));
            Dictionary<Rect, Tuple<string, IEnumerable<uint>>> textBoundsDictionary = new Dictionary<Rect, Tuple<string, IEnumerable<uint>>>();
            foreach (InkAnalysisListItem textLine in textLineRegions)
            {
                textBoundsDictionary[textLine.BoundingRect] = new Tuple<string, IEnumerable<uint>>(textLine.RecognizedText, textLine.GetStrokeIds());
                Analyzer.RemoveDataForStrokes(textLine.GetStrokeIds());
            }

            return textBoundsDictionary;
        }

        private Dictionary<Rect, Tuple<string, IEnumerable<uint>>> GetParagraphBoundsDictionary()
        {
            List<InkAnalysisParagraph> textLineRegions = new List<InkAnalysisParagraph>(Analyzer.AnalysisRoot.FindNodes(InkAnalysisNodeKind.Paragraph).Select(o => o as InkAnalysisParagraph));
            Dictionary<Rect, Tuple<string, IEnumerable<uint>>> textBoundsDictionary = new Dictionary<Rect, Tuple<string, IEnumerable<uint>>>();
            foreach (InkAnalysisParagraph textLine in textLineRegions)
            {
                textBoundsDictionary[textLine.BoundingRect] = new Tuple<string, IEnumerable<uint>>(textLine.RecognizedText, textLine.GetStrokeIds());
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
            var position = Util.PointTransformFromVisual(topLeft, _freeformInkControl.SelectionCanvas, _freeformInkControl.FreeformView.xItemsControl.ItemsPanelRoot as Canvas);
            var fields = new Dictionary<KeyController, FieldControllerBase>();
            var doc = new DocumentController(fields, DocumentType.DefaultType);
            List<DocumentController> textFields = new List<DocumentController>();
            List<Rect> keysToRemove = new List<Rect>();
            //TODO: need better differentiation between paragraphs and lines before we can to rtf for paragraphs.
            //List<uint> idsToRemove = new List<uint>();
            //foreach (var rect in _paragraphBoundsDictionary.Keys)
            //{
            //    if (RectContainsRect(region.BoundingRect, rect))
            //    {
            //        idsToRemove.AddRange(_paragraphBoundsDictionary[rect].Item2);
            //        var str = _paragraphBoundsDictionary[rect].Item1;
            //        var key = TryGetKey(str);
            //        var text = TryGetText(str);
            //        var richText = new RichTextFieldModelController(new RichTextFieldModel.RTD(text));
            //        var relativePosition = new Point(rect.X - topLeft.X, rect.Y - topLeft.Y);
            //        doc.SetField(key, richText, true);
            //        var textBox = new RichTextBox(new ReferenceFieldModelController(doc.GetId(), key), relativePosition.X, relativePosition.Y, rect.Width, rect.Height);
            //        textFields.Add(textBox.Document);
            //        keysToRemove.Add(rect);
            //    }
            //}
            //DeleteStrokesByID(idsToRemove.ToImmutableHashSet());
            //foreach (var key in keysToRemove) _paragraphBoundsDictionary.Remove(key);
            //keysToRemove.Clear();
            foreach (var rect in _textBoundsDictionary.Keys)
            {
                if (RectContainsRect(region.BoundingRect, rect))
                {
                    DeleteStrokesByID(_textBoundsDictionary[rect].Item2.ToImmutableHashSet());
                    var str = _textBoundsDictionary[rect].Item1;
                    var key = TryGetKey(str);
                    var text = TryGetText(str);
                    var relativePosition = new Point(rect.X - topLeft.X, rect.Y - topLeft.Y);
                    doc.SetField(key, new TextFieldModelController(text), true);
                    var textBox = new TextingBox(new DocumentReferenceFieldController(doc.GetId(), key), relativePosition.X, relativePosition.Y, rect.Width, rect.Height);
                    (textBox.Document.GetField(TextingBox.FontSizeKey) as NumberFieldModelController).Data =
                        rect.Height / 1.2;
                    textFields.Add(textBox.Document);
                    keysToRemove.Add(rect);
                }
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
            {
                if (!outer.Contains(point)) return false;
            }
            return true;
        }

        private void DeleteStrokesByID(ICollection<uint> IDs)
        {
            _freeformInkControl.ClearSelection();
            foreach (var stroke in _freeformInkControl.TargetCanvas.InkPresenter.StrokeContainer.GetStrokes())
            {
                if (IDs.Contains(stroke.Id))
                {
                    stroke.Selected = true;
                }
            }
            _freeformInkControl.TargetCanvas.InkPresenter.StrokeContainer.DeleteSelected();
        }

        private bool RectCornerIntersectsTouchPoint(Rect rect)
        {
            var topLeft = new Point(rect.X, rect.Y);
            var topRight = new Point(rect.X + rect.Width, rect.Y);
            var bottomLeft = new Point(rect.X, rect.Y + rect.Height);
            var bottomRight = new Point(rect.X + rect.Width, rect.Y + rect.Height);
            var points = new List<Point>
            {
                topLeft,
                topRight,
                bottomLeft,
                bottomRight
            };
            foreach (var point in points)
            {
                if (Math.Abs(_freeformInkControl.PressedPoint.X - point.X) < 150 && Math.Abs(_freeformInkControl.PressedPoint.Y - point.Y) < 150)
                    return true;
            }
            return false;
        }

        public async void AddAnalyzerData(IEnumerable<InkStroke> strokes)
        {
            //Need this so that previously drawn circles dont get turned into collections by accident, and so that documents can get made from previously drawn squares etc.

            Analyzer.AddDataForStrokes(strokes);
            await Analyzer.AnalyzeAsync();
            var shapeRegions = Analyzer.AnalysisRoot.FindNodes(InkAnalysisNodeKind.InkDrawing);
            foreach (InkAnalysisInkDrawing region in shapeRegions)
            {
                if (region.DrawingKind == InkAnalysisDrawingKind.Circle || region.DrawingKind == InkAnalysisDrawingKind.Ellipse)
                    Analyzer.RemoveDataForStrokes(region.GetStrokeIds());
            }
        }
    }
}