using System;
using System.Diagnostics;
using Windows.Foundation;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;

namespace Dash {

    /// <summary>
    /// Instantiations of this class in a UserControl element will give that
    /// control's selected UIElement the ability to be moved and zoomed based on
    /// interactions with its given handleControl grid.
    /// </summary>
    public class ManipulationControls {

        // == MEMBERS ==
        private float _documentScale = 1.0f;
        private const float MinScale = 0.5f;
        private const float MaxScale = 2.0f;
        private bool _disabled;
        private FrameworkElement _element;
        //private Grid _grid;
        private bool _handle;
        public double CanvasScale { get; set; } = 1;
        public delegate void OnManipulatorTranslatedHandler(TransformGroupData transformationDelta);
        public event OnManipulatorTranslatedHandler OnManipulatorTranslated;

        public delegate void OnCanvasManipulatedHandler(TransformGroup composite);
        public event OnCanvasManipulatedHandler OnCanvasManipulated;

        /// <summary>
        /// Created a manipulation control to move element
        /// NOTE: bounds checking is done relative to element.Parent so the element must be in an element with the proper size for bounds checking
        /// </summary>
        /// <param name="element">The element to add manipulation to</param>
        /// <param name="isFreeform"></param>
        public ManipulationControls(FrameworkElement element, bool isFreeform = false) {
            _element = element;
            if (isFreeform)
            {
                element.ManipulationDelta += UserControl_ManipulationDelta;
                element.PointerWheelChanged += UserControl_PointerWheelChanged;
                element.ManipulationInertiaStarting += UserControl_ManipulationInertiaStarting;
                //_grid = element.GetFirstAncestorOfType<Grid>();
            }
            else
            {
                element.ManipulationDelta += ManipulateDeltaMoveAndScale;
            }
            element.ManipulationMode = ManipulationModes.All;
        }

        public void AddAllAndHandle()
        {
            if (!_disabled) return;
            _element.ManipulationDelta -= EmptyManipulationDelta;
            _element.ManipulationDelta += ManipulateDeltaMoveAndScale;
            _disabled = false;
        }

        public void RemoveAllButHandle()
        {
            RemoveAllSetHandle(true);
        }
        public void RemoveAllAndDontHandle()
        {
            RemoveAllSetHandle(false);
        }

        private void RemoveAllSetHandle(bool handle)
        {
            if (_disabled) return;
            _element.ManipulationDelta -= ManipulateDeltaMoveAndScale;
            _element.ManipulationDelta += EmptyManipulationDelta;
            _handle = handle;
            _disabled = true;
        }

        // == METHODS ==

        private void EmptyManipulationDelta(object sender, ManipulationDeltaRoutedEventArgs e)
        {
            e.Handled = _handle;
        }

        /// <summary>
        /// Applies manipulation controls (zoom, translate) in the grid manipulation event.
        /// </summary>
        private void ManipulateDeltaMoveAndScale(object sender, ManipulationDeltaRoutedEventArgs e) {
            TranslateAndScale(true, true, e);
        }

        /// <summary>
        /// Applies manipulation controls (translate) in the grid manipulation event. Typically,
        /// use this for elements that have a horizonal title bar that users use to drag.
        /// </summary>
        private void ManipulateDeltaScale(object sender, ManipulationDeltaRoutedEventArgs e) {
            TranslateAndScale(false, true, e);
        }

        /// <summary>
        /// Applies manipulation controls (zoom, translate) in the grid manipulation event.
        /// </summary>
        /// <param name="canTranslate">Are translate controls allowed?</param>
        /// <param name="canScale">Are scale controls allows?</param>
        /// <param name="e">passed in frm routed event args</param>
        private void TranslateAndScale(bool canTranslate, bool canScale, ManipulationDeltaRoutedEventArgs e) {
            FrameworkElement handleControl = VisualTreeHelper.GetParent(_element) as FrameworkElement;
            e.Handled = true;

            //Create initial composite transform 
            TransformGroup group = new TransformGroup();
            ScaleTransform scale = new ScaleTransform {
                CenterX = e.Position.X,
                CenterY = e.Position.Y,
                ScaleX = e.Delta.Scale,
                ScaleY = e.Delta.Scale
            };
            
            // set up translation transform
            TranslateTransform translate = Util.TranslateInCanvasSpace(e.Delta.Translation, handleControl);
            
            //Clamp the scale factor 
            float newScale = _documentScale * e.Delta.Scale;
            if (newScale > MaxScale) {
                scale.ScaleX = MaxScale / _documentScale;
                scale.ScaleY = MaxScale / _documentScale;
                _documentScale = MaxScale;
                return;
            } else if (newScale < MinScale) {
                scale.ScaleX = MinScale / _documentScale;
                scale.ScaleY = MinScale / _documentScale;
                _documentScale = MinScale;
                return;
            } else {
                _documentScale = newScale;
            }

            if (canScale)
                group.Children.Add(scale);
            group.Children.Add(_element.RenderTransform);
            if (canTranslate)
            {
                group.Children.Add(translate);
                OnManipulatorTranslated?.Invoke(new TransformGroupData(new Point(translate.X, translate.Y), 
                                                                        new Point(scale.CenterX, scale.CenterY),
                                                                        new Point(scale.ScaleX, scale.ScaleY)));
            }
        }

