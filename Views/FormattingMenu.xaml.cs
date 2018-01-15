using Syncfusion.UI.Xaml.Controls.Media;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Windows.UI;
using Windows.UI.Text;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;

// The User Control item template is documented at https://go.microsoft.com/fwlink/?LinkId=234236

namespace Dash
{
    public sealed partial class FormattingMenuView : UserControl
    {
        #region instance variables

        public RichTextView richTextView { get; set; }
        public RichEditBox xRichEditBox { get; set; }

        /// <summary>
        /// Default rich text paragraph format (text alignment, list, spacing... ect.)
        /// </summary>
        public ITextParagraphFormat defaultParFormat;

        /// <summary>
        /// Default rich text character format (bold, italics, underlin, script, caps.. ect.)
        /// </summary>
        public ITextCharacterFormat defaultCharFormat;

        /// <summary>
        /// Instance of a class made for word count and font size binding
        /// </summary>
        public WordCount WC;

        ObservableCollection<FontFamily> FontFamilyNames = new ObservableCollection<FontFamily>();

        private RichTextFormattingHelper _rtfHelper;

        #endregion

        /// <summary>
        /// Constructor
        /// </summary>
        public FormattingMenuView()
        {
            this.InitializeComponent();

            Loaded += FormattingMenuView_Loaded;
            //var x = new SfColorPicker();
        }


        private void FormattingMenuView_Loaded(object sender, RoutedEventArgs e)
        {  
            WC = new WordCount(xRichEditBox);
            _rtfHelper = new RichTextFormattingHelper(richTextView, xRichEditBox);

            SetUpFontFamilyComboBox();
            SetUpFontSizeComboBox();
        }


        #region set up ComboBoxes
        
        /// <summary>
        /// Add fonts to the format options flyout (under Fonts)
        /// </summary>
        private void SetUpFontFamilyComboBox()
        {
            var FontNames = new List<string>()
            {
                "Arial",
                "Calibri",
                "Cambria",
                "Cambria Math",
                "Comic Sans MS",
                "Courier New",
                "Ebrima",
                "Gadugi",
                "Georgia",
                "Javanese Text Regular Fallback font for Javanese script",
                "Leelawadee UI",
                "Lucida Console",
                "Malgun Gothic",
                "Microsoft Himalaya",
                "Microsoft JhengHei",
                "Microsoft JhengHei UI",
                "Microsoft New Tai Lue",
                "Microsoft PhagsPa",
                "Microsoft Tai Le",
                "Microsoft YaHei",
                "Microsoft YaHei UI",
                "Microsoft Yi Baiti",
                "Mongolian Baiti",
                "MV Boli",
                "Myanmar Text",
                "Nirmala UI",
                "Segoe MDL2 Assets",
                "Segoe Print",
                "Segoe UI",
                "Segoe UI Emoji",
                "Segoe UI Historic",
                "Segoe UI Symbol",
                "SimSun",
                "Times New Roman",
                "Trebuchet MS",
                "Verdana",
                "Webdings",
                "Wingdings",
                "Yu Gothic",
                "Yu Gothic UI"
            };

            foreach (var font in FontNames)
            {
                FontFamilyNames.Add(new FontFamily(font));

            }

            //set index as calibri
            //xFontFamilyComboBox.SelectedItem = null;
        }


        ObservableCollection<double> FontSizes = new ObservableCollection<double>();

        /// <summary>
        /// Add fonts to the format options flyout (under Fonts)
        /// </summary>
        private void SetUpFontSizeComboBox()
        {
            var sizes = new List<double>()
            {
                8,
                9,
                9.5,
                10,
                10.5,
                11,
                11.5,
                12,
                14,
                16,
                18,
                20,
                22,
                24,
                26,
                28,
                36,
                48,
                72,
                100,
                150
            };

            foreach (var num in sizes)
            {
                xFontSizeComboBox.Items.Add(num);
            }

            //set index as calibri
            //xFontFamilyComboBox.SelectedItem = null;
        }

        #endregion


