using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Windows.Foundation;
using Windows.UI.Core;
using Windows.UI.Input.Inking;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;

// The User Control item template is documented at https://go.microsoft.com/fwlink/?LinkId=234236

namespace Dash.Views
{
    /// <summary>
    /// The rectangle that appears when inkstrokes are selected, which can be used to transform the strokes and 
    /// has a flyout menu with options to copy, delete, cut, recognize, edit, and copy the attributes of the strokes.
    /// </summary>
    public sealed partial class InkSelectionRect : UserControl
    {
        private CollectionFreeformView _freeformView;
        private ScrollViewer _scroller;
        private InkStrokeContainer _strokeContainer;
        private Size _startSize;
        private Point _startPosition;
        private Dictionary<InkStroke, Matrix3x2> _startingTransforms;
        private bool _flyoutShowing;
        private bool _editAttributes;
        public Symbol CopyAttributesSymbol { get; set; } = Symbol.Upload;

        private Point Position()
        {
            return new Point(Canvas.GetLeft(this), Canvas.GetTop(this));
        }

        private Point InnerTopLeft()
        {
            return new Point(Canvas.GetLeft(this) + 25, Canvas.GetTop(this) + 25);
        }
        
        public InkSelectionRect(CollectionFreeformView view, InkStrokeContainer strokes, ScrollViewer scroller = null)
        {
            this.InitializeComponent();
            _freeformView = view;
            _scroller = scroller;
            _strokeContainer = strokes;
            Loaded += OnLoaded;
            GlobalInkSettings.InkSettingsUpdated += GlobalInkSettingsOnOnAttributesUpdated;
            UpdateStartingTransforms();
            if (_startingTransforms.Keys.Count != 1)
            {
                //can only copy attributes if a single stroke selected.
                CopyAttributesButton.Visibility = Visibility.Collapsed;
            }
        }

        /// <summary>
        /// Updates the attributes of all selected ink strokes to the global attributes determined 
        /// on the radial menu, if the user has selected to do so from the context flyout option.
        /// </summary>
        /// <param name="args"></param>
        private void GlobalInkSettingsOnOnAttributesUpdated(InkUpdatedEventArgs args)
        {
            if (_editAttributes)
            {
                foreach (var stroke in _startingTransforms.Keys)
                {
                    stroke.DrawingAttributes = args.InkAttributes;
                }
            }
        }


        private void UpdateStartingTransforms()
        {
            _startingTransforms = new Dictionary<InkStroke, Matrix3x2>();
            foreach (InkStroke stroke in _strokeContainer.GetStrokes())
            {
                if (stroke.Selected)
                {
                    _startingTransforms[stroke] = stroke.PointTransform;
                }
            }
        }


        /// <summary>
        /// _startSize records the starting size of the actual bounding rectangle around the inkStrokes.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="routedEventArgs"></param>
        private void OnLoaded(object sender, RoutedEventArgs routedEventArgs)
        {
            _startSize = new Size(Width - 50, Height - 50);
            _startPosition = Position();
        }

        /// <summary>
        /// Resizes the grid in the direction appropriate to the control being dragged and calls the 
        /// method to transform the inkstrokes accordingly.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void DraggerOnManipulationDelta(object sender, ManipulationDeltaRoutedEventArgs e)
        {
            e.Handled = true;
            double scale = _freeformView?.Zoom ?? _scroller.ZoomFactor;
            var translate = new Point(e.Delta.Translation.X / scale, e.Delta.Translation.Y / scale);
            var dragger = sender as Grid;
            float xScale = (float) ((Width - 50) / _startSize.Width);
            float yScale = (float) ((Height - 50) / _startSize.Height);
            if (dragger.Name == "CenterDragger" || dragger.Name == "Grid")
            {
                Canvas.SetLeft(this, Position().X + translate.X);
                Canvas.SetTop(this, Position().Y + translate.Y);
                TransformStrokes(xScale, yScale);
                return;
            }
            if (dragger.Name.Contains("Left"))
            {
                if (Width - translate.X > MinWidth) Canvas.SetLeft(this, Position().X + translate.X);
                translate.X *= -1;
            }
            if (dragger.Name.Contains("Top"))
            {
                if (Height - translate.Y > MinHeight) Canvas.SetTop(this, Position().Y + translate.Y);
                translate.Y *= -1;
            }
            if (Width + translate.X > MinWidth || translate.X > 0)
            {
                xScale = (float) ((Width - 50 + translate.X) / _startSize.Width);
                Width += translate.X;
            }
            if (Height + translate.Y > MinHeight || translate.Y > 0)
            {
                yScale = (float) ((Height - 50 + translate.Y) / _startSize.Height);
                Height += translate.Y;
            }

            TransformStrokes(xScale, yScale);
        }

