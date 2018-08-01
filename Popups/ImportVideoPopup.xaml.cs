using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Storage;
using Windows.Storage.Pickers;
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
    public sealed partial class ImportVideoPopup : UserControl, DashPopup
    {
        public ImportVideoPopup()
        {
            this.InitializeComponent();
        }

	    public void SetHorizontalOffset(double offset)
	    {
		    xPopup.HorizontalOffset = offset;
	    }

	    public void SetVerticalOffset(double offset)
	    {
		    xPopup.VerticalOffset = offset;
	    }

	    public FrameworkElement Self()
	    {
		    return this;
	    }

	    public Task<StorageFile> GetVideoFile()
		{
			StorageFile file = null;

			var tcs = new TaskCompletionSource<StorageFile>();
			xPopup.IsOpen = true;
			xUploadButton.Tapped += UploadFile_OnTapped;
			xConfirmButton.Tapped += xConfirmButton_OnTapped;

			async void UploadFile_OnTapped(object sender, TappedRoutedEventArgs e)
		    {
			    var picker = new FileOpenPicker();
			    picker.FileTypeFilter.Add(".mp4");
			    picker.FileTypeFilter.Add(".mov");
			    picker.FileTypeFilter.Add(".avi");
			    picker.FileTypeFilter.Add(".wmv");
			    picker.FileTypeFilter.Add(".flv");

			    StorageFile pickedFile = await picker.PickSingleFileAsync();
			    if (pickedFile != null)
			    {
				    file = pickedFile;
				    xCurrentVideoTextBlock.Text = "Currently selected: " + pickedFile.Name;
			    }
		    }

			void xConfirmButton_OnTapped(object sender, RoutedEventArgs e)
			{
				xPopup.IsOpen = false;
				xErrorMessageIcon.Visibility = Visibility.Collapsed;
				xErrorMessageText.Visibility = Visibility.Collapsed;
				
				if (file != null)
				{
					tcs.SetResult(file);
					xConfirmButton.Tapped -= xConfirmButton_OnTapped;
				}
				else
				{
					xPopup.IsOpen = true;
					xErrorMessageIcon.Visibility = Visibility.Visible;
					xErrorMessageText.Visibility = Visibility.Visible;
				}
			}

			return tcs.Task;
		}
    }
}