        #region font size
        /// <summary>
        /// Add delegates to manage font size in the lower left hand corner of the richtextbox (next to the word count)
        /// </summary>
        //private void AddFontSizeHandlers()
        //{

        //}

        ///// <summary>
        ///// Binds the font size of the current selection to the text property of the xFontSizeTextBox
        ///// </summary>
        //private void SetFontSizeBinding()
        //{
        //    var fontSizeBinding = new Binding()
        //    {
        //        Source = WC,
        //        Path = new PropertyPath(nameof(WC.Size)),
        //        Mode = BindingMode.TwoWay,
        //        UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged
        //    };
        //}

        #endregion


        #region FormattingMenuEventHandlers

        #region Buttons
        private void ResetButton_Tapped(object sender, TappedRoutedEventArgs e)
        {
            xRichEditBox.Document.Selection.CharacterFormat.SetClone(defaultCharFormat);
            xRichEditBox.Document.Selection.ParagraphFormat.SetClone(defaultParFormat);
        }

        private void BoldButton_Tapped(object sender, TappedRoutedEventArgs e)
        {
            _rtfHelper.Bold(true);
        }

        private void ItalicsButton_Tapped(object sender, TappedRoutedEventArgs e)
        {
            _rtfHelper.Italicize(true);
        }

        private void UnderlineButton_Tapped(object sender, TappedRoutedEventArgs e)
        {
            _rtfHelper.Underline(true);
        }

        private void AllCapsButton_Tapped(object sender, TappedRoutedEventArgs e)
        {
            _rtfHelper.AllCaps(true);
        }

        private void SmallCapsButton_Tapped(object sender, TappedRoutedEventArgs e)
        {
            _rtfHelper.SmallCaps(true);
        }

        private void SuperscriptButton_Tapped(object sender, TappedRoutedEventArgs e)
        {
            _rtfHelper.Superscript(true);
        }

        private void SubscriptButton_Tapped(object sender, TappedRoutedEventArgs e)
        {
            _rtfHelper.Subscript(true);
        }

        private void StrikethroughButton_Tapped(object sender, TappedRoutedEventArgs e)
        {
            _rtfHelper.Strikethrough(true);
        }

        private void LeftAlignButton_Tapped(object sender, TappedRoutedEventArgs e)
        {
            _rtfHelper.Alignment(ParagraphAlignment.Left, true);
        }

        private void CenterAlignButton_Tapped(object sender, TappedRoutedEventArgs e)
        {
            _rtfHelper.Alignment(ParagraphAlignment.Center, true);
        }

        private void RightAlignButton_Tapped(object sender, TappedRoutedEventArgs e)
        {
            _rtfHelper.Alignment(ParagraphAlignment.Right, true);
        }

        private void BulletedListButton_Tapped(object sender, TappedRoutedEventArgs e)
        {
            if (xRichEditBox.Document.Selection.ParagraphFormat.ListType == MarkerType.Bullet)
            {
                _rtfHelper.Marker(MarkerType.None, true);
            }
            else
            {
                _rtfHelper.Marker(MarkerType.Bullet, true);
            }
        }

        private void NumberedListButton_Tapped(object sender, TappedRoutedEventArgs e)
        {
            if (xRichEditBox.Document.Selection.ParagraphFormat.ListType == MarkerType.UnicodeSequence)
            {
                _rtfHelper.Marker(MarkerType.None, true);
            }
            else
            {
                _rtfHelper.Marker(MarkerType.UnicodeSequence, true);
            }
        }
        
        #endregion

        #region ComboBox
        private void FontFamilyComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var comboBox = sender as ComboBox;
            var selectedFontFamily = comboBox.SelectedValue as FontFamily;
            xRichEditBox.Document.Selection.CharacterFormat.Name = selectedFontFamily.Source;
        }

