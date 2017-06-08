using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
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
    public partial class DraggableView : UserControl
    {
        public DraggableView()
        {
            this.InitializeComponent();
        }
        private float _scale;

        public float MaxScale { get; set; }
        public float MinScale { get; set; }

        public Size ScreenSize { get; set; }

        public void Drag(ManipulationDeltaRoutedEventArgs e)
        {
            e.Handled = true;

            //Create initial composite transform 
            TransformGroup group = new TransformGroup();

            ScaleTransform scale = new ScaleTransform
            {
                CenterX = e.Position.X,
                CenterY = e.Position.Y,
                ScaleX = e.Delta.Scale,
                ScaleY = e.Delta.Scale
            };

            TranslateTransform translate = new TranslateTransform
            {
                X = e.Delta.Translation.X / FreeformView.Instance.CanvasScale,
                Y = e.Delta.Translation.Y / FreeformView.Instance.CanvasScale
            };

            //Clamp the scale factor 
            float newScale = _scale * e.Delta.Scale;
            if (newScale > MaxScale)
            {
                scale.ScaleX = MaxScale / _scale;
                scale.ScaleY = MaxScale / _scale;
                _scale = MaxScale;
            }
            else if (newScale < MinScale)
            {
                scale.ScaleX = MinScale / _scale;
                scale.ScaleY = MinScale / _scale;
                _scale = MinScale;
            }
            else
            {
                _scale = newScale;
            }

            group.Children.Add(scale);
            group.Children.Add(this.RenderTransform);
            group.Children.Add(translate);

            //Get top left and bottom right points of documents in canvas space
            Point p1 = group.TransformPoint(new Point(0, 0));
            Point p2 = group.TransformPoint(new Point(ScreenSize.Width, ScreenSize.Height));
            Debug.Assert(this.RenderTransform != null);
            Point oldP1 = this.RenderTransform.TransformPoint(new Point(0, 0));
            Point oldP2 = this.RenderTransform.TransformPoint(new Point(ScreenSize.Width, ScreenSize.Height));

            //Check if translating or scaling the document puts the view out of bounds of the canvas
            //Nullify scale or translate components accordingly
            bool outOfBounds = false;
            if (p1.X < 0)
            {
                outOfBounds = true;
                translate.X = -oldP1.X;
                scale.CenterX = 0;
            }
            else if (p2.X > FreeformView.Instance.Canvas.ActualWidth)
            {
                outOfBounds = true;
                translate.X = FreeformView.Instance.Canvas.ActualWidth - oldP2.X;
                scale.CenterX = this.ActualWidth;
            }
            if (p1.Y < 0)
            {
                outOfBounds = true;
                translate.Y = -oldP1.Y;
                scale.CenterY = 0;
            }
            else if (p2.Y > FreeformView.Instance.Canvas.ActualHeight)
            {
                outOfBounds = true;
                translate.Y = FreeformView.Instance.Canvas.ActualHeight - oldP2.Y;
                scale.CenterY = this.ActualHeight;
            }

            //If the view was out of bounds recalculate the composite matrix
            if (outOfBounds)
            {
                group = new TransformGroup();
                group.Children.Add(scale);
                group.Children.Add(this.RenderTransform);
                group.Children.Add(translate);
            }

            this.RenderTransform = new MatrixTransform { Matrix = group.Value };
        }
    }
}
