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
using Windows.Storage.Pickers;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using Microsoft.Toolkit.Uwp.UI.Controls.TextToolbarButtons;
using Microsoft.Toolkit.Uwp.UI.Controls;
using System.Diagnostics;
using Windows.Storage;
using Windows.Storage.Streams;
using Windows.UI.Xaml.Media.Imaging;


// The User Control item template is documented at https://go.microsoft.com/fwlink/?LinkId=234236

namespace Dash
{
    public sealed partial class MenuToolbar : UserControl
    {
        private Canvas _parentCanvas;

        public MenuToolbar(Canvas canvas)
        {
            this.InitializeComponent();
            _parentCanvas = canvas;
            this.SetUpBaseMenu();
        }

        public void SetKeyboardShortcut()
        {

        }

        UIElement subtoolbarElement = null; // currently active submenu, if null, nothing is selected

        /// <summary>
        /// Updates the toolbar with the data from the current selected. TODO: bindings with this to MainPage.SelectedDocs?
        /// </summary>
        /// <param name="docs"></param>
        public void Update(IEnumerable<DocumentView> docs)
        {
            if (subtoolbarElement != null) subtoolbarElement.Visibility = Visibility.Collapsed;

            // just single select
            if (docs.Count<DocumentView>() == 1)
            {
                // Text controls
                var text = VisualTreeHelperExtensions.GetFirstDescendantOfType<RichEditBox>(docs.First());
                if (text != null)
                {
                    xTextToolbar.SetMenuToolBarBinding(VisualTreeHelperExtensions.GetFirstDescendantOfType<RichEditBox>(docs.First()));
                    subtoolbarElement = xTextToolbar;
                }

				// TODO: Image controls
				var image = VisualTreeHelperExtensions.GetFirstDescendantOfType<Image>(docs.First());
				if (image != null)
				{
					subtoolbarElement = xImageToolbar;
				}

                // TODO: Collection controls  
                
                var col = VisualTreeHelperExtensions.GetFirstDescendantOfType<CollectionView>(docs.First());
                if (col != null)
                {
                    CollectionView thisCollection = VisualTreeHelperExtensions.GetFirstDescendantOfType<CollectionView>(docs.First());
                    subtoolbarElement = xCollectionToolbar;
                }

            }
            else if (docs.Count<DocumentView>() > 1)
            {
                // TODO: multi select
            }
            else {
                subtoolbarElement = null;
            }

                if (subtoolbarElement != null) subtoolbarElement.Visibility = Visibility.Visible;
        }

        private void SetUpBaseMenu()
        {
            _parentCanvas.Children.Add(this);
            Canvas.SetLeft(this, 325);
            Canvas.SetTop(this, 5);
        }

        private void UIElement_OnManipulationDelta(object sender, ManipulationDeltaRoutedEventArgs e)
        {
            var newLatPo = xToolbarTransform.TranslateX + e.Delta.Translation.X;
            var newVertPo = xToolbarTransform.TranslateX + e.Delta.Translation.Y;
            var actualWidth = ((Frame) Window.Current.Content).ActualWidth;
            var actualHeight = ((Frame)Window.Current.Content).ActualHeight;
            if (newLatPo > 0 && newLatPo < actualWidth)
            {
                xToolbarTransform.TranslateX += e.Delta.Translation.X;
            }
            if (newVertPo > 0 && newVertPo < actualHeight)
            {
                xToolbarTransform.TranslateY += e.Delta.Translation.Y;
            }
        }

        private async void AddImage_OnTapped(object sender, TappedRoutedEventArgs e)
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

            var imagesToAdd = await imagePicker.PickMultipleFilesAsync();
            if (imagesToAdd != null)
            {
                foreach (var thisImage in imagesToAdd)
                {
                    //Im
                    //var display = new Image();
                    //Uri source
                    //BitmapImage temporary = new BitmapImage(); 
                    //display.Source = new ImageSource();
                    //thisImage
                }
            }
            else
            {
                //Flash an 'X' over the image selection button
            }

        }
    }
}
