using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;

// The User Control item template is documented at https://go.microsoft.com/fwlink/?LinkId=234236

namespace Dash.Views
{
    public sealed partial class ImageNode : UserControl
    {
        private bool userTappedButton = false;

        public ImageNode()
        {
            this.InitializeComponent();
        }

        private void UiElementTapped(object sender, TappedRoutedEventArgs e)
        {
            userTappedButton = true;
        }
    }
}
