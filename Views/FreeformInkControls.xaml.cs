using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Devices.Input;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Core;
using Windows.UI.Input.Inking;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using Windows.UI.Xaml.Shapes;
using Dash.Views;
using Windows.UI;
using Windows.UI.Input;
using Windows.UI.Input.Inking.Analysis;
using DashShared;
using Microsoft.Graphics.Canvas.Brushes;
using Visibility = Windows.UI.Xaml.Visibility;

// The User Control item template is documented at https://go.microsoft.com/fwlink/?LinkId=234236

namespace Dash
{
    public sealed partial class FreeformInkControls : UserControl
    {
        private InkCanvas _inkCanvas;
        public InkCanvas TargetCanvas
        {
            get { return _inkCanvas; }
            set
            {
                _inkCanvas = value;
                InkToolbar.TargetInkCanvas = value;
            }
        }
        public InkFieldModelController InkFieldModelController;
        public CollectionFreeformView FreeformView;
        public Canvas SelectionCanvas;
         private enum InkSelectionMode
        {
            Document, Ink
        }
        private InkSelectionMode _inkSelectionMode;
        private Polygon _lasso;
        private Rect _boundingRect;
        private InkSelectionRect _rectangle;
        private LassoSelectHelper _lassoHelper;
        Symbol SelectIcon = (Symbol)0xEF20;
        Symbol TouchIcon = (Symbol)0xED5F;
        private Point _pressedPoint = new Point(0,0);
        private Point _doubleTapPoint = new Point(0,0);

        private bool IsPressed
        {
            get { return _isPressed; }
            set
            {
                _isPressed = value;
            }
        }

        private InkAnalyzer _analyzer;
        private double _docFromInkBuffer = 150;
        private bool _isPressed;
        private Ellipse _touchIndicator;
        private Dictionary<Rect, Tuple<string, IEnumerable<uint>>> _textBoundsDictionary;
        private Dictionary<Rect, Tuple<string, IEnumerable<uint>>> _paragraphBoundsDictionary;

        public FreeformInkControls(CollectionFreeformView view, InkCanvas canvas, Canvas selectionCanvas)
        {
            this.InitializeComponent();
            TargetCanvas = canvas;
            FreeformView = view;
            SelectionCanvas = selectionCanvas;
            InkFieldModelController = view.InkFieldModelController;
            IsDrawing = true;
            _lassoHelper = new LassoSelectHelper(FreeformView);
            _analyzer = new InkAnalyzer();
            UpdateStrokes();
            ToggleDraw();
            AddEventHandlers();
            UpdateInputType();
        }


        private void AddEventHandlers()
        {
            TargetCanvas.InkPresenter.StrokesCollected += InkPresenterOnStrokesCollected;
            TargetCanvas.InkPresenter.StrokesErased += InkPresenterOnStrokesErased;
            TargetCanvas.InkPresenter.StrokeInput.StrokeStarted += StrokeInputOnStrokeStarted;
            TargetCanvas.PointerPressed += TargetCanvasOnPointerPressed;
            TargetCanvas.Holding += TargetCanvasOnHolding;
            TargetCanvas.PointerReleased += TargetCanvasOnPointerReleased;
            TargetCanvas.PointerExited += TargetCanvas_PointerExited;
            TargetCanvas.PointerMoved += TargetCanvasOnPointerMoved;
            TargetCanvas.DoubleTapped += TargetCanvasOnDoubleTapped;
            InkFieldModelController.InkUpdated += InkFieldModelControllerOnInkUpdated;
            InkToolbar.EraseAllClicked += InkToolbarOnEraseAllClicked;
            InkToolbar.ActiveToolChanged += InkToolbarOnActiveToolChanged;
        }

        private void TargetCanvasOnDoubleTapped(object sender, DoubleTappedRoutedEventArgs e)
        {
            _doubleTapPoint = e.GetPosition(SelectionCanvas);
            RecognizeInk(true);
        }

