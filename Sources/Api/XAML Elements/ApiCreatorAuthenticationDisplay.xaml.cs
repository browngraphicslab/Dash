using Windows.UI;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;

// The User Control item template is documented at http://go.microsoft.com/fwlink/?LinkId=234236

namespace Dash {
    public sealed partial class ApiCreatorAuthenticationDisplay : UserControl {
        public ApiCreatorAuthenticationDisplay() {
            this.InitializeComponent();
        }

        // GETTERS / SETTERS
        public ListView HeaderListView { get { return xAuthHeaders.ItemListView; } }
        public ListView ParameterListView { get { return xAuthParams.ItemListView; } }
        public string Secret { get { return xSecret.Text; } }
        public string Key { get { return xKey.Text; } }

        public string AuthURL { get { return xApiURLTB.Text; } }
        public string AuthMethod => (requestTypePicker.SelectedItem as ComboBoxItem).Content.ToString();

        public TextBox UrlTB { get { return xApiURLTB; } set { xApiURLTB = value; } }
        public TextBox KeyTB { get { return xKey; } set { xKey = value; } }
        public TextBox SecretTB { get { return xSecret; } set { xSecret = value; } }
        public ApiCreatorPropertyGenerator HeaderControl{ get { return xAuthHeaders; } }
        public ApiCreatorPropertyGenerator ParameterControl { get { return xAuthParams; } }

        private void collapseStackPanel() {
            if (xCollapseStackPanel.Visibility == Visibility.Visible) {
                xCollapseStackPanel.Visibility = Visibility.Collapsed;
                xCollapseButtonText.Text = "5";
            } else {
                xCollapseStackPanel.Visibility = Visibility.Visible;
                xCollapseButtonText.Text = "6";
            }
        }

        private void xCollapseButtonText_Tapped(object sender, TappedRoutedEventArgs e) {
            collapseStackPanel();
        }

        private void xApiURLTB_TextChanged(object sender, TextChangedEventArgs e) {

        }

        private void xRequestTypeButton_OnTapped(object sender, TappedRoutedEventArgs e)
        {
            requestTypePicker.IsDropDownOpen = true;
            requestTypePicker.Visibility = Visibility.Visible;
        }

        private void RequestTypePicker_OnDropDownClosed(object sender, object e)
        {
            requestTypePicker.Visibility = Visibility.Collapsed;
            xRequestTypeButton.Content = (requestTypePicker.SelectedItem as ComboBoxItem).Content.ToString();
        }

        private void xTextBox_OnGotFocus(object sender, RoutedEventArgs e)
        {
            var textbox = sender as TextBox;
            var accentGreen = xRequestTypeButton.Background;
            if (textbox != null)
            {
                textbox.BorderBrush = accentGreen;
                if (textbox == xApiURLTB)
                {
                    xAddressLabel.Background = accentGreen;
                } else if (textbox == xKey)
                {
                    xKeyLabel.Background = accentGreen;
                } else if (textbox == xSecret)
                {
                    xSecretLabel.Background = accentGreen;
                }
            }
        }

        private void xTextBox_OnLostFocus(object sender, RoutedEventArgs e)
        {
            var textbox = sender as TextBox;
            var slateGray = new SolidColorBrush(Colors.SlateGray);
            if (textbox != null)
            {
                textbox.BorderBrush = slateGray;
                if (textbox == xApiURLTB)
                {
                    xAddressLabel.Background = slateGray;
                }
                else if (textbox == xKey)
                {
                    xKeyLabel.Background = slateGray;
                }
                else if (textbox == xSecret)
                {
                    xSecretLabel.Background = slateGray;
                }
            }
        }
    }
}
