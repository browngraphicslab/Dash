using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using System.Windows.Input;
using Windows.ApplicationModel.DataTransfer;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI;
using Windows.UI.Core;
using Windows.UI.Input.Inking;
using Windows.UI.Input.Inking.Analysis;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using Windows.UI.Xaml.Shapes;

// The User Control item template is documented at http://go.microsoft.com/fwlink/?LinkId=234236

namespace Dash
{
    public sealed partial class InkCanvasControl : SelectionElement
    {
        public InkFieldModelController InkFieldModelController;
        private readonly bool _isInterfaceBuilder;
        private ManipulationControls _controls;
        private InkAnalyzer _inkAnalyzer;
        Symbol SelectIcon = (Symbol) 0xEF20;

        public Grid Grid => XGrid;

        // Stroke selection tool.
        private Polyline lasso;
        // Stroke selection area.
        private Rect boundingRect;

        private UIElement _copyMenu;
        private Rectangle _rectangle;
        private Point _pastePos;

        /// <summary>
        /// A control that contains an InkCanvas and interacts with an InkFieldModelController to reflect user strokes 
        /// on the canvas in the underlying data.
        /// </summary>
        /// <param name="inkFieldModelController"></param>
        public InkCanvasControl(InkFieldModelController inkFieldModelController, bool isInterfaceBuilder)
        {
            this.InitializeComponent();
            _inkAnalyzer = new InkAnalyzer();
            _isInterfaceBuilder = isInterfaceBuilder;
            _copyMenu = MakeCopyMenu();
            GlobalInkSettings.Presenters.Add(XInkCanvas.InkPresenter);
            GlobalInkSettings.SetAttributes();
            XInkCanvas.InkPresenter.InputDeviceTypes = GlobalInkSettings.InkInputType;
            InkFieldModelController = inkFieldModelController;
            XInkCanvas.InkPresenter.StrokesCollected += InkPresenterOnStrokesCollected;
            XInkCanvas.InkPresenter.StrokesErased += InkPresenterOnStrokesErased;
            XInkCanvas.InkPresenter.StrokeInput.StrokeStarted += StrokeInputOnStrokeStarted;
            DoubleTapped += XInkCanvasOnDoubleTapped;
            InkFieldModelController.InkUpdated += InkFieldModelControllerOnInkUpdated;
            Loaded += OnLoaded;
            XInkCanvas.Tapped += OnTapped;
            Tapped += OnTapped;
            OnLowestActivated(false);
        }

        private void XInkCanvasOnDoubleTapped(object sender, DoubleTappedRoutedEventArgs e)
        {
            var transform = ScrollViewer.TransformToVisual(Grid);
            _pastePos = transform.TransformPoint(e.GetPosition(SelectionCanvas));
            SelectionFlyout.ShowAt(SelectionCanvas, _pastePos);
            e.Handled = true;
        }

        private UIElement MakeCopyMenu()
        {
            Button copy = new Button{Content = "Copy"};
            copy.Tapped += copyButton_Click;
            Button cut = new Button{Content = "Cut"};
            cut.Tapped += cutButton_Click;
            Button paste = new Button { Content = "Paste" };
            paste.Tapped += pasteButton_Click;
            StackPanel panel = new StackPanel() {Children = {copy, cut, paste}};
            return panel;

        }

        private void StrokeInputOnStrokeStarted(InkStrokeInput sender, PointerEventArgs args)
        {
            ClearSelection();
        }

        private void InkFieldModelControllerOnInkUpdated(InkCanvas sender, FieldUpdatedEventArgs args)
        {
            if (!sender.Equals(XInkCanvas) || args?.Action == DocumentController.FieldUpdatedAction.Replace)
            {
                UpdateStrokes();
            }
        }


        private void OnTapped(object sender, TappedRoutedEventArgs e)
        {
            if (!_isInterfaceBuilder)
            {
                OnSelected();
                e.Handled = true;
            }
        }

        private void InkFieldModelControllerOnFieldModelUpdated(FieldModelController sender, FieldUpdatedEventArgs args, Context context)
        {
            if (!IsLowestSelected || args?.Action == DocumentController.FieldUpdatedAction.Replace)
            {
                UpdateStrokes();
            }
        }

        private void UpdateStrokes()
        {
            XInkCanvas.InkPresenter.StrokeContainer.Clear();
            if (InkFieldModelController != null && InkFieldModelController.GetStrokes() != null)
                XInkCanvas.InkPresenter.StrokeContainer.AddStrokes(InkFieldModelController.GetStrokes().Select(stroke => stroke.Clone()));
        }

