using System.Diagnostics;
using Windows.Foundation;
using Windows.UI.Xaml;
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
            if (_disabled) return;
            _element.ManipulationDelta -= ManipulateDeltaMoveAndScale;
            _element.ManipulationDelta += EmptyManipulationDelta;
            _disabled = true;
        }

        // == METHODS ==

        private void EmptyManipulationDelta(object sender, ManipulationDeltaRoutedEventArgs e)
        {
            e.Handled = true;
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

        public delegate void OnManipulatorTranslatedHandler(TransformGroupData transformationDelta);
        public event OnManipulatorTranslatedHandler OnManipulatorTranslated;


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
            {
                group.Children.Add(translate);
                OnManipulatorTranslated?.Invoke(new TransformGroupData(new Point(translate.X, translate.Y), 
                                                                        new Point(scale.CenterX, scale.CenterY),
                                                                        new Point(scale.ScaleX, scale.ScaleY)));
            }
        }

    }
}
