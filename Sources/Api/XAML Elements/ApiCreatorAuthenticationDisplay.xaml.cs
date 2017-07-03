using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;

// The User Control item template is documented at http://go.microsoft.com/fwlink/?LinkId=234236

namespace Dash.Sources.Api.XAML_Elements {
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

        public TextBox UrlTB { get { return xApiURLTB; } set { xApiURLTB = value; } }
        public TextBox KeyTB { get { return xKey; } set { xKey = value; } }
        public TextBox SecretTB { get { return xSecret; } set { xSecret = value; } }
        public ApiCreatorPropertyGenerator HeaderControl{ get { return xAuthHeaders; } }
        public ApiCreatorPropertyGenerator ParameterControl { get { return xAuthParams; } }

        private void collapseStackPanel() {
            if (xCollapseStackPanel.Visibility == Visibility.Visible) {
                xCollapseStackPanel.Visibility = Visibility.Collapsed;
                xCollapseButtonText.Text = "+";
            } else {
                xCollapseStackPanel.Visibility = Visibility.Visible;
                xCollapseButtonText.Text = "-";
            }
        }

        private void xCollapseButtonText_Tapped(object sender, TappedRoutedEventArgs e) {
            collapseStackPanel();
        }

        private void xApiURLTB_TextChanged(object sender, TextChangedEventArgs e) {

        }
    }
}
