﻿using System;
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
using Windows.UI.Xaml.Navigation;

// The User Control item template is documented at https://go.microsoft.com/fwlink/?LinkId=234236

namespace Dash
{
	public sealed partial class TemplateSubtoolbar : UserControl
	{

		public static readonly DependencyProperty OrientationProperty = DependencyProperty.Register(
			"Orientation", typeof(Orientation), typeof(ImageSubtoolbar), new PropertyMetadata(default(Orientation)));

		//orientation binding, currently inactive
		public Orientation Orientation
		{
			get { return (Orientation)GetValue(OrientationProperty); }
			set { SetValue(OrientationProperty, value); }
		}



		private DocumentView _currentDocView;
		private EditableImage _currentImage;
		private DocumentController _currentDocController;

		public TemplateSubtoolbar()
		{
			this.InitializeComponent();
			//FormatDropdownMenu();
			//xToggleAnnotations.IsChecked = false;

			////binds orientation of the subtoolbar to the current orientation of the main toolbar (inactive functionality)
			//xImageCommandbar.Loaded += delegate
			//{
			//    var sp = xImageCommandbar.GetFirstDescendantOfType<StackPanel>();
			//    sp.SetBinding(StackPanel.OrientationProperty, new Binding
			//    {
			//        Source = this,
			//        Path = new PropertyPath(nameof(Orientation)),
			//        Mode = BindingMode.OneWay
			//    });
			//    Visibility = Visibility.Collapsed;
			//};
		}





		///// <summary>
		///// Formats the combo box according to Toolbar Constants.
		///// </summary>
		//private void FormatDropdownMenu()
		//{
		//    xScaleOptionsDropdown.Width = ToolbarConstants.ComboBoxWidth;
		//    xScaleOptionsDropdown.Height = ToolbarConstants.ComboBoxHeight;
		//    xScaleOptionsDropdown.Margin = new Thickness(ToolbarConstants.ComboBoxMarginOpen);
		//}

		///// <summary>
		///// Prevents command bar from hiding labels on click by setting isOpen to true every time it begins to close.
		///// </summary>
		//private void CommandBar_Closing(object sender, object e)
		//{
		//    xImageCommandbar.IsOpen = true;
		//}

		//private void Crop_Click(object sender, RoutedEventArgs e)
		//{
		//    xImageCommandbar.IsOpen = true;
		//    if (_currentImage.IsCropping) return;
		//    _currentImage.StartCrop();
		//}

		///// <summary>
		///// Called when the Replace Button is clicked. Calls on helper method to replace the most recently selected image.
		///// </summary>
		//private async void Replace_Click(object sender, RoutedEventArgs e)
		//{
		//    xImageCommandbar.IsOpen = true;
		//    if (_currentImage.IsCropping) return;
		//    await ReplaceImage();
		//}

		///// <summary>
		///// Toggles open/closed states of this subtoolbar.
		///// </summary>
		//public void CommandBarOpen(bool status)
		//{
		//    xImageCommandbar.IsOpen = status;
		//    xImageCommandbar.IsEnabled = true;
		//    xImageCommandbar.Visibility = Visibility.Visible;
		//    //updates margin to visually account for the change in size
		//    xScaleOptionsDropdown.Margin = status ? new Thickness(ToolbarConstants.ComboBoxMarginOpen) : new Thickness(ToolbarConstants.ComboBoxMarginClosed);
		//}

		//private void Revert_Click(object sender, RoutedEventArgs e)
		//{
		//    xImageCommandbar.IsOpen = true;
		//    _currentImage.Revert();
		//}

		///// <summary>
		///// Helper method for replacing the selected image. Opens file picker and and sets the field of the image controller to the new image's URI.
		///// </summary>
		//private async Task ReplaceImage()
		//{
		//    var imagePicker = new FileOpenPicker
		//    {
		//        ViewMode = PickerViewMode.Thumbnail,
		//        SuggestedStartLocation = PickerLocationId.PicturesLibrary
		//    };
		//    imagePicker.FileTypeFilter.Add(".jpg");
		//    imagePicker.FileTypeFilter.Add(".jpeg");
		//    imagePicker.FileTypeFilter.Add(".bmp");
		//    imagePicker.FileTypeFilter.Add(".png");
		//    imagePicker.FileTypeFilter.Add(".svg");

		//    var replacement = await imagePicker.PickSingleFileAsync();
		//    if (replacement != null)
		//    {
		//        UndoManager.StartBatch();
		//        _currentDocController.SetField<ImageController>(KeyStore.DataKey,
		//            await ImageToDashUtil.GetLocalURI(replacement), true);
		//        await _currentImage.ReplaceImage();
		//        UndoManager.EndBatch();
		//    }
		//}


		///// <summary>
		///// Enables the subtoolbar access to the Document View of the image that was selected on tap.
		///// </summary>
		//internal void SetImageBinding(DocumentView selection)
		//{
		//    _currentDocView = selection;
		//    _currentImage = _currentDocView.GetFirstDescendantOfType<EditableImage>();
		//    _currentDocController = _currentDocView.ViewModel.DocumentController;
		//}
	}
}
