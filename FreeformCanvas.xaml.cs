using Microsoft.Graphics.Canvas;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Numerics;
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
using Microsoft.Graphics.Canvas.Brushes;
using Microsoft.Graphics.Canvas.UI;
using Microsoft.Graphics.Canvas.UI.Xaml;
using System.Threading.Tasks;

// The User Control item template is documented at http://go.microsoft.com/fwlink/?LinkId=234236

namespace Dash
{
    public abstract partial class FreeformCanvas : UserControl
    {
        public delegate void OnDocumentViewLoadedHandler(FreeformCanvas sender, DocumentView documentView);

        public event OnDocumentViewLoadedHandler OnDocumentViewLoaded;
        protected Rect _bounds = new Rect(double.NegativeInfinity, double.NegativeInfinity, double.PositiveInfinity, double.PositiveInfinity);
        protected double _canvasScale { get; set; } = 1;
        protected const float MaxScale = 10;
        protected const float MinScale = 0.1f;
        protected CanvasImageBrush _bgBrush;
        protected bool _resourcesLoaded;
        protected const double _numberOfBackgroundRows = 2; // THIS IS A MAGIC NUMBER AND SHOULD CHANGE IF YOU CHANGE THE BACKGROUND IMAGE
        protected CanvasBitmap _bgImage;
        protected Uri _backgroundPath = new Uri("ms-appx:///Assets/gridbg.png");

        public FreeformCanvas()
        {
            InitializeComponent();
        }

        public void UserControl_PointerWheelChanged(object sender, PointerRoutedEventArgs e)
        {
            if (!IsHitTestVisible) return;
            var canvas = xItemsControl.ItemsPanelRoot as Canvas;
            Debug.Assert(canvas != null);
            e.Handled = true;
            //Get mousepoint in canvas space 
            var point = e.GetCurrentPoint(canvas);
            var scaleAmount = Math.Pow(1 + 0.15 * Math.Sign(point.Properties.MouseWheelDelta),
                Math.Abs(point.Properties.MouseWheelDelta) / 120.0f);
            scaleAmount = Math.Max(Math.Min(scaleAmount, 1.7f), 0.4f);
            _canvasScale *= (float)scaleAmount;
            Debug.Assert(canvas.RenderTransform != null);
            var p = point.Position;
            //Create initial ScaleTransform 
            var scale = new ScaleTransform
            {
                CenterX = p.X,
                CenterY = p.Y,
                ScaleX = scaleAmount,
                ScaleY = scaleAmount
            };

            //Clamp scale
            ClampScale(scale);

            //Create initial composite transform
            var composite = new TransformGroup();
            composite.Children.Add(scale);
            composite.Children.Add(canvas.RenderTransform);

            canvas.RenderTransform = new MatrixTransform { Matrix = composite.Value };

            SetTransformOnBackground(composite);
        }

        public void UserControl_ManipulationDelta(object sender, ManipulationDeltaRoutedEventArgs e)
        {
            if (!IsHitTestVisible) return;
            var canvas = xItemsControl.ItemsPanelRoot as Canvas;
            Debug.Assert(canvas != null);
            e.Handled = true;
            var delta = e.Delta;

            //Create initial translate and scale transforms
            //Translate is in screen space, scale is in canvas space
            var translate = new TranslateTransform
            {
                X = delta.Translation.X,
                Y = delta.Translation.Y
            };

            var p = Util.PointTransformFromVisual(e.Position, canvas);
            var scale = new ScaleTransform
            {
                CenterX = p.X,
                CenterY = p.Y,
                ScaleX = delta.Scale,
                ScaleY = delta.Scale
            };

            //Clamp the zoom
            _canvasScale *= delta.Scale;
            ClampScale(scale);


            //Create initial composite transform
            var composite = new TransformGroup();
            composite.Children.Add(scale);
            composite.Children.Add(canvas.RenderTransform);
            composite.Children.Add(translate);

            //Get top left and bottom right screen space points in canvas space
            var inverse = composite.Inverse;
            Debug.Assert(inverse != null);
            Debug.Assert(canvas.RenderTransform != null);
            var renderInverse = canvas.RenderTransform.Inverse;
            Debug.Assert(renderInverse != null);
            var topLeft = inverse.TransformPoint(new Point(0, 0));
            var bottomRight = inverse.TransformPoint(new Point(xOuterGrid.ActualWidth, xOuterGrid.ActualHeight));
            var preTopLeft = renderInverse.TransformPoint(new Point(0, 0));
            var preBottomRight = renderInverse.TransformPoint(new Point(xOuterGrid.ActualWidth, xOuterGrid.ActualHeight));

            //Check if the panning or zooming puts the view out of bounds of the canvas
            //Nullify scale or translate components accordingly
            var outOfBounds = false;
            //Create a canvas space translation to correct the translation if necessary
            var fixTranslate = new TranslateTransform();
            if (topLeft.X < _bounds.Left && bottomRight.X > _bounds.Right)
            {
                translate.X = 0;
                fixTranslate.X = 0;
                var scaleAmount = (bottomRight.X - topLeft.X) / _bounds.Width;
                scale.ScaleY = scaleAmount;
                scale.ScaleX = scaleAmount;
                outOfBounds = true;
            }
            else if (topLeft.X < _bounds.Left)
            {
                translate.X = 0;
                fixTranslate.X = preTopLeft.X;
                scale.CenterX = _bounds.Left;
                outOfBounds = true;
            }
            else if (bottomRight.X > _bounds.Right)
            {
                translate.X = 0;
                fixTranslate.X = -(_bounds.Right - preBottomRight.X - 1);
                scale.CenterX = _bounds.Right;
                outOfBounds = true;
            }
            if (topLeft.Y < _bounds.Top && bottomRight.Y > _bounds.Bottom)
            {
                translate.Y = 0;
                fixTranslate.Y = 0;
                var scaleAmount = (bottomRight.Y - topLeft.Y) / _bounds.Height;
                scale.ScaleX = scaleAmount;
                scale.ScaleY = scaleAmount;
                outOfBounds = true;
            }
            else if (topLeft.Y < _bounds.Top)
            {
                translate.Y = 0;
                fixTranslate.Y = preTopLeft.Y;
                scale.CenterY = _bounds.Top;
                outOfBounds = true;
            }
            else if (bottomRight.Y > _bounds.Bottom)
            {
                translate.Y = 0;
                fixTranslate.Y = -(_bounds.Bottom - preBottomRight.Y - 1);
                scale.CenterY = _bounds.Bottom;
                outOfBounds = true;
            }

            //If the view was out of bounds recalculate the composite matrix
            if (outOfBounds)
            {
                composite = new TransformGroup();
                composite.Children.Add(fixTranslate);
                composite.Children.Add(scale);
                composite.Children.Add(canvas.RenderTransform);
                composite.Children.Add(translate);
            }

            canvas.RenderTransform = new MatrixTransform { Matrix = composite.Value };
            SetTransformOnBackground(composite);
        }

