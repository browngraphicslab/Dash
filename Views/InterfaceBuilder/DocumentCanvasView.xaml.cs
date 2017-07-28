using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using Microsoft.Graphics.Canvas;
using Microsoft.Graphics.Canvas.Brushes;
using Microsoft.Graphics.Canvas.Effects;
using Microsoft.Graphics.Canvas.UI;
using Microsoft.Graphics.Canvas.UI.Xaml;

// The User Control item template is documented at https://go.microsoft.com/fwlink/?LinkId=234236

namespace Dash
{
    public sealed partial class DocumentCanvasView : UserControl
    {

        private Rect _bounds = new Rect(double.NegativeInfinity, double.NegativeInfinity, double.PositiveInfinity, double.PositiveInfinity);
        private double _canvasScale { get; set; } = 1;
        private const float MaxScale = 10;
        private const float MinScale = 0.1f;
        private DocumentCanvasViewModel _vm;
        private CanvasBitmap _bgImage;
        private bool _resourcesLoaded;
        private CanvasImageBrush _bgBrush;
        private Uri _backgroundPath = new Uri("ms-appx:///Assets/gridbg.png");
        private const double _recenterMargin = 50;
        private const double _numberOfBackgroundRows = 2; // THIS IS A MAGIC NUMBER AND SHOULD CHANGE IF YOU CHANGE THE BACKGROUND IMAGE

        public DocumentCanvasView()
        {
            this.InitializeComponent();
            DataContextChanged += DocumentCanvasView_DataContextChanged;

            var recenterButton =
                new MenuButton(Symbol.MapPin, "Recenter", Colors.SteelBlue, OnRecenterTapped)
                {
                    HorizontalAlignment = HorizontalAlignment.Left,
                    VerticalAlignment = VerticalAlignment.Bottom,
                    Margin = new Thickness(10, 0, 0, 10)
                };
            xOuterGrid.Children.Add(recenterButton);
        }

        private void DocumentCanvasView_DataContextChanged(FrameworkElement sender, DataContextChangedEventArgs args)
        {
            _vm = DataContext as DocumentCanvasViewModel;

            if (_vm != null)
            {
                var itemsBinding = new Binding()
                {
                    Source = _vm,
                    Path = new PropertyPath(nameof(_vm.DocumentViews)),
                    Mode = BindingMode.OneWay
                };
                xItemsControl.SetBinding(ItemsControl.ItemsSourceProperty, itemsBinding);
            }
        }

        /// <summary>
        /// Pans and zooms upon touch manipulation 
        /// </summary>
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

        /// <summary>
        /// Zooms upon mousewheel interaction 
        /// </summary>
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

        /// <summary>
        /// Make translation inertia slow down faster
        /// </summary>
        private void UserControl_ManipulationInertiaStarting(object sender, ManipulationInertiaStartingRoutedEventArgs e)
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


        #region Recentering

        private void OnRecenterTapped()
        {
            RecenterViewOnDocument();
        }

        public void RecenterViewOnDocument(string documentId = null)
        {
            var documentView = GetDocumentView(documentId);

            var canvas = xItemsControl.ItemsPanelRoot as Canvas;
            Debug.Assert(canvas != null);

            // get document coordinates in canvas space
            var docTransform = documentView.TransformToVisual(canvas);
            var docUpperLeft = docTransform.TransformPoint(new Point());
            var docLowerRight = docTransform.TransformPoint(new Point(documentView.ActualWidth, documentView.ActualHeight));
            var docRect = new Rect(docUpperLeft, docLowerRight);
            var docCenter = new Point((docRect.Left + docRect.Right) / 2, (docRect.Top + docRect.Bottom) / 2);

            // translate document so it's center is in the upper left corner
            var translate = new TranslateTransform
            {
                X = -docCenter.X,
                Y = -docCenter.Y
            };

            // translate canvas so that the upper left corner is in the center
            var canvasCenter = new Point(xOuterGrid.ActualWidth / 2, xOuterGrid.ActualHeight / 2);
            var canvasTranslate = new TranslateTransform
            {
                X = canvasCenter.X,
                Y = canvasCenter.Y
            };

            // find the canvas scale needed so that the doc fits the canvas width or height
            var docScaleX = xOuterGrid.ActualWidth / (docRect.Width + _recenterMargin);
            var docScaleY = xOuterGrid.ActualHeight / (docRect.Height + _recenterMargin);

            // we scale by the minimum so that the larger side of the document fills the canvas
            var docScale = Math.Min(docScaleX, docScaleY);

            var scale = new ScaleTransform
            {
                CenterX = 0,
                CenterY = 0,
                ScaleX = docScale,
                ScaleY = docScale
            };

            // update the canvas scale for clamping
            _canvasScale = docScale;

            //Create initial composite transform
            var composite = new TransformGroup();
            composite.Children.Add(translate); // doc center in upper left
            composite.Children.Add(scale); // scale canvas so doc fills it
            composite.Children.Add(canvasTranslate); // move canvas upper left to center


            canvas.RenderTransform = new MatrixTransform { Matrix = composite.Value };
            SetTransformOnBackground(composite);
        }

        #endregion

        /// <summary>
        /// if documentId gets the first document it finds on the document canvas, otherwise returns the document associated with the passed in id
        /// </summary>
        /// <param name="documentId"></param>
        /// <returns></returns>
        public DocumentView GetDocumentView(string documentId = null)
        {
            return xItemsControl.GetDescendantsOfType<DocumentView>().FirstOrDefault(dv => documentId == null || dv.ViewModel.DocumentController.GetId() == documentId); 
        }

        private void XOuterGrid_OnSizeChanged(object sender, SizeChangedEventArgs e)
        {
            xClippingRect.Rect = new Rect(0, 0, xOuterGrid.ActualWidth, xOuterGrid.ActualHeight);
        }

        #region BackgroundTiling


        private void SetTransformOnBackground(TransformGroup composite)
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

        #endregion
    }
}
