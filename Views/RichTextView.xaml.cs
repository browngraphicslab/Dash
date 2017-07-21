using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Storage;
using Windows.UI;
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
        private int _selectionStart;
        private int _selectionEnd;

        private ITextSelection _selectedText
        {
            set { _richTextFieldModelController.SelectedText = value; }
        }

        public RichTextView(RichTextFieldModelController richTextFieldModelController)
        {
            this.InitializeComponent();
            _richTextFieldModelController = richTextFieldModelController;
            Loaded += OnLoaded;
            xRichEitBox.SelectionChanged += XRichEitBox_SelectionChanged;
            xRichEitBox.LostFocus += XRichEitBox_LostFocus;
            xRichEitBox.GotFocus += XRichEitBoxOnGotFocus;
            xRichEitBox.TextChanged += XRichEitBoxOnTextChanged;
            _richTextFieldModelController.FieldModelUpdated += RichTextFieldModelControllerOnFieldModelUpdated;
        }

        private void XRichEitBoxOnGotFocus(object sender, RoutedEventArgs routedEventArgs)
        {
            ITextSelection selectedText = xRichEitBox.Document.Selection;
            if (selectedText != null)
            {
                xRichEitBox.Document.Selection.SetRange(_selectionStart, _selectionEnd);
                selectedText.CharacterFormat.BackgroundColor = Colors.White;
            }
        }

        private void XRichEitBox_SelectionChanged(object sender, RoutedEventArgs e)
        {
            _selectedText = xRichEitBox.Document.Selection;
        }


        private void XRichEitBox_LostFocus(object sender, RoutedEventArgs e)
        {
            var richText = string.Empty;
            xRichEitBox.Document.GetText(TextGetOptions.FormatRtf, out richText);
            _richTextFieldModelController.RichTextData = richText;
            xFormatRow.Height = new GridLength(0);

            xRichEitBox.ManipulationMode = ManipulationModes.All;

            _selectionEnd = xRichEitBox.Document.Selection.EndPosition;
            _selectionStart = xRichEitBox.Document.Selection.StartPosition;

            ITextSelection selectedText = xRichEitBox.Document.Selection;
            if (selectedText != null)
            {
                selectedText.CharacterFormat.BackgroundColor = Colors.LightGray;
            }
        }

        private async Task<string> LoadText()
        {
            var file = await Windows.Storage.StorageFile.GetFileFromApplicationUriAsync(new Uri("ms-appx:///Assets/rtf.txt"));
            var rtfString = await FileIO.ReadTextAsync(file);
            return rtfString;
        }

        private async void OnLoaded(object sender, RoutedEventArgs routedEventArgs)
        {
            if (_richTextFieldModelController.RichTextData != null)
            {
                xRichEitBox.Document.SetText(TextSetOptions.FormatRtf, _richTextFieldModelController.RichTextData);
            }
            else
            {
                var rtfString = await LoadText();
                xRichEitBox.Document.SetText(TextSetOptions.FormatRtf, rtfString);
            }

        }


        private void RichTextFieldModelControllerOnFieldModelUpdated(FieldModelController sender, Context c)
        {
            var text = string.Empty;
            xRichEitBox.Document.GetText(TextGetOptions.FormatRtf, out text);
            if (_richTextFieldModelController.RichTextData != null && !text.Equals(_richTextFieldModelController.RichTextData))
            {
                xRichEitBox.Document.SetText(TextSetOptions.FormatRtf, _richTextFieldModelController.RichTextData);
            }
        }

        // freezes the app
        private void XRichEitBoxOnTextChanged(object sender, RoutedEventArgs routedEventArgs)
        {

            //var richText = string.Empty;
            //xRichEitBox.Document.GetText(TextGetOptions.FormatRtf, out richText);
            //_richTextFieldModelController.RichTextData = richText;

        }
    }
}
