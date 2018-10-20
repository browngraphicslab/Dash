using Dash.Views;
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

// The User Control item template is documented at https://go.microsoft.com/fwlink/?LinkId=234236

namespace Dash
{
    /// <summary>
    /// This class acts as a manager for the InkCanvas associated with each FreeformCollectionView. It 
    /// controls what inputs the InkCanvas sees and how those inputs are processed; i.e. for ink, erasing, selecting, etc.
    /// For ink recognition, the FreeformInkControl passes ink data to the InkRecognitionHelper associated with this control.
    /// TODO: Could every FreeformInkControl use the same instance of the InkRecognitionHelper? Would that be more efficient?
    /// TODO: This class could be abstracted so that it can be applied to managing ink on documents or in other places.
    /// TODO: The LassoSelectHelper could be moved into this class.
    /// </summary>
    public class FreeformInkControl
    {
        private enum InkSelectionMode
        {
            Document,
            Ink
        }
        private Rect _boundingRect;
        private InkSelectionMode _inkSelectionMode;
        private bool _analyzeStrokes;
        private bool _selecting;
        private Polygon _lasso;
        private MenuFlyout _pasteFlyout;
        private InkSelectionRect _rectangle;
        public CollectionFreeformBase FreeformView;
        public LassoSelectHelper LassoHelper;
        public Canvas SelectionCanvas;
        public InkCanvas TargetInkCanvas { get; set; }
        public InkRecognitionHelper InkRecognitionHelper { get; }


        public FreeformInkControl(CollectionFreeformBase view, InkCanvas canvas, Canvas selectionCanvas)
        {
            TargetInkCanvas = canvas;
            FreeformView = view;
            SelectionCanvas = selectionCanvas;
            LassoHelper = new LassoSelectHelper(FreeformView);
            InkRecognitionHelper = new InkRecognitionHelper(this);
            TargetInkCanvas.InkPresenter.InputProcessingConfiguration.Mode =
                GlobalInkSettings.StrokeType == GlobalInkSettings.StrokeTypes.Eraser
                    ? InkInputProcessingMode.Erasing
                    : InkInputProcessingMode.Inking;
            TargetInkCanvas.InkPresenter.UpdateDefaultDrawingAttributes(GlobalInkSettings.Attributes);
            TargetInkCanvas.InkPresenter.InputProcessingConfiguration.RightDragAction = InkInputRightDragAction.AllowProcessing;
            //TargetInkCanvas.InkPresenter.InputDeviceTypes = CoreInputDeviceTypes.None;
            UpdateStrokes();
            SetInkInputType(GlobalInkSettings.InkInputType);
            UpdateSelectionMode();
            UndoSelection();
            AddEventHandlers();
        }

        private void AddEventHandlers()
        {
            TargetInkCanvas.InkPresenter.StrokesCollected += InkPresenterOnStrokesCollected;
            TargetInkCanvas.InkPresenter.StrokesErased += InkPresenterOnStrokesErased;
            TargetInkCanvas.InkPresenter.StrokeInput.StrokeStarted += StrokeInputOnStrokeStarted;
            TargetInkCanvas.InkPresenter.StrokeInput.StrokeContinued += StrokeInputOnStrokeContinued;
            TargetInkCanvas.RightTapped += TargetCanvasOnRightTapped;
            FreeformView.ViewModel.InkController.FieldModelUpdated += InkControllerOnInkUpdated;
            GlobalInkSettings.InkSettingsUpdated += GlobalInkSettingsOnInkSettingsUpdated;
        }

        private void MakeFlyout()
        {

            if (_inkSelectionMode == InkSelectionMode.Ink)
            {
                _pasteFlyout = new MenuFlyout();
                var paste = new MenuFlyoutItem { Text = "Paste" };
                paste.Click += PasteOnClick;
                _pasteFlyout.Items?.Add(paste);
            }
            
        }

        

        #region Updating settings