        public void UserControl_ManipulationInertiaStarting(object sender, ManipulationInertiaStartingRoutedEventArgs e)
        {
            e.TranslationBehavior.DesiredDeceleration = 0.01;
        }

        private void ClampScale(ScaleTransform scale)
        {
            if (_canvasScale > MaxScale)
            {
                _canvasScale = MaxScale;
                scale.ScaleX = 1;
                scale.ScaleY = 1;
            }
            if (_canvasScale < MinScale)
            {
                _canvasScale = MinScale;
                scale.ScaleX = 1;
                scale.ScaleY = 1;
            }
        }

        private void XOuterGrid_OnSizeChanged(object sender, SizeChangedEventArgs e)
        {
            xClippingRect.Rect = new Rect(0, 0, xOuterGrid.ActualWidth, xOuterGrid.ActualHeight);
        }

        private void DocumentViewOnLoaded(object sender, RoutedEventArgs e)
        {
            OnDocumentViewLoaded?.Invoke(this, sender as DocumentView);
        }

        protected void SetTransformOnBackground(TransformGroup composite)
        {
            var aliasSafeScale = ClampBackgroundScaleForAliasing(composite.Value.M11, _numberOfBackgroundRows);

            if (_resourcesLoaded)
            {
                _bgBrush.Transform = new Matrix3x2((float)aliasSafeScale,
                    (float)composite.Value.M12,
                    (float)composite.Value.M21,
                    (float)aliasSafeScale,
                    (float)composite.Value.OffsetX,
                    (float)composite.Value.OffsetY);
                xBackgroundCanvas.Invalidate();
            }
        }

        private double ClampBackgroundScaleForAliasing(double currentScale, double numberOfBackgroundRows)
        {
            while (currentScale / numberOfBackgroundRows > numberOfBackgroundRows)
            {
                currentScale /= numberOfBackgroundRows;
            }

            while (currentScale * numberOfBackgroundRows < numberOfBackgroundRows)
            {
                currentScale *= numberOfBackgroundRows;
            }

            return currentScale;
        }

        private void CanvasControl_OnCreateResources(CanvasControl sender, CanvasCreateResourcesEventArgs args)
        {
            args.TrackAsyncAction(Task.Run(async () =>
            {
                // Load the background image and create an image brush from it
                _bgImage = await CanvasBitmap.LoadAsync(sender, _backgroundPath);
                _bgBrush = new CanvasImageBrush(sender, _bgImage);

                // Set the brush's edge behaviour to wrap, so the image repeats if the drawn region is too big
                _bgBrush.ExtendX = _bgBrush.ExtendY = CanvasEdgeBehavior.Wrap;

                _resourcesLoaded = true;
            }).AsAsyncAction());
        }

        private void CanvasControl_OnDraw(CanvasControl sender, CanvasDrawEventArgs args)
        {
            if (!_resourcesLoaded) return;

            // Just fill a rectangle with our tiling image brush, covering the entire bounds of the canvas control
            var session = args.DrawingSession;
            session.FillRectangle(new Rect(new Point(), sender.Size), _bgBrush);
        }

    }
}
