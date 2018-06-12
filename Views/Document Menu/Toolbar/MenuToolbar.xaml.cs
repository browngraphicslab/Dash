using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;
using Windows.Storage.Pickers;
using Windows.UI;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Media;
using CsvHelper.Configuration.Attributes;
using Dash.Views.Document_Menu.Toolbar;
using Microsoft.Toolkit.Uwp.UI.Animations;
using System.Runtime.InteropServices;

// The User Control item template is documented at https://go.microsoft.com/fwlink/?LinkId=234236

namespace Dash
{
    /// <summary>
    /// The top level toolbar in Dash.
    /// </summary>
    public sealed partial class MenuToolbar : UserControl
    {
        // == STATIC ==
        public static MenuToolbar Instance;

        // specifies default left click / tap behavior
        public enum MouseMode
        {
            TakeNote,
            PanFast,
            QuickGroup,
            Ink
        };

        public enum State
        {
            Expanded,
            Collapsed
        }

        public enum Orientation
        {
            Vertical,
            Horizontal
        }

        // == FIELDS == 
        private UIElement subtoolbarElement = null; // currently active submenu, if null, nothing is selected
        private AppBarButton[] docSpecificButtons;
        private ButtonBase[] allButtons;
        private AppBarSeparator[] allSeparators;
        private Canvas _parentCanvas;
        private MouseMode mode;
        private State state;
        private Orientation orientation;

        // == CONSTRUCTORS ==
        /// <summary>
        /// Creates a new Toolbar with the given canvas as reference.
        /// </summary>
        /// <param name="canvas"></param>
        public MenuToolbar(Canvas canvas)
        {
            this.InitializeComponent();


            MenuToolbar.Instance = this;
            _parentCanvas = canvas;
            mode = MouseMode.TakeNote;
            state = State.Expanded;
            orientation = Orientation.Vertical;
            checkedButton = xTouch;
            xToolbar.OverflowButtonVisibility = CommandBarOverflowButtonVisibility.Collapsed;

            //move toolbar to ideal location on start-up
            Loaded += (sender, args) =>
            {
                xFloating.ManipulateControlPosition(325, 10, xToolbar.ActualWidth, xToolbar.ActualHeight);
            };

            // list of buttons that are enabled only if there is 1 or more selected documents
            AppBarButton[] buttons = {xCopy, xDelete};
            docSpecificButtons = buttons;

            //List of all buttons on main menu toolbar - used for collapsing and rotation
            ButtonBase[] tempButtons =
                {xCopy, xDelete, xAddGroup, xAddImage, xAddVideo, xGroup, xInk, xTouch}; //ADD NEW BUTTONS HERE!!!
            allButtons = tempButtons;

            //List of all button separators
            AppBarSeparator[] tempSeparators = {xSepOne, xSepTwo}; //ADD NEW SEPARATORS HERE!!!
            allSeparators = tempSeparators;

            this.SetUpBaseMenu();
            //this.RotateToolbar();
        }

        // == METHODS ==
        /// <summary>
        /// Gets the current MouseMode set by the toolbar.
        /// </summary>
        /// <returns></returns>
        public MouseMode GetMouseMode()
        {
            return mode;
        }

        public State GetState()
        {
            return state;
        }

        public Orientation GetOrientation()
        {
            return orientation;
        }

        /// <summary>
        /// Disables or enables toolbar level document specific icons.
        /// </summary>
        /// <param name="hasDocuments"></param>
        private void toggleSelectOptions(Boolean hasDocuments)
        {
            var o = .5;
            if (hasDocuments) o = 1;
            foreach (var b in docSpecificButtons)
            {
                b.IsEnabled = hasDocuments;
                b.Opacity = o;
            }
        }


