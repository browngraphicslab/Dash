using System.Diagnostics;
using Windows.Foundation;
using DashShared;
using iText.Kernel.Font;
using iText.Kernel.Pdf.Canvas.Parser.Data;

namespace Dash
{
    public class SelectableElement : EntityBase
    {

        public enum ElementType
        {
            Text,
            Image
        }
        public SelectableElement(int index, string text, Rect bounds, TextRenderInfo textData = null) : base(index.ToString())
        {
            Index = index;
            Contents = text;
            Type = ElementType.Text;
            Bounds = bounds;
            FontFamily = textData?.GetFont().GetFontProgram().GetFontNames().GetFontName();
            AvgWidth = textData?.GetFont().GetFontProgram().GetAvgWidth() ?? 12;
        }


        public SelectableElement(int index, string text, Rect bounds, string fontFamily, int avgWidth) : base(index.ToString())
        {
            Index = index;
            Contents = text;
            Type = ElementType.Text;
            Bounds = bounds;
            FontFamily = fontFamily;
            AvgWidth = avgWidth;
        }

        ~SelectableElement()
        {
            //Debug.WriteLine(Contents + " disposed");
        }

        public string FontFamily { get; set; }
        public int AvgWidth { get; set; }
        public Rect Bounds { get; }
        public int Index { get; set; }
        public int RawIndex { get; set; }
        public object Contents { get; }
        public ElementType Type { get; }
    }

}
