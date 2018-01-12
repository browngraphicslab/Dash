using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI;
using Windows.UI.Text;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

// The User Control item template is documented at https://go.microsoft.com/fwlink/?LinkId=234236

namespace Dash
{
    public sealed partial class FormattingMenuView : UserControl
    {
        private RichTextView richTextView;
        private RichEditBox xRichEditBox;

        public FormattingMenuView()
        {
            this.InitializeComponent();
            richTextView = this.GetFirstAncestorOfType<RichTextView>();
            xRichEditBox = richTextView.xRichEditBox;
        }


        #region dictionaries for formatting menu

        /// <summary>
        /// Sets up all format dictionaries (for creating the flyout menu for format options)
        /// </summary>
        private void SetUpEnumDictionaries()
        {
            //SetUpDictionary<ParagraphAlignment>(typeof(ParagraphAlignment), out alignments);
           // SetUpDictionary<MarkerType>(typeof(MarkerType), out markerTypes);
            //SetUpDictionary<MarkerStyle>(typeof(MarkerStyle), out markerStyles);
            //SetUpDictionary<MarkerAlignment>(typeof(MarkerAlignment), out markerAlignments);
        }

        /// <summary>
        /// Sets up an enum dictionary where the name of the enum is the key, and the enum itself
        /// is the value
        /// </summary>
        /// <typeparam name="TValue"></typeparam>
        /// <param name="type"></param>
        /// <param name="dict"></param>
        //private void SetUpDictionary<TValue>(Type type, out IDictionary<string, TValue> dict)
        //{
        //    dict = new Dictionary<string, TValue>();
        //    var keys = Enum.GetNames(type);
        //    var vals = Enum.GetValues(type);
        //    var length = keys.Length;
        //    for (int i = 0; i < length; i++)
        //    {
        //        var key = keys[i];
        //        if (key != "Undefined")
        //            dict.Add(key, (TValue)vals.GetValue(i));
        //    }
        //}
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

