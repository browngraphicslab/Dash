using System;
using System.Collections.Generic;
using System.Linq;
using Windows.Devices.Input;
using Windows.Foundation;
using Windows.UI;
using Windows.UI.Core;
using Windows.UI.Input.Inking;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Shapes;
using Dash.Views;

// The User Control item template is documented at https://go.microsoft.com/fwlink/?LinkId=234236

namespace Dash
{
    public class FreeformInkControl
    {
        private Rect _boundingRect;
        private InkSelectionMode _inkSelectionMode;
        private bool _isPressed;
        private Polygon _lasso;
        private MenuFlyout _pasteFlyout;
        private InkSelectionRect _rectangle;
        public CollectionFreeformView FreeformView;
        public InkFieldModelController InkFieldModelController;
        public LassoSelectHelper LassoHelper;

        public Canvas SelectionCanvas;
        //private Dictionary<Rect, Tuple<string, IEnumerable<uint>>> _paragraphBoundsDictionary;

        public FreeformInkControl(CollectionFreeformView view, InkCanvas canvas, Canvas selectionCanvas)
        {
            TargetCanvas = canvas;
            FreeformView = view;
            SelectionCanvas = selectionCanvas;
            InkFieldModelController = view.InkFieldModelController;
            LassoHelper = new LassoSelectHelper(FreeformView);
            InkRecognitionHelper = new InkRecognitionHelper(this);
            GlobalInkSettings.FreeformInkControls.Add(this);
            GlobalInkSettings.Presenters.Add(TargetCanvas.InkPresenter);
            GlobalInkSettings.UpdateInkPresenters();
            UpdateStrokes();
            ClearSelection();
            UpdateInputType();
            AddEventHandlers();
        }

        public InkCanvas TargetCanvas { get; set; }
        public InkRecognitionHelper InkRecognitionHelper { get; }


        private void AddEventHandlers()
        {
            TargetCanvas.InkPresenter.StrokesCollected += InkPresenterOnStrokesCollected;
            TargetCanvas.InkPresenter.StrokesErased += InkPresenterOnStrokesErased;
            TargetCanvas.InkPresenter.StrokeInput.StrokeStarted += StrokeInputOnStrokeStarted;
            TargetCanvas.DoubleTapped += TargetCanvasOnDoubleTapped;
            TargetCanvas.RightTapped += TargetCanvasOnRightTapped;
            InkFieldModelController.InkUpdated += InkFieldModelControllerOnInkUpdated;
        }
        

        private void MakeFlyout()
        {
            _pasteFlyout = new MenuFlyout();
            var paste = new MenuFlyoutItem {Text = "Paste"};
            paste.Click += PasteOnClick;
            _pasteFlyout.Items?.Add(paste);
        }

        private enum InkSelectionMode
        {
            Document,
            Ink
        }

        public void RecognizeSelected()
        {
            InkRecognitionHelper.RecognizeAndForgetStrokes(TargetCanvas.InkPresenter.StrokeContainer.GetStrokes().Where(s => s.Selected));
        }

        #region Updating State

        public void UpdateSelectionMode()
        {
            if (GlobalInkSettings.IsSelectionEnabled)
            {
                TargetCanvas.InkPresenter.InputProcessingConfiguration.RightDragAction =
                    InkInputRightDragAction.LeaveUnprocessed;
                TargetCanvas.InkPresenter.InputProcessingConfiguration.Mode = InkInputProcessingMode.None;
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
                    TargetCanvas.InkPresenter.InputProcessingConfiguration.Mode = InkInputProcessingMode.Inking;
                    TargetCanvas.InkPresenter.UnprocessedInput.PointerPressed -=
                        UnprocessedInput_PointerPressed;
                    TargetCanvas.InkPresenter.UnprocessedInput.PointerMoved -=
                        UnprocessedInput_PointerMoved;
                    TargetCanvas.InkPresenter.UnprocessedInput.PointerReleased -=
                        UnprocessedInput_PointerReleased;
                }
            }
        }


