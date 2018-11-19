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
using Dash.Popups;

// The User Control item template is documented at https://go.microsoft.com/fwlink/?LinkId=234236

namespace Dash
{
    public sealed partial class NewTemplatePopup : UserControl, DashPopup
    {
        public NewTemplatePopup()
        {
            this.InitializeComponent();
        }

        private void Popup_OnOpened(object sender, object e)
        {
            xName.Focus(FocusState.Programmatic);
        }

        public Task<(string, string)> GetFormResults()
        {
            xLayoutPopup.IsOpen = true;
            var tcs = new TaskCompletionSource<(string, string)>();
            xConfirmButton.Click += delegate
            {
                tcs.SetResult((xName.Text, xDesc.Text));
                xLayoutPopup.IsOpen = false;
            };
            xCancelButton.Click += delegate
            {
                tcs.SetResult((string.Empty, string.Empty));
                xLayoutPopup.IsOpen = false;
            };

            return tcs.Task;
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
