using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI;
using System.Diagnostics;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using DashShared;
using Microsoft.Graphics.Canvas;
using Microsoft.Graphics.Canvas.Brushes;
using Microsoft.Graphics.Canvas.UI;
using Microsoft.Graphics.Canvas.UI.Xaml;
using Path = Windows.UI.Xaml.Shapes.Path;


// The User Control item template is documented at http://go.microsoft.com/fwlink/?LinkId=234236

namespace Dash
{
    public sealed partial class CollectionFreeformView : UserControl
    {
        public bool CanLink;
        public PointerRoutedEventArgs PointerArgs;
        public Rect Bounds = new Rect(double.NegativeInfinity, double.NegativeInfinity, double.PositiveInfinity, double.PositiveInfinity);
        public double CanvasScale { get; set; } = 1;
        public const float MaxScale = 10;
        public const float MinScale = 0.001f;

        /// <summary>
        /// HashSet of current pointers in use so that the OperatorView does not respond to multiple inputs 
        /// </summary>
        private HashSet<uint> _currentPointers = new HashSet<uint>();
        /// <summary>
        /// IOReference (containing reference to fields) being referred to when creating the visual connection between fields 
        /// </summary>
        private IOReference _currReference;
        private Path _connectionLine;
        private BezierConverter _converter;
        private MultiBinding<PathFigureCollection> _lineBinding;


        private Dictionary<FieldReference, Path> _lineDict = new Dictionary<FieldReference, Path>();

        private Canvas parentCanvas;


        #region Background Translation Variables
        private CanvasBitmap _bgImage;
        private bool _resourcesLoaded;
        private CanvasImageBrush _bgBrush;
        private Uri _backgroundPath = new Uri("ms-appx:///Assets/gridbg.png");
        private const double _numberOfBackgroundRows = 2; // THIS IS A MAGIC NUMBER AND SHOULD CHANGE IF YOU CHANGE THE BACKGROUND IMAGE
        #endregion

        public CollectionFreeformView()
        {
            this.InitializeComponent();
            this.Loaded += Freeform_Loaded;
        }
        private void Freeform_Loaded(object sender, RoutedEventArgs e)
        {
            var parentGrid = this.GetFirstAncestorOfType<Grid>();
            parentGrid.PointerMoved += FreeformGrid_OnPointerMoved;
            parentGrid.PointerReleased += FreeformGrid_OnPointerReleased;
        }

        #region DraggingLinesAround

        public void StartDrag(IOReference ioReference)
        {
            if (!CanLink)
            {
                PointerArgs = ioReference.PointerArgs;
                return;
            }

            if (ioReference.PointerArgs == null) return;

            if (_currentPointers.Contains(ioReference.PointerArgs.Pointer.PointerId)) return;

            parentCanvas = xItemsControl.ItemsPanelRoot as Canvas;

            _currentPointers.Add(ioReference.PointerArgs.Pointer.PointerId);
            _currReference = ioReference;
            _connectionLine = new Path
            {
                StrokeThickness = 5,
                Stroke = new SolidColorBrush(Colors.Orange),
                IsHitTestVisible = false,
                CompositeMode =
                    ElementCompositeMode.SourceOver //TODO Bug in xaml, shouldn't need this line when the bug is fixed 
                //                                    //(https://social.msdn.microsoft.com/Forums/sqlserver/en-US/d24e2dc7-78cf-4eed-abfc-ee4d789ba964/windows-10-creators-update-uielement-clipping-issue?forum=wpdevelop)
            };
            Canvas.SetZIndex(_connectionLine, -1);
            _converter = new BezierConverter(ioReference.FrameworkElement, null, parentCanvas);

            try
            {
                _converter.Pos2 = ioReference.PointerArgs.GetCurrentPoint(parentCanvas).Position;

            }
            catch (COMException ex)
            {
            }

            _lineBinding =
                new MultiBinding<PathFigureCollection>(_converter, null);
            _lineBinding.AddBinding(ioReference.ContainerView, RenderTransformProperty);
            _lineBinding.AddBinding(ioReference.ContainerView, WidthProperty);
            _lineBinding.AddBinding(ioReference.ContainerView, HeightProperty);
            Binding lineBinding = new Binding
            {
                Source = _lineBinding,
                Path = new PropertyPath("Property")
            };
            PathGeometry pathGeo = new PathGeometry();
            BindingOperations.SetBinding(pathGeo, PathGeometry.FiguresProperty, lineBinding);
            _connectionLine.Data = pathGeo;

            parentCanvas.Children.Add(_connectionLine);

            if (!ioReference.IsOutput)
            {
                CheckLinePresence(ioReference.FieldReference);
                _lineDict.Add(ioReference.FieldReference, _connectionLine);
            }
        }

        public void CancelDrag(Pointer p)
        {
            _currentPointers.Remove(p.PointerId);
            UndoLine();
        }