        private void SetInkInputType(CoreInputDeviceTypes type)
        {
            TargetCanvas.InkPresenter.InputDeviceTypes = type;
            TargetCanvas.InkPresenter.IsInputEnabled = FreeformView.IsSelected;
            FreeformView.ManipulationControls.FilterInput = true;
            switch (type)
            {
                case CoreInputDeviceTypes.Mouse:
                    FreeformView.ManipulationControls.BlockedInputType = PointerDeviceType.Mouse;
                    break;
                case CoreInputDeviceTypes.Pen:
                    FreeformView.ManipulationControls.BlockedInputType = PointerDeviceType.Pen;
                    break;
                case CoreInputDeviceTypes.Touch:
                    FreeformView.ManipulationControls.BlockedInputType = PointerDeviceType.Touch;
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
                InkFieldModelController.UpdateStrokesFromList(TargetCanvas.InkPresenter.StrokeContainer.GetStrokes(),
                    TargetCanvas);
        }

        private void UpdateStrokes()
        {
            TargetCanvas.InkPresenter.StrokeContainer.Clear();
            if (InkFieldModelController != null && InkFieldModelController.GetStrokes() != null)
                TargetCanvas.InkPresenter.StrokeContainer.AddStrokes(InkFieldModelController.GetStrokes()
                    .Select(stroke => stroke.Clone()));
        }

        public void UpdateInputType()
        {
            SetInkInputType(GlobalInkSettings.InkInputType);
        }

        #endregion

        #region Selection

        public void ClearSelection()
        {
            var strokes = TargetCanvas.InkPresenter.StrokeContainer.GetStrokes();
            foreach (var stroke in strokes)
                stroke.Selected = false;
            if (SelectionCanvas.Children.Any())
            {
                SelectionCanvas.Children.Clear();
                _boundingRect = Rect.Empty;
            }
        }


        private void SelectDocs(PointCollection selectionPoints)
        {
            if (!FreeformView.IsSelectionEnabled)
            {
                var colView = FreeformView.GetFirstAncestorOfType<CollectionView>();
                colView.MakeSelectionModeMultiple();
            }
            SelectionCanvas.Children.Clear();
            FreeformView.DeselectAll();
            var selectionList =
                LassoHelper.GetSelectedDocuments(
                    new List<Point>(selectionPoints.Select(p => new Point(p.X - 30000, p.Y - 30000))));
            foreach (var docView in selectionList)
            {
                FreeformView.Select(docView);
                FreeformView.AddToPayload(docView);
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
                    Height = _boundingRect.Height + 50
                };

                Canvas.SetLeft(_rectangle, _boundingRect.X - 25);
                Canvas.SetTop(_rectangle, _boundingRect.Y - 25);

                SelectionCanvas.Children.Add(_rectangle);
            }
        }

        #endregion

        #region Event Handlers

        private void PasteOnClick(object sender, RoutedEventArgs routedEventArgs)
        {
            var menu = sender as MenuFlyoutItem;
            var transform = menu.TransformToVisual(TargetCanvas);
            var pointOnCanvas = transform.TransformPoint(new Point());
            TargetCanvas.InkPresenter.StrokeContainer.PasteFromClipboard(pointOnCanvas);
        }

        private void TargetCanvasOnRightTapped(object sender, RightTappedRoutedEventArgs e)
        {
            if (_pasteFlyout == null) MakeFlyout();
            _pasteFlyout.ShowAt(TargetCanvas, e.GetPosition(TargetCanvas));
        }

        private void TargetCanvasOnDoubleTapped(object sender, DoubleTappedRoutedEventArgs e)
        {
            if (GlobalInkSettings.IsRecognitionEnabled)
            {
                InkRecognitionHelper.DoubleTapped = true;
                InkRecognitionHelper.DoubleTappedPoints.Add(e.GetPosition(SelectionCanvas));
            }
        }


        // Handle unprocessed pointer events from modifed input.
        // The input is used to provide selection functionality.
        // Selection UI is drawn on a canvas under the InkCanvas.
        private void UnprocessedInput_PointerPressed(
            InkUnprocessedInput sender, PointerEventArgs args)
        {
            Window.Current.CoreWindow.PointerCursor = new CoreCursor(CoreCursorType.Cross, 0);
            if (args.CurrentPoint.Properties.IsBarrelButtonPressed || args.CurrentPoint.Properties.IsRightButtonPressed)
                _inkSelectionMode = InkSelectionMode.Document;
            else
                _inkSelectionMode = InkSelectionMode.Ink;
            // Initialize a selection lasso.
            _lasso = new Polygon
            {
                Stroke = new SolidColorBrush(Colors.Gray),
                StrokeThickness = 1.5 / FreeformView.Zoom,
                StrokeDashArray = new DoubleCollection {5, 2},
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

        private void InkFieldModelControllerOnInkUpdated(InkCanvas sender, FieldUpdatedEventArgs args)
        {
            if (!sender.Equals(TargetCanvas) || args?.Action == DocumentController.FieldUpdatedAction.Replace)
                UpdateStrokes();
        }

        private void InkPresenterOnStrokesErased(InkPresenter sender, InkStrokesErasedEventArgs e)
        {
            InkRecognitionHelper.RemoveStrokeData(new List<InkStroke>(e.Strokes));
            UpdateInkFieldModelController();
        }

        private void InkPresenterOnStrokesCollected(InkPresenter sender, InkStrokesCollectedEventArgs e)
        {
            UpdateInkFieldModelController();
            if (GlobalInkSettings.IsRecognitionEnabled)
                InkRecognitionHelper.AddStrokeData(new List<InkStroke>(e.Strokes));
        }


        private void StrokeInputOnStrokeStarted(InkStrokeInput sender, PointerEventArgs args)
        {
            ClearSelection();
        }

        #endregion
    }
}