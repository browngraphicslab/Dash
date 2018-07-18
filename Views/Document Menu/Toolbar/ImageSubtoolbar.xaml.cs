using System;
using System.Threading.Tasks;
using Windows.Storage.Pickers;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Data;
using Dash.Views.Document_Menu.Toolbar;

// The User Control item template is documented at https://go.microsoft.com/fwlink/?LinkId=234236

namespace Dash
{
    /// <summary>
    /// The subtoolbar that appears when an ImageBox is selected. Implements ICommandBarBased because it created with a CommandBar.
    /// </summary>
    public sealed partial class ImageSubtoolbar : UserControl, ICommandBarBased
    {
        public static readonly DependencyProperty OrientationProperty = DependencyProperty.Register(
            "Orientation", typeof(Orientation), typeof(ImageSubtoolbar), new PropertyMetadata(default(Orientation)));

        //orientation binding, currently inactive
        public Orientation Orientation
        {
            get { return (Orientation)GetValue(OrientationProperty); }
            set { SetValue(OrientationProperty, value); }
        }

        public void SetComboBoxVisibility(Visibility visibility) => xScaleOptionsDropdown.Visibility = visibility;

        private DocumentView _currentDocView;
        private EditableImage _currentImage;
        private DocumentController _currentDocController;


        public ImageSubtoolbar()
        {
            InitializeComponent();
            FormatDropdownMenu();
	        xToggleAnnotations.IsChecked = false;

            //binds orientation of the subtoolbar to the current orientation of the main toolbar (inactive functionality)
			
            xImageCommandbar.Loaded += delegate
            {
                var sp = xImageCommandbar;
                sp.SetBinding(StackPanel.OrientationProperty, new Binding
                {
                    Source = this,
                    Path = new PropertyPath(nameof(Orientation)),
                    Mode = BindingMode.OneWay
                });
                Visibility = Visibility.Collapsed;
            };
			
        }

        /// <summary>
        /// Formats the combo box according to Toolbar Constants.
        /// </summary>
        private void FormatDropdownMenu()
        {
            xScaleOptionsDropdown.Width = ToolbarConstants.ComboBoxWidth;
            xScaleOptionsDropdown.Height = ToolbarConstants.ComboBoxHeight;
            xScaleOptionsDropdown.Margin = new Thickness(ToolbarConstants.ComboBoxMarginOpen);
        }

        private void Crop_Click(object sender, RoutedEventArgs e)
        {
            if (_currentImage.IsCropping) return;
            _currentImage.StartCrop();
        }

        /// <summary>
        /// Called when the Replace Button is clicked. Calls on helper method to replace the most recently selected image.
        /// </summary>
        private async void Replace_Click(object sender, RoutedEventArgs e)
        {
            if (_currentImage.IsCropping) return;
            await ReplaceImage();
        }

        /// <summary>
        /// Toggles open/closed states of this subtoolbar.
        /// </summary>
        public void CommandBarOpen(bool status)
        {
            xImageCommandbar.Visibility = Visibility.Visible;
            //updates margin to visually account for the change in size
            xScaleOptionsDropdown.Margin = status ? new Thickness(ToolbarConstants.ComboBoxMarginOpen) : new Thickness(ToolbarConstants.ComboBoxMarginClosed);
        }

        private void Revert_Click(object sender, RoutedEventArgs e)
        {
            _currentImage.Revert();
        }

        /// <summary>
        /// Helper method for replacing the selected image. Opens file picker and and sets the field of the image controller to the new image's URI.
        /// </summary>
        private async Task ReplaceImage()
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
                UndoManager.StartBatch();
                _currentDocController.SetField<ImageController>(KeyStore.DataKey,
                    await ImageToDashUtil.GetLocalURI(replacement), true);
                await _currentImage.ReplaceImage();
                UndoManager.EndBatch();
            }
        }


        /// <summary>
        /// Enables the subtoolbar access to the Document View of the image that was selected on tap.
        /// </summary>
        internal void SetImageBinding(DocumentView selection)
        {
            _currentDocView = selection;
            _currentImage = _currentDocView.GetFirstDescendantOfType<EditableImage>();
            _currentDocController = _currentDocView.ViewModel.DocumentController;
	        xToggleAnnotations.IsChecked = _currentImage.AnnotationManager.AreAnnotationsVisible();
        }

        private async void Rotate_Click(object sender, RoutedEventArgs e)
        {
            if (_currentImage.IsCropping) return;
            await _currentImage.Rotate();
        }

        private async void VerticalMirror_Click(object sender, RoutedEventArgs e)
        {
            if (_currentImage.IsCropping) return;
            await _currentImage.MirrorVertical();
        }

        private async void HorizontalMirror_Click(object sender, RoutedEventArgs e)
        {
            if (_currentImage.IsCropping) return;
            await _currentImage.MirrorHorizontal();
        }

	    private void ToggleAnnotations_Checked(object sender, RoutedEventArgs e)
	    {
			_currentImage?.AnnotationManager.ShowRegions();
		    xToggleAnnotations.Label = "Visible";
	    }

	    private void ToggleAnnotations_Unchecked(object sender, RoutedEventArgs e)
	    {
			_currentImage?.AnnotationManager.HideRegions();
		    xToggleAnnotations.Label = "Hidden";
	    }

	}
}