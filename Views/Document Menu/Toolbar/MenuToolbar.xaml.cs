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
using Windows.UI.Xaml.Data;
using System.Threading.Tasks;
using Windows.UI.Xaml.Media.Imaging;

// The User Control item template is documented at https://go.microsoft.com/fwlink/?LinkId=234236

namespace Dash
{
    /// <summary>
    /// The top level toolbar in Dash, which will always be present on screen. Subtoolbars are added below the main toolbar according to the type of data that was selected. 
    /// </summary>
    public sealed partial class MenuToolbar : UserControl
    {
        public static readonly DependencyProperty OrientationProperty = DependencyProperty.Register(
            "Orientation", typeof(Orientation), typeof(MenuToolbar), new PropertyMetadata(default(Orientation)));

        public Orientation Orientation
        {
            get { return (Orientation)GetValue(OrientationProperty); }
            set { SetValue(OrientationProperty, value); }
        }

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

        public enum Pinned
        {
            Pinned,
            Unpinned
        }

        // == FIELDS == 
        private UIElement subtoolbarElement = null; // currently active submenu, if null, nothing is selected
        private AppBarButton[] docSpecificButtons;
        private ButtonBase[] allButtons;
        private RotateTransform[] buttonRotations;
        private AppBarSeparator[] allSeparators;
        private UIElement _parent;
        private MouseMode mode;
        private State state;
        private Pinned pinned;
        private BitmapImage unpinnedIcon;
        private BitmapImage pinnedIcon;

        // == CONSTRUCTORS ==
        /// <summary>
        /// Creates a new Toolbar with the given canvas as reference.
        /// </summary>
        /// <param name="canvas"></param>
        public MenuToolbar(UIElement parent)
        {
            this.InitializeComponent();


            MenuToolbar.Instance = this;
            _parent = parent;
            mode = MouseMode.TakeNote;
            state = State.Expanded;
            pinned = Pinned.Unpinned;
            checkedButton = xTouch;

            pinnedIcon = new BitmapImage(new Uri("ms-appx:///Assets/pinned.png"));
            unpinnedIcon = new BitmapImage(new Uri("ms-appx:///Assets/unpinned.png"));

            //xPadding.Width = ToolbarConstants.PaddingLong;
            //xPadding.Height = ToolbarConstants.PaddingShort;

            xToolbar.Loaded += (sender, e) => { SetUpOrientationBindings(); };

            //move toolbar to ideal location on start-up
            Loaded += (sender, args) =>
            {
                xFloating.ManipulateControlPosition(325, 10, xToolbar.ActualWidth, xToolbar.ActualHeight);
            };

            // list of buttons that are enabled only if there is 1 or more selected documents
            AppBarButton[] buttons =
            {
                xCopy,
                xDelete
            };
            docSpecificButtons = buttons;

            //List of all buttons on main menu toolbar - used for collapsing and rotation
            //ADD NEW BUTTONS HERE!!!
            ButtonBase[] tempButtons =
            {
                xCopy,
                xDelete,
                xAddGroup,
                xAddImage,
                xAddVideo,
                xGroup,
                xInk,
                xTouch,
                xPin
            }; 
            allButtons = tempButtons;

            //List of all button separators
            //ADD NEW SEPARATORS HERE!!!
            AppBarSeparator[] tempSeparators =
            {
                xSepOne,
                xSepTwo,
                xSepThree
            }; 
            allSeparators = tempSeparators;

            this.SetUpBaseMenu();
            this.AddSecondaryButtonEventHandlers();
        }

        private void AddSecondaryButtonEventHandlers()
        {
            foreach (var b in allButtons)
            {
                b.PointerPressed += (sender, args) =>
                {
                    if (IsAtTop()) xToolbar.IsOpen = (subtoolbarElement == null);
                    if (subtoolbarElement is ICommandBarBased toOpen && state == State.Expanded) toOpen.CommandBarOpen(true);
                };
            }
            foreach (var s in allSeparators)
            {
                s.PointerPressed += (sender, args) =>
                {
                    if (IsAtTop()) xToolbar.IsOpen = (subtoolbarElement == null);
                    if (subtoolbarElement is ICommandBarBased toOpen && state == State.Expanded) toOpen.CommandBarOpen(true);
                };
            }
        }