        /// <summary>
        /// Pans and zooms upon touch manipulation 
        /// </summary>
        public void UserControl_ManipulationDelta(object sender, ManipulationDeltaRoutedEventArgs e)
        {
            if (!_element.IsHitTestVisible) return;
            Debug.Assert(_element != null);
            e.Handled = true;
            var delta = e.Delta;

            //Create initial translate and scale transforms
            //Translate is in screen space, scale is in canvas space
            var translate = new TranslateTransform
            {
                X = delta.Translation.X,
                Y = delta.Translation.Y
            };

            var p = Util.PointTransformFromVisual(e.Position, _element);
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
            composite.Children.Add(_element.RenderTransform);
            composite.Children.Add(translate);

            //Get top left and bottom right screen space points in canvas space
            var inverse = composite.Inverse;
            Debug.Assert(inverse != null);
            Debug.Assert(_element.RenderTransform != null);
            var renderInverse = _element.RenderTransform.Inverse;
            Debug.Assert(renderInverse != null);

            //var topLeft = inverse.TransformPoint(new Point(0, 0));
            //var bottomRight = inverse.TransformPoint(new Point(_grid.ActualWidth, _grid.ActualHeight));
            //var preTopLeft = renderInverse.TransformPoint(new Point(0, 0));
            //var preBottomRight = renderInverse.TransformPoint(new Point(_grid.ActualWidth, _grid.ActualHeight));
            ////Check if the panning or zooming puts the view out of bounds of the canvas
            ////Nullify scale or translate components accordingly
            //var outOfBounds = false;
            ////Create a canvas space translation to correct the translation if necessary
            //var fixTranslate = new TranslateTransform();
            //if (topLeft.X < Bounds.Left && bottomRight.X > Bounds.Right)
            //{
            //    translate.X = 0;
            //    fixTranslate.X = 0;
            //    var scaleAmount = (bottomRight.X - topLeft.X) / Bounds.Width;
            //    scale.ScaleY = scaleAmount;
            //    scale.ScaleX = scaleAmount;
            //    outOfBounds = true;
            //}
            //else if (topLeft.X < Bounds.Left)
            //{
            //    translate.X = 0;
            //    fixTranslate.X = preTopLeft.X;
            //    scale.CenterX = Bounds.Left;
            //    outOfBounds = true;
            //}
            //else if (bottomRight.X > Bounds.Right)
            //{
            //    translate.X = 0;
            //    fixTranslate.X = -(Bounds.Right - preBottomRight.X - 1);
            //    scale.CenterX = Bounds.Right;
            //    outOfBounds = true;
            //}
            //if (topLeft.Y < Bounds.Top && bottomRight.Y > Bounds.Bottom)
            //{
            //    translate.Y = 0;
            //    fixTranslate.Y = 0;
            //    var scaleAmount = (bottomRight.Y - topLeft.Y) / Bounds.Height;
            //    scale.ScaleX = scaleAmount;
            //    scale.ScaleY = scaleAmount;
            //    outOfBounds = true;
            //}
            //else if (topLeft.Y < Bounds.Top)
            //{
            //    translate.Y = 0;
            //    fixTranslate.Y = preTopLeft.Y;
            //    scale.CenterY = Bounds.Top;
            //    outOfBounds = true;
            //}
            //else if (bottomRight.Y > Bounds.Bottom)
            //{
            //    translate.Y = 0;
            //    fixTranslate.Y = -(Bounds.Bottom - preBottomRight.Y - 1);
            //    scale.CenterY = Bounds.Bottom;
            //    outOfBounds = true;
            //}

            ////If the view was out of bounds recalculate the composite matrix
            //if (outOfBounds)
            //{
            //    composite = new TransformGroup();
            //    composite.Children.Add(fixTranslate);
            //    composite.Children.Add(scale);
            //    composite.Children.Add(_canvas.RenderTransform);
            //    composite.Children.Add(translate);
            //}
            _element.RenderTransform = new MatrixTransform { Matrix = composite.Value };
            OnCanvasManipulated?.Invoke(composite);
        }

        /// <summary>
        /// Zooms upon mousewheel interaction 
        /// </summary>
        public void UserControl_PointerWheelChanged(object sender, PointerRoutedEventArgs e)
        {
            if (!_element.IsHitTestVisible) return;
            Debug.Assert(_element != null);
            e.Handled = true;
            //Get mousepoint in canvas space 
            var point = e.GetCurrentPoint(_element);
            var scaleAmount = Math.Pow(1 + 0.15 * Math.Sign(point.Properties.MouseWheelDelta),
                Math.Abs(point.Properties.MouseWheelDelta) / 120.0f);
            scaleAmount = Math.Max(Math.Min(scaleAmount, 1.7f), 0.4f);
            CanvasScale *= (float)scaleAmount;
            Debug.Assert(_element.RenderTransform != null);
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
            composite.Children.Add(_element.RenderTransform);

            var inverse = composite.Inverse;
            Debug.Assert(inverse != null);
            var renderInverse = _element.RenderTransform.Inverse;
            Debug.Assert(inverse != null);
            Debug.Assert(renderInverse != null);

            _element.RenderTransform = new MatrixTransform { Matrix = composite.Value };
            OnCanvasManipulated?.Invoke(composite);
        }

        /// <summary>
        /// Make translation inertia slow down faster
        /// </summary>
        public void UserControl_ManipulationInertiaStarting(object sender, ManipulationInertiaStartingRoutedEventArgs e)
        {
            e.TranslationBehavior.DesiredDeceleration = 0.01;
        }

        private void ClampScale(ScaleTransform scale)
        {
            if (CanvasScale > 10)
            {
                CanvasScale = 10;
                scale.ScaleX = 1;
                scale.ScaleY = 1;
            }
            if (CanvasScale < 0.001f)
            {
                CanvasScale = 0.001f;
                scale.ScaleX = 1;
                scale.ScaleY = 1;
            }
        }

    }
}
