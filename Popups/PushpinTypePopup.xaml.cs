using System.Threading.Tasks;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

// The User Control item template is documented at https://go.microsoft.com/fwlink/?LinkId=234236

namespace Dash.Popups
{
    public sealed partial class PushpinTypePopup : UserControl, DashPopup
    {
        public PushpinTypePopup()
        {
            this.InitializeComponent();
        }

	    public Task<PushpinType> GetPushpinType()
	    {
		    var tcs = new TaskCompletionSource<PushpinType>();
		    xPushpinPopup.IsOpen = true;
		    xPushpinConfirmButton.Tapped += xPushpinConfirmButton_OnClick;

		    void xPushpinConfirmButton_OnClick(object sender, RoutedEventArgs e)
		    {
			    xPushpinPopup.IsOpen = false;
			    xPushpinErrorMessageIcon.Visibility = Visibility.Collapsed;
			    xPushpinErrorMessageText.Visibility = Visibility.Collapsed;
			    switch (xPushpinComboBox.SelectedIndex)
			    {
				    case 0:
					    tcs.SetResult(PushpinType.Text);
					    xPushpinConfirmButton.Tapped -= xPushpinConfirmButton_OnClick;
					    break;
				    case 1:
					    tcs.SetResult(PushpinType.Image);
					    xPushpinConfirmButton.Tapped -= xPushpinConfirmButton_OnClick;
					    break;
				    case 2:
					    tcs.SetResult(PushpinType.Video);
					    xPushpinConfirmButton.Tapped -= xPushpinConfirmButton_OnClick;
					    break;
				    // if nothing was chosen, then we don't want to close the popup
				    default:
					    xPushpinPopup.IsOpen = true;
					    xPushpinErrorMessageIcon.Visibility = Visibility.Visible;
					    xPushpinErrorMessageText.Visibility = Visibility.Visible;
					    break;
			    }
		    }

		    return tcs.Task;
		}


		public void SetHorizontalOffset(double offset)
	    {
		    xPushpinPopup.HorizontalOffset = offset;
	    }

	    public void SetVerticalOffset(double offset)
	    {
		    xPushpinPopup.VerticalOffset = offset;
	    }

	    public FrameworkElement Self()
	    {
		    return this;
	    }

	    private void Popup_OnOpened(object sender, object e)
	    {
		    xPushpinComboBox.SelectedItem = 0;
	    }
	}
}