        /// <summary>
        /// Updates the toolbar with the data from the current selected. TODO: bindings with this to MainPage.SelectedDocs?
        /// </summary>
        /// <param name="docs"></param>
        public void Update(IEnumerable<DocumentView> docs)
        {
            if (state == State.Expanded)
            {
                if (subtoolbarElement != null) subtoolbarElement.Visibility = Visibility.Collapsed;

                toggleSelectOptions(docs.Count() > 0);

                // just single select
                if (docs.Count() == 1)
                {
	                var selection = docs.First();
                
                    // Image controls
                    var image = VisualTreeHelperExtensions.GetFirstDescendantOfType<Image>(selection);
                    if (image != null)
                    {
                        subtoolbarElement = xImageToolbar;
                        xImageToolbar.SetImageBinding(selection);
                    }

                    // Collection controls  
                    var col = VisualTreeHelperExtensions.GetFirstDescendantOfType<CollectionView>(selection);
                    if (col != null)
                    {
                        CollectionView thisCollection =
                            VisualTreeHelperExtensions.GetFirstDescendantOfType<CollectionView>(selection);
                        subtoolbarElement = xCollectionToolbar;
                    }

                    // Text controls
                    var text = VisualTreeHelperExtensions.GetFirstDescendantOfType<RichEditBox>(selection);
                    if (text != null)
                    {
                        xTextToolbar.SetMenuToolBarBinding(
                            VisualTreeHelperExtensions.GetFirstDescendantOfType<RichEditBox>(selection));
                        xTextToolbar.SetCurrTextBox(text);
	                    xTextToolbar.SetDocs(docs.First());
						subtoolbarElement = xTextToolbar;
                    }

					//Annnotation controls
	                var annot = VisualTreeHelperExtensions.GetFirstDescendantOfType<ImageRegionBox>(selection);
	                if (annot != null)
	                {
		                System.Diagnostics.Debug.WriteLine("IMAGEBOX IS SELECTED");
						
	                }

					//If the user has clicked on valid content (text, image, video, etc)...
					if (subtoolbarElement != null)
                    {
                        xToolbar.IsOpen = false;
                        //If the relevant subtoolbar uses an underlying CommandBar (i.e. and can be closed/opened)
                        if (subtoolbarElement is ICommandBarBased toOpen)
                        {
                            toOpen.CommandBarOpen(true);
                            //Displays padding in stack panel only if the menu isn't collapsed
                            if (state == State.Expanded) xPadding.Visibility = Visibility.Visible;
                        }
                        else
                        {
                            //Currently, the TextSubtoolbar is the only toolbar that can't be opened/closed. Therefore, it doesn't need the additional padding
                            xPadding.Visibility = Visibility.Collapsed;
                        }
                    }
                    else
                    {
                        //If nothing is selected, open/label the main menu toolbar
                        if (state == State.Expanded) xToolbar.IsOpen = true;
                    }
                }
                else if (docs.Count<DocumentView>() > 1)
                {
                    // TODO: multi select
                }
                else
                {
                    subtoolbarElement = null;
                }

                //Displays the subtoolbar element only if it corresponds to a valid subtoolbar and if the menu isn't collapsed
                if (subtoolbarElement != null && state == State.Expanded) subtoolbarElement.Visibility = Visibility.Visible;
            }
            else if (docs.Count<DocumentView>() > 1)
            {
                // TODO: multi select
            }
            else
            {
                subtoolbarElement = null;
            }
			//set proper subtoolbar to visible
	        if (subtoolbarElement != null)
	        {
				xFloating.AdjustPositionForExpansion(ToolbarConstants.ToolbarHeight, 0);
		        subtoolbarElement.Visibility = Visibility.Visible;
				//xFloating.Floating_SizeChanged(null, null);
	        }

        }

        private void SetUpBaseMenu()
        {
            _parentCanvas.Children.Add(this);
        }

        // copy btn
        private void Copy(object sender, RoutedEventArgs e)
        {
            foreach (DocumentView d in MainPage.Instance.GetSelectedDocuments())
            {
                d.CopyDocument();
            }
        }

        // delete btn
        private void Delete(object sender, RoutedEventArgs e)
        {
            var tempDocs = MainPage.Instance.GetSelectedDocuments().ToList<DocumentView>();
            foreach (DocumentView d in tempDocs)
            {
                d.DeleteDocument();
            }
        }


