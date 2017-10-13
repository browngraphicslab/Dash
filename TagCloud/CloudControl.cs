using System;
using System.Collections.Generic;
using System.Linq;
using Gma.CodeCloud.Controls.Geometry;
using Gma.CodeCloud.Controls.TextAnalyses.Processing;
using Windows.UI;
using Windows.UI.Xaml.Controls;
using Windows.Foundation;

namespace Gma.CodeCloud.Controls
{
    public class CloudControl : UserControl
    {
        private IEnumerable<IWord> m_Words;
        readonly Color[] m_DefaultPalette = new[] { Colors.DarkRed, Colors.DarkBlue, Colors.DarkGreen, Colors.Navy, Colors.DarkCyan, Colors.DarkOrange, Colors.DarkGoldenrod, Colors.DarkKhaki, Colors.Blue, Colors.Red, Colors.Green };
        private Color[] m_Palette;
        private LayoutType m_LayoutType;

        private int m_MaxFontSize;
        private int m_MinFontSize;
        private ILayout m_Layout;
        private Color m_BackColor;
        private LayoutItem m_ItemUderMouse;
        private int m_MinWordWeight;
        private int m_MaxWordWeight;

        public CloudControl()
        {
            m_MinWordWeight = 0;
            m_MaxWordWeight = 0;

            MaxFontSize = 68;
            MinFontSize = 6;
            
            m_Palette = m_DefaultPalette;
            m_BackColor = Colors.White;
            m_LayoutType = LayoutType.Spiral;
        }


        protected override void OnPaint(PaintEventArgs e)
        {
            if (m_Words == null) { return; }
            if (m_Layout == null) { return; }

            IEnumerable<LayoutItem> wordsToRedraw = m_Layout.GetWordsInArea(e.ClipRectangle);
            var graphicEngine =
                    new GdiGraphicEngine(Windows.UI.Xaml.Media.FontFamily.XamlAutoFontFamily, Windows.UI.Text.FontStyle.Normal, m_Palette, MinFontSize, MaxFontSize, m_MinWordWeight, m_MaxWordWeight))
            {
                foreach (LayoutItem currentItem in wordsToRedraw)
                {
                    if (m_ItemUderMouse == currentItem)
                    {
                        graphicEngine.DrawEmphasized(currentItem);
                    }
                    else
                    {
                        graphicEngine.Draw(currentItem);                        
                    }
                }
            }
        }

        private void BuildLayout()
        {
            if (m_Words == null) { return; }
            
            IGraphicEngine graphicEngine =
                new GdiGraphicEngine(graphics, this.Font.FontFamily, FontStyle.Regular, m_Palette, MinFontSize, MaxFontSize, m_MinWordWeight, m_MaxWordWeight);
            m_Layout = LayoutFactory.CrateLayout(m_LayoutType, new Size(Width,Height));
            m_Layout.Arrange(m_Words, graphicEngine);
        }

        public LayoutType LayoutType
        {
            get { return m_LayoutType; }
            set
            {
                if (value == m_LayoutType)
                {
                    return;
                }

                m_LayoutType = value;
                BuildLayout();
                Invalidate();
            }
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            LayoutItem nextItemUnderMouse;
            Point mousePositionRelativeToControl = this.PointToClient(new Point(MousePosition.X, MousePosition.Y));
            this.TryGetItemAtLocation(mousePositionRelativeToControl, out nextItemUnderMouse);
            if (nextItemUnderMouse != m_ItemUderMouse)
            {
                if (nextItemUnderMouse != null)
                {
                    Rectangle newRectangleToInvalidate = RectangleGrow(nextItemUnderMouse.Rectangle, 6);
                    this.Invalidate(newRectangleToInvalidate);
                }
                if (m_ItemUderMouse != null)
                {
                    Rectangle prevRectangleToInvalidate = RectangleGrow(m_ItemUderMouse.Rectangle, 6);
                    this.Invalidate(prevRectangleToInvalidate);
                }
                m_ItemUderMouse = nextItemUnderMouse;
            }
            base.OnMouseMove(e);
        }

        protected override void OnResize(EventArgs eventargs)
        {
            BuildLayout();
            base.OnResize(eventargs);
        }

        private static Rect RectangleGrow(Rect original, int growByPixels)
        {
            return new Rect(
                (int)(original.X - growByPixels),
                (int)(original.Y - growByPixels),
                (int)(original.Width + growByPixels + 1),
                (int)(original.Height + growByPixels + 1));
        }


        public Color BackColor
        {
            get
            {
                return m_BackColor;
            }
            set
            {
                if (m_BackColor == value)
                {
                    return;
                }
                m_BackColor = value;
                Invalidate();
            }
        }

        public int MaxFontSize
        {
            get { return m_MaxFontSize; }
            set
            {
                m_MaxFontSize = value;
                BuildLayout();
                Invalidate();
            }
        }

        public int MinFontSize
        {
            get { return m_MinFontSize; }
            set
            {
                m_MinFontSize = value;
                BuildLayout();
                Invalidate();
            }
        }

        public Color[] Palette
        {
            get { return m_Palette; }
            set
            {
                m_Palette = value;
                BuildLayout();
                Invalidate();
            }
        }

        public IEnumerable<IWord> WeightedWords
        {
            get { return m_Words; }
            set
            {
                m_Words = value;
                if (value==null) {return;}

                IWord first = m_Words.First();
                if (first!=null)
                {
                    m_MaxWordWeight = first.Occurrences;
                    m_MinWordWeight = m_Words.Last().Occurrences;
                }

                BuildLayout();
                Invalidate();
            }
        }

        public IEnumerable<LayoutItem> GetItemsInArea(Rect area)
        {
            if (m_Layout == null)
            {
                return new LayoutItem[] {};
            }

            return m_Layout.GetWordsInArea(area);
        }

        public bool TryGetItemAtLocation(Point location, out LayoutItem foundItem)
        {
            foundItem = null;
            IEnumerable<LayoutItem> itemsInArea = GetItemsInArea(new Rect(location, new Size(0, 0)));
            foreach (LayoutItem item in itemsInArea)
            {
                foundItem = item;
                return true;
            }
            return false;
        }
    }
}