        /// <summary>
        /// If the field model already has strokes, adds them to the new ink canvas.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="routedEventArgs"></param>
        private void OnLoaded(object sender, RoutedEventArgs routedEventArgs)
        {
            XInkCanvas.InkPresenter.StrokeContainer = new InkStrokeContainer();
            if (InkFieldModelController != null && InkFieldModelController.GetStrokes() != null)
                XInkCanvas.InkPresenter.StrokeContainer.AddStrokes(InkFieldModelController.GetStrokes().Select(stroke => stroke.Clone()));
            ScrollViewer.ChangeView(XInkCanvas.Width / 2 - ActualWidth / 2, XInkCanvas.Height / 2 - ActualHeight / 2, 1);
        }

        /// <summary>
        /// When strokes are erased, modifies the controller's Strokes field to remove those strokes.
        /// Then calls update data on the controller so that the field model reflects the changes.
        /// TODO: the field model need not be updated with every stroke
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="args"></param>
        private void InkPresenterOnStrokesErased(InkPresenter sender, InkStrokesErasedEventArgs e)
        {
            UpdateInkFieldModelController();
        }

        /// <summary>
        /// When strokes are collected, adds them to the controller's HashSet of InkStrokes.
        /// Then calls update data on the controller so that the field model reflects the changes.
        /// TODO: the field model need not be updated with every stroke
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="args"></param>
        private void InkPresenterOnStrokesCollected(InkPresenter sender, InkStrokesCollectedEventArgs args)
        {
            UpdateInkFieldModelController();
        }

        public bool RecognizeText { get; set; }

        private void UpdateInkFieldModelController()
        {
            if (InkFieldModelController != null)
                InkFieldModelController.UpdateStrokesFromList(XInkCanvas.InkPresenter.StrokeContainer.GetStrokes(),
                    XInkCanvas);
        }

        protected override void OnActivated(bool isSelected)
        {
            // Do nothing
        }

        protected override void OnLowestActivated(bool act)
        {
            UpdateStrokes();
            //When lowest activated, ink canvas is drawable
            if (act)
            {
                EditingSymbol.Foreground = new SolidColorBrush(Colors.Black);
                EditButton.IsHitTestVisible = true;
                XGrid.BorderBrush = (SolidColorBrush)Application.Current.Resources["WindowsBlue"];
                XInkCanvas.InkPresenter.IsInputEnabled = true;
                ScrollViewer.HorizontalScrollMode = ScrollMode.Enabled;
                ScrollViewer.VerticalScrollMode = ScrollMode.Enabled;
                ManipulationMode = ManipulationModes.None;
            } else
            {
                ClearSelection();
                EditingSymbol.Foreground = new SolidColorBrush(Colors.LightGray);
                XGrid.BorderBrush = new SolidColorBrush(Colors.Black);
                XInkCanvas.InkPresenter.IsInputEnabled = false;
                ScrollViewer.HorizontalScrollMode = ScrollMode.Disabled;
                ScrollViewer.VerticalScrollMode = ScrollMode.Disabled;
                if(InkToolbar.Visibility == Visibility.Visible) xCollapseSettings.Begin();
                ManipulationMode = ManipulationModes.All;
                EditButton.IsHitTestVisible = false;
                
            }
        }

        private void XCollapseSettingsOnCompleted(object sender, object o)
        {
            InkToolbar.Visibility = Visibility.Collapsed;
            RedoButton.Visibility = Visibility.Collapsed;
            UndoButton.Visibility = Visibility.Collapsed;
            ToolbarScroller.Visibility = Visibility.Collapsed;
        }

        private void Grid_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            ClipRect.Rect = new Rect(0, 0, e.NewSize.Width, e.NewSize.Height);
        }

