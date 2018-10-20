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
    public sealed partial class JoinGroupMenuPopup : UserControl, DashPopup
    {
        private KeyController _comparisonKey;
        private List<KeyController> _includeKeys = new List<KeyController>();

        public JoinGroupMenuPopup(List<KeyController> comparisonKeys, List<KeyController> diffKeys)
        {
            this.InitializeComponent();

            xComparisonKeyList.ItemsSource = comparisonKeys;
            xIncludeKeyList.ItemsSource = diffKeys;
        }

        private void Popup_OnOpened(object sender, object e)
        {

        }

        public Task<(KeyController, List<KeyController>)> GetFormResults()
        {
            xLayoutPopup.IsOpen = true;
            var tcs = new TaskCompletionSource<(KeyController, List<KeyController>)>();
            xConfirmButton.Click += delegate
            {
                tcs.SetResult((_comparisonKey, _includeKeys));
                xLayoutPopup.IsOpen = false;
            };
            xCancelButton.Click += delegate
            {
                tcs.SetResult((null, null));
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

        private void ComparisonKeyChecked(object sender, RoutedEventArgs e)
        {
            _comparisonKey = (sender as RadioButton).DataContext as KeyController;
        }

        private void IncludeKeyChecked(object sender, RoutedEventArgs e)
        {
            _includeKeys.Add((sender as CheckBox).DataContext as KeyController);
        }

        private void IncludeKeyUnchecked(object sender, RoutedEventArgs e)
        {
            _includeKeys.Remove((sender as CheckBox).DataContext as KeyController);
        }
    }
}
