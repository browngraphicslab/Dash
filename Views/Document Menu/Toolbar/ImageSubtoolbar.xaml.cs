using System;
using Windows.Storage.Pickers;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Dash.Views.Document_Menu.Toolbar;

// The User Control item template is documented at https://go.microsoft.com/fwlink/?LinkId=234236

namespace Dash
{
    /**
     * The subtoolbar that appears when an ImageBox is selected.
     */
    public sealed partial class ImageSubtoolbar : UserControl, ICommandBarBased
    {
        private DocumentController _currentDocControl;
        private DocumentView _currentDocView;

        public ImageSubtoolbar()
        {
            InitializeComponent();
            FormatDropdownMenu();
        }

        public void CommandBarOpen(bool status)
        {
            xImageCommandbar.IsOpen = status;
            xImageCommandbar.IsEnabled = true;
            xImageCommandbar.Visibility = Visibility.Visible;
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
            xImageCommandbar.IsOpen = true;
            _currentDocView.OnCropClick?.Invoke();
        }

        private void Replace_Click(object sender, RoutedEventArgs e)
        {
            ReplaceImage();
            xImageCommandbar.IsOpen = true;
        }

        private void Revert_Click(object sender, RoutedEventArgs e)
        {
            xImageCommandbar.IsOpen = true;
            _currentDocView.OnRevert?.Invoke();
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
                _currentDocControl.SetField<ImageController>(KeyStore.DataKey,
                    await ImageToDashUtil.GetLocalURI(replacement), true);
        }

        internal void SetImageBinding(DocumentView selection)
        {
            _currentDocView = selection;
            _currentDocControl = _currentDocView.ViewModel.DocumentController;
        }
    }
}