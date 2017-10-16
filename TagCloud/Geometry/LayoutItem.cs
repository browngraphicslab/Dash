using Gma.CodeCloud.Controls.TextAnalyses.Processing;
using Windows.Foundation;

namespace Gma.CodeCloud.Controls.Geometry
{
    public class LayoutItem
    {
        public LayoutItem(Rect rectangle, IWord word)
        {
            this.Rectangle = rectangle;
            Word = word;
        }

        public Rect Rectangle { get; private set; }
        public IWord Word { get; private set; }

        public LayoutItem Clone()
        {
            return new LayoutItem(this.Rectangle, this.Word);
        }
    }
}
