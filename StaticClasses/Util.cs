using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Media;

namespace Dash.StaticClasses
{
    public static class Util
    {
        /// <summary>
        /// Transforms point p to relative point in Window.Current.Content 
        /// </summary>
        /// <param name="p"></param>
        /// <param name="to"></param>
        /// <returns></returns>
        public static Point DeltaTransformFromVisual(Point p, UIElement to)
        {
            //GeneralTransform r = this.TransformToVisual(Window.Current.Content).Inverse;
            //Rect rect = new Rect(new Point(0, 0), new Point(1, 1));
            //Rect newRect = r.TransformBounds(rect);
            //Point p = new Point(rect.Width * e.Delta.Translation.X, rect.Height * e.Delta.Translation.Y);

            MatrixTransform r = to.TransformToVisual(Window.Current.Content) as MatrixTransform;
            Debug.Assert(r != null);
            Matrix m = r.Matrix;
            return new MatrixTransform { Matrix = new Matrix(1 / m.M11, 0, 0, 1 / m.M22, 0, 0) }.TransformPoint(p);
        }

        public static Point PointTransformFromVisual(Point p, UIElement to)
        {
            GeneralTransform r = to.TransformToVisual(Window.Current.Content).Inverse;
            Debug.Assert(r != null);
            return r.TransformPoint(p);
        }
    }
}
