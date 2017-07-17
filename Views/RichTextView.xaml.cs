using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Text;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

// The User Control item template is documented at http://go.microsoft.com/fwlink/?LinkId=234236

namespace Dash
{
    public sealed partial class RichTextView : UserControl
    {
        private RichTextFieldModelController _richTextFieldModelController;
        ObservableCollection<FontFamily> fonts = new ObservableCollection<FontFamily>();

        public RichTextView(RichTextFieldModelController richTextFieldModelController)
        {
            this.InitializeComponent();
            _richTextFieldModelController = richTextFieldModelController;
            Loaded += OnLoaded;
            xRichEitBox.LostFocus += XRichEitBox_LostFocus;
            xRichEitBox.GotFocus += XRichEitBox_GotFocus;
            _richTextFieldModelController.FieldModelUpdated += RichTextFieldModelControllerOnFieldModelUpdated;
            this.AddFonts();
        }

        private void XRichEitBox_GotFocus(object sender, RoutedEventArgs e)
        {
            xFormatRow.Height = new GridLength(30);
        }

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
                fonts.Add(new FontFamily(font));
            }
        }
        private void XRichEitBox_LostFocus(object sender, RoutedEventArgs e)
        {
            var richText = string.Empty;
            xRichEitBox.Document.GetText(TextGetOptions.FormatRtf, out richText);
            _richTextFieldModelController.RichTextData = richText;
            xFormatRow.Height = new GridLength(0);
        }

        private void OnLoaded(object sender, RoutedEventArgs routedEventArgs)
        {
            if (_richTextFieldModelController.RichTextData != null)
            {
                xRichEitBox.Document.SetText(TextSetOptions.FormatRtf, _richTextFieldModelController.RichTextData);
            }
        }

        private void RichTextFieldModelControllerOnFieldModelUpdated(FieldModelController sender)
        {
            var text = string.Empty;
            xRichEitBox.Document.GetText(TextGetOptions.FormatRtf, out text);
            if (_richTextFieldModelController.RichTextData != null && !text.Equals(_richTextFieldModelController.RichTextData))
            {
                xRichEitBox.Document.SetText(TextSetOptions.FormatRtf, _richTextFieldModelController.RichTextData);
            }
        }

        // freezes the app
        //private void XRichEitBoxOnTextChanged(object sender, RoutedEventArgs routedEventArgs)
        //{
        //    var richText = string.Empty;
        //    xRichEitBox.Document.GetText(TextGetOptions.FormatRtf, out richText);
        //    _richTextFieldModelController.RichTextData = richText;
        //}

        private void BoldButton_Tapped(object sender, TappedRoutedEventArgs e)
        {
            // TODO: place break point in textchanged to see if format changes trigger textchange event
            ITextSelection selectedText = xRichEitBox.Document.Selection;
            if (selectedText != null)
            {
                ITextCharacterFormat charFormatting = selectedText.CharacterFormat;
                charFormatting.Bold = FormatEffect.Toggle;
                selectedText.CharacterFormat = charFormatting;
            }
        }

        private void ItalicButton_Tapped(object sender, TappedRoutedEventArgs e)
        {
            ITextSelection selectedText = xRichEitBox.Document.Selection;
            if (selectedText != null)
            {
                ITextCharacterFormat charFormatting = selectedText.CharacterFormat;
                charFormatting.Italic = FormatEffect.Toggle;
                selectedText.CharacterFormat = charFormatting;
            }
        }

        private void UnderlineButton_Tapped(object sender, TappedRoutedEventArgs e)
        {
            ITextSelection selectedText = xRichEitBox.Document.Selection;
            if (selectedText != null)
            {
                ITextCharacterFormat charFormatting = selectedText.CharacterFormat;
                if (charFormatting.Underline == UnderlineType.None)
                {
                    charFormatting.Underline = UnderlineType.Single;
                }
                else
                {
                    charFormatting.Underline = UnderlineType.None;
                }
                selectedText.CharacterFormat = charFormatting;
            }
        }

        private void Selector_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ITextSelection selectedText = xRichEitBox.Document.Selection;
            if (selectedText != null)
            {
                ITextCharacterFormat charFormatting = selectedText.CharacterFormat;
                charFormatting.Name = (xFontSizeComboBox.SelectedItem as FontFamily).Source;
                selectedText.CharacterFormat = charFormatting;
            }
        }
    }
}
