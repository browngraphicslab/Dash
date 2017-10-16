using System;
using System.Collections.Generic;
using Gma.CodeCloud.Controls.Geometry.DataStructures;
using Gma.CodeCloud.Controls.TextAnalyses.Processing;
using Windows.Foundation;

namespace Gma.CodeCloud.Controls.Geometry
{
    public abstract class BaseLayout : ILayout
    {
        protected QuadTree<LayoutItem> QuadTree { get; set; }
        protected Point Center { get; set; }
        protected Rect Surface { get; set; }

        protected BaseLayout(Size size)
        {
            Surface = new Rect(new Point(0, 0), size);
            QuadTree = new QuadTree<LayoutItem>(Surface);
            Center = new Point(Surface.X + size.Width / 2, Surface.Y + size.Height / 2);
        }

        public void Arrange(IEnumerable<IWord> words, IGraphicEngine graphicEngine)
        {
            if (words == null)
            {
                throw new ArgumentNullException("words");
            }

            foreach (IWord word in words)
            {
                Size size = graphicEngine.Measure(word.Text, word.Occurrences);
                Rect freeRectangle;
                if (!TryFindFreeRectangle(size, out freeRectangle))
                {
                    return;
                }
                LayoutItem item = new LayoutItem(freeRectangle, word);
                QuadTree.Insert(item);
            }
        }

        public abstract bool TryFindFreeRectangle(Size size, out Rect foundRectangle);

        public IEnumerable<LayoutItem> GetWordsInArea(Rect area)
        {
            return QuadTree.Query(area);
        }

        protected bool IsInsideSurface(Rect targetRectangle)
        {
            return IsInside(Surface, targetRectangle);
        }

        private static bool IsInside(Rect outer, Rect inner)
        {
            return
                inner.X >= outer.X &&
                inner.Y >= outer.Y &&
                inner.Bottom <= outer.Bottom &&
                inner.Right <= outer.Right;
        }
    }
}
