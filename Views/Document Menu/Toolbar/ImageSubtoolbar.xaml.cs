using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
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
using Windows.UI.Xaml.Media.Imaging;
using Windows.UI.Xaml.Navigation;
using Dash.Views.Document_Menu.Toolbar;

// The User Control item template is documented at https://go.microsoft.com/fwlink/?LinkId=234236

namespace Dash
{
	/**
	 * The subtoolbar that appears when an ImageBox is selected.
	 */
	public sealed partial class ImageSubtoolbar : UserControl, ICommandBarBased
	{
	    private DocumentView currentDocView;
	    private DocumentController currentDocControl;
	    private Action OnCropClick;
        public ImageSubtoolbar()
		{
			this.InitializeComponent();
		    FormatDropdownMenu();
		}

        private void FormatDropdownMenu()
        {
            xScaleOpetionsDropdown.Width = ToolbarConstants.ComboBoxWidth;
            xScaleOpetionsDropdown.Height = ToolbarConstants.ComboBoxHeight;
            xScaleOpetionsDropdown.Margin = new Thickness(ToolbarConstants.ComboBoxMargin);
        }

        /**
         * Prevents command bar from hiding labels on click by setting isOpen to true every time it begins to close.
        */
        private void CommandBar_Closing(object sender, object e)
		{
			xImageCommandbar.IsOpen = true;
		}

		private void Crop_Click(object sender, RoutedEventArgs e)
		{
            //TODO: Implement cropping on the selected image
		    xImageCommandbar.IsOpen = true;
            currentDocView.OnCropClick?.Invoke();
        }

		private void Replace_Click(object sender, RoutedEventArgs e)
		{
            ReplaceImage();
		    xImageCommandbar.IsOpen = true;
		}

	    public void CommandBarOpen(bool status)
	    {
	        xImageCommandbar.IsOpen = status;
	        xImageCommandbar.IsEnabled = true;
	        xImageCommandbar.Visibility = Visibility.Visible;
	    }
        
	    private async void ReplaceImage()
	    {
	        var imagePicker = new FileOpenPicker
	        {
	            ViewMode = PickerViewMode.Thumbnail,
	            SuggestedStartLocation = PickerLocationId.PicturesLibrary
	        };
	        imagePicker.FileTypeFilter.Add(".jpg");
	        imagePicker.FileTypeFilter.Add(".jpeg");
	        imagePicker.FileTypeFilter.Add(".bmp");
	        imagePicker.FileTypeFilter.Add(".png");
	        imagePicker.FileTypeFilter.Add(".svg");

	        var replacement = await imagePicker.PickSingleFileAsync();
	        if (replacement != null) { currentDocControl.SetField<ImageController>(KeyStore.DataKey, await ImageToDashUtil.GetLocalURI(replacement), true); }
	    }

        internal void SetImageBinding(DocumentView selection)
        {
            currentDocView = selection;
            currentDocControl = currentDocView.ViewModel.DocumentController;
        }
    }
}
