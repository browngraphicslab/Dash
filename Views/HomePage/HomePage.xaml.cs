using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=234238

namespace Dash.Views.HomePage
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class HomePage : Page
    {
        public HomePage()
        {
            this.InitializeComponent();
        }

        private void GridView_DoubleTapped(object sender, DoubleTappedRoutedEventArgs e)
        {
            this.Frame.Navigate(typeof(MainPage));
        }

        /// <summary>
        /// Show confirm logout dialog when user clicks on the logout button
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void xLogoutButton_Tapped(object sender, TappedRoutedEventArgs e)
        {
            ContentDialog dialog = new ContentDialog()
            {
                Title = "Confirm Logout",
                MaxWidth = this.ActualWidth,
                MaxHeight = this.ActualHeight,
                PrimaryButtonText = "Yes",
                SecondaryButtonText = "Cancel",
                Content = new TextBlock
                {
                    Text = "Are you sure you want to logout?",
                    FontSize = 12,
                },
            };
            dialog.PrimaryButtonClick += Dialog_PrimaryButtonClick;
            dialog.SecondaryButtonClick += Dialog_SecondaryButtonClick;
            await dialog.ShowAsync();
        }

        /// <summary>
        /// Close confirm logout dialog if user clicks on the "Cancel" button
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="args"></param>
        private void Dialog_SecondaryButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
        {
            sender.Hide();
        }

        /// <summary>
        /// Logout and return to login page if user clicks on the "Yes" button
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="args"></param>
        private void Dialog_PrimaryButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
        {
            this.Frame.Navigate(typeof(LoginPage));
        }

        private void Button_Tapped(object sender, TappedRoutedEventArgs e)
        {
            this.Frame.Navigate(typeof(MainPage));
        }

        /// <summary>
        /// Toggle between collapsing and expanding the submenu under MyDashBoards
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void xMyDashBoardsButton_Tapped(object sender, TappedRoutedEventArgs e)
        {
            xMyDashboardsSubMenu.Visibility = xMyDashboardsSubMenu.Visibility == Visibility.Collapsed ? Visibility.Visible: Visibility.Collapsed;
            xExpand.Visibility = xExpand.Visibility == Visibility.Collapsed ? Visibility.Visible : Visibility.Collapsed;
            xCollapse.Visibility = xCollapse.Visibility == Visibility.Collapsed ? Visibility.Visible : Visibility.Collapsed;
        }

        /// <summary>
        /// Bring user back to previous page (disabled since user shouldn't back out to the login page without loging out)
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void xBackButton_Tapped(object sender, TappedRoutedEventArgs e)
        {
            this.Frame.Navigate(typeof(LoginPage));
        }

        /// <summary>
        /// Content of splitview navigates to the CollectionScreen when the AllDashboardsButton is tapped on
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void xAllDashboardsButton_Tapped(object sender, TappedRoutedEventArgs e)
        {
            xContentFrame.Navigate(typeof(CollectionScreen));
        }

        /// <summary>
        /// Content of splitview navigates to the RecentlyViewedPage when the RecentlyViewedButton is tapped on
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void xRecentlyViewedButton_Tapped(object sender, TappedRoutedEventArgs e)
        {
            xContentFrame.Navigate(typeof(RecentlyViewedPage));
        }

        private void xDeletedButton_Tapped(object sender, TappedRoutedEventArgs e)
        {
            xContentFrame.Navigate(typeof(DeletedPage));
        }

        private void xProfileButton_Tapped(object sender, TappedRoutedEventArgs e)
        {
            xContentFrame.Navigate(typeof(ProfilePage));
        }

        private void xSettingsButton_Tapped(object sender, TappedRoutedEventArgs e)
        {
            xContentFrame.Navigate(typeof(SettingsPage));
        }
    }
}