        /// <summary>
        /// Applies the grid's width + height changes to the point transforms of each stroke by using the net change 
        /// in the rectangle's shape and the stroke's starting transform (when it was selected) to update its point transform.
        /// </summary>
        /// <param name="xScale"></param>
        /// <param name="yScale"></param>
        private void TransformStrokes(float xScale, float yScale)
        {
            var totalTranslation = new Point(Position().X - _startPosition.X, Position().Y - _startPosition.Y);
            Matrix3x2 translationMatrix =
                Matrix3x2.CreateTranslation(new Vector2((float) totalTranslation.X, (float) totalTranslation.Y));
            Matrix3x2 scaleMatrix = Matrix3x2.CreateScale(xScale, yScale,
                new Vector2((float) InnerTopLeft().X, (float) InnerTopLeft().Y));
            foreach (var stroke in _startingTransforms.Keys)
            {
                var startingTransform = _startingTransforms[stroke];
                var translate = Matrix3x2.Multiply(startingTransform, translationMatrix);
                var translateAndScale = Matrix3x2.Multiply(translate, scaleMatrix);
                stroke.PointTransform = translateAndScale;
            }
        }

        /// <summary>
        /// Re-adds the ink data to the InkRecognitionHelper and changes the cursor type back to normal.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnManipulationCompleted(object sender, ManipulationCompletedRoutedEventArgs e)
        {
            //_freeformView.InkControl.InkRecognitionHelper.AddNewStrokeData(new List<InkStroke>(_strokeContainer
            //    .GetStrokes().Where(s => s.Selected)));
            Grid.Opacity = 1.0;
            Window.Current.CoreWindow.PointerCursor = new CoreCursor(CoreCursorType.Arrow, 0);
        }

        /// <summary>
        /// Removes the data for the strokes being transformed to that they are not recognized in their pre-transformed state by the InkRecognitionHelper.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnManipulationStarted(object sender, ManipulationStartedRoutedEventArgs e)
        {
            _freeformView.InkControl.InkRecognitionHelper.RemoveStrokeData(new List<InkStroke>(_strokeContainer
                .GetStrokes().Where(s => s.Selected)));
            Grid.Opacity = 0.0;
            Window.Current.CoreWindow.PointerCursor = new CoreCursor(GetPointerCursor(sender as Grid), 0);
            e.Handled = true;
        }

        private CoreCursorType GetPointerCursor(Grid grid)
        {
            if (grid == null)
            {
                return CoreCursorType.SizeAll;
            }
            var draggerName = grid.Name;
            switch (draggerName)
            {
                case "BottomRightDragger":
                    return CoreCursorType.SizeNorthwestSoutheast;
                case "BottomCenterDragger":
                    return CoreCursorType.SizeNorthSouth;
                case "BottomLeftDragger":
                    return CoreCursorType.SizeNortheastSouthwest;
                case "TopRightDragger":
                    return CoreCursorType.SizeNortheastSouthwest;
                case "TopCenterDragger":
                    return CoreCursorType.SizeNorthSouth;
                case "TopLeftDragger":
                    return CoreCursorType.SizeNorthwestSoutheast;
                case "RightCenterDragger":
                    return CoreCursorType.SizeWestEast;
                case "LeftCenterDragger":
                    return CoreCursorType.SizeWestEast;
                default:
                    return CoreCursorType.SizeAll;
            }
        }

        private void Delete()
        {
            _strokeContainer.DeleteSelected();
            _freeformView.InkControl.UndoSelection();
        }

        private void Cut()
        {
            Copy();
            Delete();
        }

        private void Copy()
        {
            _strokeContainer.CopySelectedToClipboard();
        }

        private void Grid_OnTapped(object sender, TappedRoutedEventArgs e)
        {
            if (_flyoutShowing) Grid.ContextFlyout.Hide();
            else (Grid.ContextFlyout as MenuFlyout).ShowAt(Grid, e.GetPosition(Grid));
            _flyoutShowing = !_flyoutShowing;
        }

        private void DeleteButton_OnClick(object sender, RoutedEventArgs e)
        {
            Delete();
        }

        private void CopyButton_OnClick(object sender, RoutedEventArgs e)
        {
            Copy();
        }

        private void CutButton_OnClick(object sender, RoutedEventArgs e)
        {
            Cut();
        }

        private void RecognizeButton_OnClick(object sender, RoutedEventArgs e)
        {
            _freeformView.InkControl.RecognizeSelectedStrokes();
        }

        private void CopyAttributesButton_OnClick(object sender, RoutedEventArgs e)
        {
            var stroke = _strokeContainer.GetStrokes().First(s => s.Selected);
            GlobalInkSettings.ForceUpdateFromAttributes(stroke.DrawingAttributes);
        }

        private void AdjustSettingsButton_OnClick(object sender, RoutedEventArgs e)
        {
            _editAttributes = !_editAttributes;
            if (_editAttributes)
            {
                GlobalInkSettings.ForceUpdateFromAttributes(_startingTransforms.Keys.First().DrawingAttributes);
            }
        }

        public void Dispose()
        {
            _freeformView.InkControl.UpdateInkController();
            _strokeContainer = null;
            _startingTransforms = null;
            _editAttributes = false;
            GlobalInkSettings.InkSettingsUpdated -= GlobalInkSettingsOnOnAttributesUpdated;
        }
    }
}