        #region font family
        /// <summary>
        /// Add fonts to the format options flyout (under Fonts)
        /// </summary>
        private void AddFonts()
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
                var item = new MenuFlyoutItem();
                item.Text = font;
                item.Foreground = new SolidColorBrush(Colors.White);
                item.Click += delegate
                {
                    //currentCharFormat = xRichEditBox.Document.Selection.CharacterFormat.GetClone();
                    richTextView.UpdateDocument();
                };
                item.GotFocus += delegate
                {
                    xRichEditBox.Document.Selection.CharacterFormat.Name = font;
                };
                //xFont?.Items?.Add(item); Ellen commented out
            }
        }
        #endregion

        #region FormattingMenuEventHandlers
        private void ResetButton_Tapped(object sender, TappedRoutedEventArgs e)
        {
            xRichEditBox.Document.Selection.CharacterFormat.SetClone(defaultCharFormat);
            xRichEditBox.Document.Selection.ParagraphFormat.SetClone(defaultParFormat);
        }

        private void BoldButton_Tapped(object sender, TappedRoutedEventArgs e)
        {
            Bold(true);
        }

        private void ItalicsButton_Tapped(object sender, TappedRoutedEventArgs e)
        {
            Italicize(true);
        }

        private void UnderlineButton_Tapped(object sender, TappedRoutedEventArgs e)
        {
            Underline(true);
        }

        private void AllCapsButton_Tapped(object sender, TappedRoutedEventArgs e)
        {
            AllCaps(true);
        }

        private void SmallCapsButton_Tapped(object sender, TappedRoutedEventArgs e)
        {
            SmallCaps(true);
        }

        private void SuperscriptButton_Tapped(object sender, TappedRoutedEventArgs e)
        {
            Superscript(true);
        }

        private void SubscriptButton_Tapped(object sender, TappedRoutedEventArgs e)
        {
            Subscript(true);
        }

        private void StrikethroughButton_Tapped(object sender, TappedRoutedEventArgs e)
        {
            Strikethrough(true);
        }

        private void LeftAlignButton_Tapped(object sender, TappedRoutedEventArgs e)
        {
            Alignment(ParagraphAlignment.Left, true);
        }

        private void CenterAlignButton_Tapped(object sender, TappedRoutedEventArgs e)
        {
            Alignment(ParagraphAlignment.Center, true);
        }

        private void RightAlignButton_Tapped(object sender, TappedRoutedEventArgs e)
        {
            Alignment(ParagraphAlignment.Right, true);
        }

        private void BulletedListButton_Tapped(object sender, TappedRoutedEventArgs e)
        {
            if (xRichEditBox.Document.Selection.ParagraphFormat.ListType == MarkerType.Bullet)
            {
                Marker(MarkerType.None, true);
            }
            else
            {
                Marker(MarkerType.Bullet, true);
            }
        }

        private void NumberedListButton_Tapped(object sender, TappedRoutedEventArgs e)
        {
            if (xRichEditBox.Document.Selection.ParagraphFormat.ListType == MarkerType.UnicodeSequence)
            {
                Marker(MarkerType.None, true);
            }
            else
            {
                Marker(MarkerType.UnicodeSequence, true);
            }
        }

        private void FontColorBackground_Tapped(object sender, TappedRoutedEventArgs e)
        {

        }

        private void FontHighlightButton_Tapped_1(object sender, TappedRoutedEventArgs e)
        {

        }

        #endregion

        #region formatting helpers
        /// <summary>
        /// Makes current selection in the xRichEditBox bold
        /// Updates document if updateDocument is true
        /// </summary>
        /// <param name="updateDocument"></param>
        private void Bold(bool updateDocument)
        {
            // on/off instead of toggle to know exactly what state it is in (to determine whether a selection is bold or not)
            if (this.xRichEditBox.Document.Selection.CharacterFormat.Bold == FormatEffect.On)
            {
                this.xRichEditBox.Document.Selection.CharacterFormat.Bold = FormatEffect.Off;
            }
            else
            {
                this.xRichEditBox.Document.Selection.CharacterFormat.Bold = FormatEffect.On;
            }
            if (updateDocument) UpdateDocument();
        }

        /// <summary>
        /// Italicizes current selection in xRichEditBox
        /// Updates document if updateDocument is true
        /// </summary>
        /// <param name="updateDocument"></param>
        private void Italicize(bool updateDocument)
        {
            if (this.xRichEditBox.Document.Selection.CharacterFormat.Italic == FormatEffect.On)
            {
                this.xRichEditBox.Document.Selection.CharacterFormat.Italic = FormatEffect.Off;
            }
            else
            {
                this.xRichEditBox.Document.Selection.CharacterFormat.Italic = FormatEffect.On;
            }
            //this.xRichEditBox.Document.Selection.CharacterFormat.Italic = FormatEffect.Toggle;
            if (updateDocument) UpdateDocument();
        }

        /// <summary>
        /// Underlines the current selection in xRichEditBox
        /// Updates document if updateDocument is true
        /// </summary>
        /// <param name="updateDocument"></param>
        private void Underline(bool updateDocument)
        {
            if (this.xRichEditBox.Document.Selection.CharacterFormat.Underline == UnderlineType.None)
                this.xRichEditBox.Document.Selection.CharacterFormat.Underline = UnderlineType.Single;
            else
                this.xRichEditBox.Document.Selection.CharacterFormat.Underline = UnderlineType.None;
            if (updateDocument) UpdateDocument();
        }

        /// <summary>
        /// Strikethrough the current selection in xRichEditBox
        /// Updates document if updateDocument is true
        /// </summary>
        /// <param name="updateDocument"></param>
        private void Strikethrough(bool updateDocument)
        {
            if (xRichEditBox.Document.Selection.CharacterFormat.Strikethrough == FormatEffect.On)
                xRichEditBox.Document.Selection.CharacterFormat.Strikethrough = FormatEffect.Off;
            else
                xRichEditBox.Document.Selection.CharacterFormat.Strikethrough = FormatEffect.On;
            if (updateDocument) UpdateDocument();
        }

        /// <summary>
        /// Formats the current selection in xRichEditBox into superscripts
        /// Updates document if updateDocument is true
        /// </summary>
        /// <param name="updateDocument"></param>
        private void Superscript(bool updateDocument)
        {
            if (xRichEditBox.Document.Selection.CharacterFormat.Superscript == FormatEffect.On)
                xRichEditBox.Document.Selection.CharacterFormat.Superscript = FormatEffect.Off;
            else
                xRichEditBox.Document.Selection.CharacterFormat.Superscript = FormatEffect.On;
            if (updateDocument) UpdateDocument();
        }

        /// <summary>
        /// Formats the current selection in xRichEditBox into subscripts
        /// Updates document if updateDocument is true
        /// </summary>
        /// <param name="updateDocument"></param>
        private void Subscript(bool updateDocument)
        {
            if (xRichEditBox.Document.Selection.CharacterFormat.Subscript == FormatEffect.On)
                xRichEditBox.Document.Selection.CharacterFormat.Subscript = FormatEffect.Off;
            else
                xRichEditBox.Document.Selection.CharacterFormat.Subscript = FormatEffect.On;
            if (updateDocument) UpdateDocument();
        }

        /// <summary>
        /// Formats the current selection in xRichEditBox into smallcaps
        /// Updates document if updateDocument is true
        /// </summary>
        /// <param name="updateDocument"></param>
        private void SmallCaps(bool updateDocument)
        {
            if (xRichEditBox.Document.Selection.CharacterFormat.SmallCaps == FormatEffect.On)
                xRichEditBox.Document.Selection.CharacterFormat.SmallCaps = FormatEffect.Off;
            else
                xRichEditBox.Document.Selection.CharacterFormat.SmallCaps = FormatEffect.On;
            if (updateDocument) UpdateDocument();
        }

        /// <summary>
        /// Formats the current selection in xRichEditBox into allcaps
        /// Updates document if updateDocument is true
        /// </summary>
        /// <param name="updateDocument"></param>
        private void AllCaps(bool updateDocument)
        {
            if (xRichEditBox.Document.Selection.CharacterFormat.AllCaps == FormatEffect.On)
                xRichEditBox.Document.Selection.CharacterFormat.AllCaps = FormatEffect.Off;
            else
                xRichEditBox.Document.Selection.CharacterFormat.AllCaps = FormatEffect.On;
            if (updateDocument) UpdateDocument();
        }

        /// <summary>
        /// Highlights the current selection in xRichEditBox, the color of the highlight is specified by background
        /// Updates document if updateDocument is true
        /// </summary>
        /// <param name="background"></param>
        /// <param name="updateDocument"></param>
        private void Highlight(Color background, bool updateDocument)
        {
            xRichEditBox.Document.Selection.CharacterFormat.BackgroundColor = background;
            if (updateDocument) UpdateDocument();
        }

        /// <summary>
        /// Changes the color of the font of the current selection in xRichEditBox, the font color is specified by color
        /// Updates document if updateDocument is true
        /// </summary>
        /// <param name="color"></param>
        /// <param name="updateDocument"></param>
        private void Foreground(Color color, bool updateDocument)
        {
            xRichEditBox.Document.Selection.CharacterFormat.ForegroundColor = color;
            if (updateDocument) UpdateDocument();
        }

        /// <summary>
        /// Sets the paragraph alignment of the current selection to be what's specified by alignment
        /// Updates document if updateDocument is true
        /// </summary>
        /// <param name="alignment"></param>
        /// <param name="updateDocument"></param>
        private void Alignment(object alignment, bool updateDocument)
        {
            if (alignment != null && alignment.GetType() == typeof(ParagraphAlignment))
            {
                xRichEditBox.Document.Selection.ParagraphFormat.Alignment = (ParagraphAlignment)alignment;
                if (updateDocument) UpdateDocument();
            }
        }

        /// <summary>
        /// Sets the list marker of the current selection to be what's specified by type
        /// Updates document if updateDocument is true
        /// </summary>
        /// <param name="type"></param>
        /// <param name="updateDocument"></param>
        private void Marker(object type, bool updateDocument)
        {
            if (type != null && type.GetType() == typeof(MarkerType))
            {
                xRichEditBox.Document.Selection.ParagraphFormat.ListType = (MarkerType)type;
                if (updateDocument) UpdateDocument();
            }
        }

        /// <summary>
        /// Sets the list marker style of the current selection to be what's specified by type
        /// Updates document if updateDocument is true
        /// </summary>
        /// <param name="type"></param>
        /// <param name="updateDocument"></param>
        private void MarkerStyle(object type, bool updateDocument)
        {
            if (type != null && type.GetType() == typeof(MarkerStyle))
            {
                xRichEditBox.Document.Selection.ParagraphFormat.ListStyle = (MarkerStyle)type;
                if (updateDocument) UpdateDocument();
            }
        }

        /// <summary>
        /// Sets the marker alignment of the current selection to be what's specified by type
        /// Updates document if updateDocument is true
        /// </summary>
        /// <param name="type"></param>
        /// <param name="updateDocument"></param>
        private void MarkerAlignment(object type, bool updateDocument)
        {
            if (type != null && type.GetType() == typeof(MarkerAlignment))
            {
                xRichEditBox.Document.Selection.ParagraphFormat.ListAlignment = (MarkerAlignment)type;
                if (updateDocument) UpdateDocument();
            }
        }

        #endregion


    }
}