        /// <summary>
        /// Adds/removes unprocessed input handlers depending whether selection is enabled, so that 
        /// drawing either makes a selection polyline or InkStrokes, respectively
        /// </summary>
        public void UpdateSelectionMode()
        {
            if (GlobalInkSettings.StrokeType == GlobalInkSettings.StrokeTypes.Selection && !_selecting)
            {
                TargetInkCanvas.InkPresenter.InputProcessingConfiguration.RightDragAction =
                    InkInputRightDragAction.LeaveUnprocessed;
                TargetInkCanvas.InkPresenter.InputProcessingConfiguration.Mode = InkInputProcessingMode.None;
                TargetInkCanvas.InkPresenter.UnprocessedInput.PointerPressed +=
                    UnprocessedInput_PointerPressed;
                TargetInkCanvas.InkPresenter.UnprocessedInput.PointerMoved +=
                    UnprocessedInput_PointerMoved;
                TargetInkCanvas.InkPresenter.UnprocessedInput.PointerReleased +=
                    UnprocessedInput_PointerReleased;
                _selecting = true;
            }
            else
            {
                if (TargetInkCanvas != null && _selecting && GlobalInkSettings.StrokeType != GlobalInkSettings.StrokeTypes.Selection)
                {
                    TargetInkCanvas.InkPresenter.InputProcessingConfiguration.RightDragAction = InkInputRightDragAction.AllowProcessing;
                    TargetInkCanvas.InkPresenter.InputProcessingConfiguration.Mode = InkInputProcessingMode.Inking;
                    TargetInkCanvas.InkPresenter.UnprocessedInput.PointerPressed -=
                        UnprocessedInput_PointerPressed;
                    TargetInkCanvas.InkPresenter.UnprocessedInput.PointerMoved -=
                        UnprocessedInput_PointerMoved;
                    TargetInkCanvas.InkPresenter.UnprocessedInput.PointerReleased -=
                        UnprocessedInput_PointerReleased;
                    _selecting = false;
                }
            }
        }

        /// <summary>
        /// - Sets the device type that the InkCanvas will accept input from. 
        /// - Blocks input from that device type in the host CollectionFreeformView, so that you don't accidentally scroll the background of the freeform
        /// view when you intend to draw.
        /// </summary>
        /// <param name="type"></param>
        private void SetInkInputType(CoreInputDeviceTypes type)
        {
            TargetInkCanvas.InkPresenter.InputDeviceTypes = type;
            TargetInkCanvas.InkPresenter.IsInputEnabled = true;
            FreeformView.ViewManipulationControls.FilterInput = true;
            switch (type)
            {
                case CoreInputDeviceTypes.Mouse:
                    FreeformView.ViewManipulationControls.BlockedInputType = PointerDeviceType.Mouse;
                    break;
                case CoreInputDeviceTypes.Pen:
                    FreeformView.ViewManipulationControls.BlockedInputType = PointerDeviceType.Pen;
                    break;
                case CoreInputDeviceTypes.Touch:
                    FreeformView.ViewManipulationControls.BlockedInputType = PointerDeviceType.Touch;
                    break;
                default:
                    FreeformView.ViewManipulationControls.FilterInput = false;
                    TargetInkCanvas.InkPresenter.IsInputEnabled = false;
                    break;
            }
        }

        /// <summary>
        /// Updates the InkFieldModel's data from the InkStrokes
        /// </summary>
        public void UpdateInkController()
        {
            FreeformView.ViewModel.InkController?.UpdateStrokesFromList(TargetInkCanvas.InkPresenter.StrokeContainer.GetStrokes());
        }

        /// <summary>
        /// Updates InkStrokes from the InkFieldModel's data
        /// </summary>
        private void UpdateStrokes()
        {
            TargetInkCanvas.InkPresenter.StrokeContainer.Clear();
            if (FreeformView.ViewModel.InkController?.GetStrokes() != null)
                TargetInkCanvas.InkPresenter.StrokeContainer.AddStrokes(FreeformView.ViewModel.InkController.GetStrokes()
                    .Select(stroke => stroke.Clone()));
        }

        #endregion

        #region Selection

        /// <summary>
        /// Tells the InkRecognitionHelper to try to recognize all selected InkStrokes.
        /// Clears selection so that InkSelectionRect does not contain deleted strokes.
        /// </summary>
        public void RecognizeSelectedStrokes()
        {
            //InkRecognitionHelper.RecognizeAndForgetStrokes(TargetCanvas.InkPresenter.StrokeContainer.GetStrokes().Where(s => s.Selected));
            InkRecognitionHelper.RecognizeInk(true);
            UndoSelection();
        }

        /// <summary>
        /// Clears all UI required for selection and deselects InkStrokes, disposes of InkSelectionRectangle.
        /// </summary>
        public void UndoSelection()
        {
            var strokes = TargetInkCanvas.InkPresenter.StrokeContainer.GetStrokes();
            foreach (var stroke in strokes)
                stroke.Selected = false;
            _rectangle?.Dispose();
            _rectangle = null;
            _boundingRect = Rect.Empty;
            _lasso = null;
            SelectionCanvas.Children.Clear();
        }

