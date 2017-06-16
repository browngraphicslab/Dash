using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;

namespace Dash {

    /// <summary>
    /// Instantiations of this class in a UserControl element will give that
    /// control's selected xGrid the ability to be moved and zoomed.
    /// </summary>
    public class ManipulationControls {

        // == MEMBERS ==
        private float _documentScale = 1.0f;
        public const float MinScale = 0.5f;
        public const float MaxScale = 2.0f;
        public Grid XGrid;
        public UserControl usercontrol;

        // == CONSTRUCTORS ==
        public ManipulationControls(Grid xGrid, UserControl usercontrol) {
            xGrid.ManipulationDelta += Grid_ManipulationDelta;
            //xGrid.ManipulationMode = ManipulationModes.All; for data binding of manip mode
            this.usercontrol = usercontrol;
            XGrid = xGrid;
        }

        // == METHODS ==

        /// <summary>
        /// Applies manipulation controls (zoom, pan) in the grid manipulation event.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void Grid_ManipulationDelta(object sender, ManipulationDeltaRoutedEventArgs e) {
            e.Handled = true;

            //Create initial composite transform 
            TransformGroup group = new TransformGroup();

            ScaleTransform scale = new ScaleTransform {
                CenterX = e.Position.X,
                CenterY = e.Position.Y,
                ScaleX = e.Delta.Scale,
                ScaleY = e.Delta.Scale
            };

            //Point p = Util.DeltaTransformFromVisual(e.Delta.Translation, this);
            //TranslateTransform translate = new TranslateTransform
            //{
            //    X = p.X,
            //    Y = p.Y
            //};
            TranslateTransform translate = Util.TranslateInCanvasSpace(e.Delta.Translation, usercontrol);


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

            group.Children.Add(scale);
            group.Children.Add(usercontrol.RenderTransform);
            group.Children.Add(translate);
            /*
            //Get top left and bottom right points of documents in canvas space
            Point p1 = group.TransformPoint(new Point(0, 0));
            Point p2 = group.TransformPoint(new Point(XGrid.ActualWidth, XGrid.ActualHeight));
            Debug.Assert(usercontrol.RenderTransform != null);
            Point oldP1 = usercontrol.RenderTransform.TransformPoint(new Point(0, 0));
            Point oldP2 = usercontrol.RenderTransform.TransformPoint(new Point(XGrid.ActualWidth, XGrid.ActualHeight));

            //Check if translating or scaling the document puts the view out of bounds of the canvas
            //Nullify scale or translate components accordingly
            bool outOfBounds = false;
            if (p1.X < 0) {
                outOfBounds = true;
                translate.X = -oldP1.X;
                scale.CenterX = 0;
            } else if (p2.X > FreeformView.MainFreeformView.Canvas.ActualWidth) {
                outOfBounds = true;
                translate.X = FreeformView.MainFreeformView.Canvas.ActualWidth - oldP2.X;
                scale.CenterX = XGrid.ActualWidth;
            }
            if (p1.Y < 0) {
                outOfBounds = true;
                translate.Y = -oldP1.Y;
                scale.CenterY = 0;
            } else if (p2.Y > FreeformView.MainFreeformView.Canvas.ActualHeight) {
                outOfBounds = true;
                translate.Y = FreeformView.MainFreeformView.Canvas.ActualHeight - oldP2.Y;
                scale.CenterY = XGrid.ActualHeight;
            }

            //If the view was out of bounds recalculate the composite matrix
            if (outOfBounds) {
                group = new TransformGroup();
                group.Children.Add(scale);
                group.Children.Add(usercontrol.RenderTransform);
                group.Children.Add(translate);
            }
            */
            usercontrol.RenderTransform = new MatrixTransform { Matrix = group.Value };
        }

    }
}
