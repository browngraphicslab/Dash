using Gma.CodeCloud.Controls;
using Gma.CodeCloud.Controls.Geometry;
using Gma.CodeCloud.Controls.TextAnalyses.Blacklist;
using Gma.CodeCloud.Controls.TextAnalyses.Extractors;
using Gma.CodeCloud.Controls.TextAnalyses.Processing;
using Gma.CodeCloud.Controls.TextAnalyses.Stemmers;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using TagCloud;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

// The User Control item template is documented at https://go.microsoft.com/fwlink/?LinkId=234236

namespace NewControls
{
    public class NullProgressIndicator : IProgressIndicator
    {
        int _max;
        int IProgressIndicator.Maximum { get => _max; set => _max = value; }

        void IProgressIndicator.Increment(int value)
        {
        }
    }
    public sealed partial class WordCloud : UserControl
    {
        private IEnumerable<IWord> m_Words;
        readonly Color[] m_DefaultPalette = new[] { Colors.DarkRed, Colors.DarkBlue, Colors.DarkGreen, Colors.Navy, Colors.DarkCyan, Colors.DarkOrange, Colors.DarkGoldenrod, Colors.DarkKhaki, Colors.Blue, Colors.Red, Colors.Green };
        private Color[] m_Palette;
        private LayoutType m_LayoutType = LayoutType.Spiral;

        private int m_MaxFontSize = 68;
        private int m_MinFontSize = 6;
        private ILayout m_Layout;
        private Color m_BackColor = Colors.White;
        private LayoutItem m_ItemUderMouse;
        private int m_MinWordWeight = 1;
        private int m_MaxWordWeight = 100;
        public static WordCloud Instance;
        public WordCloud()
        {
            this.InitializeComponent();
            SizeChanged += WordCloud_SizeChanged;
            graphicEngine = new GdiGraphicEngine(FontFamily.XamlAutoFontFamily, Windows.UI.Text.FontStyle.Normal, m_DefaultPalette, m_MinFontSize, m_MaxFontSize, m_MinWordWeight, m_MaxWordWeight);
            Instance = this;
        }

        private void ProcessText(string text)
        {
            IBlacklist blacklist = ComponentFactory.CreateBlacklist(false); //  checkBoxExcludeEnglishCommonWords.Checked);
            IBlacklist customBlacklist = CommonBlacklist.CreateFromTextFile(""); //  s_BlacklistTxtFileName);

            var inputType = ComponentFactory.DetectInputType(text);
            // IProgressIndicator progress = ComponentFactory.CreateProgressBar(inputType, progressBar);
            IEnumerable<string> terms = ComponentFactory.CreateExtractor(inputType, text, new NullProgressIndicator());
            IWordStemmer stemmer = ComponentFactory.CreateWordStemmer(false); //  checkBoxGroupSameStemWords.Checked);

            IEnumerable<IWord> words = terms
                .Filter(blacklist)
                .Filter(customBlacklist)
                .CountOccurences();

            WordCloud.Instance.WeightedWords =
                words
                    .GroupByStem(stemmer)
                    .SortByOccurences()
                    .Cast<IWord>();
        }
        private void WordCloud_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            graphicEngine = new GdiGraphicEngine(FontFamily.XamlAutoFontFamily, Windows.UI.Text.FontStyle.Normal, m_DefaultPalette, m_MinFontSize, m_MaxFontSize, m_MinWordWeight, m_MaxWordWeight);

            var text = "hello bye hello more than hello and bye and there are hello more bye what wonderful stuff";
            ProcessText(text);
            BuildLayout();
            Redraw();
        }
        GdiGraphicEngine graphicEngine;
        protected void Redraw()
        {
            if (m_Words == null) { return; }
            if (m_Layout == null) { return; }

           var wordsToRedraw = m_Layout.GetWordsInArea(new Rect(0, 0, ActualWidth, ActualHeight));

            foreach (var currentItem in wordsToRedraw)
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

        private void BuildLayout()
        {
            if (m_Words == null) { return; }
            
            m_Layout = LayoutFactory.CrateLayout(m_LayoutType, new Size(ActualWidth, ActualHeight));
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
            }
        }

        //protected override void OnMouseMove(MouseEventArgs e)
        //{
        //    LayoutItem nextItemUnderMouse;
        //    Point mousePositionRelativeToControl = this.PointToClient(new Point(MousePosition.X, MousePosition.Y));
        //    this.TryGetItemAtLocation(mousePositionRelativeToControl, out nextItemUnderMouse);
        //    if (nextItemUnderMouse != m_ItemUderMouse)
        //    {
        //        if (nextItemUnderMouse != null)
        //        {
        //            Rectangle newRectangleToInvalidate = RectangleGrow(nextItemUnderMouse.Rectangle, 6);
        //            this.Invalidate(newRectangleToInvalidate);
        //        }
        //        if (m_ItemUderMouse != null)
        //        {
        //            Rectangle prevRectangleToInvalidate = RectangleGrow(m_ItemUderMouse.Rectangle, 6);
        //            this.Invalidate(prevRectangleToInvalidate);
        //        }
        //        m_ItemUderMouse = nextItemUnderMouse;
        //    }
        //    base.OnMouseMove(e);
        //}

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
                if (m_BackColor != value)
                    m_BackColor = value;
            }
        }

        public int MaxFontSize
        {
            get { return m_MaxFontSize; }
            set
            {
                m_MaxFontSize = value;
                BuildLayout();
                Redraw();
            }
        }

        public int MinFontSize
        {
            get { return m_MinFontSize; }
            set
            {
                m_MinFontSize = value;
                BuildLayout();
                Redraw();
            }
        }

        public Color[] Palette
        {
            get { return m_Palette; }
            set
            {
                m_Palette = value;
                BuildLayout();
                Redraw();
            }
        }

        public IEnumerable<IWord> WeightedWords
        {
            get { return m_Words; }
            set
            {
                m_Words = value;
                if (value == null) { return; }

                IWord first = m_Words.First();
                if (first != null)
                {
                    m_MaxWordWeight = first.Occurrences;
                    m_MinWordWeight = m_Words.Last().Occurrences;
                }

                BuildLayout();
                Redraw();
            }
        }

        public IEnumerable<LayoutItem> GetItemsInArea(Rect area)
        {
            if (m_Layout == null)
            {
                return new LayoutItem[] { };
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