        // controls which MouseMode is currently activated
        AppBarToggleButton checkedButton = null;

        private void AppBarToggleButton_Checked(object sender, RoutedEventArgs e)
        {
            if (checkedButton != sender as AppBarToggleButton)
            {
                AppBarToggleButton temp = checkedButton;
                checkedButton = sender as AppBarToggleButton;
                temp.IsChecked = false;
                if (checkedButton == xTouch) mode = MouseMode.TakeNote;
                else if (checkedButton == xInk) mode = MouseMode.Ink;
                else if (checkedButton == xGroup) mode = MouseMode.PanFast;
            }
        }

        private void AppBarToggleButton_UnChecked(object sender, RoutedEventArgs e)
        {
            AppBarToggleButton toggle = sender as AppBarToggleButton;
            if (toggle == checkedButton)
            {
                checkedButton = xTouch;
                checkedButton.IsChecked = true;
                mode = MouseMode.TakeNote;
            }
        }

        /**
        * When the "Add Image" btn is clicked, this launches an image file picker & adds selected video(s) to the workspace.
       */
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
                    var parser = new ImageToDashUtil();
                    var docController = await parser.ParseFileAsync(thisImage);
                    if (docController != null)
                    {
                        var mainPageCollectionView =
                            MainPage.Instance.MainDocView.GetFirstDescendantOfType<CollectionView>();
                        var where = Util.GetCollectionFreeFormPoint(
                            mainPageCollectionView.CurrentView as CollectionFreeformView, new Point(500, 500));
                        docController.GetPositionField().Data = where;
                        mainPageCollectionView.ViewModel.AddDocument(docController);
                    }
                }
            }
            else
            {
                //Flash an 'X' over the image selection button
            }
        }

        /**
		 * When the "Add Video" btn is clicked, this launches a file picker & adds selected video(s) to the workspace.
		*/
        private async void Add_Video_On_Click(object sender, RoutedEventArgs e)
        {
            //instantiates a file picker, set to open in user's video library
            var picker = new Windows.Storage.Pickers.FileOpenPicker();
            picker.ViewMode = Windows.Storage.Pickers.PickerViewMode.Thumbnail;
            picker.SuggestedStartLocation = Windows.Storage.Pickers.PickerLocationId.VideosLibrary;

            picker.FileTypeFilter.Add(".avi");
            picker.FileTypeFilter.Add(".mp4");
            picker.FileTypeFilter.Add(".wmv");

            //awaits user upload of video 
            var files = await picker.PickMultipleFilesAsync();

            if (files != null)
            {
                foreach (Windows.Storage.StorageFile file in files)
                {
                    //create a doc controller for the video, set position, and add to canvas
                    var docController = await new VideoToDashUtil().ParseFileAsync(file);
                    var mainPageCollectionView =
                        MainPage.Instance.MainDocView.GetFirstDescendantOfType<CollectionView>();
                    var where = Util.GetCollectionFreeFormPoint(
                        mainPageCollectionView.CurrentView as CollectionFreeformView, new Point(500, 500));
                    docController.GetPositionField().Data = where;
                    MainPage.Instance.MainDocView.GetFirstDescendantOfType<CollectionView>().ViewModel.AddDocument(docController);
                }

                //add error message for null file?
            }
        }

        private async void Add_Audio_On_Click(object sender, RoutedEventArgs e)
        {
            //instantiates a file picker, set to open in user's audio library
            var picker = new Windows.Storage.Pickers.FileOpenPicker();
            picker.ViewMode = Windows.Storage.Pickers.PickerViewMode.Thumbnail;
            picker.SuggestedStartLocation = Windows.Storage.Pickers.PickerLocationId.MusicLibrary;

            picker.FileTypeFilter.Add(".mp3");


            //awaits user upload of audio 
            var files = await picker.PickMultipleFilesAsync();

            if (files != null)
            {
                foreach (Windows.Storage.StorageFile file in files)
                {
                    //create a doc controller for the audio, set position, and add to canvas
                    var docController = await new AudioToDashUtil().ParseFileAsync(file);
                    var mainPageCollectionView = MainPage.Instance.MainDocView.GetFirstDescendantOfType<CollectionView>();
                    var where = Util.GetCollectionFreeFormPoint(mainPageCollectionView.CurrentView as CollectionFreeformView, new Point(500, 500));
                    docController.GetPositionField().Data = where;
                    MainPage.Instance.MainDocView.GetFirstDescendantOfType<CollectionView>().ViewModel.AddDocument(docController);
                }
                //add error message for null file?
            }
        }

        private void XCollapse_OnChecked(object sender, RoutedEventArgs e)
        {
            //Toggles vertical/horizontal orientation if shift is pressed
            if (xCollapse.IsShiftPressed())
            {
                xToolbar.IsOpen = true;
                orientation = (orientation == Orientation.Horizontal) ? Orientation.Vertical : Orientation.Horizontal;
                Debug.WriteLine("Toggle Orientation");
            }
            //If not, collapses toolbar and changes its icon
            else
            {
				//xFadeAnimation.Begin();
				state = State.Collapsed;
                xCollapse.Icon = new SymbolIcon(Symbol.FullScreen);
                xCollapse.Label = "";
                ToggleVisibility(Visibility.Collapsed);
                subtoolbarElement = null;
				
            }
        }

        private void XCollapse_OnUnchecked(object sender, RoutedEventArgs e)
        {
            //Expands toolbar and reopens it as long as nothing is selected
            state = State.Expanded;
            xCollapse.Icon = new SymbolIcon(Symbol.BackToWindow);
            xCollapse.Label = "Collapse";
            xCollapse.Background = new SolidColorBrush(Colors.Red);
            ToggleVisibility(Visibility.Visible);
            xToolbar.IsOpen = subtoolbarElement == null;
        }

        private void ToggleVisibility(Visibility status)
        {
            foreach (var b in allButtons)
            {
                b.Visibility = status;
            }

            foreach (var s in allSeparators)
            {
                s.Visibility = status;
            }

            xPadding.Visibility = (status == Visibility.Visible)
                ? ((subtoolbarElement is ICommandBarBased) ? Visibility.Visible : Visibility.Collapsed)
                : status;
            if (subtoolbarElement != null)
            {
                subtoolbarElement.Visibility = status;
                //if (subtoolbarElement is ICommandBarBased toOpen) toOpen.CommandBarOpen(status != Visibility.Collapsed);
            }
        }

        private void RotateToolbar()
        {
            //xStackPanel.RenderTransform.TryTransform();
            xStackPanel.Orientation = Windows.UI.Xaml.Controls.Orientation.Vertical;
        }

        private void XToolbar_OnPointerEntered(object sender, PointerRoutedEventArgs e)
        {
            if (state == State.Expanded && state == State.Collapsed)
            {
                xToolbar.IsOpen = true;
                if (subtoolbarElement != null && subtoolbarElement is ICommandBarBased toClose) toClose.CommandBarOpen(false);
            }
        }

        private void XToolbar_OnPointerExited(object sender, PointerRoutedEventArgs e)
        {
            if (state == State.Expanded && state == State.Collapsed)
            {
                xToolbar.IsOpen = subtoolbarElement == null;
                if (subtoolbarElement != null && subtoolbarElement is ICommandBarBased toOpen) toOpen.CommandBarOpen(true);
            }
        }

	    public void SwitchTheme(bool nightModeOn)
	    {
			
			//toggle night mode styles
		    if (nightModeOn)
		    {
			    xToolbar.Foreground = new SolidColorBrush(Colors.White);
		    }
		    else
		    {
			    xToolbar.Foreground = new SolidColorBrush(Colors.Black);
		    }

		    //ensure toolbar is visible
		    xToolbar.IsEnabled = true;
		    xToolbar.Visibility = Visibility.Visible;

		}
	}

}
