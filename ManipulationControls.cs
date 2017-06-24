using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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

        /// <summary>
        /// Created a manipulation control to move element
        /// NOTE: bounds checking is done relative to element.Parent so the element must be in an element with the proper size for bounds checking
        /// </summary>
        /// <param name="element">The element to add manipulation to</param>
        public ManipulationControls(FrameworkElement element) {
            _element = element;
            element.ManipulationDelta += ManipulateDeltaMoveAndScale;
            element.ManipulationMode = ManipulationModes.All;
            //element.ManipulationMode = ManipulationModes.Scale;
            //element.ManipulationDelta += ManipulateDeltaScale;
        }

        //public void TurnOff()
        //{
        //    FrameworkElement handleControl = _element.Parent as FrameworkElement;

        //    if (handleControl != null)
        //        handleControl.ManipulationDelta -= ManipulateDeltaMoveAndScale;
        //    _element.ManipulationDelta       -= ManipulateDeltaScale;
        //}

        public void AddAllAndHandle()
        {
            if (!_disabled) return;
            _element.ManipulationDelta += ManipulateDeltaMoveAndScale;
            _element.ManipulationDelta -= EmptyManipulationDelta;
        }

        public void RemoveAllButHandle()
        {
            if (_disabled) return;
            _element.ManipulationDelta -= ManipulateDeltaMoveAndScale;
            _element.ManipulationDelta += EmptyManipulationDelta;
        }

        // == METHODS ==

        public void EmptyManipulationDelta(object sender, ManipulationDeltaRoutedEventArgs e)
        {
            e.Handled = true;
        }
        /// <summary>
        /// Applies manipulation controls (zoom, translate) in the grid manipulation event.
        /// </summary>
        public void ManipulateDeltaMoveAndScale(object sender, ManipulationDeltaRoutedEventArgs e) {
            TranslateAndScale(true, true, e);
        }

        /// <summary>
        /// Applies manipulation controls (translate) in the grid manipulation event. Typically,
        /// use this for elements that have a horizonal title bar that users use to drag.
        /// </summary>
        public void ManipulateDeltaScale(object sender, ManipulationDeltaRoutedEventArgs e) {
            TranslateAndScale(false, true, e);
        }

        /// <summary>
        /// Applies manipulation controls (zoom, translate) in the grid manipulation event.
        /// </summary>
        /// <param name="canTranslate">Are translate controls allowed?</param>
        /// <param name="canScale">Are scale controls allows?</param>
        /// <param name="e">passed in frm routed event args</param>
        private void TranslateAndScale(bool canTranslate, bool canScale, ManipulationDeltaRoutedEventArgs e) {
            FrameworkElement handleControl = _element.Parent as FrameworkElement;
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
            TranslateTransform translate = Util.TranslateInCanvasSpace(e.Delta.Translation, _element);
            
            //Clamp the scale factor 
            float newScale = _documentScale * e.Delta.Scale;
            if (newScale > MaxScale) {
                scale.ScaleX = MaxScale / _documentScale;
                scale.ScaleY = MaxScale / _documentScale;
                _documentScale = MaxScale;
            } else if (newScale < MinScale) {
                scale.ScaleX = MinScale / _documentScale;
                scale.ScaleY = MinScale / _documentScale;
                _documentScale = MinScale;
            } else {
                _documentScale = newScale;
            }

            if (canScale)
                group.Children.Add(scale);
            group.Children.Add(_element.RenderTransform);
            if (canTranslate)
                group.Children.Add(translate);

            ////Get top left and bottom right points of documents in canvas space
            //Point p1 = group.TransformPoint(new Point(0, 0));
            //Point p2 = group.TransformPoint(new Point(_element.ActualWidth, _element.ActualHeight));
            //Debug.Assert(_element.RenderTransform != null);
            //Point oldP1 = _element.RenderTransform.TransformPoint(new Point(0, 0));
            //Point oldP2 = _element.RenderTransform.TransformPoint(new Point(_element.ActualWidth, _element.ActualHeight));

            ////Check if translating or scaling the document puts the view out of bounds of the canvas
            ////Nullify scale or translate components accordingly
            //bool outOfBounds = false;
            //if (p1.X < 0)
            //{
            //    outOfBounds = true;
            //    translate.X = -oldP1.X;
            //    scale.CenterX = 0;
            //}
            //else if (p2.X > handleControl.ActualWidth)
            //{
            //    outOfBounds = true;
            //    translate.X = handleControl.ActualWidth - oldP2.X;
            //    scale.CenterX = _element.ActualWidth;
            //}
            //if (p1.Y < 0)
            //{
            //    outOfBounds = true;
            //    translate.Y = -oldP1.Y;
            //    scale.CenterY = 0;
            //}
            //else if (p2.Y > handleControl.ActualHeight)
            //{
            //    outOfBounds = true;
            //    translate.Y = handleControl.ActualHeight - oldP2.Y;
            //    scale.CenterY = _element.ActualHeight;
            //}

            ////If the view was out of bounds recalculate the composite matrix
            //if (outOfBounds)
            //{
            //    group = new TransformGroup();
            //    if (canScale)
            //        group.Children.Add(scale);
            //    group.Children.Add(_element.RenderTransform);
            //    if (canTranslate)
            //        group.Children.Add(translate);
            //}

            // apply the transformation group
            _element.RenderTransform = new MatrixTransform { Matrix = group.Value };
        }

    }
}
