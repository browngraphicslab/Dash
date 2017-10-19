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
using System.Threading.Tasks;

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
        public delegate void TermDragStartingHandler(string term, DragStartingEventArgs args);
        public event TermDragStartingHandler TermDragStarting;

        private IEnumerable<IWord> m_Words;
        readonly Color[] m_DefaultPalette = new[] { Colors.DarkRed, Colors.DarkBlue, Colors.DarkGreen, Colors.Navy, Colors.DarkCyan, Colors.DarkOrange, Colors.DarkGoldenrod, Colors.DarkKhaki, Colors.Blue, Colors.Red, Colors.Green };
        private Color[] m_Palette;
        private LayoutType m_LayoutType = LayoutType.Typewriter;

        private int m_MaxFontSize = 100;
        private int m_MinFontSize = 9;
        private ILayout m_Layout;
        private Color m_BackColor = Colors.White;
        private int m_MinWordWeight = 1;
        private int m_MaxWordWeight = 100;
        private bool m_wordStemmer = true;
        private bool m_excludeEnglishCommonWords = true;
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
                        processText(TheText);
                    }
                }
            }
        }
        public WordCloud()
        {
            this.InitializeComponent();
            SizeChanged += WordCloud_SizeChanged;
        }

        async void processText(string text)
        {
            await Task.Run(async () => {
                var blacklist = ComponentFactory.CreateBlacklist(ExcludeEnglishCommonWords); 
                var customBlacklist = CommonBlacklist.CreateFromTextFile(""); //  s_BlacklistTxtFileName);

               // var inputType = ComponentFactory.DetectInputType(text);
                // var progress = ComponentFactory.CreateProgressBar(inputType, progressBar);
                var terms = new StringExtractor(text, new NullProgressIndicator()); //  ComponentFactory.CreateExtractor(inputType, text, new NullProgressIndicator());
                var stemmer = ComponentFactory.CreateWordStemmer(m_wordStemmer);  
                var words = terms.Filter(blacklist).Filter(customBlacklist).CountOccurences();
#pragma warning disable CS4014
                MainPage.Instance.Dispatcher.RunIdleAsync((args) =>
                    WeightedWords = words.GroupByStem(stemmer).SortByOccurences().Cast<IWord>());
#pragma warning disable CS4014
            });
        }
        void WordCloud_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            m_MaxFontSize = Math.Max(1, ((int)Math.Min(ActualWidth, ActualHeight)) / 8);
            buildLayout();
        }

        public void TriggerDragStarting(string text, DragStartingEventArgs e)
        {
            if (TermDragStarting != null)
                TermDragStarting(text, e);
        }

        void buildLayout()
        {
            if (m_Words != null)
            {
                xLayoutGrid.Children.Clear();
                var graphicEngine = new GdiGraphicEngine(this, FontFamily.XamlAutoFontFamily, Windows.UI.Text.FontStyle.Normal, m_DefaultPalette, m_MinFontSize, m_MaxFontSize, m_MinWordWeight, m_MaxWordWeight);
                m_Layout = LayoutFactory.CrateLayout(m_LayoutType, new Size(ActualWidth, ActualHeight));
                m_Layout.Arrange(m_Words, graphicEngine);

                var wordsToRedraw = m_Layout.GetWordsInArea(new Rect(0, 0, ActualWidth, ActualHeight));

                foreach (var currentItem in wordsToRedraw)
                {
                    graphicEngine.Draw(xLayoutGrid, currentItem);
                }
            }
        }

        public bool ExcludeEnglishCommonWords {
            get { return m_excludeEnglishCommonWords; }
            set { m_excludeEnglishCommonWords = value;
                processText(TheText);
            }
        }

        public bool UseWordStemmer
        {
            get { return m_wordStemmer; }
            set { m_wordStemmer = value;
                processText(TheText);
            }
        }
        

        public LayoutType LayoutType
        {
            get { return m_LayoutType; }
            set {
                m_LayoutType = value;
                buildLayout();
            }
        }

        public int MaxFontSize
        {
            get { return m_MaxFontSize; }
            set
            {
                m_MaxFontSize = value;
                buildLayout();
            }
        }

        public int MinFontSize
        {
            get { return m_MinFontSize; }
            set
            {
                m_MinFontSize = value;
                buildLayout();
            }
        }

        public Color[] Palette
        {
            get { return m_Palette; }
            set
            {
                m_Palette = value;
                buildLayout();
            }
        }

        public IEnumerable<IWord> WeightedWords
        {
            get { return m_Words; }
            set
            {
                m_Words = value;
                if (value == null) { return; }

                var first = m_Words?.First();
                if (first != null)
                {
                    m_MaxWordWeight = first.Occurrences;
                    m_MinWordWeight = m_Words.Last().Occurrences;
                }

                buildLayout();
            }
        }
    }
}
