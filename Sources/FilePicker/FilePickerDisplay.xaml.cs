using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Imaging;
using Windows.UI.Xaml.Navigation;

// The User Control item template is documented at http://go.microsoft.com/fwlink/?LinkId=234236

namespace Dash.Sources.FilePicker {
    public sealed partial class FilePickerDisplay : UserControl {
        string fileType; // i.e. image, text, word, pdf-- not the extensions!
        List<string> fileExtensions; // i.e. [png, jpg, tiff]

        public FilePickerDisplay() {
            this.InitializeComponent();
            new ManipulationControls(xGrid, this);

        }

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

        

        private void xFilePickBtn_Tapped(object sender, TappedRoutedEventArgs e) {
            getFile();
        }
    }
}
