using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Core;
using Windows.UI.Input;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

// The User Control item template is documented at http://go.microsoft.com/fwlink/?LinkId=234236

namespace PanZoomCanvas
{
    public sealed partial class FreeformView : UserControl
    {
        private float canvasScale = 1;
        public const float MaxScale = 10;
        public const float MinScale = 0.5f;
        public Transform CanvasTransform
        {
            get { return XCanvas.RenderTransform; }
            set { XCanvas.RenderTransform = value; }
        }

        private FrameworkElement _parentElement = null;
        private FrameworkElement ParentElement
        {
            get
            {
                if (_parentElement == null)
                {
                    _parentElement = XCanvas.Parent as FrameworkElement;
                }
                Debug.Assert(_parentElement != null);
                return _parentElement;
            }
        }

        public static FreeformView Instance = null;

        public FreeformView()
        {
            this.InitializeComponent();

            XInkCanvas.InkPresenter.InputDeviceTypes = CoreInputDeviceTypes.Pen | CoreInputDeviceTypes.Mouse;
            Canvas.SetZIndex(XInkCanvas, 5000);    //Make sure ink canvas stays on top

            // set screen in middle of canvas 
            CanvasTransform = new TranslateTransform { X = -XCanvas.Width / 2, Y = -XCanvas.Height / 2 };

            Debug.Assert(Instance == null);
            Instance = this; 
        }

        private void UserControl_ManipulationDelta(object sender, ManipulationDeltaRoutedEventArgs e)
        {
            ManipulationDelta delta = e.Delta;

            //Create initial translate and scale transforms
            TranslateTransform translate = new TranslateTransform
            {
                X = delta.Translation.X,
                Y = delta.Translation.Y
            };


            ScaleTransform scale = new ScaleTransform
            {
                CenterX = e.Position.X,
                CenterY = e.Position.Y,
                ScaleX = delta.Scale,
                ScaleY = delta.Scale
            };
            //Clamp the zoom
            canvasScale *= delta.Scale;
            if (canvasScale > MaxScale)
            {
                canvasScale = MaxScale;
                scale.ScaleX = 1;
                scale.ScaleY = 1;
            }
            if (canvasScale < MinScale)
            {
                canvasScale = MinScale;
                scale.ScaleX = 1;
                scale.ScaleY = 1;
            }

            //Create initial composite transform
            TransformGroup composite = new TransformGroup();
            composite.Children.Add(scale);
            composite.Children.Add(CanvasTransform);
            composite.Children.Add(translate);

            //Get top left and bottom right screen space points in canvas space
            GeneralTransform inverse = composite.Inverse;
            Debug.Assert(inverse != null);
            Debug.Assert(XCanvas.RenderTransform != null);
            GeneralTransform renderInverse = XCanvas.RenderTransform.Inverse;
            Debug.Assert(renderInverse != null);
            Point topLeft = inverse.TransformPoint(new Point(0, 0));
            var MyGrid = VisualTreeHelper.GetParent(XCanvas) as FrameworkElement;
            Point bottomRight = inverse.TransformPoint(new Point(ParentElement.ActualWidth, ParentElement.ActualHeight));
            Point preTopLeft = renderInverse.TransformPoint(new Point(0, 0));
            Point preBottomRight = renderInverse.TransformPoint(new Point(ParentElement.ActualWidth, ParentElement.ActualHeight));

            //Check if the panning or zooming puts the view out of bounds of the canvas
            //Nullify scale or translate components accordingly
            bool outOfBounds = false;
            TranslateTransform fixTranslate = new TranslateTransform();
            if (topLeft.X < 0)
            {
                translate.X = 0;
                fixTranslate.X = preTopLeft.X;
                scale.CenterX = 0;
                outOfBounds = true;
            }
            else if (bottomRight.X > XCanvas.ActualWidth - 1)
            {
                translate.X = 0;
                fixTranslate.X = -(XCanvas.ActualWidth - preBottomRight.X - 1);
                scale.CenterX = XCanvas.ActualWidth;
                outOfBounds = true;
            }
            if (topLeft.Y < 0)
            {
                translate.Y = 0;
                fixTranslate.Y = preTopLeft.Y;
                scale.CenterY = 0;
                outOfBounds = true;
            }
            else if (bottomRight.Y > XCanvas.ActualHeight - 1)
            {
                translate.Y = 0;
                fixTranslate.Y = -(XCanvas.ActualHeight - preBottomRight.Y - 1);
                scale.CenterY = XCanvas.ActualHeight;
                outOfBounds = true;
            }

            //If the view was out of bounds recalculate the composite matrix
            if (outOfBounds)
            {
                composite = new TransformGroup();
                composite.Children.Add(fixTranslate);
                composite.Children.Add(scale);
                composite.Children.Add(CanvasTransform);
                composite.Children.Add(translate);
            }

            CanvasTransform = new MatrixTransform { Matrix = composite.Value };
        }