        private void FontSizeComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var comboBox = sender as ComboBox;
            var selectedFontSize = comboBox.SelectedValue;
            xRichEditBox.Document.Selection.CharacterFormat.Size = (float) Convert.ToDouble(selectedFontSize.ToString());
        }


        #endregion

        #endregion

        private void xForegroundColorPicker_SelectedColorChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            var colorPicker = sender as SfColorPicker;
            if(colorPicker != null)
            {
                var color = colorPicker.SelectedColor;
                _rtfHelper.Foreground(color, true);
            }
        }

        private void xBackgroundColorPicker_SelectedColorChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            var colorPicker = sender as SfColorPicker;
            if (colorPicker != null)
            {
                var color = colorPicker.SelectedColor;
                _rtfHelper.Highlight(color, true);
            }
        }
    }

    public class RichTextFormattingHelper
    {
        private RichEditBox _richEditBox;
        private RichTextView richTextView;

        public RichTextFormattingHelper(RichTextView inRichTextView, RichEditBox richEditBox)
        {
            _richEditBox = richEditBox;
            richTextView = inRichTextView;
        }

        /// <summary>
        /// Makes current selection in the xRichEditBox bold
        /// Updates document if updateDocument is true
        /// </summary>
        /// <param name="updateDocument"></param>
        public void Bold(bool updateDocument)
        {
            // on/off instead of toggle to know exactly what state it is in (to determine whether a selection is bold or not)
            if (this._richEditBox.Document.Selection.CharacterFormat.Bold == FormatEffect.On)
            {
                this._richEditBox.Document.Selection.CharacterFormat.Bold = FormatEffect.Off;
            }
            else
            {
                this._richEditBox.Document.Selection.CharacterFormat.Bold = FormatEffect.On;
            }
            if (updateDocument) richTextView.UpdateDocument();
        }

        /// <summary>
        /// Italicizes current selection in xRichEditBox
        /// Updates document if updateDocument is true
        /// </summary>
        /// <param name="updateDocument"></param>
        public void Italicize(bool updateDocument)
        {
            if (this._richEditBox.Document.Selection.CharacterFormat.Italic == FormatEffect.On)
            {
                this._richEditBox.Document.Selection.CharacterFormat.Italic = FormatEffect.Off;
            }
            else
            {
                this._richEditBox.Document.Selection.CharacterFormat.Italic = FormatEffect.On;
            }
            //this.xRichEditBox.Document.Selection.CharacterFormat.Italic = FormatEffect.Toggle;
            if (updateDocument) richTextView.UpdateDocument();
        }

        /// <summary>
        /// Underlines the current selection in xRichEditBox
        /// Updates document if updateDocument is true
        /// </summary>
        /// <param name="updateDocument"></param>
        public void Underline(bool updateDocument)
        {
            if (this._richEditBox.Document.Selection.CharacterFormat.Underline == UnderlineType.None)
                this._richEditBox.Document.Selection.CharacterFormat.Underline = UnderlineType.Single;
            else
                this._richEditBox.Document.Selection.CharacterFormat.Underline = UnderlineType.None;
            if (updateDocument) richTextView.UpdateDocument();
        }

        /// <summary>
        /// Strikethrough the current selection in xRichEditBox
        /// Updates document if updateDocument is true
        /// </summary>
        /// <param name="updateDocument"></param>
        public void Strikethrough(bool updateDocument)
        {
            if (_richEditBox.Document.Selection.CharacterFormat.Strikethrough == FormatEffect.On)
                _richEditBox.Document.Selection.CharacterFormat.Strikethrough = FormatEffect.Off;
            else
                _richEditBox.Document.Selection.CharacterFormat.Strikethrough = FormatEffect.On;
            if (updateDocument) richTextView.UpdateDocument();
        }

        /// <summary>
        /// Formats the current selection in xRichEditBox into superscripts
        /// Updates document if updateDocument is true
        /// </summary>
        /// <param name="updateDocument"></param>
        public void Superscript(bool updateDocument)
        {
            if (_richEditBox.Document.Selection.CharacterFormat.Superscript == FormatEffect.On)
                _richEditBox.Document.Selection.CharacterFormat.Superscript = FormatEffect.Off;
            else
                _richEditBox.Document.Selection.CharacterFormat.Superscript = FormatEffect.On;
            if (updateDocument) richTextView.UpdateDocument();
        }

        /// <summary>
        /// Formats the current selection in xRichEditBox into subscripts
        /// Updates document if updateDocument is true
        /// </summary>
        /// <param name="updateDocument"></param>
        public void Subscript(bool updateDocument)
        {
            if (_richEditBox.Document.Selection.CharacterFormat.Subscript == FormatEffect.On)
                _richEditBox.Document.Selection.CharacterFormat.Subscript = FormatEffect.Off;
            else
                _richEditBox.Document.Selection.CharacterFormat.Subscript = FormatEffect.On;
            if (updateDocument) richTextView.UpdateDocument();
        }

        /// <summary>
        /// Formats the current selection in xRichEditBox into smallcaps
        /// Updates document if updateDocument is true
        /// </summary>
        /// <param name="updateDocument"></param>
        public void SmallCaps(bool updateDocument)
        {
            if (_richEditBox.Document.Selection.CharacterFormat.SmallCaps == FormatEffect.On)
                _richEditBox.Document.Selection.CharacterFormat.SmallCaps = FormatEffect.Off;
            else
                _richEditBox.Document.Selection.CharacterFormat.SmallCaps = FormatEffect.On;
            if (updateDocument) richTextView.UpdateDocument();
        }

        /// <summary>
        /// Formats the current selection in xRichEditBox into allcaps
        /// Updates document if updateDocument is true
        /// </summary>
        /// <param name="updateDocument"></param>
        public void AllCaps(bool updateDocument)
        {
            if (_richEditBox.Document.Selection.CharacterFormat.AllCaps == FormatEffect.On)
                _richEditBox.Document.Selection.CharacterFormat.AllCaps = FormatEffect.Off;
            else
                _richEditBox.Document.Selection.CharacterFormat.AllCaps = FormatEffect.On;
            if (updateDocument) richTextView.UpdateDocument();
        }

        /// <summary>
        /// Highlights the current selection in xRichEditBox, the color of the highlight is specified by background
        /// Updates document if updateDocument is true
        /// </summary>
        /// <param name="background"></param>
        /// <param name="updateDocument"></param>
        public void Highlight(Color background, bool updateDocument)
        {
            _richEditBox.Document.Selection.CharacterFormat.BackgroundColor = background;
            if (updateDocument) richTextView.UpdateDocument();
        }

        /// <summary>
        /// Changes the color of the font of the current selection in xRichEditBox, the font color is specified by color
        /// Updates document if updateDocument is true
        /// </summary>
        /// <param name="color"></param>
        /// <param name="updateDocument"></param>
        public void Foreground(Color color, bool updateDocument)
        {
            _richEditBox.Document.Selection.CharacterFormat.ForegroundColor = color;
            if (updateDocument) richTextView.UpdateDocument();
        }

        /// <summary>
        /// Sets the paragraph alignment of the current selection to be what's specified by alignment
        /// Updates document if updateDocument is true
        /// </summary>
        /// <param name="alignment"></param>
        /// <param name="updateDocument"></param>
        public void Alignment(object alignment, bool updateDocument)
        {
            if (alignment != null && alignment.GetType() == typeof(ParagraphAlignment))
            {
                _richEditBox.Document.Selection.ParagraphFormat.Alignment = (ParagraphAlignment)alignment;
                if (updateDocument) richTextView.UpdateDocument();
            }
        }

        /// <summary>
        /// Sets the list marker of the current selection to be what's specified by type
        /// Updates document if updateDocument is true
        /// </summary>
        /// <param name="type"></param>
        /// <param name="updateDocument"></param>
        public void Marker(object type, bool updateDocument)
        {
            if (type != null && type.GetType() == typeof(MarkerType))
            {
                _richEditBox.Document.Selection.ParagraphFormat.ListType = (MarkerType)type;
                if (updateDocument) richTextView.UpdateDocument();
            }
        }


    }
}