        private void UndoLine()
        {
            parentCanvas.Children.Remove(_connectionLine);
            _connectionLine = null;
            _currReference = null;
        }

        public void EndDrag(IOReference ioReference)
        {
            IOReference inputReference = ioReference.IsOutput ? _currReference : ioReference;
            IOReference outputReference = ioReference.IsOutput ? ioReference : _currReference;

            _currentPointers.Remove(ioReference.PointerArgs.Pointer.PointerId);
            if (_connectionLine == null) return;

            if (_currReference.IsOutput == ioReference.IsOutput)
            {
                UndoLine();
                return;
            }
            if (_currReference.FieldReference == null) return; 

            _converter.Element2 = ioReference.FrameworkElement;
            _lineBinding.AddBinding(ioReference.ContainerView, RenderTransformProperty);
            _lineBinding.AddBinding(ioReference.ContainerView, WidthProperty);
            _lineBinding.AddBinding(ioReference.ContainerView, HeightProperty);

            DocumentController inputController =
                inputReference.FieldReference.GetDocumentController(null);
            var thisRef = (outputReference.ContainerView.DataContext as DocumentViewModel).DocumentController.GetDereferencedField(DashConstants.KeyStore.ThisKey, null);
            if (inputController.DocumentType == OperatorDocumentModel.OperatorType &&
                (inputController.GetDereferencedField(OperatorDocumentModel.OperatorKey, null) as OperatorFieldModelController).Inputs[inputReference.FieldReference.FieldKey] == TypeInfo.Document &&
                inputReference.FieldReference is DocumentFieldReference && thisRef != null)
                inputController.SetField(inputReference.FieldReference.FieldKey, thisRef, true);
            else
                inputController.SetField(inputReference.FieldReference.FieldKey,
                    new ReferenceFieldModelController(outputReference.FieldReference), true);

            if (!ioReference.IsOutput && _connectionLine != null)
            {
                CheckLinePresence(ioReference.FieldReference);
                _lineDict.Add(ioReference.FieldReference, _connectionLine);
                _connectionLine = null;
            }
            CancelDrag(ioReference.PointerArgs.Pointer);
        }

        /// <summary>
        /// Method to add the dropped field to the documentview 
        /// </summary>
        public void EndDragOnDocumentView(ref DocumentController cont, IOReference ioReference)
        {
            if (_currReference != null)
            {
                cont.SetField(_currReference.FieldKey, _currReference.FMController, true);
                EndDrag(ioReference); 
            }
        }

        /// <summary>
        /// Helper function that checks if connection line is already present for input ellipse; if so, destroy that line and create a new one  
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        private void CheckLinePresence(FieldReference model)
        {
            if (!_lineDict.ContainsKey(model)) return;
            var line = _lineDict[model];
            parentCanvas.Children.Remove(line);
            _lineDict.Remove(model);
        }

        private void FreeformGrid_OnPointerMoved(object sender, PointerRoutedEventArgs e)
        {
            if (_connectionLine != null)
            {
                Point pos = e.GetCurrentPoint(parentCanvas).Position;
                _converter.Pos2 = pos;
                _lineBinding.ForceUpdate();
            }
        }


        #endregion


        #region Manipulation

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
            CanvasScale *= delta.Scale;
            ClampScale(scale);


            //Create initial composite transform
            var composite = new TransformGroup();
            composite.Children.Add(scale);
            composite.Children.Add(canvas.RenderTransform);
            composite.Children.Add(translate);

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
            CanvasScale *= (float)scaleAmount;
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
        }


        #endregion

        private void FreeformGrid_OnPointerReleased(object sender, PointerRoutedEventArgs e)
        {
            DBTest.ResetCycleDetection();
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

        private void SetInitialTransformOnBackground()
        {
            var composite = new TransformGroup();
            var scale = new ScaleTransform
            {
                CenterX = 0,
                CenterY = 0,
                ScaleX = CanvasScale,
                ScaleY = CanvasScale
            };

            composite.Children.Add(scale);
            SetTransformOnBackground(composite);
        }

        private void CanvasControl_OnCreateResources(CanvasControl sender, CanvasCreateResourcesEventArgs args)
        {
            var task = Task.Run(async () =>
            {
                // Load the background image and create an image brush from it
                _bgImage = await CanvasBitmap.LoadAsync(sender, _backgroundPath);
                _bgBrush = new CanvasImageBrush(sender, _bgImage);

                // Set the brush's edge behaviour to wrap, so the image repeats if the drawn region is too big
                _bgBrush.ExtendX = _bgBrush.ExtendY = CanvasEdgeBehavior.Wrap;

                _resourcesLoaded = true;
            });
            args.TrackAsyncAction(task.AsAsyncAction());

            task.ContinueWith(continuationTask =>
            {
                SetInitialTransformOnBackground();
            }, TaskScheduler.FromCurrentSynchronizationContext());

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