        #region Documents From Drawings

        private async void RecognizeInk(bool doubleTapped = false, InkStroke newStroke = null)
        {
            var result = await _analyzer.AnalyzeAsync();
            if (result.Status == InkAnalysisStatus.Updated)
            {
                //Dictionary<Rect, Tuple<string, IEnumerable<uint>>> listBoundsDictionary = GetListBoundsDictionary();
                //_paragraphBoundsDictionary = GetParagraphBoundsDictionary();
                _textBoundsDictionary = GetTextBoundsDictionary();
                // Make Documents
                var shapeRegions = _analyzer.AnalysisRoot.FindNodes(InkAnalysisNodeKind.InkDrawing);
                foreach (InkAnalysisInkDrawing region in shapeRegions)
                {
                    if (region.DrawingKind == InkAnalysisDrawingKind.Rectangle ||
                                         region.DrawingKind == InkAnalysisDrawingKind.Square)
                    {
                        if (doubleTapped && region.BoundingRect.Contains(_doubleTapPoint))
                        {
                            AddDocumentFromShapeRegion(region);
                            _analyzer.RemoveDataForStrokes(region.GetStrokeIds());
                        }
                    }
                    else if (region.DrawingKind == InkAnalysisDrawingKind.Circle ||
                             region.DrawingKind == InkAnalysisDrawingKind.Ellipse)
                    {
                        var rect = region.BoundingRect;
                        var newRect = new Rect(rect.X - 100, rect.Y - 100, rect.Width + 200, rect.Width + 200);

                        if (IsPressed && newRect.Contains(_pressedPoint))
                        {
                            AddCollectionFromShapeRegion(region);
                        }
                        _analyzer.RemoveDataForStrokes(region.GetStrokeIds());
                    }
                }
                foreach (var key in _textBoundsDictionary.Keys)
                {
                    var ids = _textBoundsDictionary[key]?.Item2
                        ?.Select(id => TargetCanvas.InkPresenter.StrokeContainer.GetStrokeById(id));
                    if (ids != null) _analyzer.AddDataForStrokes(ids);
                }
            }
            if (newStroke != null)
            {
                TryDeleteWithStroke(newStroke);
            }
            UpdateInkFieldModelController();
        }

