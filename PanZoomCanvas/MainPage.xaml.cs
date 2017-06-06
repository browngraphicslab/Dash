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
using Windows.UI.Input.Inking;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace PanZoomCanvas
{
    /// <summary>
    /// Zoomable pannable canvas. Has an overlay canvas unaffected by pan / zoom. 
    /// </summary>
    public sealed partial class MainPage : Page
    {
        private float canvasScale = 1;
        public const float MaxScale = 10;
        public const float MinScale = 0.1f;

        public Transform CanvasTransform
        {
            get { return MyCanvas.RenderTransform; }
            set { MyCanvas.RenderTransform = value; }
        }

        public MainPage()
        {
            this.InitializeComponent();
            MyInkCanvas.InkPresenter.InputDeviceTypes = CoreInputDeviceTypes.Pen | CoreInputDeviceTypes.Mouse;
            Canvas.SetZIndex(MyInkCanvas, 5000);    //Make sure ink canvas stays on top

            // set screen in middle of canvas 
            CanvasTransform = new TranslateTransform { X = -1 * MyCanvas.Width / 2, Y = -1 * MyCanvas.Height / 2 };
        }

        // Pan and zoom 
        private void MyCanvas_ManipulationDelta(object sender, ManipulationDeltaRoutedEventArgs e)
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
            composite.Children.Add(CanvasTransform);
            composite.Children.Add(translate);
            composite.Children.Add(scale);

            //Get top left and bottom right screen space points in canvas space
            GeneralTransform inverse = composite.Inverse;
            Debug.Assert(MyCanvas.RenderTransform != null);
            GeneralTransform renderInverse = MyCanvas.RenderTransform.Inverse;
            Debug.Assert(inverse != null);
            Debug.Assert(renderInverse != null);
            Point topLeft = inverse.TransformPoint(new Point(0, 0));
            Point bottomRight = inverse.TransformPoint(new Point(MyGrid.ActualWidth, MyGrid.ActualHeight));
            Point preTopLeft = renderInverse.TransformPoint(new Point(0, 0));
            Point preBottomRight = renderInverse.TransformPoint(new Point(MyGrid.ActualWidth, MyGrid.ActualHeight));

            //Debug.WriteLine("topLeft " + topLeft);
            //Debug.WriteLine("bottomRight " + bottomRight);
            Debug.WriteLine("Pre topLeft " + preTopLeft);
            Debug.WriteLine("Pre bottomRight " + preBottomRight);
            //Check if the panning or zooming puts the view out of bounds of the canvas
            //Nullify scale or translate components accordingly 
            bool outOfBounds = false;
            if (topLeft.X < 0)
            {
                translate.X = preTopLeft.X;
                scale.CenterX = 0;
                outOfBounds = true;
            }
            else if (bottomRight.X > MyCanvas.ActualWidth - 1)
            {
                translate.X = (preBottomRight.X - (MyCanvas.ActualWidth));
                scale.CenterX = MyCanvas.ActualWidth - 1;
                outOfBounds = true;
            }
            if (topLeft.Y < 0)
            {
                translate.Y = preTopLeft.Y;
                scale.CenterY = 0;
                outOfBounds = true;
            }
            else if (bottomRight.Y > MyCanvas.ActualHeight - 1)
            {
                translate.Y = (preBottomRight.Y - (MyCanvas.ActualHeight));
                scale.CenterY = MyCanvas.ActualHeight - 1;
                outOfBounds = true;
            }

            //If the view was out of bounds recalculate the composite matrix
            if (outOfBounds)
            {
                composite = new TransformGroup();
                composite.Children.Add(scale);
                composite.Children.Add(translate);
                composite.Children.Add(CanvasTransform);
            }

            //Debug.WriteLine("scale " + scale.ScaleX);
            //Debug.WriteLine("scale center " + scale.CenterX + " Y " + scale.CenterY);
            CanvasTransform = new MatrixTransform {Matrix = composite.Value};
        }

        private void MyGrid_PointerWheelChanged(object sender, PointerRoutedEventArgs e)
        {
        //    /* 
            //e.Pointer
            PointerPoint point = e.GetCurrentPoint(MyCanvas);
            double scale = Math.Pow(1 + 0.15 * Math.Sign(point.Properties.MouseWheelDelta),
                Math.Abs(point.Properties.MouseWheelDelta) / 120.0f);
            scale = Math.Max(Math.Min(scale, 1.7f), 0.4f);
            canvasScale *= (float)scale;
            //float scale = point.Properties.MouseWheelDelta * 0.05f / 120.0f;
            //canvasScale *= 1 + scale;
            Debug.Assert(MyCanvas.RenderTransform != null);
            Point screenPos = MyCanvas.RenderTransform.TransformPoint(point.Position);
            //Debug.WriteLine("screenPos = " + screenPos);
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
            Debug.WriteLine("PointPos " + point.Position);
        //    */ 
            /*
            var delta = e.GetCurrentPoint(MyCanvas).Properties.MouseWheelDelta;
            var POINT = MyCanvas.TransformToVisual(Window.Current.Content);
            Point mousePos = e.GetCurrentPoint(MyCanvas).Position; 
            Point screenCoord = POINT.TransformPoint(mousePos);

            ScaleTransform scaleTransform = new ScaleTransform
            {
                CenterX = screenCoord.X,
                CenterY = screenCoord.Y
            };
            float scaleFactor = 0; 
            if (delta > 0)
            {
                scaleFactor = 1.2f;
            }
            if (delta < 0)
            {
                scaleFactor = 0.83333f;
            }
            canvasScale *= scaleFactor; 
            if (canvasScale > MaxScale)
            {
                canvasScale = MaxScale;
                scaleFactor = 1; 
            }
            if (canvasScale < MinScale)
            {
                canvasScale = MinScale;
                scaleFactor = 1; 
            }

            scaleTransform.ScaleX = scaleFactor;
            scaleTransform.ScaleY = scaleFactor; 
            
                */

            TransformGroup composite = new TransformGroup();
            composite.Children.Add(CanvasTransform);
            composite.Children.Add(scaleTransform);

            GeneralTransform inverse = composite.Inverse;
            Debug.Assert(inverse != null);
            GeneralTransform renderInverse = MyCanvas.RenderTransform.Inverse;
            Debug.Assert(inverse != null);
            Debug.Assert(renderInverse != null);
            Point topLeft = inverse.TransformPoint(new Point(0, 0));
            Point bottomRight = inverse.TransformPoint(new Point(MyGrid.ActualWidth, MyGrid.ActualHeight));
            Point preBottomRight = renderInverse.TransformPoint(new Point(MyGrid.ActualWidth, MyGrid.ActualHeight));

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
            else if (bottomRight.X >= MyCanvas.ActualWidth)
            {
                translate.X = preBottomRight.X - MyCanvas.ActualWidth;
                scaleTransform.CenterX = MyCanvas.ActualWidth;
                outOfBounds = true;
            }
            if (topLeft.Y < 0)
            {
                scaleTransform.CenterY = 0;
                outOfBounds = true;
            }
            else if (bottomRight.Y >= MyCanvas.ActualHeight)
            {
                translate.Y = preBottomRight.Y - MyCanvas.ActualHeight;
                scaleTransform.CenterY = MyCanvas.ActualHeight;
                outOfBounds = true;
            }

            //If the view was out of bounds recalculate the composite matrix
            if (outOfBounds)
            {
                composite = new TransformGroup();
                composite.Children.Add(scaleTransform);
                composite.Children.Add(translate);
                composite.Children.Add(CanvasTransform);
            }
            CanvasTransform = new MatrixTransform {Matrix = composite.Value};
        }
    }
}
