

using NewControls.Geometry;
using Windows.Foundation;

namespace Gma.CodeCloud.Controls.Geometry
{
    public class TypewriterLayout : BaseLayout
    {
        public TypewriterLayout(Size size) : base(size)
        {
            m_Carret = new Point(size.Width, 0);
        }

        private Point m_Carret;
        private double m_LineHeight;

        public override bool TryFindFreeRectangle(Size size, out Rect foundRectangle)
        {
            foundRectangle = new Rect(m_Carret, size);
            if (HorizontalOverflow(foundRectangle))
            {
                foundRectangle = LineFeed(foundRectangle);
                if (!IsInsideSurface(foundRectangle))
                {
                    return false;
                }
            }
            m_Carret = new Point(foundRectangle.Right, foundRectangle.Y);

            return true;
        }

        private Rect LineFeed(Rect rectangle)
        {
            Rect result = new Rect(new Point(0, m_Carret.Y + m_LineHeight), rectangle.Size());
            m_LineHeight = rectangle.Height();
            return result;
        }

        private bool HorizontalOverflow(Rect rectangle)
        {
            return rectangle.Right > Surface.Right;
        }
    }
}
