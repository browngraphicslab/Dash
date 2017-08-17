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
                //InkToolbar.TargetInkCanvas = value;
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
        public LassoSelectHelper LassoHelper;
        
        Symbol TouchIcon = (Symbol)0xED5F;
        public Point PressedPoint = new Point(0,0);
        public Point DoubleTapPoint = new Point(0,0);

        public bool IsPressed
        {
            get { return _isPressed; }
            set
            {
                _isPressed = value;
            }
        }

        
        private bool _isPressed;
        private Dictionary<Rect, Tuple<string, IEnumerable<uint>>> _paragraphBoundsDictionary;
        private readonly InkRecognitionHelper _inkRecognitionHelper;

        public FreeformInkControls(CollectionFreeformView view, InkCanvas canvas, Canvas selectionCanvas)
        {
            this.InitializeComponent();
            TargetCanvas = canvas;
            FreeformView = view;
            SelectionCanvas = selectionCanvas;
            InkFieldModelController = view.InkFieldModelController;
            IsDrawing = true;
            LassoHelper = new LassoSelectHelper(FreeformView);
            _inkRecognitionHelper = new InkRecognitionHelper(this);
            GlobalInkSettings.FreeformInkControls.Add(this);
            GlobalInkSettings.Presenters.Add(TargetCanvas.InkPresenter);
            GlobalInkSettings.SetAttributes();
            UpdateStrokes();
            ToggleDraw();
            AddEventHandlers();
            UpdateInputType();
            
        }

        private void GlobalInkSettingsOnInkInputChanged(CoreInputDeviceTypes newInputType)
        {
            SetInkInputType(newInputType);
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
            TargetCanvas.PointerCaptureLost += TargetCanvasOnPointerCaptureLost;
            InkFieldModelController.InkUpdated += InkFieldModelControllerOnInkUpdated;
            GlobalInkSettings.InkInputChanged += GlobalInkSettingsOnInkInputChanged;
            //InkToolbar.EraseAllClicked += InkToolbarOnEraseAllClicked;
            //InkToolbar.ActiveToolChanged += InkToolbarOnActiveToolChanged;
        }

       

        #region Documents From Drawings

        #endregion

        #region Updating State

        public void ToggleDraw()
        {
            if (IsDrawing)
            {
                //InkSettingsPanel.Visibility = Visibility.Collapsed;
                ClearSelection();
                
            }
            else
            {
                //InkSettingsPanel.Visibility = Visibility.Visible;
                UpdateSelectionMode();
                //InkToolbar.ActiveTool = InkToolbar.GetToolButton(InkToolbarTool.BallpointPen);
            }
            IsDrawing = !IsDrawing;
            UpdateInputType();

        }

        public void UpdateSelectionMode()
        {
            if(GlobalInkSettings.IsSelectionEnabled)
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

        public bool IsDrawing { get; set; }

        public InkRecognitionHelper InkRecognitionHelper
        {
            get { return _inkRecognitionHelper; }
        }

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
            _inkRecognitionHelper.AddAnalyzerData(TargetCanvas.InkPresenter.StrokeContainer.GetStrokes());
        }

        

        public void UpdateInputType()
        {
            if (IsDrawing && FreeformView.IsLowestSelected)
            {
                SetInkInputType(GlobalInkSettings.InkInputType);
            }
            else SetInkInputType(CoreInputDeviceTypes.None);
        }

        #endregion

        #region Selection
        public void ClearSelection()
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
            var selectionList =  LassoHelper.GetSelectedDocuments(new List<Point>(selectionPoints.Select(p => new Point(p.X - 30000, p.Y-30000))));
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

        private void TargetCanvasOnPointerCaptureLost(object sender, PointerRoutedEventArgs e)
        {
            if (e.Pointer.PointerDeviceType == PointerDeviceType.Touch)
            {
                IsPressed = false;
            }
        }

        private void TargetCanvasOnDoubleTapped(object sender, DoubleTappedRoutedEventArgs e)
        {
            DoubleTapPoint = e.GetPosition(SelectionCanvas);
            InkRecognitionHelper.RecognizeInk(true);
        }

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
                PressedPoint = e.GetCurrentPoint(SelectionCanvas).Position;
                IsPressed = true;
                Debug.WriteLine(IsPressed);
            }
        }

        private void TargetCanvasOnHolding(object sender, HoldingRoutedEventArgs e)
        {
            if (e.PointerDeviceType == PointerDeviceType.Touch)
            {
                PressedPoint = e.GetPosition(SelectionCanvas);
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
                PressedPoint = e.GetCurrentPoint(SelectionCanvas).Position;
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
            {
                _inkSelectionMode = InkSelectionMode.Document;
            }
            else
            {
                _inkSelectionMode = InkSelectionMode.Ink;
            }
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
            _inkRecognitionHelper.Analyzer.RemoveDataForStrokes(e.Strokes.Select(stroke => stroke.Id));
            UpdateInkFieldModelController();
        }

        private void InkPresenterOnStrokesCollected(InkPresenter sender, InkStrokesCollectedEventArgs e)
        {
            _inkRecognitionHelper.Analyzer.AddDataForStrokes(e.Strokes);
            UpdateInkFieldModelController();
            InkRecognitionHelper.RecognizeInk(false, e.Strokes.Last());
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
