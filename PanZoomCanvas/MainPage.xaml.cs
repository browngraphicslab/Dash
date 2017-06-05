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
            Debug.Assert(inverse != null);
            Point topLeft = inverse.TransformPoint(new Point(0, 0));
            Point bottomRight = inverse.TransformPoint(new Point(MyGrid.ActualWidth, MyGrid.ActualHeight));

            //Check if the panning or zooming puts the view out of bounds of the canvas
            //Nullify scale or translate components accordingly 
            bool outOfBounds = false;
            if (topLeft.X < 0)
            {
                translate.X = 0;
                scale.CenterX = 0;
                outOfBounds = true;
            }
            else if (bottomRight.X > MyCanvas.Width)
            {
                translate.X = 0;
                scale.CenterX = MyCanvas.Width;
                outOfBounds = true;
            }
            if (topLeft.Y < 0)
            {
                translate.Y = 0;
                scale.CenterY = 0;
                outOfBounds = true;
            }
            else if (bottomRight.Y > MyCanvas.Height)
            {
                translate.Y = 0;
                scale.CenterY = MyCanvas.Height;
                outOfBounds = true;
            }

            //If the view was out of bounds recalculate the composite matrix
            if (outOfBounds)
            {
                composite = new TransformGroup();
                composite.Children.Add(CanvasTransform);
                composite.Children.Add(translate);
                composite.Children.Add(scale);
            }
            CanvasTransform = new MatrixTransform {Matrix = composite.Value};
            MyCanvas.RenderTransform = CanvasTransform; 
        }

    }
}
