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

// The User Control item template is documented at https://go.microsoft.com/fwlink/?LinkId=234236

namespace Dash.Popups
{
    public sealed partial class HTMLRTFPopup : UserControl, DashPopup
    {
		public HTMLRTFPopup()
        {
            this.InitializeComponent();
        }

	    public Task<SettingsView.WebpageLayoutMode> GetLayoutMode()
		{
			var tcs = new TaskCompletionSource<SettingsView.WebpageLayoutMode>();
			xLayoutPopup.IsOpen = true;
			xConfirmButton.Tapped += XConfirmButton_OnClick;

			void XConfirmButton_OnClick(object sender, RoutedEventArgs e)
		    {
			    xLayoutPopup.IsOpen = false;
			    xErrorMessageIcon.Visibility = Visibility.Collapsed;
			    xErrorMessageText.Visibility = Visibility.Collapsed;

			    var remember = xSaveHtmlType.IsChecked ?? false;

			    if (xComboBox.SelectedIndex == 0)
			    {
				    if (remember)
				    {
					    SettingsView.Instance.WebpageLayout = SettingsView.WebpageLayoutMode.HTML;
					    xSaveHtmlType.IsChecked = false;
				    }
				    tcs.SetResult(SettingsView.WebpageLayoutMode.HTML);
				    xConfirmButton.Tapped -= XConfirmButton_OnClick;
			    }
			    else if (xComboBox.SelectedIndex == 1)
			    {
				    if (remember)
				    {
					    SettingsView.Instance.WebpageLayout = SettingsView.WebpageLayoutMode.RTF;
					    xSaveHtmlType.IsChecked = false;
				    }
				    tcs.SetResult(SettingsView.WebpageLayoutMode.RTF);
				    xConfirmButton.Tapped -= XConfirmButton_OnClick;
			    }
			    else
			    {
				    xLayoutPopup.IsOpen = true;
				    xErrorMessageIcon.Visibility = Visibility.Visible;
				    xErrorMessageText.Visibility = Visibility.Visible;
			    }
		    }

			return tcs.Task;
		}

	    private void Popup_OnOpened(object sender, object e)
	    {
		    xComboBox.SelectedItem = null;
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
    }
}
