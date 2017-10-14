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
using Dash;

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

        private int m_MaxFontSize = 100;
        private int m_MinFontSize = 6;
        private ILayout m_Layout;
        private Color m_BackColor = Colors.White;
        private int m_MinWordWeight = 1;
        private int m_MaxWordWeight = 100;
        string _theText = "";
        public string TheText
        {
            get { return _theText; }
            set {
                if (_theText != value)
                {
                    _theText = value;
                     if (this.IsInVisualTree())
                    {
                        m_MaxFontSize = Math.Max(1, ((int)Math.Min(ActualWidth, ActualHeight)) / 8);
                        ProcessText(TheText);
                    }
                }
            }
        }
        public WordCloud()
        {
            this.InitializeComponent();
            SizeChanged += WordCloud_SizeChanged;
        }

        private void ProcessText(string text)
        {
            var blacklist       = ComponentFactory.CreateBlacklist(true); //  checkBoxExcludeEnglishCommonWords.Checked);
            var customBlacklist = CommonBlacklist.CreateFromTextFile(""); //  s_BlacklistTxtFileName);

            var inputType = ComponentFactory.DetectInputType(text);
            // var progress = ComponentFactory.CreateProgressBar(inputType, progressBar);
            var terms   = ComponentFactory.CreateExtractor(inputType, text, new NullProgressIndicator());
            var stemmer = ComponentFactory.CreateWordStemmer(true); //  checkBoxGroupSameStemWords.Checked);

            var words   = terms .Filter(blacklist)  .Filter(customBlacklist)  .CountOccurences();

            WeightedWords =  words .GroupByStem(stemmer) .SortByOccurences() .Cast<IWord>();
        }
        private void WordCloud_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            m_MaxFontSize = Math.Max(1, ((int)Math.Min(ActualWidth, ActualHeight)) / 8);
            BuildLayout();
        }

        private void BuildLayout()
        {
            if (m_Words != null)
            {
                xLayoutGrid.Children.Clear();
                var graphicEngine = new GdiGraphicEngine(FontFamily.XamlAutoFontFamily, Windows.UI.Text.FontStyle.Normal, m_DefaultPalette, m_MinFontSize, m_MaxFontSize, m_MinWordWeight, m_MaxWordWeight);
                m_Layout = LayoutFactory.CrateLayout(m_LayoutType, new Size(ActualWidth, ActualHeight));
                m_Layout.Arrange(m_Words, graphicEngine);

                var wordsToRedraw = m_Layout.GetWordsInArea(new Rect(0, 0, ActualWidth, ActualHeight));

                foreach (var currentItem in wordsToRedraw)
                {
                    graphicEngine.Draw(xLayoutGrid, currentItem);
                }
            }
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

        public int MaxFontSize
        {
            get { return m_MaxFontSize; }
            set
            {
                m_MaxFontSize = value;
                BuildLayout();
            }
        }

        public int MinFontSize
        {
            get { return m_MinFontSize; }
            set
            {
                m_MinFontSize = value;
                BuildLayout();
            }
        }

        public Color[] Palette
        {
            get { return m_Palette; }
            set
            {
                m_Palette = value;
                BuildLayout();
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
            }
        }
    }
}
