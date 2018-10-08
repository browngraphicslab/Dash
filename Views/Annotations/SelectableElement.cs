using Windows.Foundation;
using iText.Kernel.Font;
using iText.Kernel.Pdf.Canvas.Parser.Data;

namespace Dash
{
    public class SelectableElement
    {

        public enum ElementType
        {
            Text,
            Image
        }
        public SelectableElement(int index, string text, Rect bounds, TextRenderInfo textData = null)
        {
            Index = index;
            Contents = text;
            Type = ElementType.Text;
            Bounds = bounds;
            TextData = textData;
        }

        public TextRenderInfo TextData { get; set; }
        public Rect Bounds { get; }
        public int Index { get; set; }
        public int RawIndex { get; set; }
        public object Contents { get; }
        public ElementType Type { get; }
    }

}
