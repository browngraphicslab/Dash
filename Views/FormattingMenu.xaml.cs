﻿using Syncfusion.UI.Xaml.Controls.Media;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
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

        #endregion

        /// <summary>
        /// Constructor
        /// </summary>
        public FormattingMenuView()
        {
            this.InitializeComponent();

            Loaded += FormattingMenuView_Loaded;
        }


        private void FormattingMenuView_Loaded(object sender, RoutedEventArgs e)
        {  
            WC = new WordCount(xRichEditBox);

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

            var currentFontStyle = xRichEditBox.Document.Selection.CharacterFormat.Name;
            xFontFamilyComboBox.SelectedIndex = FontNames.IndexOf(currentFontStyle);
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

            foreach (var num in sizes)
            {
                xFontSizeComboBox.Items.Add(num);
            }

            var currentFontSize = xRichEditBox.Document.Selection.CharacterFormat.Size;
            xFontSizeComboBox.SelectedIndex = sizes.IndexOf(currentFontSize);
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

        private void SuperscriptButton_Tapped(object sender, TappedRoutedEventArgs e)
        {
            richTextView.Superscript(true);
        }

        private void SubscriptButton_Tapped(object sender, TappedRoutedEventArgs e)
        {
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
            var comboBox = sender as ComboBox;
            var selectedFontFamily = comboBox.SelectedValue as FontFamily;
            xRichEditBox.Document.Selection.CharacterFormat.Name = selectedFontFamily.Source;
        }

        private void FontSizeComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var comboBox = sender as ComboBox;
            var selectedFontSize = comboBox?.SelectedValue;
            if (selectedFontSize != null)
                xRichEditBox.Document.Selection.CharacterFormat.Size = (float) Convert.ToDouble(selectedFontSize.ToString());
        }

        #endregion

        private void xForegroundColorPicker_SelectedColorChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            var colorPicker = sender as SfColorPicker;
            if(colorPicker != null)
            {
                var color = colorPicker.SelectedColor;
                richTextView.Foreground(color, true);
            }
        }

        private void xBackgroundColorPicker_SelectedColorChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            var colorPicker = sender as SfColorPicker;
            if (colorPicker != null)
            {
                var color = colorPicker.SelectedColor;
                richTextView.Highlight(color, true);
            }
        }

        #endregion

    }


}
