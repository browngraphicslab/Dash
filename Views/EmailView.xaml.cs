using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Storage;
using Windows.Storage.Pickers;
using Windows.UI;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=234238

namespace Dash
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class EmailView : WindowTemplate
    {
        private StorageFile _attachmentFile;
        private string _to;
        private string _from;
        private string _password; 
        private string _message = "";
        private string _subject = "";

        public EmailView()
        {
            InitializeComponent();
        }

        private async void Attachment_Tapped(object sender, TappedRoutedEventArgs e)
        {
            FileOpenPicker picker = new FileOpenPicker();
            picker.FileTypeFilter.Add("*");
            _attachmentFile = await picker.PickSingleFileAsync();

            if (_attachmentFile != null)
            {
                AttachmentButton.Content = "Attachment: " + _attachmentFile.Name;
                AttachmentButton.Background = new SolidColorBrush(Colors.LightBlue);
            } else
            {
                AttachmentButton.Content = "Attachment: None";
                AttachmentButton.Background = new SolidColorBrush(Colors.LightGray);
            }
        }

        private void Send_Tapped(object sender, TappedRoutedEventArgs e)
        {
            if (_from == null || _to == null || _password == null) return;
            Util.SendEmail2(_to, _password, _from, _message, _subject, _attachmentFile); 
        }

        private void Subject_TextChanged(object sender, TextChangedEventArgs e)
        {
            _subject = (sender as TextBox).Text;
        }

        private void Message_TextChanged(object sender, TextChangedEventArgs e)
        {
            _message = (sender as TextBox).Text;
        }

        private void To_TextChanged(object sender, TextChangedEventArgs e)
        {
            _to = (sender as TextBox).Text;
        }

        private void From_TextChanged(object sender, TextChangedEventArgs e)
        {
            _from = (sender as TextBox).Text;
        }

        private void Password_TextChanged(object sender, TextChangedEventArgs e)
        {
            _password = (sender as TextBox).Text;
        }

        private void PasswordBox_PasswordChanged(object sender, RoutedEventArgs args)
        {
            _password = (sender as PasswordBox).Password;
        }
        
    }
}
