using System;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;

// The User Control item template is documented at https://go.microsoft.com/fwlink/?LinkId=234236

namespace Dash.ViewModels
{
    public sealed partial class ReplLineNode : UserControl
    {
        public ReplLineNode()
        {
            this.InitializeComponent();
        }

        private void MenuFlyoutItem_OnClick(object sender, RoutedEventArgs e)
        {
            throw new NotImplementedException();
        }

        private void XTextBlock_OnDoubleTapped(object sender, DoubleTappedRoutedEventArgs e)
        {
            throw new NotImplementedException();
        }

        private void XArrowBlock_OnTapped(object sender, TappedRoutedEventArgs e)
        {
            e.Handled = true;
            //Toggle visibility
            if (Visibility == Visibility.Collapsed)
            {
                Visibility = Visibility.Visible;
                xArrowBlock.Text = (string)Application.Current.Resources["ContractArrowIcon"];
            }
            else
            {
                Visibility = Visibility.Collapsed;
                xArrowBlock.Text = (string)Application.Current.Resources["ExpandArrowIcon"];
            }
        }

        private void XSnapshotArrowBlock_OnTapped(object sender, TappedRoutedEventArgs e)
        {
            throw new NotImplementedException();
        }
    }
}
