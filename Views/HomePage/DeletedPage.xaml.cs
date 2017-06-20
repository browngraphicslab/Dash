using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
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
    public sealed partial class DeletedPage : Page
    {
        private bool _isListView;
        public DeletedPage()
        {
            this.InitializeComponent();
            _isListView = false;
        }

        private void Grid_Holding(object sender, HoldingRoutedEventArgs e)
        {
            this.EnterMultipleSelectionMode();
        }

        private void xCancelButton_Tapped(object sender, TappedRoutedEventArgs e)
        {
                xClearSelectionButton.Visibility = Visibility.Collapsed;
                if (_isListView)
                {
                    xGridToggle.Visibility = Visibility.Visible;
                    xMainListView.SelectionMode = ListViewSelectionMode.Single;
                }
                else
                {
                    xListToggle.Visibility = Visibility.Visible;
                    xMainGridView.SelectionMode = ListViewSelectionMode.Single;
                }
                xSelectButton.Visibility = Visibility.Visible;
                xSelectAllButton.Visibility = Visibility.Collapsed;
                xRestoreButton.Visibility = Visibility.Collapsed;
                xCancelButton.Visibility = Visibility.Collapsed;
        }

        private void xListToggle_Tapped(object sender, TappedRoutedEventArgs e)
        {
            xListToggle.Visibility = Visibility.Collapsed;
            xGridToggle.Visibility = Visibility.Visible;
            xMainGridView.Visibility = Visibility.Collapsed;
            xMainListView.Visibility = Visibility.Visible;
            _isListView = true;
        }

        private void xGridToggle_Tapped(object sender, TappedRoutedEventArgs e)
        {
            xListToggle.Visibility = Visibility.Visible;
            xGridToggle.Visibility = Visibility.Collapsed;
            xMainGridView.Visibility = Visibility.Visible;
            xMainListView.Visibility = Visibility.Collapsed;
            _isListView = false;
        }

        private void xSelectAllButton_Tapped(object sender, TappedRoutedEventArgs e)
        {
            if (_isListView)
            {
                xMainListView.SelectAll();
            }
            else
            {
                xMainGridView.SelectAll();
            }
        }

        private void xSelectButton_Tapped(object sender, TappedRoutedEventArgs e)
        {
            this.EnterMultipleSelectionMode();
        }

        private void EnterMultipleSelectionMode()
        {
                if (_isListView)
                {
                    xMainListView.SelectionMode = ListViewSelectionMode.Multiple;
                } else
                {
                    xMainGridView.SelectionMode = ListViewSelectionMode.Multiple;
                }
                xClearSelectionButton.Visibility = Visibility.Visible;
                xListToggle.Visibility = Visibility.Collapsed;
                xGridToggle.Visibility = Visibility.Collapsed;
                xSelectButton.Visibility = Visibility.Collapsed;
                xSelectAllButton.Visibility = Visibility.Visible;
                xRestoreButton.Visibility = Visibility.Visible;
                xCancelButton.Visibility = Visibility.Visible;
        }

        private void xDateModifiedItem_Tapped(object sender, TappedRoutedEventArgs e)
        {
            // sort underlying observable collection by date modified
        }

        private void xCollectionNameItem_Tapped(object sender, TappedRoutedEventArgs e)
        {
            // sort underlying observable collection by collection name
        }

        private void xAuthorItem_Tapped(object sender, TappedRoutedEventArgs e)
        {
            // sort underlying observable collection by author
        }
    }
}
