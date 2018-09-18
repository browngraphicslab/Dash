using System;
using System.Threading.Tasks;
using Windows.Storage;
using Windows.Storage.Pickers;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;

// The User Control item template is documented at https://go.microsoft.com/fwlink/?LinkId=234236

namespace Dash.Popups
{
	public sealed partial class ImportImagePopup : UserControl, DashPopup
	{
		public ImportImagePopup()
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

		public Task<StorageFile> GetImageFile()
		{
			StorageFile file = null;

			var tcs = new TaskCompletionSource<StorageFile>();
			xPopup.IsOpen = true;
			xUploadButton.Tapped += UploadFile_OnTapped;
			xConfirmButton.Tapped += xConfirmButton_OnTapped;
			xCancelButton.Tapped += xCancelButton_OnTapped;

			async void UploadFile_OnTapped(object sender, TappedRoutedEventArgs e)
			{
				var picker = new FileOpenPicker();
				picker.FileTypeFilter.Add(".jpg");
				picker.FileTypeFilter.Add(".png");
				picker.FileTypeFilter.Add(".gif");
				picker.FileTypeFilter.Add(".tiff");
				picker.FileTypeFilter.Add(".jpeg");

				StorageFile pickedFile = await picker.PickSingleFileAsync();
				if (pickedFile != null)
				{
					file = pickedFile;
					xCurrentImageTextBlock.Text = "Currently selected: " + pickedFile.Name;
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

			// if cancel is pressed, null is returned.
			void xCancelButton_OnTapped(object sender, RoutedEventArgs e)
			{
				xPopup.IsOpen = false;
				tcs.SetResult(null);
				xCancelButton.Tapped -= xCancelButton_OnTapped;
			}

			return tcs.Task;
		}
	}
}
