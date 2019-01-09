using System;
using Windows.UI.Xaml.Media;
using Windows.UI.Text;
using Windows.UI;
using Windows.Foundation;
using Gma.CodeCloud.Controls.Geometry;
using Windows.UI.Xaml.Controls;
using NewControls;
using System.Diagnostics;

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

        WordCloud _cloud;
        public GdiGraphicEngine(WordCloud cloud, FontFamily fontFamily, FontStyle fontStyle, Color[] palette, double minFontSize, double maxFontSize, int minWordWeight, int maxWordWeight)
        {
            _cloud = cloud;
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

        bool _rightMousePressed = false;
        public void Draw(Panel xLayoutGrid, LayoutItem layoutItem)
        {
            var font  = GetFont(layoutItem.Word.Occurrences);
            var color = GetPresudoRandomColorFromPalette(layoutItem);
            var point = new Point((int)layoutItem.Rectangle.X, (int)layoutItem.Rectangle.Y);
            var tb    = new TextBlock();

            tb.HorizontalAlignment = Windows.UI.Xaml.HorizontalAlignment.Left;
            tb.VerticalAlignment   = Windows.UI.Xaml.VerticalAlignment.Top;
            tb.Text                = layoutItem.Word.Text;
            tb.FontFamily          = font.FontFamily;
            tb.FontSize            = font.Size;
            tb.FontStyle           = font.FontStyle;
            tb.Padding             = new Windows.UI.Xaml.Thickness();
            tb.Foreground          = new SolidColorBrush(color);
            tb.CanDrag             = true;
            tb.ManipulationMode    = Windows.UI.Xaml.Input.ManipulationModes.All;
            tb.PointerPressed      += (sender, e) => _rightMousePressed = e.GetCurrentPoint(tb).Properties.IsRightButtonPressed;
            tb.DragStarting        += (sender, e) => _cloud.TriggerDragStarting(tb.Text, e);
            tb.ManipulationStarted += (sender, e) => { if (!_rightMousePressed) { e.Handled = true; e.Complete(); } };
            tb.ManipulationDelta   += (sender, e) => { if (!_rightMousePressed) e.Handled = true; };
            tb.Tapped              += (sender, e) => Debug.WriteLine(tb.Text); 
            tb.PointerEntered      += (sender, e) => tb.FontWeight = FontWeights.ExtraBold; 
            tb.PointerExited       += (sender, e) =>  tb.FontWeight = FontWeights.Normal;
            //tb.Margin = new Windows.UI.Xaml.Thickness(point.X, point.Y, 0, 0);
            tb.RenderTransform = new TranslateTransform() { X = point.X, Y = point.Y };
            xLayoutGrid.Children.Add(tb);
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

        private Font GetFont(double weight)
        {
            var fontSize = (weight - m_MinWordWeight) / (m_MaxWordWeight - m_MinWordWeight) * (MaxFontSize - MinFontSize) + MinFontSize;
            if (double.IsNaN(fontSize))
                fontSize = 10;
            if (m_LastUsedFont.Size != fontSize)
            {
                m_LastUsedFont = new Font(this.FontFamily, fontSize, this.FontStyle);
            }
            return m_LastUsedFont;
        }

        private Color GetPresudoRandomColorFromPalette(LayoutItem layoutItem)
        {
            return Palette[layoutItem.Word.Occurrences * layoutItem.Word.Text.Length % Palette.Length];
        }
    }
}
