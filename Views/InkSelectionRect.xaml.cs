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
        private Point _startPosition;

        private Point Position()
        {
            return new Point(Canvas.GetLeft(this), Canvas.GetTop(this));
        } 

        private List<Grid> _draggers;

        private double RectStrokeThickness = 1;

        private Dictionary<InkStroke, Matrix3x2> _startingTransforms;

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
            UpdateStartingTransforms();
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
                    Debug.WriteLine("stroke ID: " + stroke.Id);
                    Debug.WriteLine("x scale: " + stroke.PointTransform.M11);
                    Debug.WriteLine("y scale: " + stroke.PointTransform.M22);
                    Debug.WriteLine("Center: " + "(" + stroke.PointTransform.M31 + ", " + stroke.PointTransform.M32 + ")");
                    Debug.WriteLine("Rect Pos: (" + Position().X + ", " + Position().Y + ")");
                }
            }
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
            _startPosition = Position();
        }

        private void DraggerOnManipulationDelta(object sender, ManipulationDeltaRoutedEventArgs e)
        {
            e.Handled = true;
            var translate = e.Delta.Translation;
            var dragger = sender as Grid;
            Point oldCenter = new Point(Position().X + 15, Position().Y + 15);
            float xScale = (float) ((Width - 30)/_manipulationStartSize.Width);
            float yScale = (float)((Height - 30) / _manipulationStartSize.Height);
            if (dragger.Name == "CenterDragger")
            {
                Canvas.SetLeft(this, Position().X + translate.X);
                Canvas.SetTop(this, Position().Y + translate.Y);
                //StrokeContainer.MoveSelected(translate);
                ResizeStrokes(new Point(Position().X + 15, Position().Y + 15), translate, xScale, yScale);
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
            Point newCenter = new Point(Position().X + 15, Position().Y + 15);
            Point deltaPos = new Point(newCenter.X - oldCenter.X, newCenter.Y - oldCenter.Y);
            Point center = GetScaleCenter(dragger.Name);
            ResizeStrokes(center, deltaPos, xScale, yScale);
        }

        private void ResizeStrokes(Point center, Point translation, float xScale, float yScale)
        {
            var totalTranslation = new Point(Position().X - _startPosition.X, Position().Y - _startPosition.Y);
            Matrix3x2 translationMatrix = Matrix3x2.CreateTranslation(new Vector2((float)totalTranslation.X, (float)totalTranslation.Y));
            Vector2 centerVect = new Vector2((float) center.X, (float) center.Y);
            foreach (var stroke in StrokeContainer.GetStrokes())
            {
                if (stroke.Selected)
                {
                    var ogTransform = _startingTransforms[stroke];
                    //Account for stroke already having point transform
                    var dXScale = ogTransform.M11 * xScale - ogTransform.M11;
                    var dYScale = ogTransform.M22 * yScale - ogTransform.M22;
                    var scaleMatrix = Matrix3x2.CreateScale(ogTransform.M11 + dXScale, ogTransform.M22 + dYScale, centerVect);
                    Debug.WriteLine("Scale matrix " + scaleMatrix);
                    var matrix = new Matrix3x2(xScale, 0, 0, yScale, - centerVect.X * (xScale - 1), - centerVect.Y * (yScale - 1)) * translationMatrix;
                    Debug.WriteLine("Custom matrix: " + matrix);
                    stroke.PointTransform = matrix;
                }
            }
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
                case "TopRightDragger":
                case "RightCenterDragger":
                case "BottomCenterDragger":
                    return new Point(Position().X + margin, Position().Y + margin);
                case "BottomLeftDragger":
                case "LeftCenterDragger":
                case "TopCenterDragger":
                case "TopLeftDragger":
                    return new Point(Position().X + Width - margin, Position().Y + Height - margin);
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
