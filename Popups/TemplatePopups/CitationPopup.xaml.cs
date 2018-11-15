using System;
using System.Collections.Generic;
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
    public sealed partial class CitationPopup : UserControl
    {
        public CitationPopup()
        {
            this.InitializeComponent();
        }

        public Task<SettingsView.WebpageLayoutMode> GetTemplate()
        {
            var tcs = new TaskCompletionSource<SettingsView.WebpageLayoutMode>();
            xLayoutPopup.IsOpen = true;
            xConfirmButton.Tapped += XConfirmButton_OnClick;
            void XConfirmButton_OnClick(object sender, RoutedEventArgs e)
            {
                xLayoutPopup.IsOpen = false;
                SettingsView.Instance.WebpageLayout = SettingsView.WebpageLayoutMode.RTF;
                tcs.SetResult(SettingsView.WebpageLayoutMode.RTF);
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
