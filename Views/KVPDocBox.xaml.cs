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
                xDocBoxContainer.Background = new SolidColorBrush(Color.FromArgb(255, 235, 113, 113));
            }
            else if (docType.Equals(ImageBox.DocumentType))
            {
                _icon.Icon = FontAwesomeIcon.FileImageOutline;
                xDocBoxContainer.Background = new SolidColorBrush(Color.FromArgb(255, 253, 147, 50));

            }
            else if (docType.Equals(AudioBox.DocumentType))
            {
                _icon.Icon = FontAwesomeIcon.FileAudioOutline;
                xDocBoxContainer.Background = new SolidColorBrush(Color.FromArgb(255, 252, 212, 69));

            } else if (docType.Equals(InkBox.DocumentType))
            {
                _icon.Icon = FontAwesomeIcon.PencilSquareOutline;
                xDocBoxContainer.Background = new SolidColorBrush(Color.FromArgb(255, 148, 229, 98));

            } else if (docType.Equals(PdfBox.DocumentType))
            {
                _icon.Icon = FontAwesomeIcon.FilePdfOutline;
                xDocBoxContainer.Background = new SolidColorBrush(Color.FromArgb(255, 113, 219, 181));

            } else if (docType.Equals(VideoBox.DocumentType))
            {
                _icon.Icon = FontAwesomeIcon.FileVideoOutline;
                xDocBoxContainer.Background = new SolidColorBrush(Color.FromArgb(255, 95, 188, 217));

            } else if (docType.Equals(WebBox.DocumentType))
            {
                _icon.Icon = FontAwesomeIcon.FileCodeOutline;
                xDocBoxContainer.Background = new SolidColorBrush(Color.FromArgb(255, 104, 139, 228));
            }
            else
            {
                _icon.Icon = FontAwesomeIcon.FileOutline;
                xDocBoxContainer.Background = new SolidColorBrush(Color.FromArgb(255, 158, 104, 228));
            }

            xIcon.Children.Add(_icon);
        }

    private void DeleteButton_PointerEntered(object sender, PointerRoutedEventArgs e)
    {
            //_prev = _icon.Icon;
            Window.Current.CoreWindow.PointerCursor = new CoreCursor(CoreCursorType.Hand, 1);
            xIcon.Children.Remove(_icon);
            xDeleteIcon.Visibility = Visibility.Visible;
        }

        private void DeleteButton_PointerExited(object sender, PointerRoutedEventArgs e)
        {
            Window.Current.CoreWindow.PointerCursor = new CoreCursor(CoreCursorType.Arrow, 1);
            xDeleteIcon.Visibility = Visibility.Collapsed;
            xIcon.Children.Add(_icon);
        }

        private void DeleteButton_Tapped(object sender, TappedRoutedEventArgs e)
        {
            throw new NotImplementedException();
        }
    }
}
