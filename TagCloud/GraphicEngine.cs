using System;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Shapes;
using Windows.UI.Text;
using Windows.UI.Core;
using Windows.UI;
using Windows.Foundation;
using Gma.CodeCloud.Controls.Geometry;
using NewControls.Geometry;
using Windows.UI.Xaml.Controls;
using NewControls;

namespace Gma.CodeCloud.Controls
{
    public class GdiGraphicEngine : IGraphicEngine
    {

        private readonly int m_MinWordWeight;
        private readonly int m_MaxWordWeight;
        private Font m_LastUsedFont;

        public FontFamily FontFamily { get; set; }
        public FontStyle FontStyle { get; set; }
        public Color[] Palette { get; private set; }
        public double MinFontSize { get; set; }
        public double MaxFontSize { get; set; }

        public GdiGraphicEngine( FontFamily fontFamily, FontStyle fontStyle, Color[] palette, double minFontSize, double maxFontSize, int minWordWeight, int maxWordWeight)
        {
            m_MinWordWeight = minWordWeight;
            m_MaxWordWeight = maxWordWeight;
            FontFamily = fontFamily;
            FontStyle = fontStyle;
            Palette = palette;
            MinFontSize = minFontSize;
            MaxFontSize = maxFontSize;
            m_LastUsedFont = new Font(this.FontFamily, maxFontSize, this.FontStyle);
           // m_Graphics.SmoothingMode = SmoothingMode.AntiAlias;
        }

        public Size Measure(string text, int weight)
        {
            var font = GetFont(weight);
            var tb = new TextBlock();
            tb.Text = text;
            tb.FontSize = font.Size;
            tb.FontFamily = font.FontFamily;
            tb.Measure(new Size(Double.PositiveInfinity, Double.PositiveInfinity));
            
            return tb.DesiredSize;
        }

        public void Draw(LayoutItem layoutItem)
        {
            var font = GetFont(layoutItem.Word.Occurrences);
            Color color = GetPresudoRandomColorFromPalette(layoutItem);
            var point = new Point((int)layoutItem.Rectangle.X, (int)layoutItem.Rectangle.Y);
            var tb = new TextBlock();
            tb.Text = layoutItem.Word.Text;
            tb.FontFamily = font.FontFamily;
            tb.FontSize = font.Size;
            tb.FontStyle = font.FontStyle;
            tb.Foreground = new SolidColorBrush(color);
            tb.RenderTransform = new TranslateTransform() { X = point.X, Y = point.Y };
            WordCloud.Instance.xLayoutGrid.Children.Add(tb);
        }

        public void DrawEmphasized(LayoutItem layoutItem)
        {
            var font = GetFont(layoutItem.Word.Occurrences);
            Color color = GetPresudoRandomColorFromPalette(layoutItem);
            //m_Graphics.DrawString(layoutItem.Word, font, brush, layoutItem.Rectangle);
            Point point = new Point((int)layoutItem.Rectangle.X, (int)layoutItem.Rectangle.Y);


            //TextRenderer.DrawText(layoutItem.Word.Text, font, point, Colors.LightGray);
            //int offset = (int)(5 *font.Size / MaxFontSize)+1;
            //point.Offset(-offset, -offset);
            //TextRenderer.DrawText(layoutItem.Word.Text, font, point, color);
            var tb = new TextBlock();
            tb.Text = layoutItem.Word.Text;
            tb.FontFamily = font.FontFamily;
            tb.FontSize = font.Size;
            tb.FontStyle = font.FontStyle;
            tb.Foreground = new SolidColorBrush(color);
            tb.RenderTransform = new TranslateTransform() { X = point.X, Y = point.Y };
            WordCloud.Instance.xLayoutGrid.Children.Add(tb);
        }
        public class Font {
            public FontFamily FontFamily;
            public double Size;
            public FontStyle FontStyle;
            public Font (FontFamily fontFamily, double fontSize, FontStyle fontStyle)
            {
                FontFamily = fontFamily;
                 Size = fontSize;
                FontStyle = fontStyle;
            }
        }

        private Font GetFont(int weight)
        {
            var fontSize = (weight - m_MinWordWeight) / (m_MaxWordWeight - m_MinWordWeight) * (MaxFontSize - MinFontSize) + MinFontSize;
            if (m_LastUsedFont.Size != fontSize)
            {
                m_LastUsedFont = new Font(this.FontFamily, fontSize, this.FontStyle);
            }
            return m_LastUsedFont;
        }

        private Color GetPresudoRandomColorFromPalette(LayoutItem layoutItem)
        {
            Color color = Palette[layoutItem.Word.Occurrences * layoutItem.Word.Text.Length % Palette.Length];
            return color;
        }

        public void Dispose()
        {
        }
    }
}
