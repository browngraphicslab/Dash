using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Reflection;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Windows.Input;
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

// The User Control item template is documented at https://go.microsoft.com/fwlink/?LinkId=234236

namespace Dash.Views
{
    public sealed partial class InkSelectionRect : UserControl
    {
        public CollectionFreeformView FreeformView;
        public ScrollViewer Scroller;
        public InkStrokeContainer StrokeContainer;
        private Size _startSize;
        private Point _startPosition;
        private List<Grid> _draggers;
        private Dictionary<InkStroke, Matrix3x2> _startingTransforms;
        private bool _flyoutShowing;
        private double outerMargin = 15;
        private double innerMargin = 10;
        //public double TotalMargin = 25;

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
            FreeformView = view;
            Scroller = scroller;
            StrokeContainer = strokes;
            Loaded += OnLoaded;
            if(view != null) FreeformView.ManipulationControls.OnManipulatorTranslatedOrScaled += ManipulationControlsOnOnManipulatorTranslatedOrScaled;
            if(scroller != null) Scroller.ViewChanged += Scroller_ViewChanged;
            _draggers = new List<Grid>
            {
                BottomRightDragger,
                BottomCenterDragger,
                BottomLeftDragger,
                TopRightDragger,
                TopCenterDragger,
                TopLeftDragger,
                LeftCenterDragger,
                RightCenterDragger
            };
            UpdateStrokeThickness();
            UpdateStartingTransforms();
        }

        private void Scroller_ViewChanged(object sender, ScrollViewerViewChangedEventArgs e)
        {
            UpdateStrokeThickness();
        }

        private void ManipulationControlsOnOnManipulatorTranslatedOrScaled(TransformGroupData transformationDelta)
        {
            UpdateStrokeThickness();
        }

        private void UpdateStartingTransforms()
        {
            _startingTransforms = new Dictionary<InkStroke, Matrix3x2>();
            foreach (InkStroke stroke in StrokeContainer.GetStrokes())
            {
                if (stroke.Selected)
                {
                    _startingTransforms[stroke] = stroke.PointTransform;
                }
            }
        }

        private void UpdateStrokeThickness()
        {
            double scale = FreeformView?.Zoom ?? Scroller.ZoomFactor;
            double rectStrokeThickness = 1 / scale;
            foreach (var grid in _draggers)
            {
                (grid.Children[0] as Shape).StrokeThickness = rectStrokeThickness;
            }
            //(CenterDragger.Children[0] as Shape).StrokeThickness = 2 * _rectStrokeThickness;
            (Grid.Children[0] as Shape).StrokeThickness = rectStrokeThickness;
        }

        private void OnLoaded(object sender, RoutedEventArgs routedEventArgs)
        {
            _startSize = new Size(Width - 50, Height - 50);
            _startPosition = Position();
        }

        private void DraggerOnManipulationDelta(object sender, ManipulationDeltaRoutedEventArgs e)
        {
            e.Handled = true;
            double scale = FreeformView?.Zoom ?? Scroller.ZoomFactor;
            var translate = new Point(e.Delta.Translation.X / scale, e.Delta.Translation.Y / scale);
            var dragger = sender as Grid;
            float xScale = (float)((Width - 50) / _startSize.Width);
            float yScale = (float)((Height - 50) / _startSize.Height);
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
                xScale = (float)((Width - 50 + translate.X) / _startSize.Width);
                Width += translate.X;

            }
            if (Height + translate.Y > MinHeight || translate.Y > 0)
            {
                yScale = (float)((Height - 50 + translate.Y) / _startSize.Height);
                Height += translate.Y;
            }

            TransformStrokes(xScale, yScale);

        }

        //private void UpdateCenterVisibility()
        //{
        //    if (Height < 100 || Width < 100)
        //    {
        //        CenterDragger.Visibility = Visibility.Collapsed;
        //    }
        //    else CenterDragger.Visibility = Visibility.Visible;
        //}

        private void TransformStrokes(float xScale, float yScale)
        {
            var totalTranslation = new Point(Position().X - _startPosition.X, Position().Y - _startPosition.Y);
            Matrix3x2 translationMatrix = Matrix3x2.CreateTranslation(new Vector2((float)totalTranslation.X, (float)totalTranslation.Y));
            Matrix3x2 scaleMatrix = Matrix3x2.CreateScale(xScale, yScale, new Vector2((float)InnerTopLeft().X, (float)InnerTopLeft().Y));
            foreach (var stroke in StrokeContainer.GetStrokes())
            {
                if (stroke.Selected)
                {
                    var startingTransform = _startingTransforms[stroke];
                    var translate = Matrix3x2.Multiply(startingTransform, translationMatrix);
                    var translateAndScale = Matrix3x2.Multiply(translate, scaleMatrix);
                    stroke.PointTransform = translateAndScale;
                }
            }
        }

        private void OnManipulationCompleted(object sender, ManipulationCompletedRoutedEventArgs e)
        {
            FreeformView.InkControl.UpdateInkFieldModelController();
            Grid.Opacity = 1.0;
            Window.Current.CoreWindow.PointerCursor = new CoreCursor(CoreCursorType.Arrow, 0);
        }

        private void OnManipulationStarted(object sender, ManipulationStartedRoutedEventArgs e)
        {
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
            StrokeContainer.DeleteSelected();
            var canvas = Parent as Canvas;
            canvas?.Children.Clear();
        }

        private void Cut()
        {
            Copy();
            Delete();
        }

        private void Copy()
        {
            StrokeContainer.CopySelectedToClipboard();
        }

        private void Grid_OnTapped(object sender, TappedRoutedEventArgs e)
        {
            if (_flyoutShowing) Grid.ContextFlyout.Hide();
            else Grid.ContextFlyout.ShowAt(Grid);
            _flyoutShowing = !_flyoutShowing;

        }

        private void DeleteButton_OnTapped(object sender, TappedRoutedEventArgs e)
        {
            Delete();
        }

        private void CopyButton_OnTapped(object sender, TappedRoutedEventArgs e)
        {
            Copy();
        }

        private void CutButton_OnTappedButton_OnTapped(object sender, TappedRoutedEventArgs e)
        {
            Cut();
        }
    }
}
