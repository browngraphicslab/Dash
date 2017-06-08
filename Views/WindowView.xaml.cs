using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;

// The User Control item template is documented at http://go.microsoft.com/fwlink/?LinkId=234236

namespace Dash
{
    public sealed partial class WindowView : UserControl
    {
        public WindowView()
        {
            this.InitializeComponent();
        }

        public void Close()
        {
            VisualTreeHelper.DisconnectChildrenRecursive(this);
        }

        private void Rectangle_Tapped(object sender, Windows.UI.Xaml.Input.TappedRoutedEventArgs e)
        {
            Close();
        }

        private void Rectangle_ManipulationDelta(object sender, Windows.UI.Xaml.Input.ManipulationDeltaRoutedEventArgs e)
        {
            
        }
    }
}
