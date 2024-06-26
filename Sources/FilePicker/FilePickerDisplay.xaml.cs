﻿using System;
using System.Collections.Generic;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media.Imaging;

// The User Control item template is documented at http://go.microsoft.com/fwlink/?LinkId=234236

namespace Dash.Sources.FilePicker {
    public sealed partial class FilePickerDisplay : UserControl {
        string fileType; // i.e. image, text, word, pdf-- not the extensions!
        List<string> fileExtensions; // i.e. [png, jpg, tiff]

        public FilePickerDisplay() {
            this.InitializeComponent();
            //new ManipulationControls(this);

        }

        /// <summary>
        /// Opens a pop up for user to choose a file of given fileExtensions types. Loads
        /// in the file and stores a preview of it in the resultSource element. 
        /// </summary>
        public async void getFile() {

            var picker = new Windows.Storage.Pickers.FileOpenPicker();
            picker.ViewMode = Windows.Storage.Pickers.PickerViewMode.Thumbnail;

            picker.SuggestedStartLocation = Windows.Storage.Pickers.PickerLocationId.PicturesLibrary;
            picker.FileTypeFilter.Add(".jpg");
            picker.FileTypeFilter.Add(".jpeg");
            picker.FileTypeFilter.Add(".png");

            Windows.Storage.StorageFile file = await picker.PickSingleFileAsync();
            if (file != null) {
                // Application now has read/write access to the picked file
                xResultTB.Text = file.Name;
                using (var fileStream = await file.OpenAsync(Windows.Storage.FileAccessMode.Read)) {
                    BitmapImage b = new BitmapImage();
                    await b.SetSourceAsync(fileStream);
                    xImageResult.Source = b;
                }
            } else {
                xResultTB.Text = "Operation cancelled.";
            }    
        }

        
        /// <summary>
        /// Wrapper for calling getFile when the file picker button is tapped.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void xFilePickBtn_Tapped(object sender, TappedRoutedEventArgs e) {
            getFile();
        }

        private void xImageResult_DragStarting(UIElement sender, DragStartingEventArgs args) {
            args.Data.Properties.Add("image", sender);
        }
    }
}
