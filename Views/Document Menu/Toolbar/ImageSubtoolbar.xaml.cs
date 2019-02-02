using System;
using System.Linq;
using System.Threading.Tasks;
using Windows.Storage.Pickers;
using Windows.UI;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;

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

        private void AlertModified() => _currentDocController.CaptureNeighboringContext();

        public void SetComboBoxVisibility(Visibility visibility) => xScaleOptionsDropdown.Visibility = visibility;

        private DocumentView _currentDocView;
        private EditableImage _currentImage;
        private DocumentController _currentDocController;


        public ImageSubtoolbar()
        {
            InitializeComponent();
            FormatDropdownMenu();
            SetUpToolTips();
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
        //    xScaleOptionsDropdown.Width = ToolbarConstants.ComboBoxWidth;
        //    xScaleOptionsDropdown.Height = ToolbarConstants.ComboBoxHeight;
        //    xScaleOptionsDropdown.Margin = new Thickness(ToolbarConstants.ComboBoxMarginOpen);
        }

        private void Crop_Click(object sender, RoutedEventArgs e)
        {
            _currentImage.IsCropping = !_currentImage.IsCropping;
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
            ////updates margin to visually account for the change in size
            //xScaleOptionsDropdown.Margin = status ? new Thickness(ToolbarConstants.ComboBoxMarginOpen) : new Thickness(ToolbarConstants.ComboBoxMarginClosed);
        }

        private void Revert_Click(object sender, RoutedEventArgs e)
        {
            _currentImage.Revert();
            AlertModified();
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
                using (UndoManager.GetBatchHandle())
                {
                    _currentDocController.SetField<ImageController>(KeyStore.DataKey,
                    await ImageToDashUtil.GetLocalURI(replacement), true);
                    await _currentImage.ReplaceImage();
                    AlertModified();
                }
            }
        }
        
        /// <summary>
        /// Enables the subtoolbar access to the Document View of the image that was selected on tap.
        /// </summary>
        internal void SetImageBinding(DocumentView selection)
        {
            _currentDocView       = selection;
            _currentImage         = _currentDocView.GetFirstDescendantOfType<EditableImage>();
            _currentDocController = _currentDocView.ViewModel.DocumentController;
	        xToggleAnnotations.IsChecked = _currentImage?.AreAnnotationsVisible;
            var modes = new Stretch[] { Stretch.None, Stretch.Fill, Stretch.Uniform, Stretch.UniformToFill };
            var fillMode = _currentDocController.GetField<TextController>(KeyStore.ImageStretchKey)?.Data ?? "Uniform";
            xScaleOptionsDropdown.SelectedIndex = modes.Select((m) => m.ToString()).ToList().IndexOf(fillMode);
        }

        private async void Rotate_Click(object sender, RoutedEventArgs e)
        {
            if (!_currentImage.IsCropping)
            {
                await _currentImage.Rotate();
                AlertModified();
            }
        }

        private async void VerticalMirror_Click(object sender, RoutedEventArgs e)
        {
            if (!_currentImage.IsCropping)
            {
                await _currentImage.MirrorVertical();
                AlertModified();
            }
        }

        private async void HorizontalMirror_Click(object sender, RoutedEventArgs e)
        {
            if (!_currentImage.IsCropping)
            {
                await _currentImage.MirrorHorizontal();
                AlertModified();
            }
        }

	    private void ToggleAnnotations_Checked(object sender, RoutedEventArgs e)
        {
            _currentImage?.SetRegionVisibility(Visibility.Visible);
            xToggleAnnotations.Label = "Visible";
        }

	    private void ToggleAnnotations_Unchecked(object sender, RoutedEventArgs e)
	    {
            _currentImage?.SetRegionVisibility(Visibility.Collapsed);
            xToggleAnnotations.Label = "Hidden";
        }
        private void XInkToggle_OnChecked(object sender, RoutedEventArgs e)
        {
            _currentImage._annotationOverlay.CurrentAnnotationType = AnnotationType.Ink;
            xInkToggleIcon.Foreground = new SolidColorBrush(Colors.Black);
        }

        private void XInkToggle_Unchecked(object sender, RoutedEventArgs e)
        {
            _currentImage._annotationOverlay.CurrentAnnotationType = AnnotationType.Region;
            xInkToggleIcon.Foreground = new SolidColorBrush(Colors.White);
        }

        private ToolTip _toggle;
        private ToolTip _crop;
        private ToolTip _replace;
        private ToolTip _rotate;
        private ToolTip _hoz;
        private ToolTip _vert;
        private ToolTip _revert;

        private void SetUpToolTips()
        {
            var placementMode = PlacementMode.Bottom;
            const int offset = 5;

            _toggle = new ToolTip()
            {
                Content = "Toggle Annotations",
                Placement = placementMode,
                VerticalOffset = offset
            };
            ToolTipService.SetToolTip(xToggleAnnotations, _toggle);

            _crop = new ToolTip()
            {
                Content = "Crop Image",
                Placement = placementMode,
                VerticalOffset = offset
            };
            ToolTipService.SetToolTip(xCrop, _crop);

            _replace = new ToolTip()
            {
                Content = "Replace Image",
                Placement = placementMode,
                VerticalOffset = offset
            };
            ToolTipService.SetToolTip(xReplace, _replace);

            _rotate = new ToolTip()
            {
                Content = "Rotate",
                Placement = placementMode,
                VerticalOffset = offset
            };
            ToolTipService.SetToolTip(xRotate, _rotate);

            _hoz = new ToolTip()
            {
                Content = "Horizontal Mirror",
                Placement = placementMode,
                VerticalOffset = offset
            };
            ToolTipService.SetToolTip(xHorizontalMirror, _hoz);

            _vert = new ToolTip()
            {
                Content = "Vertical Mirror",
                Placement = placementMode,
                VerticalOffset = offset
            };
            ToolTipService.SetToolTip(xVerticalMirror, _vert);

            _revert = new ToolTip()
            {
                Content = "Revert Image",
                Placement = placementMode,
                VerticalOffset = offset
            };
            ToolTipService.SetToolTip(xRevert, _revert);
        }

        private void ShowAppBarToolTip(object sender, PointerRoutedEventArgs e)
        {
            if (sender is AppBarButton button && ToolTipService.GetToolTip(button) is ToolTip tip)
            {
                tip.IsOpen = true;
            }
            else if (sender is AppBarToggleButton toggleButton && ToolTipService.GetToolTip(toggleButton) is ToolTip toggleTip)
            {
                toggleTip.IsOpen = true;
            }
        }

        private void HideAppBarToolTip(object sender, PointerRoutedEventArgs e)
        {
            if (sender is AppBarButton button && ToolTipService.GetToolTip(button) is ToolTip tip)
            {
                tip.IsOpen = false;
            }
            else if (sender is AppBarToggleButton toggleButton && ToolTipService.GetToolTip(toggleButton) is ToolTip toggleTip)
            {
                toggleTip.IsOpen = false;
            }
        }

        private void xScaleOptionsDropdown_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var modes = new Stretch[] { Stretch.None, Stretch.Fill, Stretch.Uniform, Stretch.UniformToFill };
            _currentDocController?.SetField<TextController>(KeyStore.ImageStretchKey, modes[xScaleOptionsDropdown.SelectedIndex >= 0 ? xScaleOptionsDropdown.SelectedIndex : 0].ToString(), true);
        }

    }
}
