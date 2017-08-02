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

        public delegate void OnDocumentViewLoadedHandler(DocumentCanvasView sender, DocumentView documentView);
        public event OnDocumentViewLoadedHandler OnDocumentViewLoaded;

        private Rect _bounds = new Rect(double.NegativeInfinity, double.NegativeInfinity, double.PositiveInfinity, double.PositiveInfinity);
        private double _canvasScale { get; set; } = 1;
        private const float MaxScale = 10;
        private const float MinScale = 0.1f;
        private const double _recenterMargin = 50;

        private DocumentCanvasViewModel _vm;
        private CanvasBitmap _bgImage;
        private bool _resourcesLoaded;
        private CanvasImageBrush _bgBrush;
        private Uri _backgroundPath = new Uri("ms-appx:///Assets/gridbg.png");
        private const double _numberOfBackgroundRows = 2; // THIS IS A MAGIC NUMBER AND SHOULD CHANGE IF YOU CHANGE THE BACKGROUND IMAGE
        private ManipulationControls _manipulator;

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

        /// <summary>
        /// if documentId gets the first document it finds on the document canvas, otherwise returns the document associated with the passed in id
        /// </summary>
        /// <param name="documentId"></param>
        /// <returns></returns>
        private DocumentView GetDocumentView(string documentId = null)
        {
            return xItemsControl.GetDescendantsOfType<DocumentView>().FirstOrDefault(dv => documentId == null || dv.ViewModel.DocumentController.GetId() == documentId);
        }

        private void XOuterGrid_OnSizeChanged(object sender, SizeChangedEventArgs e)
        {
            xClippingRect.Rect = new Rect(0, 0, xOuterGrid.ActualWidth, xOuterGrid.ActualHeight);
        }

        private void DocumentViewOnLoaded(object sender, RoutedEventArgs e)
        {
            OnDocumentViewLoaded?.Invoke(this, sender as DocumentView);
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
