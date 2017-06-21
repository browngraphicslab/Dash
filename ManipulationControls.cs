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
        private FrameworkElement handleControl;
        private FrameworkElement element;

        // == CONSTRUCTORS ==
        public ManipulationControls(FrameworkElement handleControl, FrameworkElement element) {
            element.ManipulationDelta += ManipulateDeltaaMoveAndScale;
            element.ManipulationMode = ManipulationModes.All;
            this.element = element;
            this.element.ManipulationMode = ManipulationModes.Scale;
            this.element.ManipulationDelta += ManipulateDeltaScale;
            this.handleControl = handleControl;
        }

        // == METHODS ==

        /// <summary>
        /// Applies manipulation controls (zoom, translate) in the grid manipulation event.
        /// </summary>
        public void ManipulateDeltaaMoveAndScale(object sender, ManipulationDeltaRoutedEventArgs e) {
            translateAndScale(true, true, e);
        }


        /// <summary>
        /// Applies manipulation controls (translate) in the grid manipulation event. Typically,
        /// use this for elements that have a horizonal title bar that users use to drag.
        /// </summary>
        public void ManipulateDeltaScale(object sender, ManipulationDeltaRoutedEventArgs e) {
            translateAndScale(false, true, e);
        }

        /// <summary>
        /// Applies manipulation controls (zoom, translate) in the grid manipulation event.
        /// </summary>
        /// <param name="canTranslate">Are translate controls allowed?</param>
        /// <param name="canScale">Are scale controls allows?</param>
        /// <param name="e">passed in frm routed event args</param>
        private void translateAndScale(bool canTranslate, bool canScale, ManipulationDeltaRoutedEventArgs e) {
            handleControl = element.Parent as FrameworkElement;
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
            } else if (newScale < MinScale) {
                scale.ScaleX = MinScale / _documentScale;
                scale.ScaleY = MinScale / _documentScale;
                _documentScale = MinScale;
            } else {
                _documentScale = newScale;
            }

            if (canScale)
                group.Children.Add(scale);
            group.Children.Add(element.RenderTransform);
            if (canTranslate)
                group.Children.Add(translate);

            //Get top left and bottom right points of documents in canvas space
            Point p1 = group.TransformPoint(new Point(0, 0));
            Point p2 = group.TransformPoint(new Point(element.ActualWidth, element.ActualHeight));
            Debug.Assert(element.RenderTransform != null);
            Point oldP1 = element.RenderTransform.TransformPoint(new Point(0, 0));
            Point oldP2 = element.RenderTransform.TransformPoint(new Point(element.ActualWidth, element.ActualHeight));

            //Check if translating or scaling the document puts the view out of bounds of the canvas
            //Nullify scale or translate components accordingly
            bool outOfBounds = false;
            if (p1.X < 0)
            {
                outOfBounds = true;
                translate.X = -oldP1.X;
                scale.CenterX = 0;
            }
            else if (p2.X > handleControl.ActualWidth)
            {
                outOfBounds = true;
                translate.X = handleControl.ActualWidth - oldP2.X;
                scale.CenterX = element.ActualWidth;
            }
            if (p1.Y < 0)
            {
                outOfBounds = true;
                translate.Y = -oldP1.Y;
                scale.CenterY = 0;
            }
            else if (p2.Y > handleControl.ActualHeight)
            {
                outOfBounds = true;
                translate.Y = handleControl.ActualHeight - oldP2.Y;
                scale.CenterY = element.ActualHeight;
            }

            //If the view was out of bounds recalculate the composite matrix
            if (outOfBounds)
            {
                group = new TransformGroup();
                if (canScale)
                    group.Children.Add(scale);
                group.Children.Add(element.RenderTransform);
                if (canTranslate)
                    group.Children.Add(translate);
            }

            // apply the transformation group
            element.RenderTransform = new MatrixTransform { Matrix = group.Value };
        }

    }
}