        private void SelectionElement_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            XGrid.Width = Width;
            XGrid.Height = Height;
        }

        private void EditButton_OnTapped(object sender, TappedRoutedEventArgs e)
        {
            if (InkToolbar.Visibility == Visibility.Visible)
            {
                xCollapseSettings.Begin();
            }
            else
            {
                InkToolbar.Visibility = Visibility.Visible;
                RedoButton.Visibility = Visibility.Visible;
                UndoButton.Visibility = Visibility.Visible;
                ToolbarScroller.Visibility = Visibility.Visible;
                xExpandSettings.Begin();
            }
        }

        private void RedoButton_OnTapped(object sender, TappedRoutedEventArgs e)
        {
            InkFieldModelController?.Redo(XInkCanvas);
        }

        private void UndoButton_OnTapped(object sender, TappedRoutedEventArgs e)
        {
            InkFieldModelController?.Undo(XInkCanvas);
        }

        private void SelectButton_OnTapped(object sender, TappedRoutedEventArgs e)
        {
            XInkCanvas.InkPresenter.InputProcessingConfiguration.RightDragAction =
                InkInputRightDragAction.LeaveUnprocessed;

            // Listen for unprocessed pointer events from modified input.
            // The input is used to provide selection functionality.
            XInkCanvas.InkPresenter.UnprocessedInput.PointerPressed +=
                UnprocessedInput_PointerPressed;
            XInkCanvas.InkPresenter.UnprocessedInput.PointerMoved +=
                UnprocessedInput_PointerMoved;
            XInkCanvas.InkPresenter.UnprocessedInput.PointerReleased +=
                UnprocessedInput_PointerReleased;
        }

        // Clean up selection UI.
        private void ClearSelection()
        {
            var strokes = XInkCanvas.InkPresenter.StrokeContainer.GetStrokes();
            foreach (var stroke in strokes)
            {
                stroke.Selected = false;
            }
            ClearBoundingRect();
        }

        private void ClearBoundingRect()
        {
            if (SelectionCanvas.Children.Any())
            {
                SelectionCanvas.Children.Clear();
                boundingRect = Rect.Empty;
            }
        }

         // Handle unprocessed pointer events from modifed input.
        // The input is used to provide selection functionality.
        // Selection UI is drawn on a canvas under the InkCanvas.
        private void UnprocessedInput_PointerPressed(
            InkUnprocessedInput sender, PointerEventArgs args)
        {
            // Initialize a selection lasso.
            lasso = new Polyline()
            {
                Stroke = new SolidColorBrush(Colors.Gray),
                StrokeThickness = 1,
                StrokeDashArray = new DoubleCollection() { 5, 2 },
            };

            lasso.Points.Add(args.CurrentPoint.RawPosition);

            SelectionCanvas.Children.Add(lasso);
        }

        private void UnprocessedInput_PointerMoved(
            InkUnprocessedInput sender, PointerEventArgs args)
        {
            // Add a point to the lasso Polyline object.
            lasso.Points.Add(args.CurrentPoint.RawPosition);
        }

        private void UnprocessedInput_PointerReleased(
            InkUnprocessedInput sender, PointerEventArgs args)
        {
            // Add the final point to the Polyline object and 
            // select strokes within the lasso area.
            // Draw a bounding box on the selection canvas 
            // around the selected ink strokes.
            lasso.Points.Add(args.CurrentPoint.RawPosition);

            boundingRect =
                XInkCanvas.InkPresenter.StrokeContainer.SelectWithPolyLine(
                    lasso.Points);

            DrawBoundingRect();
        }

        // Draw a bounding rectangle, on the selection canvas, encompassing 
        // all ink strokes within the lasso area.
        private void DrawBoundingRect()
        {
            // Clear all existing content from the selection canvas.
            SelectionCanvas.Children.Clear();

            // Draw a bounding rectangle only if there are ink strokes 
            // within the lasso area.
            if (!((boundingRect.Width == 0) ||
                  (boundingRect.Height == 0) ||
                  boundingRect.IsEmpty))
            {
                _rectangle = new Rectangle()
                {
                    Stroke = new SolidColorBrush(Colors.DarkGray),
                    StrokeThickness = 2,
                    StrokeDashArray = new DoubleCollection() { 5, 2 },
                    Width = boundingRect.Width,
                    Height = boundingRect.Height,
                    Fill = new SolidColorBrush(Colors.Transparent),
                    ManipulationMode = ManipulationModes.TranslateX | ManipulationModes.TranslateY
                };
                _rectangle.ManipulationDelta += RectangleOnManipulationDelta;

                Canvas.SetLeft(_rectangle, boundingRect.X);
                Canvas.SetTop(_rectangle, boundingRect.Y);

                SelectionCanvas.Children.Add(_rectangle);
            }
        }

        private void RectangleOnManipulationDelta(object sender, ManipulationDeltaRoutedEventArgs e)
        {
            var delta = new Point(e.Delta.Translation.X / ScrollViewer.ZoomFactor, e.Delta.Translation.Y/ ScrollViewer.ZoomFactor);
            Canvas.SetLeft(_rectangle, Canvas.GetLeft(_rectangle) + delta.X);
            Canvas.SetTop(_rectangle, Canvas.GetTop(_rectangle) + delta.Y);
            XInkCanvas.InkPresenter.StrokeContainer.MoveSelected(delta);
            UpdateInkFieldModelController();
            e.Handled = true;
        }

        private void cutButton_Click(object sender, RoutedEventArgs e)
        {
            XInkCanvas.InkPresenter.StrokeContainer.CopySelectedToClipboard();
            XInkCanvas.InkPresenter.StrokeContainer.DeleteSelected();
            ClearSelection();
        }

        private void copyButton_Click(object sender, RoutedEventArgs e)
        {
            XInkCanvas.InkPresenter.StrokeContainer.CopySelectedToClipboard();
        }

        private void pasteButton_Click(object sender, RoutedEventArgs e)
        {
            if (XInkCanvas.InkPresenter.StrokeContainer.CanPasteFromClipboard())
            {
                XInkCanvas.InkPresenter.StrokeContainer.PasteFromClipboard(
                    _pastePos);
                
            }
            else
            {
                // Cannot paste from clipboard.
            }
        }

        private void ScrollViewer_ViewChanged(object sender, ScrollViewerViewChangedEventArgs e)
        {
            if (_rectangle != null) _rectangle.StrokeThickness = 2 / ScrollViewer.ZoomFactor;
        }
    }
}
