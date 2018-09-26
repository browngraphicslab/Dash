using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using Dash.FontIcons;
using DashShared;

// The User Control item template is documented at https://go.microsoft.com/fwlink/?LinkId=234236

namespace Dash
{
    public sealed partial class KVPDocBox : UserControl
    {
        public string Text
        {
            get => _text;
            set { _text = value; }
        }

        private string _text;
        private FontAwesomeIcon _prev;
        private FontIcons.FontAwesome _icon;

        public KVPDocBox(DocumentType docType, String text)
        {
            this.InitializeComponent();
            xText.Text = text;
            _text = text;
            GenerateIcon(docType);
           
        }

        private void GenerateIcon(DocumentType docType)
        {

            _icon = new FontIcons.FontAwesome();
            _icon.FontSize = 10;
            _icon.Foreground = new SolidColorBrush(Colors.White);
            if (docType.Equals(TextingBox.DocumentType) || docType.Equals(RichTextBox.DocumentType))
            {
                _icon.Icon = FontAwesomeIcon.FileTextOutline;
            }
            else if (docType.Equals(ImageBox.DocumentType))
            {
                _icon.Icon = FontAwesomeIcon.FileImageOutline;
            }
            else if (docType.Equals(AudioBox.DocumentType))
            {
                _icon.Icon = FontAwesomeIcon.FileAudioOutline;

            } else if (docType.Equals(InkBox.DocumentType))
            {
                _icon.Icon = FontAwesomeIcon.PencilSquareOutline;

            } else if (docType.Equals(PdfBox.DocumentType))
            {
                _icon.Icon = FontAwesomeIcon.FilePdfOutline;

            } else if (docType.Equals(VideoBox.DocumentType))
            {
                _icon.Icon = FontAwesomeIcon.FileVideoOutline;

            } else if (docType.Equals(WebBox.DocumentType))
            {
                _icon.Icon = FontAwesomeIcon.FileCodeOutline;
            }
            else
            {
                _icon.Icon = FontAwesomeIcon.FileOutline;
            }

            xIcon.Children.Add(_icon);
        }

    private void DeleteButton_PointerEntered(object sender, PointerRoutedEventArgs e)
    {
            _prev = _icon.Icon;
            Window.Current.CoreWindow.PointerCursor = new CoreCursor(CoreCursorType.Hand, 1);
            _icon.Icon = FontAwesomeIcon.Times;
        }

        private void DeleteButton_PointerExited(object sender, PointerRoutedEventArgs e)
        {
            Window.Current.CoreWindow.PointerCursor = new CoreCursor(CoreCursorType.Arrow, 1);
            _icon.Icon = _prev;
        }

        private void DeleteButton_Tapped(object sender, TappedRoutedEventArgs e)
        {
            throw new NotImplementedException();
        }
    }
}