        private void TryDeleteWithStroke(InkStroke newStroke)
        {
            var point1 = Util.PointTransformFromVisual((newStroke.GetInkPoints()[0].Position),
                                SelectionCanvas, FreeformView.xItemsControl.ItemsPanelRoot);
            var point2 = Util.PointTransformFromVisual((newStroke.GetInkPoints().Last().Position),
                SelectionCanvas, FreeformView.xItemsControl.ItemsPanelRoot);
            var rectToDocView = GetDocViewRects();
            foreach (var rect in rectToDocView.Keys)
            {
                bool docsRemoved = false;
                if (IsLinear(newStroke) && (point1.X < rect.X && point2.X > rect.X + rect.Width ||
                                            point1.X > rect.X + Width && point2.X < rect.X))
                {
                    if (PathIntersectsRect(
                        newStroke.GetInkPoints().Select(p => Util.PointTransformFromVisual(p.Position,
                            SelectionCanvas, FreeformView.xItemsControl.ItemsPanelRoot)), rect))
                    {
                        docsRemoved = true;
                        FreeformView.ViewModel.RemoveDocument(rectToDocView[rect].ViewModel
                            .DocumentController);
                    }
                }
                if (docsRemoved)
                {
                    ClearSelection();
                    newStroke.Selected = true;
                    TargetCanvas.InkPresenter.StrokeContainer.DeleteSelected();
                    _analyzer.RemoveDataForStroke(newStroke.Id);
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
            IEnumerable<DocumentViewModelParameters> parameters =
                FreeformView.xItemsControl.Items.OfType<DocumentViewModelParameters>();
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
                    if (FreeformView.xItemsControl.ItemContainerGenerator != null && FreeformView.xItemsControl
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
            var selectionPoints = new List<Point>(GetPointsFromStrokeIDs(region.GetStrokeIds()).Select(p => Util.PointTransformFromVisual(p, SelectionCanvas,
                FreeformView.xItemsControl.ItemsPanelRoot as Canvas)));
            var selectedDocuments = _lassoHelper.GetSelectedDocuments(selectionPoints);
            var docControllers = new List<DocumentController>();
            var topLeft = new Point(region.BoundingRect.X, region.BoundingRect.Y);
            var size = new Size(region.BoundingRect.Width, region.BoundingRect.Height);
            var position = Util.PointTransformFromVisual(topLeft, SelectionCanvas,
                FreeformView.xItemsControl.ItemsPanelRoot as Canvas);
            foreach (var view in selectedDocuments)
            {
                var doc = (view.DataContext as DocumentViewModel).DocumentController;
                var ogPos = doc.GetPositionField().Data;
                var newPos = Util.PointTransformFromVisual(ogPos,
                    FreeformView.xItemsControl.ItemsPanelRoot, SelectionCanvas);
                var relativePos = new Point(newPos.X - topLeft.X, newPos.Y - topLeft.Y);
                doc.GetPositionField().Data = relativePos;
                FreeformView.ViewModel.RemoveDocument(doc);
                docControllers.Add(doc);
            }
            var fields = new Dictionary<KeyController, FieldModelController>()
            {
                [DocumentCollectionFieldModelController.CollectionKey] = new DocumentCollectionFieldModelController(docControllers),
            };
            var documentController = new DocumentController(fields, DocumentType.DefaultType);
            documentController.SetActiveLayout(new CollectionBox(new ReferenceFieldModelController(documentController.GetId(), DocumentCollectionFieldModelController.CollectionKey), position.X, position.Y, size.Width, size.Height).Document, true, true);
            FreeformView.ViewModel.AddDocument(documentController, null);
            DeleteStrokesByID(region.GetStrokeIds().ToImmutableHashSet());
        }

        private List<Point> GetPointsFromStrokeIDs(IEnumerable<uint> ids)
        {
            var points = new List<Point>();
            foreach (var id in ids)
            {
                points.AddRange(TargetCanvas.InkPresenter.StrokeContainer.GetStrokeById(id).GetInkPoints().Select(inkPoint => inkPoint.Position));
            }
            return points;
        }

        private Dictionary<Rect, Tuple<string, IEnumerable<uint>>> GetTextBoundsDictionary()
        {
            List<InkAnalysisLine> textLineRegions = new List<InkAnalysisLine>(_analyzer.AnalysisRoot.FindNodes(InkAnalysisNodeKind.Line).Select(o => o as InkAnalysisLine));
            Dictionary<Rect, Tuple<string, IEnumerable<uint>>> textBoundsDictionary = new Dictionary<Rect, Tuple<string, IEnumerable<uint>>>();
            foreach (InkAnalysisLine textLine in textLineRegions)
            {
                textBoundsDictionary[textLine.BoundingRect] = new Tuple<string, IEnumerable<uint>>(textLine.RecognizedText, textLine.GetStrokeIds());
                _analyzer.RemoveDataForStrokes(textLine.GetStrokeIds());
            }

            return textBoundsDictionary;
        }

        private Dictionary<Rect, Tuple<string, IEnumerable<uint>>> GetListBoundsDictionary()
        {
            List<InkAnalysisListItem> textLineRegions = new List<InkAnalysisListItem>(_analyzer.AnalysisRoot.FindNodes(InkAnalysisNodeKind.ListItem).Select(o => o as InkAnalysisListItem));
            Dictionary<Rect, Tuple<string, IEnumerable<uint>>> textBoundsDictionary = new Dictionary<Rect, Tuple<string, IEnumerable<uint>>>();
            foreach (InkAnalysisListItem textLine in textLineRegions)
            {
                textBoundsDictionary[textLine.BoundingRect] = new Tuple<string, IEnumerable<uint>>(textLine.RecognizedText, textLine.GetStrokeIds());
                _analyzer.RemoveDataForStrokes(textLine.GetStrokeIds());
            }

            return textBoundsDictionary;
        }

        private Dictionary<Rect, Tuple<string, IEnumerable<uint>>> GetParagraphBoundsDictionary()
        {
            List<InkAnalysisParagraph> textLineRegions = new List<InkAnalysisParagraph>(_analyzer.AnalysisRoot.FindNodes(InkAnalysisNodeKind.Paragraph).Select(o => o as InkAnalysisParagraph));
            Dictionary<Rect, Tuple<string, IEnumerable<uint>>> textBoundsDictionary = new Dictionary<Rect, Tuple<string, IEnumerable<uint>>>();
            foreach (InkAnalysisParagraph textLine in textLineRegions)
            {
                textBoundsDictionary[textLine.BoundingRect] = new Tuple<string, IEnumerable<uint>>(textLine.RecognizedText, textLine.GetStrokeIds());
                _analyzer.RemoveDataForStrokes(textLine.GetStrokeIds());
            }

            return textBoundsDictionary;
        }

        private void AddDocumentFromShapeRegion(InkAnalysisInkDrawing region)
        {
            DeleteStrokesByID(region.GetStrokeIds().ToImmutableHashSet());
            _analyzer.RemoveDataForStrokes(region.GetStrokeIds());
            var topLeft = new Point(region.BoundingRect.X, region.BoundingRect.Y);
            var size = new Size(region.BoundingRect.Width, region.BoundingRect.Height);
            var position = Util.PointTransformFromVisual(topLeft, SelectionCanvas,
                FreeformView.xItemsControl.ItemsPanelRoot as Canvas);
            var fields = new Dictionary<KeyController, FieldModelController>();
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
                    var textBox = new TextingBox(new ReferenceFieldModelController(doc.GetId(), key), relativePosition.X, relativePosition.Y, rect.Width, rect.Height);
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
            FreeformView.ViewModel.AddDocument(doc, null);
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
            ClearSelection();
            foreach (var stroke in TargetCanvas.InkPresenter.StrokeContainer.GetStrokes())
            {
                if (IDs.Contains(stroke.Id))
                {
                    stroke.Selected = true;
                }
            }
            TargetCanvas.InkPresenter.StrokeContainer.DeleteSelected();
        }

        private bool RectIntersectsTouchPoint(Rect rect)
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
                if (Math.Abs(_pressedPoint.X - point.X) < 150 && Math.Abs(_pressedPoint.Y - point.Y) < 150)
                    return true;
            }
            return false;
        }

        #endregion

        #region Updating State

        public void ToggleDraw()
        {
            if (IsDrawing)
            {
                InkSettingsPanel.Visibility = Visibility.Collapsed;
                ClearSelection();
                
            }
            else
            {
                InkSettingsPanel.Visibility = Visibility.Visible;
                UpdateSelectionMode();
                InkToolbar.ActiveTool = InkToolbar.GetToolButton(InkToolbarTool.BallpointPen);
            }
            IsDrawing = !IsDrawing;
            UpdateInputType();

        }

        private void UpdateSelectionMode()
        {
            if (SelectButton.IsChecked != null && (bool)SelectButton.IsChecked)
            {
                if (InkSelect.IsChecked != null && (bool)InkSelect.IsChecked)
                {
                    _inkSelectionMode = InkSelectionMode.Ink;
                }
                if (DocumentSelect.IsChecked != null && (bool)DocumentSelect.IsChecked)
                {
                    _inkSelectionMode = InkSelectionMode.Document;
                    FreeformView.IsSelectionEnabled = true;
                }
                TargetCanvas.InkPresenter.InputProcessingConfiguration.RightDragAction =
                    InkInputRightDragAction.LeaveUnprocessed;

                TargetCanvas.InkPresenter.UnprocessedInput.PointerPressed +=
                    UnprocessedInput_PointerPressed;
                TargetCanvas.InkPresenter.UnprocessedInput.PointerMoved +=
                    UnprocessedInput_PointerMoved;
                TargetCanvas.InkPresenter.UnprocessedInput.PointerReleased +=
                    UnprocessedInput_PointerReleased;
            }
            else
            {
                if (TargetCanvas != null)
                {
                    TargetCanvas.InkPresenter.UnprocessedInput.PointerPressed -=
                        UnprocessedInput_PointerPressed;
                    TargetCanvas.InkPresenter.UnprocessedInput.PointerMoved -=
                        UnprocessedInput_PointerMoved;
                    TargetCanvas.InkPresenter.UnprocessedInput.PointerReleased -=
                        UnprocessedInput_PointerReleased;
                }
            }
        }

        public bool IsDrawing { get; set; }

        private void SetInkInputType(CoreInputDeviceTypes type)
        {
            TargetCanvas.InkPresenter.InputDeviceTypes = type;
            TargetCanvas.InkPresenter.IsInputEnabled = true;
            switch (type)
            {
                case CoreInputDeviceTypes.Mouse:
                    FreeformView.ManipulationControls.BlockedInputType = PointerDeviceType.Mouse;
                    FreeformView.ManipulationControls.FilterInput = IsDrawing;
                    break;
                case CoreInputDeviceTypes.Pen:
                    FreeformView.ManipulationControls.BlockedInputType = PointerDeviceType.Pen;
                    FreeformView.ManipulationControls.FilterInput = IsDrawing;
                    break;
                case CoreInputDeviceTypes.Touch:
                    FreeformView.ManipulationControls.BlockedInputType = PointerDeviceType.Touch;
                    FreeformView.ManipulationControls.FilterInput = IsDrawing;
                    break;
                default:
                    FreeformView.ManipulationControls.FilterInput = false;
                    TargetCanvas.InkPresenter.IsInputEnabled = false;
                    break;
            }
        }

        public void UpdateInkFieldModelController()
        {
            if (InkFieldModelController != null)
                InkFieldModelController.UpdateStrokesFromList(TargetCanvas.InkPresenter.StrokeContainer.GetStrokes(), TargetCanvas);
        }

        private void UpdateStrokes()
        {
            TargetCanvas.InkPresenter.StrokeContainer.Clear();
            if (InkFieldModelController != null && InkFieldModelController.GetStrokes() != null)
                TargetCanvas.InkPresenter.StrokeContainer.AddStrokes(InkFieldModelController.GetStrokes()
                    .Select(stroke => stroke.Clone()));
            AddAnalyzerData();
        }

        private async void AddAnalyzerData()
        {
            //Need this so that previously drawn circles dont get turned into collections by accident, and so that documents can get made from previously drawn squares etc.
            foreach (var stroke in TargetCanvas.InkPresenter.StrokeContainer.GetStrokes())
            {
                _analyzer.AddDataForStroke(stroke);
            }
            await _analyzer.AnalyzeAsync();
            var shapeRegions = _analyzer.AnalysisRoot.FindNodes(InkAnalysisNodeKind.InkDrawing);
            foreach (InkAnalysisInkDrawing region in shapeRegions)
            {
                if(region.DrawingKind == InkAnalysisDrawingKind.Circle || region.DrawingKind == InkAnalysisDrawingKind.Ellipse)
                _analyzer.RemoveDataForStrokes(region.GetStrokeIds());
            }
        }

        public void UpdateInputType()
        {
            if (IsDrawing && FreeformView.IsLowestSelected)
            {
                if (TouchInputToggle.IsChecked != null && (bool)TouchInputToggle.IsChecked) SetInkInputType(CoreInputDeviceTypes.Touch);
                else SetInkInputType(CoreInputDeviceTypes.Pen);
            }
            else SetInkInputType(CoreInputDeviceTypes.None);
        }

        #endregion

        #region Selection
        private void ClearSelection()
        {
            var strokes = TargetCanvas.InkPresenter.StrokeContainer.GetStrokes();
            foreach (var stroke in strokes)
            {
                stroke.Selected = false;
            }
            if (SelectionCanvas.Children.Any())
            {
                SelectionCanvas.Children.Clear();
                _boundingRect = Rect.Empty;
            }
        }
        

        private void SelectDocs(PointCollection selectionPoints)
        {
            SelectionCanvas.Children.Clear();
            FreeformView.DeselectAll();
            var selectionList =  _lassoHelper.GetSelectedDocuments(new List<Point>(selectionPoints.Select(p => new Point(p.X - 30000, p.Y-30000))));
            foreach (var docView in selectionList)
            {
                FreeformView.Select(docView);
            }
        }

        private void DrawBoundingRect()
        {
            SelectionCanvas.Children.Clear();

            // Draw a bounding rectangle only if there are ink strokes 
            // within the lasso area.
            if (!(_boundingRect.Width == 0 ||
                  _boundingRect.Height == 0 ||
                  _boundingRect.IsEmpty))
            {
                _rectangle = new InkSelectionRect(FreeformView, TargetCanvas.InkPresenter.StrokeContainer)
                {
                    Width = _boundingRect.Width + 50,
                    Height = _boundingRect.Height + 50,
                };

                Canvas.SetLeft(_rectangle, _boundingRect.X - 25);
                Canvas.SetTop(_rectangle, _boundingRect.Y - 25);

                SelectionCanvas.Children.Add(_rectangle);
            }
        }

        #endregion

        #region Event Handlers

        private void InkToolbarOnEraseAllClicked(InkToolbar sender, object args)
        {
            UpdateInkFieldModelController();
        }

        private void UndoButton_OnTapped(object sender, TappedRoutedEventArgs e)
        {
            InkFieldModelController?.Undo(TargetCanvas);
            ClearSelection();
        }

        private void RedoButton_OnTapped(object sender, TappedRoutedEventArgs e)
        {
            InkFieldModelController?.Redo(TargetCanvas);
            ClearSelection();
        }

        private void TargetCanvasOnPointerPressed(object sender, PointerRoutedEventArgs e)
        {
            if (e.Pointer.PointerDeviceType == PointerDeviceType.Touch)
            {
                _pressedPoint = e.GetCurrentPoint(SelectionCanvas).Position;
                IsPressed = true;
                Debug.WriteLine(IsPressed);
            }
        }

        private void TargetCanvasOnHolding(object sender, HoldingRoutedEventArgs e)
        {
            if (e.PointerDeviceType == PointerDeviceType.Touch)
            {
                _pressedPoint = e.GetPosition(SelectionCanvas);
                IsPressed = true;
                Debug.WriteLine(IsPressed);
            }
        }

        private void TargetCanvas_PointerExited(object sender, PointerRoutedEventArgs e)
        {
            if (e.Pointer.PointerDeviceType == PointerDeviceType.Touch)
            {
                //IsPressed = false;
                Debug.WriteLine(IsPressed);
            }
        }

        private void TargetCanvasOnPointerReleased(object sender, PointerRoutedEventArgs e)
        {
            if (e.Pointer.PointerDeviceType == PointerDeviceType.Touch)
            {
                IsPressed = false;
                Debug.WriteLine(IsPressed);
            }
        }

        private void TargetCanvasOnPointerMoved(object sender, PointerRoutedEventArgs e)
        {
            if (e.Pointer.PointerDeviceType == PointerDeviceType.Touch && IsPressed)
            {
                _pressedPoint = e.GetCurrentPoint(SelectionCanvas).Position;
            }
        }


        // Handle unprocessed pointer events from modifed input.
        // The input is used to provide selection functionality.
        // Selection UI is drawn on a canvas under the InkCanvas.
        private void UnprocessedInput_PointerPressed(
            InkUnprocessedInput sender, PointerEventArgs args)
        {
            Window.Current.CoreWindow.PointerCursor = new CoreCursor(CoreCursorType.Hand, 0);
            // Initialize a selection lasso.
            _lasso = new Polygon()
            {
                Stroke = new SolidColorBrush(Colors.Gray),
                StrokeThickness = 1.5 / FreeformView.Zoom,
                StrokeDashArray = new DoubleCollection() { 5, 2 },
                CompositeMode = ElementCompositeMode.SourceOver
            };

            _lasso.Points.Add(args.CurrentPoint.RawPosition);

            SelectionCanvas.Children.Add(_lasso);
        }

        private void UnprocessedInput_PointerMoved(
            InkUnprocessedInput sender, PointerEventArgs args)
        {
            // Add a point to the lasso Polyline object.
            _lasso.Points.Add(args.CurrentPoint.RawPosition);
        }

        private void UnprocessedInput_PointerReleased(
            InkUnprocessedInput sender, PointerEventArgs args)
        {
            Window.Current.CoreWindow.PointerCursor = new CoreCursor(CoreCursorType.Arrow, 0);
            // Add the final point to the Polyline object and 
            // select strokes within the lasso area.
            // Draw a bounding box on the selection canvas 
            // around the selected ink strokes.
            _lasso.Points.Add(args.CurrentPoint.RawPosition);

            _boundingRect =
                TargetCanvas.InkPresenter.StrokeContainer.SelectWithPolyLine(
                    _lasso.Points);

            if (_inkSelectionMode == InkSelectionMode.Ink) DrawBoundingRect();
            else if (_inkSelectionMode == InkSelectionMode.Document) SelectDocs(_lasso.Points);
        }

        private void TouchInputToggle_OnTapped(object sender, TappedRoutedEventArgs e)
        {
            UpdateInputType();
        }

        private void InkFieldModelControllerOnInkUpdated(InkCanvas sender, FieldUpdatedEventArgs args)
        {
            if (!sender.Equals(TargetCanvas) || args?.Action == DocumentController.FieldUpdatedAction.Replace)
            {
                UpdateStrokes();
            }
        }

        private void SelectButton_Unchecked(object sender, RoutedEventArgs e)
        {
            UpdateSelectionMode();
        }

        private void SelectButton_OnTapped(object sender, TappedRoutedEventArgs e)
        {
            UpdateSelectionMode();
        }

        private void InkPresenterOnStrokesErased(InkPresenter sender, InkStrokesErasedEventArgs e)
        {
            _analyzer.RemoveDataForStrokes(e.Strokes.Select(stroke => stroke.Id));
            UpdateInkFieldModelController();
        }

        private void InkPresenterOnStrokesCollected(InkPresenter sender, InkStrokesCollectedEventArgs e)
        {
            _analyzer.AddDataForStrokes(e.Strokes);
            UpdateInkFieldModelController();
            RecognizeInk(false, e.Strokes.Last());
        }

        private void InkSelect_OnChecked(object sender, RoutedEventArgs e)
        {
            UpdateSelectionMode();
        }

        private void DocumentSelect_OnChecked(object sender, RoutedEventArgs e)
        {
            UpdateSelectionMode();
        }
        private void InkToolbarOnActiveToolChanged(InkToolbar sender, object args)
        {
            UpdateSelectionMode();
            if (TargetCanvas.InkPresenter.InputProcessingConfiguration.Mode == InkInputProcessingMode.Erasing)
            {
                ClearSelection();
            }
        }

        private void StrokeInputOnStrokeStarted(InkStrokeInput sender, PointerEventArgs args)
        {
            ClearSelection();
        }

        #endregion

        
    }
}
