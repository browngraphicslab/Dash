using System;
using System.Collections.Generic;
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
        public RichTextView(RichTextFieldModelController richTextFieldModelController)
        {
            this.InitializeComponent();
            _richTextFieldModelController = richTextFieldModelController;
            xRichEitBox.Paste += XRichEitBoxOnPaste;
            xRichEitBox.TextChanged += XRichEitBoxOnTextChanged;
            Loaded += OnLoaded;
            _richTextFieldModelController.FieldModelUpdated += RichTextFieldModelControllerOnFieldModelUpdated;
        }

        private void OnLoaded(object sender, RoutedEventArgs routedEventArgs)
        {
            if(_richTextFieldModelController.RichTextData != null)
                xRichEitBox.Document.SetText(TextSetOptions.FormatRtf, _richTextFieldModelController.RichTextData);
        }

        private void RichTextFieldModelControllerOnFieldModelUpdated(FieldModelController sender)
        {
            if (_richTextFieldModelController.RichTextData != null)
                xRichEitBox.Document.SetText(TextSetOptions.FormatRtf, _richTextFieldModelController.RichTextData);
        }

        private void XRichEitBoxOnTextChanged(object sender, RoutedEventArgs routedEventArgs)
        {
            var richText = string.Empty;
                xRichEitBox.Document.GetText(TextGetOptions.FormatRtf, out richText);
            _richTextFieldModelController.RichTextData = richText;
        }

        private void XRichEitBoxOnPaste(object sender, TextControlPasteEventArgs textControlPasteEventArgs)
        {
            var richText = string.Empty;
            xRichEitBox.Document.GetText(TextGetOptions.FormatRtf, out richText);
            _richTextFieldModelController.RichTextData = richText;
        }
    }
}