        private void UserControl_PointerWheelChanged(object sender, PointerRoutedEventArgs e)
        {
            PointerPoint point = e.GetCurrentPoint(XCanvas);
            double scale = Math.Pow(1 + 0.15 * Math.Sign(point.Properties.MouseWheelDelta),
                Math.Abs(point.Properties.MouseWheelDelta) / 120.0f);
            scale = Math.Max(Math.Min(scale, 1.7f), 0.4f);
            canvasScale *= (float)scale;
            Debug.Assert(XCanvas.RenderTransform != null);
            Point screenPos = XCanvas.RenderTransform.TransformPoint(point.Position);
            ScaleTransform scaleTransform = new ScaleTransform
            {
                CenterX = screenPos.X,
                CenterY = screenPos.Y,
                ScaleX = scale,
                ScaleY = scale
            };

            if (canvasScale > MaxScale)
            {
                canvasScale = MaxScale;
                scaleTransform.ScaleX = 1;
                scaleTransform.ScaleY = 1;
            }
            if (canvasScale < MinScale)
            {
                canvasScale = MinScale;
                scaleTransform.ScaleX = 1;
                scaleTransform.ScaleY = 1;
            }


            TransformGroup composite = new TransformGroup();
            composite.Children.Add(CanvasTransform);
            composite.Children.Add(scaleTransform);

            GeneralTransform inverse = composite.Inverse;
            Debug.Assert(inverse != null);
            GeneralTransform renderInverse = XCanvas.RenderTransform.Inverse;
            Debug.Assert(inverse != null);
            Debug.Assert(renderInverse != null);
            Point topLeft = inverse.TransformPoint(new Point(0, 0));
            Point bottomRight = inverse.TransformPoint(new Point(ParentElement.ActualWidth, ParentElement.ActualHeight));
            Point preBottomRight = renderInverse.TransformPoint(new Point(ParentElement.ActualWidth, ParentElement.ActualHeight));

            TranslateTransform translate = new TranslateTransform
            {
                X = 0,
                Y = 0
            };

            //Check if the panning or zooming puts the view out of bounds of the canvas
            //Nullify scale or translate components accordingly 
            bool outOfBounds = false;
            if (topLeft.X < 0)
            {
                scaleTransform.CenterX = 0;
                outOfBounds = true;
            }
            else if (bottomRight.X >= XCanvas.ActualWidth)
            {
                translate.X = preBottomRight.X - XCanvas.ActualWidth;
                scaleTransform.CenterX = ParentElement.ActualWidth;
                outOfBounds = true;
            }
            if (topLeft.Y < 0)
            {
                scaleTransform.CenterY = 0;
                outOfBounds = true;
            }
            else if (bottomRight.Y >= XCanvas.ActualHeight)
            {
                translate.Y = preBottomRight.Y - XCanvas.ActualHeight;
                scaleTransform.CenterY = ParentElement.ActualHeight;
                outOfBounds = true;
            }

            //If the view was out of bounds recalculate the composite matrix
            if (outOfBounds)
            {
                composite = new TransformGroup();
                composite.Children.Add(translate);
                composite.Children.Add(CanvasTransform);
                composite.Children.Add(scaleTransform);
            }
            CanvasTransform = new MatrixTransform { Matrix = composite.Value };
        }

        private void UserControl_ManipulationInertiaStarting(object sender, ManipulationInertiaStartingRoutedEventArgs e)
        {
            e.TranslationBehavior.DesiredDeceleration = 0.01;
        }

        private void UserControl_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            TranslateTransform translate = new TranslateTransform();

            //Calculate bottomRight corner of screen in canvas space before and after resize 
            Debug.Assert(XCanvas.RenderTransform != null);
            Debug.Assert(XCanvas.RenderTransform.Inverse != null);
            Point oldBottomRight =
                XCanvas.RenderTransform.Inverse.TransformPoint(new Point(e.PreviousSize.Width, e.PreviousSize.Height));
            Point bottomRight =
                XCanvas.RenderTransform.Inverse.TransformPoint(new Point(e.NewSize.Width, e.NewSize.Height));

            bool outOfBounds = false;
            if (bottomRight.X > XCanvas.ActualWidth - 1)
            {
                translate.X = -(oldBottomRight.X - bottomRight.X);
                outOfBounds = true;
            }
            if (bottomRight.Y > XCanvas.ActualHeight - 1)
            {
                translate.Y = -(oldBottomRight.Y - bottomRight.Y);
                outOfBounds = true;
            }
            if (outOfBounds)
            {
                TransformGroup composite = new TransformGroup();
                composite.Children.Add(translate);
                composite.Children.Add(XCanvas.RenderTransform);
                XCanvas.RenderTransform = new MatrixTransform { Matrix = composite.Value };
            }
        }
    }
}