        /// <summary>
        /// uses LassoHelper to get and select all of the documents with at least 3 points 
        /// (including center) inside _lasso's convex hull. If
        /// </summary>
        /// <param name="selectionPoints"></param>
        private void LassoSelectDocs(PointCollection selectionPoints)
        {
            SelectionManager.DeselectAll();
            var selectionList =
                LassoHelper.GetSelectedDocuments(
                    new List<Point>(selectionPoints.Select(p => new Point(p.X - 30000, p.Y - 30000)))); //Adjust for offset of InkCanvas vs FreeformView's ItemsControl
            SelectionManager.SelectDocuments(selectionList, false);
        }

        /// <summary>
        /// Adds an InkSelectionRect around the selected InkStrokes.
        /// </summary>
        private void SelectInk()
        {
            SelectionCanvas.Children.Clear();
            _lasso = null;
            _rectangle?.Dispose();
            _rectangle = null;

            // Draw a bounding rectangle only if there are ink strokes 
            // within the lasso area.
            if (!(_boundingRect.Width == 0 ||
                  _boundingRect.Height == 0 ||
                  _boundingRect.IsEmpty))
            {
                _rectangle = new InkSelectionRect(FreeformView, TargetInkCanvas.InkPresenter.StrokeContainer)
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
            var transform = menu.TransformToVisual(TargetInkCanvas);
            var pointOnCanvas = transform.TransformPoint(new Point());
            TargetInkCanvas.InkPresenter.StrokeContainer.PasteFromClipboard(pointOnCanvas);
        }

        private void TargetCanvasOnRightTapped(object sender, RightTappedRoutedEventArgs e)
        {

            if (_inkSelectionMode == InkSelectionMode.Ink)
            {
                if (_pasteFlyout == null) MakeFlyout();
                _pasteFlyout.ShowAt(TargetInkCanvas, e.GetPosition(TargetInkCanvas));
            }
        }

        // Handle unprocessed pointer events from modifed input.
        // The input is used to provide selection functionality.
        // Selection UI is drawn on a canvas under the InkCanvas.
        private void UnprocessedInput_PointerPressed(
            InkUnprocessedInput sender, PointerEventArgs args)
        {
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
                TargetInkCanvas.InkPresenter.StrokeContainer.SelectWithPolyLine(
                    _lasso.Points);

            if (_inkSelectionMode == InkSelectionMode.Ink)
            {
                SelectInk();
            }
            else if (_inkSelectionMode == InkSelectionMode.Document)
            {
                LassoSelectDocs(_lasso.Points);
            }
        }

        /// <summary>
        /// Updates ink on canvas if there were changes made to the ink field's data 
        /// that didn't come from this control's ink canvas
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="args"></param>
        private void InkControllerOnInkUpdated(FieldControllerBase sender, FieldUpdatedEventArgs args, Context c)
        {
                UpdateStrokes();
        }

        /// <summary>
        /// Removes erased strokes from the analyzer's data and updates ink field, undos selection if anything is selected.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void InkPresenterOnStrokesErased(InkPresenter sender, InkStrokesErasedEventArgs e)
        {
            //InkRecognitionHelper.RemoveStrokeData(new List<InkStroke>(e.Strokes));
            UpdateInkController();
            UndoSelection();
        }

        /// <summary>
        /// Updates the recognition helper with the newly collected strokes, and analyzes them if the barrel/right button was pressed during the last stroke
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void InkPresenterOnStrokesCollected(InkPresenter sender, InkStrokesCollectedEventArgs e)
        {
            UpdateInkController();
            InkRecognitionHelper.SetNewStrokes(new List<InkStroke>(e.Strokes));
            if(_analyzeStrokes) InkRecognitionHelper.RecognizeInk();
            _analyzeStrokes = false;
        }

        private void StrokeInputOnStrokeStarted(InkStrokeInput sender, PointerEventArgs args)
        {
            UndoSelection();
        }
        private void GlobalInkSettingsOnInkSettingsUpdated(InkUpdatedEventArgs args)
        {
            UpdateSelectionMode();
            SetInkInputType(args.InputDeviceTypes);
            TargetInkCanvas.InkPresenter.UpdateDefaultDrawingAttributes(args.InkAttributes);
            TargetInkCanvas.InkPresenter.InputProcessingConfiguration.Mode = args.InputProcessingMode;
        }

        /// <summary>
        /// If you right-click or press the barrel button while drawing a stroke, that stroke will be analyzed at the end of the stroke.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void StrokeInputOnStrokeContinued(InkStrokeInput sender, PointerEventArgs e)
        {
            if (e.CurrentPoint.Properties.IsBarrelButtonPressed ||
                e.CurrentPoint.Properties.IsRightButtonPressed)
            {
                _analyzeStrokes = true;
            }
        }

        #endregion
    }
}
