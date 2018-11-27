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
            xCloseButton.Tapped += XCloseButton_Click;
        }

        public Task<TemplateList.TemplateType> GetTemplate()
        {
            _tcs = new TaskCompletionSource<TemplateList.TemplateType>();
            xLayoutPopup.IsOpen = true;
            xCitation.Tapped += XCitation_OnClick;
            xNote.Tapped += XNote_OnClick;
            xCard.Tapped += XCard_OnClick;
            xTitle.Tapped += XTitle_OnClick;
            xProfile.Tapped += XProfile_OnClick;
            xArticle.Tapped += XArticle_OnClick;
            xBiography.Tapped += XBiography_OnClick;
            xFlashcard.Tapped += XFlashcard_OnClick;

            return _tcs.Task;
        }

        private void XCloseButton_Click(object sender, object e)
        {
            _tcs.SetResult(TemplateList.TemplateType.None);
            xLayoutPopup.IsOpen = false;
            xCloseButton.Tapped -= XCloseButton_Click;
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
            xCard.Tapped -= XCard_OnClick;
        }

        void XTitle_OnClick(object sender, RoutedEventArgs e)
        {
            _tcs.SetResult(TemplateList.TemplateType.Title);
            xLayoutPopup.IsOpen = false;
            xTitle.Tapped -= XTitle_OnClick;
        }

        void XProfile_OnClick(object sender, RoutedEventArgs e)
        {
            _tcs.SetResult(TemplateList.TemplateType.Profile);
            xLayoutPopup.IsOpen = false;
            xProfile.Tapped -= XProfile_OnClick;
        }

        void XArticle_OnClick(object sender, RoutedEventArgs e)
        {
            _tcs.SetResult(TemplateList.TemplateType.Article);
            xLayoutPopup.IsOpen = false;
            xArticle.Tapped -= XArticle_OnClick;
        }

        void XBiography_OnClick(object sender, RoutedEventArgs e)
        {
            _tcs.SetResult(TemplateList.TemplateType.Biography);
            xLayoutPopup.IsOpen = false;
            xBiography.Tapped -= XBiography_OnClick;
        }

        void XFlashcard_OnClick(object sender, RoutedEventArgs e)
        {
            _tcs.SetResult(TemplateList.TemplateType.Flashcard);
            xLayoutPopup.IsOpen = false;
            xFlashcard.Tapped -= XFlashcard_OnClick;
        }

    }
}
