using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Runtime.InteropServices.WindowsRuntime;
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
        public InkStrokeContainer StrokeContainer;
        private Size _manipulationStartSize;

        private Point Position()
        {
            return new Point(Canvas.GetLeft(this), Canvas.GetTop(this));
        } 

        private List<Grid> _draggers;

        private double RectStrokeThickness = 1;

        public InkSelectionRect(CollectionFreeformView view, InkStrokeContainer strokes)
        {
            this.InitializeComponent();
            FreeformView = view;
            StrokeContainer = strokes;
            Loaded += OnLoaded;
            FreeformView.ManipulationControls.OnManipulatorTranslatedOrScaled += ManipulationControlsOnOnManipulatorTranslatedOrScaled;
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
        }

        private void ManipulationControlsOnOnManipulatorTranslatedOrScaled(TransformGroupData transformationDelta)
        {
            UpdateStrokeThickness();
        }

        private void UpdateStrokeThickness()
        {
            RectStrokeThickness = 1 / FreeformView.Zoom;
            foreach (var grid in _draggers)
            {
                (grid.Children[0] as Shape).StrokeThickness = RectStrokeThickness;
            }
                    (CenterDragger.Children[0] as Shape).StrokeThickness = 2 * RectStrokeThickness;
            (Grid.Children[0] as Shape).StrokeThickness = RectStrokeThickness;
        }

        private void OnLoaded(object sender, RoutedEventArgs routedEventArgs)
        {
            _manipulationStartSize = new Size(Width - 30, Height - 30);
        }

        private void DraggerOnManipulationDelta(object sender, ManipulationDeltaRoutedEventArgs e)
        {
            e.Handled = true;
            var translate = e.Delta.Translation;
            var dragger = sender as Grid;
            float xScale = 1;
            float yScale = 1;
            if (dragger.Name == "CenterDragger")
            {
                Canvas.SetLeft(this, Position().X + translate.X);
                Canvas.SetTop(this, Position().Y + translate.Y);
                StrokeContainer.MoveSelected(translate);
                return;
            }
            if (dragger.Name.Contains("Left") && Width - translate.X > MinWidth)
            {
                Canvas.SetLeft(this, Position().X + translate.X);
                translate.X *= -1;
            }
            if (dragger.Name.Contains("Top") && Height - translate.Y > MinHeight)
            {
                Canvas.SetTop(this, Position().Y + translate.Y);
                translate.Y *= -1;
            }
            if (Width + translate.X > MinWidth)
            {
                Width += translate.X;
                xScale = (float) ((Width - 30 + translate.X) / _manipulationStartSize.Width);
            }
            if (Height + translate.Y > MinHeight)
            {
                Height += translate.Y;
                yScale = (float) ((Height - 30 + translate.Y) / _manipulationStartSize.Height);
            }
            Point center = GetScaleCenter(dragger.Name);
            Point scaleCompensation = GetScaleCompensation(dragger.Name, xScale, yScale);
            ResizeStrokes(center, xScale, yScale, scaleCompensation);
        }

        private void ResizeStrokes(Point center, float xScale, float yScale, Point scaleCompensation)
        {
            Vector2 centerVect = new Vector2((float) center.X, (float) center.Y);
            var matrix = Matrix3x2.CreateScale(xScale, yScale, centerVect);
            foreach (var stroke in StrokeContainer.GetStrokes())
            {
                if (stroke.Selected)
                {
                    stroke.PointTransform = matrix;
                }
            }
            StrokeContainer.MoveSelected(scaleCompensation);
        }

        private void OnManipulationCompleted(object sender, ManipulationCompletedRoutedEventArgs e)
        {
            FreeformView.UpdateInkFieldModelController();
            Grid.Opacity = 1.0;
            Window.Current.CoreWindow.PointerCursor = new CoreCursor(CoreCursorType.Arrow, 0);
        }

        private void OnManipulationStarted(object sender, ManipulationStartedRoutedEventArgs e)
        {
            Grid.Opacity = 0.0;
            Window.Current.CoreWindow.PointerCursor = new CoreCursor(GetPointerCursor(sender as Grid), 0);
        }

        private CoreCursorType GetPointerCursor(Grid grid)
        {
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
                    return CoreCursorType.Custom;
            }
        }

        /// <summary>
        /// Returns point used as center for scale transform depending on dragger used to resize.
        /// </summary>
        /// <param name="dragger"></param>
        /// <returns></returns>
        private Point GetScaleCenter(string draggerName)
        {
            //account for margin around selection
            double margin = 15;
            switch (draggerName)
            {
                case "BottomRightDragger":
                    return new Point(Position().X + margin, Position().Y + margin);
                case "BottomCenterDragger":
                    return new Point(Position().X + Width/2 + margin, Position().Y + margin);
                case "BottomLeftDragger":
                    return new Point(Position().X + Width - margin, Position().Y + margin);
                case "TopRightDragger":
                    return new Point(Position().X + margin, Position().Y + Height - margin);
                case "TopCenterDragger":
                    return new Point(Position().X + Width / 2 + margin, Position().Y + Height - margin);
                case "TopLeftDragger":
                    return new Point(Position().X + Width - margin, Position().Y + Height - margin);
                case "RightCenterDragger":
                    return new Point(Position().X + margin, Position().Y + Height / 2);
                case "LeftCenterDragger":
                    return new Point(Position().X + Width - margin, Position().Y + Height / 2);
                default:
                    return Position();
            }
        }

        private Point GetScaleCompensation(string draggerName, double xScale, double yScale)
        {
            switch (draggerName)
            {
                case "BottomRightDragger":
                    return new Point(0 , 0);
                case "BottomCenterDragger":
                    return new Point(0,0);
                case "BottomLeftDragger":
                    return new Point(0, 0);
                case "TopRightDragger":
                    return new Point(0, 0);
                case "TopCenterDragger":
                    return new Point(0, 0);
                case "TopLeftDragger":
                    return new Point(0, 0);
                case "RightCenterDragger":
                    return new Point(0, 0);
                case "LeftCenterDragger":
                    return new Point(0, 0);

                default:
                    return new Point(0,0);
            }
        }
    }
}
