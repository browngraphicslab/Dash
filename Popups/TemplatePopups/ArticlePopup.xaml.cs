using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

// The User Control item template is documented at https://go.microsoft.com/fwlink/?LinkId=234236

namespace Dash.Popups.TemplatePopups
{
    public sealed partial class ArticlePopup : UserControl, ICustomTemplate
    {
        private ObservableCollection<string> fields = new ObservableCollection<string>();
        public ArticlePopup(IEnumerable<string> hashFields)
        {
            this.InitializeComponent();
            foreach (var field in hashFields)
            {
                fields.Add(field);
            }
        }

        public Task<List<string>> GetLayout()
        {
            var tcs = new TaskCompletionSource<List<string>>();
            xLayoutPopup.IsOpen = true;
            xConfirmButton.Tapped += XConfirmButton_OnClick;
            xCancelButton.Tapped += xCancelButton_OnClick;
            void xCancelButton_OnClick(object sender, object e)
            {
                xLayoutPopup.IsOpen = false;
                tcs.SetResult(null);
                xCancelButton.Tapped -= xCancelButton_OnClick;
            }
            void XConfirmButton_OnClick(object sender, RoutedEventArgs e)
            {
                var input = new List<string>
                {
                    fields.ElementAtOrDefault(xTextField0.SelectedIndex),
                    fields.ElementAtOrDefault(xTextField1.SelectedIndex),
                    fields.ElementAtOrDefault(xTextFieldImage.SelectedIndex),
                    fields.ElementAtOrDefault(xTextField3.SelectedIndex)
                };

                xLayoutPopup.IsOpen = false;
                tcs.SetResult(input);
                xConfirmButton.Tapped -= XConfirmButton_OnClick;
            }

            return tcs.Task;
        }

        

        private void Popup_OnOpened(object sender, object e)
        {
        }

        public void SetHorizontalOffset(double offset)
        {
            xLayoutPopup.HorizontalOffset = offset;
        }

        public void SetVerticalOffset(double offset)
        {
            xLayoutPopup.VerticalOffset = offset;
        }

        public FrameworkElement Self()
        {
            return this;
        }
    }
}
