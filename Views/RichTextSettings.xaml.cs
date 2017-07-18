using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Reflection.Metadata;
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
using Dash.Views;
using DashShared;

// The User Control item template is documented at http://go.microsoft.com/fwlink/?LinkId=234236

namespace Dash
{
    public sealed partial class RichTextSettings : UserControl
    {
        ObservableCollection<FontFamily> fonts = new ObservableCollection<FontFamily>();
        ObservableCollection<NamedColor> colors { get; set; }

        public RichTextSettings()
        {
            this.InitializeComponent();
        }

        public RichTextSettings(DocumentController docController, Context context) : this()
        {
            Debug.Assert(docController.DocumentType == CourtesyDocuments.RichTextBox.DocumentType,
                "you can only create rich text settings for a rich text box");
            var docContextList = context.DocContextList;
            xSizeRow.Children.Add(new SizeSettings(docController,docContextList));
            xPositionRow.Children.Add(new PositionSettings(docController,docContextList));
            this.AddFonts();
            this.AddColors();
            this.AddHandlers(docController, context);
        }

        private void AddHandlers(DocumentController docController, Context context)
        {
            xFontSizeSlider.ValueChanged += delegate
            {
                this.FontSizeChanged(docController, context);
            };
            xFontWeightSlider.ValueChanged += delegate
            {
                this.FontWeightChanged(docController, context);
            };
            xFontComboBox.SelectionChanged += delegate
            {
                this.FontSelectionChanged(docController, context);
            };
            xBoldButton.Tapped += delegate
            {
                this.BoldTapped(docController, context);
            };
            xItalicButton.Tapped += delegate
            {
                this.ItalicTapped(docController, context);
            };
            xUnderlineButton.Tapped += delegate
            {
                this.UnderlineTapped(docController, context);
            };
            xSuperScriptButton.Tapped += delegate
            {
                this.SuperScriptTapped(docController, context);
            };
            xSubScriptButton.Tapped += delegate
            {
                this.SubScriptTapped(docController, context);
            };
            xAllCapsButton.Tapped += delegate
            {
                this.AllCapsTapped(docController, context);
            };
            xAlignLeftButton.Tapped += delegate
            {
                this.AlignLeftTapped(docController, context);
            };
            xAlignCenterButton.Tapped += delegate
            {
                this.AlignCenterTapped(docController, context);
            };
            xAlignRightButton.Tapped += delegate
            {
                this.AlignRightTapped(docController, context);
            };
            xFontColorComboBox.SelectionChanged += delegate
            {
                this.ColorSelectionChanged(docController, context);
            };
            xHighlightColorComboBox.SelectionChanged += delegate
            {
                this.HighlightSelectionChanged(docController, context);
            };
        }

        private void HighlightSelectionChanged(DocumentController docController, Context context)
        {
            var richTextController = docController.GetDereferencedField(DashConstants.KeyStore.DataKey, context) as RichTextFieldModelController;
            Debug.Assert(richTextController != null);
            ITextSelection selectedText = richTextController.SelectedText;
            if (selectedText != null)
            {
                ITextCharacterFormat charFormatting = selectedText.CharacterFormat;
                charFormatting.BackgroundColor = (xFontComboBox.SelectedItem as NamedColor).Color;
                richTextController.SelectedText.CharacterFormat = charFormatting;
            }
        }

        private void ColorSelectionChanged(DocumentController docController, Context context)
        {
            var richTextController = docController.GetDereferencedField(DashConstants.KeyStore.DataKey, context) as RichTextFieldModelController;
            Debug.Assert(richTextController != null);
            ITextSelection selectedText = richTextController.SelectedText;
            if (selectedText != null)
            {
                ITextCharacterFormat charFormatting = selectedText.CharacterFormat;
                charFormatting.ForegroundColor = (xFontComboBox.SelectedItem as NamedColor).Color;
                richTextController.SelectedText.CharacterFormat = charFormatting;
            }
        }

        private void AlignRightTapped(DocumentController docController, Context context)
        {
            var richTextController = docController.GetDereferencedField(DashConstants.KeyStore.DataKey, context) as RichTextFieldModelController;
            Debug.Assert(richTextController != null);
            ITextSelection selectedText = richTextController.SelectedText;
            if (selectedText != null)
            {
                ITextParagraphFormat paragraphFormatting = selectedText.ParagraphFormat;
                paragraphFormatting.Alignment = ParagraphAlignment.Right;
                richTextController.SelectedText.ParagraphFormat = paragraphFormatting;
            }
        }

        private void AlignCenterTapped(DocumentController docController, Context context)
        {
            var richTextController = docController.GetDereferencedField(DashConstants.KeyStore.DataKey, context) as RichTextFieldModelController;
            Debug.Assert(richTextController != null);
            ITextSelection selectedText = richTextController.SelectedText;
            if (selectedText != null)
            {
                ITextParagraphFormat paragraphFormatting = selectedText.ParagraphFormat;
                paragraphFormatting.Alignment = ParagraphAlignment.Center;
                richTextController.SelectedText.ParagraphFormat = paragraphFormatting;
            }
        }

        private void AlignLeftTapped(DocumentController docController, Context context)
        {
            var richTextController = docController.GetDereferencedField(DashConstants.KeyStore.DataKey, context) as RichTextFieldModelController;
            Debug.Assert(richTextController != null);
            ITextSelection selectedText = richTextController.SelectedText;
            if (selectedText != null)
            {
                ITextParagraphFormat paragraphFormatting = selectedText.ParagraphFormat;
                paragraphFormatting.Alignment = ParagraphAlignment.Left;
                richTextController.SelectedText.ParagraphFormat = paragraphFormatting;
            }
        }

