using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

// The User Control item template is documented at https://go.microsoft.com/fwlink/?LinkId=234236

namespace Dash
{
    public sealed partial class ApiView : UserControl
    {
        public ApiView()
        {
            this.InitializeComponent();

            XApiCreator.MakeApi += XApiCreatorOnMakeApi;
            XApiSource.EditApi += XApiSourceOnEditApi;
        }

        private void XApiSourceOnEditApi()
        {
            XApiSource.Visibility = Visibility.Collapsed;
            XApiCreator.Visibility = Visibility.Visible;
        }

        private void XApiCreatorOnMakeApi()
        {
            XApiSource.Visibility = Visibility.Visible;
            XApiCreator.Visibility = Visibility.Collapsed;
        }
    }
}
