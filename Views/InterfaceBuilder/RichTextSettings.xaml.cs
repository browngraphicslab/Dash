using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Reflection;
using Windows.UI;
using Windows.UI.Text;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Animation;
using Visibility = Windows.UI.Xaml.Visibility;

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
            Debug.Assert(docController.DocumentType.Equals(RichTextBox.DocumentType), "you can only create rich text settings for a rich text box");
            xSizeRow.Children.Add(new SizeSettings(docController, context));
            xPositionRow.Children.Add(new PositionSettings(docController, context));
            this.AddFonts();
            this.AddColors();
            this.AddHandlers(docController, context);
        }

        private void AddHandlers(DocumentController docController, Context context)
        {
            xFontSizeTextBox.TextChanged += delegate
            {
                this.FontSizeChanged(docController, context);
            };
            xFontWeightTextBox.TextChanged += delegate
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
            var richTextController = docController.GetDereferencedField(KeyStore.DataKey, context) as RichTextController;
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
            var richTextController = docController.GetDereferencedField(KeyStore.DataKey, context) as RichTextController;
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
            var richTextController = docController.GetDereferencedField(KeyStore.DataKey, context) as RichTextController;
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
            var richTextController = docController.GetDereferencedField(KeyStore.DataKey, context) as RichTextController;
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
            var richTextController = docController.GetDereferencedField(KeyStore.DataKey, context) as RichTextController;
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
            var richTextController = docController.GetDereferencedField(KeyStore.DataKey, context) as RichTextController;
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
            var richTextController = docController.GetDereferencedField(KeyStore.DataKey, context) as RichTextController;
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
            var richTextController = docController.GetDereferencedField(KeyStore.DataKey, context) as RichTextController;
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
            var richTextController = docController.GetDereferencedField(KeyStore.DataKey, context) as RichTextController;
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
            var richTextController = docController.GetDereferencedField(KeyStore.DataKey, context) as RichTextController;
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
            var richTextController = docController.GetDereferencedField(KeyStore.DataKey, context) as RichTextController;
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
            var richTextController = docController.GetDereferencedField(KeyStore.DataKey, context) as RichTextController;
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
            var richTextController = docController.GetDereferencedField(KeyStore.DataKey, context) as RichTextController;
            Debug.Assert(richTextController != null);
            ITextSelection selectedText = richTextController.SelectedText;
            if (selectedText != null)
            {
                ITextCharacterFormat charFormatting = selectedText.CharacterFormat;
                var currentValue = int.Parse(xFontWeightTextBox.Text);
                charFormatting.Size = currentValue;
                richTextController.SelectedText.CharacterFormat = charFormatting;
            }
        }

        private void FontSizeChanged(DocumentController docController, Context context)
        {
            var richTextController = docController.GetDereferencedField(KeyStore.DataKey, context) as RichTextController;
            Debug.Assert(richTextController != null);
            ITextSelection selectedText = richTextController.SelectedText;
            if (selectedText != null)
            {
                ITextCharacterFormat charFormatting = selectedText.CharacterFormat;
                var currentValue = int.Parse(xFontSizeTextBox.Text);
                charFormatting.Size = currentValue;
                richTextController.SelectedText.CharacterFormat = charFormatting;
            }
        }

        private void AddColors()
        {
            if (colors == null)  colors = new ObservableCollection<NamedColor>();
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
        #region ValueSlider

        private void XMovementDetectionGrid_OnTapped(object sender, TappedRoutedEventArgs e)
        {
            if (sender == xFontWeightMovementDetectionGrid)
            {
                xFontWeightTextBox.Focus(FocusState.Programmatic);
                xFontWeightMovementDetectionGrid.IsHitTestVisible = false;
                xFontWeightMovementDetectionGrid.Visibility = Visibility.Collapsed;

            } else if (sender == xFontSizeMovementDetectionGrid)
            {
                xFontSizeTextBox.Focus(FocusState.Programmatic);
                xFontSizeMovementDetectionGrid.IsHitTestVisible = false;
                xFontSizeMovementDetectionGrid.Visibility = Visibility.Collapsed;
            }
        }

        private void XMovementDetectionGrid_OnPointerPressed(object sender, PointerRoutedEventArgs e)
        {
            var grid = sender as Panel;
            var children = grid?.Children;
            if (children != null)
            {
                foreach (var child in children)
                {
                    this.CreateAndRunOpacityAnimation(child, 0, 0.5);
                    if ((string)(child as Border)?.Tag == "Deduct")
                    {
                        this.CreateAndRunRepositionAnimation(child, 100);
                    }
                    else if ((string)(child as Border)?.Tag == "Increment")
                    {
                        this.CreateAndRunRepositionAnimation(child, -100);
                    }
                }
            }
            e.Handled = true;
        }


        private void XMovementDetectionGrid_OnPointerReleased(object sender, PointerRoutedEventArgs e)
        {
            this.HideDeductAndIncrement(sender);
            e.Handled = true;
        }

        private void XMovementDetectionGrid_OnPointerCanceled(object sender, PointerRoutedEventArgs e)
        {
            // event not firing?
            this.HideDeductAndIncrement(sender);
            e.Handled = true;
        }

        private void xTextBox_OnLostFocus(object sender, RoutedEventArgs e)
        {
            if (sender == xFontWeightTextBox)
            {
                xFontWeightMovementDetectionGrid.IsHitTestVisible = true;
                xFontWeightMovementDetectionGrid.Visibility = Visibility.Visible;
            } else if (sender == xFontSizeTextBox)
            {
                xFontSizeMovementDetectionGrid.IsHitTestVisible = true;
                xFontWeightMovementDetectionGrid.Visibility = Visibility.Visible;
            }
        }

        private void CreateAndRunOpacityAnimation(UIElement target, double from, double to)
        {
            Duration duration = new Duration(TimeSpan.FromSeconds(0.3));

            DoubleAnimation opacityAnimation = new DoubleAnimation()
            {
                SpeedRatio = 2,
                From = from,
                To = to,
                Duration = duration,
                EnableDependentAnimation = true
            };
            Storyboard opacityStoryboard = new Storyboard()
            {
                Duration = duration
            };
            opacityStoryboard.Children.Add(opacityAnimation);
            Storyboard.SetTarget(opacityAnimation, target);
            Storyboard.SetTargetProperty(opacityAnimation, "Opacity");
            opacityStoryboard.Begin();
        }

        private void CreateAndRunRepositionAnimation(UIElement target, double horizontalOffset)
        {
            Duration duration = new Duration(TimeSpan.FromSeconds(0.5));

            RepositionThemeAnimation repositionAnimation = new RepositionThemeAnimation()
            {
                SpeedRatio = 1.3,
                FromHorizontalOffset = horizontalOffset,
                Duration = duration
            };
            Storyboard repositionStoryboard = new Storyboard()
            {
                Duration = duration
            };
            repositionStoryboard.Children.Add(repositionAnimation);
            Storyboard.SetTarget(repositionAnimation, target);
            repositionStoryboard.Begin();
        }

        private void XMovementDetectionGrid_OnManipulationDelta(object sender, ManipulationDeltaRoutedEventArgs e)
        {
            var deltaX = e.Delta.Translation.X;
            if (deltaX > 0)
            {
                if (sender == xFontWeightMovementDetectionGrid)
                {
                    (xDeduct.Child as TextBlock).FontSize = 20;
                    this.CreateAndRunOpacityAnimation(xDeduct, xDeduct.Opacity, 0.5);
                    (xIncrement.Child as TextBlock).FontSize = 26;
                    this.CreateAndRunOpacityAnimation(xIncrement, xIncrement.Opacity, 1);
                    double currentValue = 0;
                    if (!xFontWeightTextBox.Text.Equals(string.Empty))
                    {
                        currentValue = double.Parse(xFontWeightTextBox.Text);
                    }
                        xFontWeightTextBox.SetValue(TextBox.TextProperty,
                            (currentValue + 10).ToString());
                } else if (sender == xFontSizeMovementDetectionGrid)
                {
                    (xSizeDeduct.Child as TextBlock).FontSize = 20;
                    this.CreateAndRunOpacityAnimation(xSizeDeduct, xSizeDeduct.Opacity, 0.5);
                    (xSizeIncrement.Child as TextBlock).FontSize = 26;
                    this.CreateAndRunOpacityAnimation(xSizeIncrement, xSizeIncrement.Opacity, 1);
                    double currentValue = 0;
                    if (!xFontSizeTextBox.Text.Equals(string.Empty))
                    {
                        currentValue = double.Parse(xFontSizeTextBox.Text);
                    }
                        xFontSizeTextBox.SetValue(TextBox.TextProperty, (currentValue + 1).ToString());
                    
                }
            }
            if (deltaX < 0)
            {
                if (sender == xFontWeightMovementDetectionGrid)
                {
                    (xIncrement.Child as TextBlock).FontSize = 20;
                    this.CreateAndRunOpacityAnimation(xIncrement, xIncrement.Opacity, 0.5);
                    (xDeduct.Child as TextBlock).FontSize = 26;
                    this.CreateAndRunOpacityAnimation(xDeduct, xDeduct.Opacity, 1);
                    double currentValue = 0;
                    if (!xFontWeightTextBox.Text.Equals(string.Empty))
                    {
                        currentValue = double.Parse(xFontWeightTextBox.Text);
                    }
                    if (currentValue > 0)
                    {
                        xFontWeightTextBox.SetValue(TextBox.TextProperty,
                            (currentValue - 10).ToString());
                    }
                } else if (sender == xFontSizeMovementDetectionGrid)
                {
                    (xSizeIncrement.Child as TextBlock).FontSize = 20;
                    this.CreateAndRunOpacityAnimation(xSizeIncrement, xSizeIncrement.Opacity, 0.5);
                    (xSizeDeduct.Child as TextBlock).FontSize = 26;
                    this.CreateAndRunOpacityAnimation(xSizeDeduct, xSizeDeduct.Opacity, 1);
                    double currentValue = 0;
                    if (!xFontSizeTextBox.Text.Equals(string.Empty))
                    {
                        currentValue = double.Parse(xFontSizeTextBox.Text);
                    }
                    if (currentValue > 4)
                    {
                        xFontSizeTextBox.SetValue(TextBox.TextProperty,
                            (currentValue - 1).ToString());
                    }
                }
            }
            e.Handled = true;
        }

        private void XMovementDetectionGrid_OnManipulationCompleted(object sender, ManipulationCompletedRoutedEventArgs e)
        {
            this.HideDeductAndIncrement(sender);
            e.Handled = true;
        }

        private void HideDeductAndIncrement(object sender)
        {
            var grid = sender as Panel;
            var children = grid?.Children;
            if (children != null)
            {
                foreach (var child in children)
                {
                    this.CreateAndRunOpacityAnimation(child, child.Opacity, 0);
                }
            }
            (xIncrement.Child as TextBlock).FontSize = 20;
            (xDeduct.Child as TextBlock).FontSize = 20;
            (xSizeIncrement.Child as TextBlock).FontSize = 20;
            (xSizeDeduct.Child as TextBlock).FontSize = 20;
        }

        #endregion
    }
    public class NamedColor
    {
        public string Name { get; set; }
        public Color Color { get; set; }
    }
}
