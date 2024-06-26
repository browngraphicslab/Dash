﻿using System;
using System.Threading.Tasks;
using Windows.Storage;
using Windows.Storage.Pickers;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;

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

	    public Task<VideoWrapper> GetVideoFile()
		{
			VideoWrapper file = null;

			var tcs = new TaskCompletionSource<VideoWrapper>();
			xPopup.IsOpen = true;
			xUploadButton.Tapped += UploadFile_OnTapped;
			xConfirmButton.Tapped += xConfirmButton_OnTapped;
			xCancelButton.Tapped += xCancelButton_OnTapped;
			xYouTubeButton.Tapped += xYouTubeButton_OnTapped;

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
				    file = new VideoWrapper{ Type = VideoType.StorageFile, File = pickedFile };
				    xCurrentVideoTextBlock.Text = "Currently selected: " + pickedFile.Name;
			    }
		    }

			async void xYouTubeButton_OnTapped(object sender, TappedRoutedEventArgs e)
			{
				var youtubePopup = new YouTubeLinkPopup();

				youtubePopup.HorizontalOffset = -250;
				youtubePopup.VerticalOffset = -75;

				xGrid.Children.Add(youtubePopup);
				var result = await youtubePopup.GetUri();

				if (result != null)
				{
					file = new VideoWrapper {Type = VideoType.Uri, Uri = result };
					xCurrentVideoTextBlock.Text = "Currently selected: " + result;
				}

				xGrid.Children.Remove(youtubePopup);
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
