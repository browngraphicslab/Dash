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
        public ImageSubtoolbar()
		{
			this.InitializeComponent();
		    xImageCommandbar.OverflowButtonVisibility = CommandBarOverflowButtonVisibility.Collapsed;
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
        }

		private void Replace_Click(object sender, RoutedEventArgs e)
		{
            ReplaceImage();
		    var open = xImageCommandbar.IsOpen;
		    xImageCommandbar.IsOpen = true;
		}

	    public CommandBar GetCommandBar()
	    {
	        return xImageCommandbar;
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
	        if (replacement != null)
	        {
	            //var stream = await replacement.OpenAsync(FileAccessMode.Read);
             //   var temp = new BitmapImage();
             //   temp.SetSource(stream);
             //   var replaced = new Image {Source = temp};

                var parser = new ImageToDashUtil();
	            var replacementDoc = await parser.ParseFileAsync(replacement);
	            if (replacementDoc != null)
	            {
	                var mainPageCollectionView = MainPage.Instance.MainDocView.GetFirstDescendantOfType<CollectionView>();
                    mainPageCollectionView.ViewModel.RemoveDocument(currentDocControl);
	                replacementDoc.GetPositionField().Data = currentDocControl.GetPositionField().Data;
	                mainPageCollectionView.ViewModel.AddDocument(replacementDoc, null);
	                currentDocView.ViewModel.DocumentController = replacementDoc;
	            }
            }
	    }

        internal void SetImageBinding(DocumentView selection)
        {
            currentDocView = selection;
            currentDocControl = currentDocView.ViewModel.DocumentController;
        }
    }
}
