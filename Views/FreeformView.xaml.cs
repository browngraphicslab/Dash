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

namespace Dash
{
    public sealed partial class FreeformView : UserControl
    {
        public float CanvasScale { get; set; } = 1;
        public const float MaxScale = 10;
        public const float MinScale = 0.5f;

        public Transform CanvasTransform
        {
            get { return XCanvas.RenderTransform; }
            set { XCanvas.RenderTransform = value; }
        }

        public Canvas Canvas => XCanvas;

        //Get the parent of XCanvas 
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

        public static FreeformView Instance;

        private FreeformViewModel _vm;

        public FreeformView()
        {
            this.InitializeComponent();

            _vm = new FreeformViewModel();
            _vm.OnElementAdded += VmOnOnElementAdded;

            XInkCanvas.InkPresenter.InputDeviceTypes = CoreInputDeviceTypes.Mouse;

            // set screen in middle of canvas 
            CanvasTransform = new TranslateTransform { X = -XCanvas.Width / 2, Y = -XCanvas.Height / 2 };

            Debug.Assert(Instance == null);
            Instance = this;
        }

        private void VmOnOnElementAdded(UIElement element, float left, float top)
        {
            XCanvas.Children.Add(element);
            Canvas.SetLeft(element, left);
            Canvas.SetTop(element, top);
        }

        /**
         * Pans and zooms upon touch manipulation 
         */
        private void UserControl_ManipulationDelta(object sender, ManipulationDeltaRoutedEventArgs e)
        {
            ManipulationDelta delta = e.Delta;

            //Create initial translate and scale transforms
            //Translate is in screen space, scale is in canvas space
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
            CanvasScale *= delta.Scale;
            if (CanvasScale > MaxScale)
            {
                CanvasScale = MaxScale;
                scale.ScaleX = 1;
                scale.ScaleY = 1;
            }
            if (CanvasScale < MinScale)
            {
                CanvasScale = MinScale;
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
            Point bottomRight = inverse.TransformPoint(new Point(ParentElement.ActualWidth, ParentElement.ActualHeight));
            Point preTopLeft = renderInverse.TransformPoint(new Point(0, 0));
            Point preBottomRight = renderInverse.TransformPoint(new Point(ParentElement.ActualWidth, ParentElement.ActualHeight));

            //Check if the panning or zooming puts the view out of bounds of the canvas
            //Nullify scale or translate components accordingly
            bool outOfBounds = false;
            //Create a canvas space translation to correct the translation if necessary
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

        /**
         * Zooms upon mousewheel manipulation 
         */
        private void UserControl_PointerWheelChanged(object sender, PointerRoutedEventArgs e)
        {
            //Get mousepoint in canvas space 
            PointerPoint point = e.GetCurrentPoint(XCanvas);
            double scale = Math.Pow(1 + 0.15 * Math.Sign(point.Properties.MouseWheelDelta),
                Math.Abs(point.Properties.MouseWheelDelta) / 120.0f);
            scale = Math.Max(Math.Min(scale, 1.7f), 0.4f);
            CanvasScale *= (float)scale;
            Debug.Assert(XCanvas.RenderTransform != null);
            Point canvasPos = XCanvas.RenderTransform.TransformPoint(point.Position);

            //Create initial ScaleTransform 
            ScaleTransform scaleTransform = new ScaleTransform
            {
                CenterX = canvasPos.X,
                CenterY = canvasPos.Y,
                ScaleX = scale,
                ScaleY = scale
            };

            //Clamp scale
            if (CanvasScale > MaxScale)
            {
                CanvasScale = MaxScale;
                scaleTransform.ScaleX = 1;
                scaleTransform.ScaleY = 1;
            }
            if (CanvasScale < MinScale)
            {
                CanvasScale = MinScale;
                scaleTransform.ScaleX = 1;
                scaleTransform.ScaleY = 1;
            }

            //Create initial composite transform
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

            //Create a canvas space translation to correct the translation if necessary
            TranslateTransform translate = new TranslateTransform
            {
                X = 0,
                Y = 0
            };

            //Check if the zooming puts the view out of bounds of the canvas
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

        /**
         * Make translation inertia slow down faster 
         */
        private void UserControl_ManipulationInertiaStarting(object sender, ManipulationInertiaStartingRoutedEventArgs e)
        {
            e.TranslationBehavior.DesiredDeceleration = 0.01;
        }

        /**
         * Make sure the canvas is still in bounds after resize
         */
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

            //Check if new bottom right is out of bounds
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
            //If it is out of bounds, translate so that is is in bounds
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