        private void AllCapsTapped(DocumentController docController, Context context)
        {
            var richTextController = docController.GetDereferencedField(DashConstants.KeyStore.DataKey, context) as RichTextFieldModelController;
            Debug.Assert(richTextController != null);
            ITextSelection selectedText = richTextController.SelectedText;
            if (selectedText != null)
            {
                ITextCharacterFormat charFormatting = selectedText.CharacterFormat;
                charFormatting.AllCaps = FormatEffect.Toggle;
                richTextController.SelectedText.CharacterFormat = charFormatting;
            }
        }

        private void SubScriptTapped(DocumentController docController, Context context)
        {
            var richTextController = docController.GetDereferencedField(DashConstants.KeyStore.DataKey, context) as RichTextFieldModelController;
            Debug.Assert(richTextController != null);
            ITextSelection selectedText = richTextController.SelectedText;
            if (selectedText != null)
            {
                ITextCharacterFormat charFormatting = selectedText.CharacterFormat;
                charFormatting.Subscript = FormatEffect.Toggle;
                richTextController.SelectedText.CharacterFormat = charFormatting;
            }
        }

        private void SuperScriptTapped(DocumentController docController, Context context)
        {
            var richTextController = docController.GetDereferencedField(DashConstants.KeyStore.DataKey, context) as RichTextFieldModelController;
            Debug.Assert(richTextController != null);
            ITextSelection selectedText = richTextController.SelectedText;
            if (selectedText != null)
            {
                ITextCharacterFormat charFormatting = selectedText.CharacterFormat;
                charFormatting.Superscript = FormatEffect.Toggle;
                richTextController.SelectedText.CharacterFormat = charFormatting;
            }
        }

        private void UnderlineTapped(DocumentController docController, Context context)
        {
            var richTextController = docController.GetDereferencedField(DashConstants.KeyStore.DataKey, context) as RichTextFieldModelController;
            Debug.Assert(richTextController != null);
            ITextSelection selectedText = richTextController.SelectedText;
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
                richTextController.SelectedText.CharacterFormat = charFormatting;
            }
        }

        private void ItalicTapped(DocumentController docController, Context context)
        {
            var richTextController = docController.GetDereferencedField(DashConstants.KeyStore.DataKey, context) as RichTextFieldModelController;
            Debug.Assert(richTextController != null);
            ITextSelection selectedText = richTextController.SelectedText;
            if (selectedText != null)
            {
                ITextCharacterFormat charFormatting = selectedText.CharacterFormat;
                charFormatting.Italic = FormatEffect.Toggle;
                richTextController.SelectedText.CharacterFormat = charFormatting;
            }
        }

        private void BoldTapped(DocumentController docController, Context context)
        {
            var richTextController = docController.GetDereferencedField(DashConstants.KeyStore.DataKey, context) as RichTextFieldModelController;
            Debug.Assert(richTextController != null);
            ITextSelection selectedText = richTextController.SelectedText;
            if (selectedText != null)
            {
                ITextCharacterFormat charFormatting = selectedText.CharacterFormat;
                charFormatting.Bold = FormatEffect.Toggle;
                richTextController.SelectedText.CharacterFormat = charFormatting;
            }
        }

        private void FontSelectionChanged(DocumentController docController, Context context)
        {
            var richTextController = docController.GetDereferencedField(DashConstants.KeyStore.DataKey, context) as RichTextFieldModelController;
            Debug.Assert(richTextController != null);
            ITextSelection selectedText = richTextController.SelectedText;
            if (selectedText != null)
            {
                ITextCharacterFormat charFormatting = selectedText.CharacterFormat;
                charFormatting.Name = (xFontComboBox.SelectedItem as FontFamily).Source;
                richTextController.SelectedText.CharacterFormat = charFormatting;
            }
        }

        private void FontWeightChanged(DocumentController docController, Context context)
        {
            var richTextController = docController.GetDereferencedField(DashConstants.KeyStore.DataKey, context) as RichTextFieldModelController;
            Debug.Assert(richTextController != null);
            ITextSelection selectedText = richTextController.SelectedText;
            if (selectedText != null)
            {
                ITextCharacterFormat charFormatting = selectedText.CharacterFormat;
                charFormatting.Weight = (int) xFontWeightSlider.Value;
                richTextController.SelectedText.CharacterFormat = charFormatting;
            }
        }

        private void FontSizeChanged(DocumentController docController, Context context)
        {
            var richTextController = docController.GetDereferencedField(DashConstants.KeyStore.DataKey, context) as RichTextFieldModelController;
            Debug.Assert(richTextController != null);
            ITextSelection selectedText = richTextController.SelectedText;
            if (selectedText != null)
            {
                ITextCharacterFormat charFormatting = selectedText.CharacterFormat;
                charFormatting.Size = (int)xFontWeightSlider.Value;
                richTextController.SelectedText.CharacterFormat = charFormatting;
            }
        }

        private void AddColors()
        {
            foreach (var color in typeof(Colors).GetRuntimeProperties())
            {
                colors.Add(new NamedColor() {Name = color.Name, Color = (Color) color.GetValue(null)});
            }
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
    }
    public class NamedColor
    {
        public string Name { get; set; }
        public Color Color { get; set; }
    }
}
