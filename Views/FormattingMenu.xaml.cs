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
    /// <summary>
    /// This class is displayed in the flyout of RichTextViews and contains options for formatting rich text
    /// </summary>
    public sealed partial class FormattingMenuView : UserControl
    {
        #region instance variables

        private RichTextView richTextView { get; set; }
        private RichEditBox xRichEditBox { get; set; }

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

        public ObservableCollection<TextBlock> FontFamilyNames { get; } = new ObservableCollection<TextBlock>();

        private bool _fontSizeChanged = false;
        private bool _fontSizeTextChanged = false;
        private bool _fontFamilyChanged = false;

        #endregion

        /// <summary>
        /// Constructor
        /// </summary>
        public FormattingMenuView()
        {
            this.InitializeComponent();
            Loaded += FormattingMenuView_Loaded;
            SetUpFontFamilyComboBox();
            SetUpFontSizeComboBox();
            Loading += (sender, args) =>
            {
                UpdateFontFamilyDisplay();
                UpdateFontSizeDisplay();
            };
        }


        public void FormattingMenuView_Loaded(object sender, RoutedEventArgs e)
        {
            WC = new WordCount(xRichEditBox);
            //xBackgroundColorPicker.ParentFlyout = xBackgroundColorFlyout;
            //xForegroundColorPicker.ParentFlyout = xForegroundColorFlyout;
        }

        public void SetRichTextBinding(RichTextView view)
        {
            richTextView = view;
            xRichEditBox = view.xRichEditBox;
            UpdateFontFamilyDisplay();
            UpdateFontSizeDisplay();
        }

        private List<double> _sizes;
        private List<string> _fontNames;

        #region set up ComboBoxes

        /// <summary>
        /// Add fonts to the format options flyout (under Fonts)
        /// </summary>
        private void SetUpFontFamilyComboBox()
        {
            //people like lots of fancy and pretty fonts
            _fontNames = new List<string>()
            {
                "Arial",
                "Bahnschrift",
                "Bauhaus 93",
                "Bodoni MT",
                "Broadway",
                "Brush Script MT",
                "Calibri",
                "Cambria",
                "Castellar",
                "Century Gothic",
                "Comic Sans MS",
                "Courier New",
                "Elephant",
                "French Script MT",
                "Futura",
                "Garamond",
                "Georgia",
                "Impact",
                "Ink Free",
                "Lucida Console",
                "Monotype Corsiva",
                "MV Boli",
                "Old English Text MT",
                "Papyrus",
                "Rockwell",
                "Segoe Print",
                "Segoe UI",
                "SimSun",
                "Stencil",
                "Times New Roman",
                "Trebuchet MS",
                "Verdana",
                "Yu Gothic UI",
                "Webdings"
            };

            foreach (var font in _fontNames)
            {
                var newBlock = new TextBlock
                {
                    Text = font,
                    FontFamily = new FontFamily(font)
                };

                FontFamilyNames.Add(newBlock);
            }
        }

        private void UpdateFontFamilyDisplay()
        {

            if (xFontFamilyComboBox.Items.Count > 0)
            {
                var currentFontStyle = xRichEditBox.Document.Selection.CharacterFormat.Name;
                var index = _fontNames.IndexOf(currentFontStyle);
                if (xFontFamilyComboBox.SelectedIndex != index)
                {
                    _fontFamilyChanged = true;
                    xFontFamilyComboBox.SelectedIndex = index;
                }
            }
        }


        ObservableCollection<double> FontSizes = new ObservableCollection<double>();

        /// <summary>
        /// Add fonts to the format options flyout (under Fonts)
        /// </summary>
        private void SetUpFontSizeComboBox()
        {
            _sizes = new List<double>()
            {
                8,
                9,
                9.5,
                10,
                10.5,
                11,
                11.25,
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

            foreach (var num in _sizes)
            {
                xFontSizeComboBox.Items.Add(num);
            }

        }

        private void UpdateFontSizeDisplay()
        {
            var currentFontSize = xRichEditBox.Document.Selection.CharacterFormat.Size;
            var index = _sizes.IndexOf(currentFontSize);
            if (index != xFontSizeComboBox.SelectedIndex)
            {
                _fontSizeChanged = true;
                _fontSizeTextChanged = true;
                xFontSizeComboBox.SelectedIndex = index;
                xFontSizeTextBox.Text = currentFontSize.ToString();
            }
        }

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
            richTextView.Bold(true);
        }

        private void ItalicsButton_Tapped(object sender, TappedRoutedEventArgs e)
        {
            richTextView.Italicize(true);
        }

        private void UnderlineButton_Tapped(object sender, TappedRoutedEventArgs e)
        {
            richTextView.Underline(true);
        }

        private void AllCapsButton_Tapped(object sender, TappedRoutedEventArgs e)
        {
            richTextView.AllCaps(true);
        }

        private void SmallCapsButton_Tapped(object sender, TappedRoutedEventArgs e)
        {
            richTextView.SmallCaps(true);
        }

        public void SuperscriptButton_Tapped(object sender, TappedRoutedEventArgs e)
        {
            using (UndoManager.GetBatchHandle())
                richTextView.Superscript(true);
        }

        public void SubscriptButton_Tapped(object sender, TappedRoutedEventArgs e)
        {
            using (UndoManager.GetBatchHandle())
                richTextView.Subscript(true);
        }

        private void StrikethroughButton_Tapped(object sender, TappedRoutedEventArgs e)
        {
            richTextView.Strikethrough(true);
        }

        private void LeftAlignButton_Tapped(object sender, TappedRoutedEventArgs e)
        {
            richTextView.Alignment(ParagraphAlignment.Left, true);
        }

        private void CenterAlignButton_Tapped(object sender, TappedRoutedEventArgs e)
        {
            richTextView.Alignment(ParagraphAlignment.Center, true);
        }

        private void RightAlignButton_Tapped(object sender, TappedRoutedEventArgs e)
        {
            richTextView.Alignment(ParagraphAlignment.Right, true);
        }

        private void BulletedListButton_Tapped(object sender, TappedRoutedEventArgs e)
        {
            if (xRichEditBox.Document.Selection.ParagraphFormat.ListType == MarkerType.Bullet)
            {
                richTextView.Marker(MarkerType.None, true);
            }
            else
            {
                richTextView.Marker(MarkerType.Bullet, true);
            }
        }

        private void NumberedListButton_Tapped(object sender, TappedRoutedEventArgs e)
        {
            if (xRichEditBox.Document.Selection.ParagraphFormat.ListType == MarkerType.UnicodeSequence)
            {
                richTextView.Marker(MarkerType.None, true);
            }
            else
            {
                richTextView.Marker(MarkerType.UnicodeSequence, true);
            }
        }

        #endregion

        #region ComboBox
        private void FontFamilyComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!_fontFamilyChanged)
            {
                var comboBox = sender as ComboBox;
                var selectedFontFamily = (comboBox.SelectedValue as TextBlock).FontFamily;

                using (UndoManager.GetBatchHandle())
                {
                    //select all if nothing is selected
                    if (xRichEditBox.Document.Selection == null || xRichEditBox.Document.Selection.StartPosition ==
                        xRichEditBox.Document.Selection.EndPosition)
                    {
                        xRichEditBox.Document.GetText(TextGetOptions.UseObjectText, out var text);
                        var end = text.Length;
                        xRichEditBox.Document.Selection.SetRange(0, end);
                        xRichEditBox.Document.Selection.CharacterFormat.Name = selectedFontFamily.Source;
                        xRichEditBox.Document.Selection.SetRange(end, end);
                    }
                    else
                    {
                        xRichEditBox.Document.Selection.CharacterFormat.Name = selectedFontFamily.Source;
                    }

                    richTextView.UpdateDocumentFromXaml();
                }
            }
            else
            {
                _fontFamilyChanged = false;
            }
        }

        private void FontSizeComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var comboBox = sender as ComboBox;
            var selectedFontSize = comboBox?.SelectedValue;
            if (selectedFontSize == null)
            {
                return;
            }
            _fontSizeTextChanged = true;
            xFontSizeTextBox.Text = selectedFontSize.ToString();
            if (!_fontSizeChanged)
            {
                if (selectedFontSize != null)
                {
                    //select all if nothing is selected
                    using (UndoManager.GetBatchHandle())
                    {
                        if (xRichEditBox.Document.Selection == null || xRichEditBox.Document.Selection.StartPosition ==
                            xRichEditBox.Document.Selection.EndPosition)
                        {
                            xRichEditBox.Document.GetText(TextGetOptions.UseObjectText, out var text);
                            var end = text.Length;
                            xRichEditBox.Document.Selection.SetRange(0, end);
                            xRichEditBox.Document.Selection.CharacterFormat.Size =
                                (float)Convert.ToDouble(selectedFontSize.ToString());
                            xRichEditBox.Document.Selection.SetRange(end, end);
                        }
                        else
                        {
                            xRichEditBox.Document.Selection.CharacterFormat.Size =
                                (float)Convert.ToDouble(selectedFontSize.ToString());
                        }

                        richTextView.UpdateDocumentFromXaml();
                    }
                }
            }
            else
            {
                _fontSizeChanged = false;
            }
        }


        private void XFontSizeTextBox_OnTextChanged(object sender, TextChangedEventArgs e)
        {
            if (!_fontSizeTextChanged)
            {
                var selectedFontSize = xFontSizeTextBox.Text;

                if (!double.TryParse(selectedFontSize, out double fontSize))
                {
                    return;
                }
                if (fontSize > 1600)
                {
                    return;
                }
                using (UndoManager.GetBatchHandle())
                {
                    if (xRichEditBox.Document.Selection == null || xRichEditBox.Document.Selection.StartPosition ==
                        xRichEditBox.Document.Selection.EndPosition)
                    {
                        xRichEditBox.Document.GetText(TextGetOptions.UseObjectText, out var text);
                        var end = text.Length;
                        xRichEditBox.Document.Selection.SetRange(0, end);
                        xRichEditBox.Document.Selection.CharacterFormat.Size = (float)fontSize;
                        xRichEditBox.Document.Selection.SetRange(end, end);
                    }
                    else
                    {
                        xRichEditBox.Document.Selection.CharacterFormat.Size = (float)fontSize;
                    }

                    richTextView.UpdateDocumentFromXaml();
                }
            }
            else
            {
                _fontSizeTextChanged = false;
            }
        }
        #endregion

        public void xForegroundColorPicker_SelectedColorChanged(object sender, Color e)
        {
            if (sender is DashColorPicker colorPicker)
            {
                var color = colorPicker.SelectedColor;
                richTextView.Foreground(color, true);
            }
        }

        public void xHighlightColorPicker_SelectedColorChanged(object sender, Color e)
        {
            if (sender is DashColorPicker colorPicker)
            {
                var color = colorPicker.SelectedColor;
                richTextView.Highlight(color, true);
            }
        }

        #endregion

        /*
	    public void UpdateDropDowns()
	    {
			//set font size and font combo boxes
		    double size = xRichEditBox.Document.Selection.CharacterFormat.Size;
		    string font = xRichEditBox.Document.Selection.CharacterFormat.Name;

		    if (_sizes != null && _sizes.Contains(size)) xFontSizeComboBox.SelectedIndex = _sizes.IndexOf(size);
		    if (_fontNames != null && _fontNames.Contains(font)) xFontFamilyComboBox.SelectedIndex = _fontNames.IndexOf(font);
			//xFontSizeComboBox.SelectedValue = xRichEditBox.Document.Selection.CharacterFormat.Size;
		   // xFontFamilyComboBox.SelectedValue = xRichEditBox.Document.Selection.CharacterFormat.Name;

	    }
		*/


    }


}
