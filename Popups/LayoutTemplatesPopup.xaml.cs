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

namespace Dash.Popups
{
    public sealed partial class LayoutTemplatesPopup : UserControl, DashPopup
    {
        private TaskCompletionSource<TemplateList.TemplateType> _tcs;
        public LayoutTemplatesPopup()
        {
            this.InitializeComponent();
            HorizontalAlignment = HorizontalAlignment.Left;
            VerticalAlignment = VerticalAlignment.Top;
        }

        public Task<TemplateList.TemplateType> GetTemplate()
        {
            _tcs = new TaskCompletionSource<TemplateList.TemplateType>();
            xLayoutPopup.IsOpen = true;
            xCitation.Tapped += XCitation_OnClick;
            xNote.Tapped += XNote_OnClick;
            xCard.Tapped += XCard_OnClick;

            return _tcs.Task;
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

        void XCitation_OnClick(object sender, RoutedEventArgs e)
        {
            _tcs.SetResult(TemplateList.TemplateType.Citation);
            xLayoutPopup.IsOpen = false;
            xCitation.Tapped -= XCitation_OnClick;
        }
        void XNote_OnClick(object sender, RoutedEventArgs e)
        {
            _tcs.SetResult(TemplateList.TemplateType.Note);
            xLayoutPopup.IsOpen = false;
            xNote.Tapped -= XNote_OnClick;
        }
        void XCard_OnClick(object sender, RoutedEventArgs e)
        {
            _tcs.SetResult(TemplateList.TemplateType.Card);
            xLayoutPopup.IsOpen = false;
            xNote.Tapped -= XCard_OnClick;
        }

    }
}
