using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Foundation;

namespace NewControls.Geometry
{
    public static class GeomExtensions
    {
        public static Size Size(this Rect r) {

            return new Size(r.Right - r.Left, r.Bottom - r.Top);
        }
        public static double Height(this Rect r)
        {

            return r.Bottom - r.Top;
        }
        public static double Width(this Rect r)
        {

            return r.Right - r.Left;
        }
        public static bool Contains(this Rect r, Rect r2)
        {

            if (r.Left <= r2.Left && r.Right >= r2.Right && r.Top <= r2.Top && r.Bottom >= r2.Bottom)
                return true;
            return false;
        }
        public static bool IntersectsWith(this Rect r1, Rect r2)
        {
            return !(r2.Left >= r1.Right
                   || r2.Right <= r1.Left
                   || r2.Top >= r1.Bottom
                   || r2.Bottom <= r1.Top
                   );
        }

        public static void Offset(this Point r,double x, double y)
        {
            r.X += x;
            r.Y += y;
        }
    }
}
