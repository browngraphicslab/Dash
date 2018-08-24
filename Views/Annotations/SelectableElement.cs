using Windows.Foundation;

namespace Dash
{
    public class SelectableElement
    {

        public enum ElementType
        {
            Text,
            Image
        }
        public SelectableElement(int index, string text, Rect bounds)
        {
            Index = index;
            Contents = text;
            Type = ElementType.Text;
            Bounds = bounds;
        }

        public Rect Bounds { get; }
        public int Index { get; set; }

        public int RawIndex { get; set; }
        public object Contents { get; }
        public ElementType Type { get; }
    }

}