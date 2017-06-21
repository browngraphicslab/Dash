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
        private Grid handleControl;
        private UIElement element;

        // == CONSTRUCTORS ==
        public ManipulationControls(Grid handleControl, UIElement element) {
            handleControl.ManipulationDelta += ManipulateDeltaaMoveAndScale;
            handleControl.ManipulationMode = ManipulationModes.All;
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
            TranslateTransform translate = Util.TranslateInCanvasSpace(e.Delta.Translation, element);
            
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

            // apply the transformation group
            if (canScale)
                group.Children.Add(scale);
            group.Children.Add(element.RenderTransform);
            if (canTranslate)
                group.Children.Add(translate);
            element.RenderTransform = new MatrixTransform { Matrix = group.Value };
        }

    }
}
