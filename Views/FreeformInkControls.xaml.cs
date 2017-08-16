using System;
using System.Collections.Generic;
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

// The User Control item template is documented at https://go.microsoft.com/fwlink/?LinkId=234236

namespace Dash
{
    public sealed partial class FreeformInkControls : UserControl
    {
        public CollectionFreeformView FreeformView;
        public Canvas SelectionCanvas;
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
        Symbol SelectIcon = (Symbol)0xEF20;
        Symbol TouchIcon = Symbol.TouchPointer;
        private enum InkSelectionMode
        {
            Document, Ink
        }
        private InkSelectionMode _inkSelectionMode;
        private Polygon _lasso;
        private Rect _boundingRect;
        private InkSelectionRect _rectangle;
        private LassoSelectHelper _lassoHelper;

        public FreeformInkControls(CollectionFreeformView view, InkCanvas canvas, Canvas selectionCanvas)
        {
            this.InitializeComponent();
            TargetCanvas = canvas;
            FreeformView = view;
            SelectionCanvas = selectionCanvas;
            InkFieldModelController = view.InkFieldModelController;
            IsDrawing = true;
            _lassoHelper = new LassoSelectHelper(FreeformView);
            TargetCanvas.InkPresenter.InputDeviceTypes = GlobalInkSettings.InkInputType;
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
            InkFieldModelController.InkUpdated += InkFieldModelControllerOnInkUpdated;
            InkToolbar.EraseAllClicked += InkToolbarOnEraseAllClicked;
            InkToolbar.ActiveToolChanged += InkToolbarOnActiveToolChanged;
        }

        private void Paste_Tapped(object sender, TappedRoutedEventArgs e)
        {
            TargetCanvas.InkPresenter.StrokeContainer.PasteFromClipboard(e.GetPosition(TargetCanvas));
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
                SetInkInputType(GlobalInkSettings.InkInputType);
                UpdateSelectionMode();
            }
            IsDrawing = !IsDrawing;
            UpdateInputType();

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

        private void UpdateStrokes()
        {
            TargetCanvas.InkPresenter.StrokeContainer.Clear();
            if (InkFieldModelController != null && InkFieldModelController.GetStrokes() != null)
                TargetCanvas.InkPresenter.StrokeContainer.AddStrokes(InkFieldModelController.GetStrokes().Select(stroke => stroke.Clone()));
        }

        private void InkPresenterOnStrokesErased(InkPresenter sender, InkStrokesErasedEventArgs e)
        {
            UpdateInkFieldModelController();
        }

        private void InkPresenterOnStrokesCollected(InkPresenter sender, InkStrokesCollectedEventArgs args)
        {
            UpdateInkFieldModelController();
        }

        private void SelectDocs(PointCollection selectionPoints)
        {
            SelectionCanvas.Children.Clear();
            FreeformView.ViewModel.SelectionGroup = _lassoHelper.GetSelectedDocuments(new List<Point>(selectionPoints.Select(p => new Point(p.X - 30000, p.Y-30000))));
        }

        //TODO: position ruler
        private void InkToolbar_OnIsRulerButtonCheckedChanged(InkToolbar sender, object args)
        {
            InkPresenterRuler ruler = new InkPresenterRuler(TargetCanvas.InkPresenter);
            ruler.Transform = Matrix3x2.CreateTranslation(new Vector2(30000, 30000));
            ruler.IsVisible = true;

        }

        private void SelectButton_OnTapped(object sender, TappedRoutedEventArgs e)
        {
            UpdateSelectionMode();
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

        public void UpdateInputType()
        {
            if (IsDrawing && FreeformView.IsLowestSelected)
            {
                if (TouchInputToggle.IsChecked != null && (bool)TouchInputToggle.IsChecked) SetInkInputType(CoreInputDeviceTypes.Touch);
                else SetInkInputType(CoreInputDeviceTypes.Pen);
            }
            else SetInkInputType(CoreInputDeviceTypes.None);
        }
    }
}