        private void SetUpOrientationBindings()
        {
            var binding = new Binding
            {
                Mode = BindingMode.OneWay,
                Source = this,
                Path = new PropertyPath(nameof(Orientation))
            };
            var inverseBinding = new Binding
            {
                Mode = BindingMode.OneWay,
                Source = this,
                Path = new PropertyPath(nameof(Orientation)),
                Converter = new OrientationInverter()
        };

            var sp = xToolbar.GetFirstDescendantOfType<StackPanel>();
            sp.SetBinding(StackPanel.OrientationProperty, binding);
            xStackPanel.SetBinding(StackPanel.OrientationProperty, inverseBinding);
            RotateToolbar();
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
                    if (selection.ViewModel.DocumentController.DocumentType.Equals(ImageBox.DocumentType))
                    { 
                        subtoolbarElement = xImageToolbar;
                        xImageToolbar.SetImageBinding(selection);
                    }

                    // Collection controls  
                    if (selection.ViewModel.DocumentController.DocumentType.Equals(CollectionBox.DocumentType))
                    {
                        CollectionView thisCollection = VisualTreeHelperExtensions.GetFirstDescendantOfType<CollectionView>(selection);
                        subtoolbarElement = xCollectionToolbar;
                    }

                    // Text controls
                    if (selection.ViewModel.DocumentController.DocumentType.Equals(RichTextBox.DocumentType))
                    {
                        xTextToolbar.SetMenuToolBarBinding(
                            VisualTreeHelperExtensions.GetFirstDescendantOfType<RichEditBox>(selection));
                        xTextToolbar.SetCurrTextBox(selection.GetFirstDescendantOfType<RichEditBox>());
	                    xTextToolbar.SetDocs(selection);
						subtoolbarElement = xTextToolbar;
                    }

                    // Group controls  
                    if (selection.ViewModel.DocumentController.DocumentType.Equals(BackgroundShape.DocumentType))
                    {
                        xGroupToolbar.SetGroupBinding(selection);
                        subtoolbarElement = xGroupToolbar;
                    }

                    //If the user has clicked on valid content (text, image, video, etc)...
                    if (subtoolbarElement != null)
                    {
                        AdjustComboBoxes();
                        xToolbar.IsOpen = false;
                        //If the relevant subtoolbar uses an underlying CommandBar (i.e. and can be closed/opened)
                        if (subtoolbarElement is ICommandBarBased toOpen)
                        {
                            toOpen.CommandBarOpen(true);
                            //Displays padding in stack panel only if the menu isn't collapsed
                            //if (state == State.Expanded) xPadding.Visibility = Visibility.Visible;
                            var margin = xSubtoolbarStackPanel.Margin;
                            margin.Top = 20;
                            xSubtoolbarStackPanel.Margin = margin;
                        }
                        else
                        {
                            //Currently, the TextSubtoolbar is the only toolbar that can't be opened/closed. Therefore, it doesn't need the additional padding
                            var margin = xSubtoolbarStackPanel.Margin;
                            margin.Top = 7;
                            xSubtoolbarStackPanel.Margin = margin;
                            //xPadding.Visibility = Visibility.Collapsed;
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
                    xToolbar.IsOpen = true;
                    subtoolbarElement = null;
                    //xPadding.Visibility = Visibility.Collapsed;
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
				//if vertical, adjust position for additional height
				if (Orientation == Orientation.Vertical)
				{
					xFloating.AdjustPositionForExpansion(0, xToolbar.ActualWidth);
				}
				//otherwise, adjust position for additional width
				else
				{
					xFloating.AdjustPositionForExpansion(xToolbar.ActualHeight, 0);
				}
				
		        subtoolbarElement.Visibility = Visibility.Visible;
				//xFloating.Floating_SizeChanged(null, null);
	        }
        }

        private void SetUpBaseMenu()
        {
            if (_parent is Grid grid) grid.Children.Add(this);
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
            xToolbar.IsOpen = (subtoolbarElement == null) ? true : IsAtTop();
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
            xToolbar.IsOpen = (subtoolbarElement == null) ? true : IsAtTop();
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
            xToolbar.IsOpen = (subtoolbarElement == null) ? true : IsAtTop();
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

        public bool IsAtTop()
        {
            return (int) xFloating.GetCurrentTop() == 0;
        }

        /**
		 * When the "Add Video" btn is clicked, this launches a file picker & adds selected video(s) to the workspace.
		*/
        private async void Add_Video_On_Click(object sender, RoutedEventArgs e)
        {
            xToolbar.IsOpen = (subtoolbarElement == null) ? true : IsAtTop();
            //instantiates a file picker, set to open in user's video library
            var picker = new FileOpenPicker
            {
                ViewMode = PickerViewMode.Thumbnail,
                SuggestedStartLocation = PickerLocationId.VideosLibrary
            };

            picker.FileTypeFilter.Add(".avi");
            picker.FileTypeFilter.Add(".mp4");
            picker.FileTypeFilter.Add(".wmv");

            //awaits user upload of video 
            var files = await picker.PickMultipleFilesAsync();

            if (files != null)
            {
                foreach (var file in files)
                {
                    //create a doc controller for the video, set position, and add to canvas
                    var docController = await new VideoToDashUtil().ParseFileAsync(file);
                    var mainPageCollectionView = MainPage.Instance.MainDocView.GetFirstDescendantOfType<CollectionView>();
                    var where = Util.GetCollectionFreeFormPoint(mainPageCollectionView.CurrentView as CollectionFreeformView, new Point(500, 500));
                    docController.GetPositionField().Data = where;
                    MainPage.Instance.MainDocView.GetFirstDescendantOfType<CollectionView>().ViewModel.AddDocument(docController);
                }
                //add error message for null file?
            }
        }

		/// <summary>
		/// When the "Add Audio" btn is clicked, this launches a file picker & adds selected audio files to the workspace
		/// </summary>
		private async void Add_Audio_On_Click(object sender, RoutedEventArgs e)
        {
            //instantiates a file picker, set to open in user's audio library
            var picker = new FileOpenPicker
            {
                ViewMode = PickerViewMode.Thumbnail,
                SuggestedStartLocation = PickerLocationId.MusicLibrary
            };

            picker.FileTypeFilter.Add(".mp3");
      

            //awaits user upload of audio 
            var files = await picker.PickMultipleFilesAsync();

            if (files != null)
            {
                foreach (var file in files)
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

        private void RotateToolbar()
        {
            Orientation = (Orientation == Orientation.Vertical) ? Orientation.Horizontal : Orientation.Vertical;
            //Appropriately adds and removes the drop down menus (ComboBoxes) based on the updated orientation
            AdjustComboBoxes();
            xToolbar.IsOpen = (subtoolbarElement == null) ? true : (Orientation == Orientation.Vertical);
        }



        /// <summary>
        /// Toggles orientation of toolbar, currently inactive
        /// </summary>
        private async void ToggleVisibilityAsync(Visibility status)
        {
            //xPadding.Visibility = (status == Visibility.Visible) ? ((subtoolbarElement is ICommandBarBased) ? Visibility.Visible : Visibility.Collapsed) : status;
            if (subtoolbarElement != null) subtoolbarElement.Visibility = status;

			if (state == State.Expanded)
			{
				foreach (var b in allButtons)
				{
				    if (b != xPin)
				    {
				        b.Visibility = status;
				        if (Orientation == Orientation.Horizontal) await Task.Delay(ToolbarConstants.ExpansionDelay);
                    }
				}
				foreach (var s in allSeparators)
				{
					s.Visibility = status;
				}
			} else
			{
				foreach (var s in allSeparators)
				{
					s.Visibility = status;
				}
				foreach (var b in allButtons)
				{
				    if (b != xPin)
				    {
				        b.Visibility = status;
				        if (Orientation == Orientation.Horizontal) await Task.Delay(ToolbarConstants.ExpansionDelay);
				    }
				}
			}
        }

		/// <summary>
		/// Inverts toolbar orientation, currently inactive
		/// </summary>
		private class OrientationInverter : SafeDataToXamlConverter<Orientation, Orientation>
        {
            public override Orientation ConvertDataToXaml(Orientation data, object parameter = null)
            {
                return data == Orientation.Horizontal ? Orientation.Vertical : Orientation.Horizontal;
            }

            public override Orientation ConvertXamlToData(Orientation xaml, object parameter = null)
            {
                return xaml == Orientation.Horizontal ? Orientation.Vertical : Orientation.Horizontal;
            }
        }

        private void AdjustComboBoxes()
        {
            if (subtoolbarElement is ICommandBarBased cmd)
            {
                cmd.GetComboBox().Visibility = (Orientation == Orientation.Horizontal) ? Visibility.Visible : Visibility.Collapsed;
            }
        }

        private void XToolbar_OnPointerEntered(object sender, PointerRoutedEventArgs e)
        {
            Debug.WriteLine(xFloating.GetCurrentTop());
            if (state == State.Expanded && IsAtTop())
            {
                xToolbar.IsOpen = true;
                if (subtoolbarElement is ICommandBarBased toClose)
                {
                    toClose.CommandBarOpen(false);
                }
                else if (subtoolbarElement is TextSubtoolbar txt)
                {
                    var margin = txt.Margin;
                    margin.Top = 12;
                    txt.Margin = margin;
                }
            }
        }

        private void XToolbar_OnPointerExited(object sender, PointerRoutedEventArgs e)
        {
            Debug.WriteLine(xFloating.GetCurrentTop());
            if (state == State.Expanded && IsAtTop())
            {
                xToolbar.IsOpen = subtoolbarElement == null;
                if (subtoolbarElement is ICommandBarBased toClose)
                {
                    toClose.CommandBarOpen(true);
                }
                else if (subtoolbarElement is TextSubtoolbar txt)
                {
                    var margin = txt.Margin;
                    margin.Top = 0;
                    txt.Margin = margin;
                }
            }
        }

		
        private void XCollapse_OnTapped(object sender, TappedRoutedEventArgs e)
        {
            state = (state == State.Expanded) ? State.Collapsed : State.Expanded;
            xCollapse.Label = (state == State.Expanded) ? "Collapse" : "";
            xCollapse.Icon = (state == State.Expanded) ? new SymbolIcon(Symbol.BackToWindow) : new SymbolIcon(Symbol.FullScreen);

            var visibility = (state == State.Expanded) ? Visibility.Visible : Visibility.Collapsed;
            ToggleVisibilityAsync(visibility);

            if (subtoolbarElement == xTextToolbar) xTextToolbar.CloseSubMenu();
            subtoolbarElement = null;
            xToolbar.IsOpen = (state == State.Expanded);

            xFloating.AdjustPositionForExpansion(0, 800 - xToolbar.ActualWidth);
        }

        public void SwitchTheme(bool nightModeOn)
	    {

            //toggle night mode styles
	        xToolbar.Foreground = (nightModeOn) ? new SolidColorBrush(Colors.White) : new SolidColorBrush(Colors.Black);

		    //ensure toolbar is visible
		    xToolbar.IsEnabled = true;
		    xToolbar.Visibility = Visibility.Visible;

		}

        private void MenuToolbar_PointerReleased(object sender, PointerRoutedEventArgs e)
        {
            if ( subtoolbarElement is ICommandBarBased toOpen) toOpen.CommandBarOpen(true);
        }

        private void MenuToolbar_PointerPressed(object sender, PointerRoutedEventArgs e)
        {
            Debug.WriteLine("GAHGHAGHAHGHAHDEHGHDSHSDKH!!!");
        }

        private void XPin_OnClick(object sender, RoutedEventArgs e)
        {
            pinned = (pinned == Pinned.Unpinned) ? Pinned.Pinned : Pinned.Unpinned;
            xFloating.ShouldManipulateChild = (pinned != Pinned.Unpinned);
            xPin.Label = (pinned == Pinned.Unpinned) ? "Unpin" : "Pin";
            xLockIcon.Source = (pinned == Pinned.Unpinned) ? unpinnedIcon : pinnedIcon;
            xToolbar.IsOpen = (state == State.Collapsed) ? false : (subtoolbarElement == null ? true : IsAtTop());
        }
    }
}
