using System;
using System.Collections.Generic;
using System.Linq;
using Windows.Foundation;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;
using Windows.Storage.Pickers;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Media;
using Microsoft.Toolkit.Uwp.UI.Animations;
using Windows.UI.Xaml.Data;
using System.Threading.Tasks;
using Windows.System;
using Windows.UI.Core;
using Windows.UI.Xaml.Media.Imaging;
using Dash.Controllers;
using DashShared;

// The User Control item template is documented at https://go.microsoft.com/fwlink/?LinkId=234236

namespace Dash
{
    /// <summary>
    /// The top level toolbar in Dash, which will always be present on screen. Subtoolbars are added below the main toolbar according to the type of data that was selected. 
    /// </summary>
    public sealed partial class MenuToolbar
    {
        public static readonly DependencyProperty OrientationProperty = DependencyProperty.Register(
            "Orientation", typeof(Orientation), typeof(MenuToolbar), new PropertyMetadata(default(Orientation)));

        public Orientation Orientation
        {
            get => (Orientation)GetValue(OrientationProperty);
            set => SetValue(OrientationProperty, value);
        }

        public static readonly DependencyProperty ExpandColorProperty = DependencyProperty.Register(
            "ExpandColor", typeof(SolidColorBrush), typeof(MenuToolbar), new PropertyMetadata(default(SolidColorBrush)));

        public SolidColorBrush ExpandColor
        {
            get => (SolidColorBrush)GetValue(ExpandColorProperty);
            set => SetValue(ExpandColorProperty, value);
        }

        public static readonly DependencyProperty CollapseColorProperty = DependencyProperty.Register(
            "CollapseColor", typeof(SolidColorBrush), typeof(MenuToolbar), new PropertyMetadata(default(SolidColorBrush)));

        public void ChangeVisibility(bool isVisible)
        {
            //Making the command bar collapsed while it is open causes a bug with making it visible again, so we close it first
            if (!isVisible)
            {
                xToolbar.IsOpen = false;
            }
            Visibility = isVisible ? Visibility.Visible : Visibility.Collapsed;
            if (isVisible)
            {
              //  xToolbar.IsOpen = true;
            }
        }

        public SolidColorBrush CollapseColor
        {
            get => (SolidColorBrush)GetValue(CollapseColorProperty);
            set => SetValue(CollapseColorProperty, value);
        }


        public void SetUndoEnabled(bool enabled)
        {
            xUndo.IsEnabled = enabled;
            xUndo.Opacity = enabled ? 1.0 : 0.5;
        }

        public void SetRedoEnabled(bool enabled)
        {
            xRedo.IsEnabled = enabled;
            xRedo.Opacity = enabled ? 1.0 : 0.5;
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
        private UIElement baseLevelContentToolbar;
        private AppBarButton[] docSpecificButtons;
        private ButtonBase[] allButtons;
        private AppBarSeparator[] allSeparators;
        private MouseMode mode;
        private State state;
        private Pinned pinned;
        private BitmapImage unpinnedIcon;
        private BitmapImage pinnedIcon;
        private bool containsInternalContent;

	    private DocumentType _selectedType = null;

		// == CONSTRUCTORS ==
		/// <summary>
		/// Creates a new Toolbar with the given canvas as reference.
		/// </summary>
		/// <param name="canvas"></param>
		public MenuToolbar()
        {
            InitializeComponent();

            SetUpToolTips();

            MenuToolbar.Instance = this;
            //set enum defaults
            mode = MouseMode.TakeNote;
            state = State.Expanded;
            pinned = Pinned.Unpinned;
            _checkedButton = xTouch;

            pinnedIcon = new BitmapImage(new Uri("ms-appx:///Assets/pinned.png"));
            unpinnedIcon = new BitmapImage(new Uri("ms-appx:///Assets/unpinned.png"));

            //xPadding.Width = ToolbarConstants.PaddingLong;
            //xPadding.Height = ToolbarConstants.PaddingShort;

            xToolbar.Loaded += (sender, e) => { SetUpOrientationBindings(); };
            SelectionManager.SelectionChanged += (sender) => { Update(SelectionManager.GetSelectedDocs()); };

            //move toolbar to ideal location on start-up
            Loaded += (sender, args) =>
            {
                xFloating.ManipulateControlPosition(ToolbarConstants.DefaultXOnLoaded, ToolbarConstants.DefaultYOnLoaded, xToolbar.ActualWidth, xToolbar.ActualHeight);
            };

            // list of buttons that are enabled only if there is 1 or more selected documents
            AppBarButton[] buttons =
            {
                xCopy,
                xDelete,
                xMakeInstance,
                xFitWidth,
                xFitHeight
            };
            docSpecificButtons = buttons;

            //List of all buttons on main menu toolbar - used for collapsing and event handler assignment
            //ADD NEW BUTTONS HERE!!!
            ButtonBase[] tempButtons =
            {
                xCopy,
                xDelete,
                xMakeInstance,
                xFitWidth,
                xFitHeight,
                xAddGroup,
                xAddImage,
                xAddVideo,
                xAddAudio,
                xGroup,
                xInk,
                xTouch,
                xPin,
                xUndo,
                xRedo
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

            AddSecondaryButtonEventHandlers();
        }

        /// <summary>
        /// Adds PointerPressed event handlers to each button & separator so that the subtoolbar opens on click.
        /// </summary>
        private void AddSecondaryButtonEventHandlers()
        {
            foreach (var b in allButtons) { b.PointerPressed += TopBarHoverBehavior; }
            xCollapse.PointerPressed += TopBarHoverBehavior;
            foreach (var s in allSeparators) { s.PointerPressed += TopBarHoverBehavior; }
            xToolbar.PointerPressed += TopBarHoverBehavior;
        }

        private void TopBarHoverBehavior(object sender, PointerRoutedEventArgs e)
        {
            ///if (IsAtTop()) xToolbar.IsOpen = (subtoolbarElement == null);
            if (subtoolbarElement is ICommandBarBased toOpen && state == State.Expanded)
            {
                toOpen.CommandBarOpen(true);
            }
            else if (subtoolbarElement is RichTextSubtoolbar txt)
            {
                var margin = txt.Margin;
                margin.Top = 0;
                txt.Margin = margin;
            }
        }

        /// <summary>
        /// Binds orientation of subtoolbars with that of the main toolbar. Not currently active.
        /// </summary>
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
        public MouseMode GetMouseMode() => mode;

        /// <summary>
        /// Gets the current State of the toolbar.
        /// </summary>
        public State GetState() => state;

        /// <summary>
        /// Disables or enables toolbar level document specific icons.
        /// </summary>
        /// <param name="hasDocuments"></param>
        private void ToggleSelectOptions(bool hasDocuments)
        {
            var o = .5;
            if (hasDocuments) o = 1;
            foreach (AppBarButton b in docSpecificButtons)
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

	            subtoolbarElement = null;
                docs = docs.ToList();
                ToggleSelectOptions(docs.Any());

                // just single select
                if (docs.Count() == 1 )
                {
                    DocumentView selection = docs.First();
	                //_selectedType = selection.ViewModel.DocumentController.DocumentType;

                    if (selection.ViewModel == null) return;

					//Find the type of the selected node and update the subtoolbar binding appropriately.

					// Image controls
					if (selection.ViewModel.DocumentController.DocumentType.Equals(ImageBox.DocumentType))
                    {
                        containsInternalContent = true;
                        baseLevelContentToolbar = xImageToolbar;
                        subtoolbarElement = xImageToolbar;
                        xImageToolbar.SetImageBinding(selection);
                        xGroupToolbar.TryMakeGroupEditable(false);
                    }

                    // Text controls
                    if (selection.ViewModel.DocumentController.DocumentType.Equals(RichTextBox.DocumentType))
                    {
                        containsInternalContent = true;
                        baseLevelContentToolbar = xRichTextToolbar;
                        if (FocusManager.GetFocusedElement() is RichEditBox reb)
                        {
                            xRichTextToolbar.SetMenuToolBarBinding(reb);
                            //give toolbar access to the most recently selected text box for editing purposes
                            xRichTextToolbar.SetCurrTextBox(reb);
                            xRichTextToolbar.SetDocs(selection);
                            subtoolbarElement = xRichTextToolbar;
                            xGroupToolbar.TryMakeGroupEditable(false);
                        }
                    }

                    // Group controls  
                    if (selection.ViewModel.DocumentController.DocumentType.Equals(BackgroundShape.DocumentType))
                    {
                        containsInternalContent = true;
                        baseLevelContentToolbar = xGroupToolbar;
                        xGroupToolbar.SetGroupBinding(selection);
                        xGroupToolbar.TryMakeGroupEditable(true);
                        xGroupToolbar.AcknowledgeAttributes();
                        subtoolbarElement = xGroupToolbar;
                    }

                    // Data box controls
                    if (selection.ViewModel.DocumentController.DocumentType.Equals(DataBox.DocumentType))
                    {
                        DocumentController documentController = selection.ViewModel.DocumentController;
                        var context = new Context(documentController);
                        var data = documentController.GetDereferencedField<FieldControllerBase>(KeyStore.DataKey, context);
                        //switch statement for type of data
                        if (data is ImageController)
                        {
                            containsInternalContent = true;
                            baseLevelContentToolbar = xImageToolbar;
                            subtoolbarElement = xImageToolbar;
                            xImageToolbar.SetImageBinding(selection);
                            xGroupToolbar.TryMakeGroupEditable(false);
                        }
                        else if (data is ListController<DocumentController>)
                        {
                            if (Window.Current.CoreWindow.GetKeyState(VirtualKey.Control).HasFlag(CoreVirtualKeyStates.Down))
                            {
                                if (!containsInternalContent)
                                {
                                    var thisCollection = selection.GetFirstDescendantOfType<CollectionView>();
                                    xCollectionToolbar.SetCollectionBinding(thisCollection, selection.ViewModel.DocumentController);
                                    subtoolbarElement = xCollectionToolbar;
                                }
                                else
                                {
                                    subtoolbarElement = baseLevelContentToolbar;
                                }
                            }
                            else
                            {
                                var thisCollection = selection.GetFirstDescendantOfType<CollectionView>();
                                xCollectionToolbar.SetCollectionBinding(thisCollection, selection.ViewModel.DocumentController);
                                subtoolbarElement = xCollectionToolbar;
                            }
                            xGroupToolbar.TryMakeGroupEditable(false);
                        }
                        else if (data is RichTextController)
                        {
                            containsInternalContent = true;
                            baseLevelContentToolbar = xRichTextToolbar;
                            xRichTextToolbar.SetMenuToolBarBinding(selection.GetFirstDescendantOfType<RichEditBox>());
                            //give toolbar access to the most recently selected text box for editing purposes
                            xRichTextToolbar.SetCurrTextBox(selection.GetFirstDescendantOfType<RichEditBox>());
                            xRichTextToolbar.SetDocs(selection);
                            subtoolbarElement = xRichTextToolbar;
                            xGroupToolbar.TryMakeGroupEditable(false);
                        }
                        else if (data is TextController || data is DateTimeController || data is NumberController)
                        {
                            containsInternalContent = true;
                            baseLevelContentToolbar = xPlainTextToolbar;
                            xPlainTextToolbar.SetMenuToolBarBinding(selection.GetFirstDescendantOfType<TextBox>());
                            //give toolbar access to the most recently selected text box for editing purposes
                            xPlainTextToolbar.SetCurrTextBox(selection.GetFirstDescendantOfType<TextBox>());
                            xPlainTextToolbar.SetDocs(selection);
                            subtoolbarElement = xPlainTextToolbar;
                            xGroupToolbar.TryMakeGroupEditable(false);
                        }
                    }
                    
                    // <------------------- ADD BASE LEVEL CONTENT TYPES ABOVE THIS LINE -------------------> 
                    if (selection.ViewModel.DocumentController.DocumentType.Equals(PdfBox.DocumentType))
                    {
                        containsInternalContent = true;
                        baseLevelContentToolbar = xPdfToolbar;
                        xPdfToolbar.SetPdfBinding(selection);
                        subtoolbarElement = xPdfToolbar;
                        xGroupToolbar.TryMakeGroupEditable(true);
                    }
                    
					// <------------------- ADD BASE LEVEL CONTENT TYPES ABOVE THIS LINE -------------------> 

					// TODO Revisit this when selection is refactored
					// Collection controls  
					if (selection.ViewModel.DocumentController.DocumentType.Equals(CollectionBox.DocumentType))
                    {
                        if (Window.Current.CoreWindow.GetKeyState(VirtualKey.Control).HasFlag(CoreVirtualKeyStates.Down))
                        {
                            if (!containsInternalContent)
                            {
                                var thisCollection = selection.GetFirstDescendantOfType<CollectionView>();
                                xCollectionToolbar.SetCollectionBinding(thisCollection, selection.ViewModel.DocumentController);
                                subtoolbarElement = xCollectionToolbar;
                            }
                            else
                            {
                                subtoolbarElement = baseLevelContentToolbar;
                            }
                        }
                        else
                        {
                            var thisCollection = selection.GetFirstDescendantOfType<CollectionView>();
                            xCollectionToolbar.SetCollectionBinding(thisCollection, selection.ViewModel.DocumentController);
                            subtoolbarElement = xCollectionToolbar;
                        }
                        xGroupToolbar.TryMakeGroupEditable(false);
                    }

                    //Annnotation controls
                    //var annot = VisualTreeHelperExtensions.GetFirstDescendantOfType<RegionBox>(selection);
                    //if (annot != null)
                    //{
                    //    System.Diagnostics.Debug.WriteLine("IMAGEBOX IS SELECTED");

                    //}

                    

                    //If the user has clicked on valid content (text, image, video, etc)...
                    if (subtoolbarElement != null)
                    {
                        AdjustComboBoxes();
                        xToolbar.IsOpen = false;
                        //If the relevant subtoolbar uses an underlying CommandBar (i.e. and can be closed/opened)
                          //Currently, the RichTextSubtoolbar is the only toolbar that can't be opened/closed. Therefore, it doesn't need the additional padding
                            Thickness margin = xSubtoolbarStackPanel.Margin;
                            margin.Top = 7;
                          xSubtoolbarStackPanel.Margin = margin;
                         //xPadding.Visibility = Visibility.Collapsed;
                     
                    }
                    else
                    {
                        //If nothing is selected, open/label the main menu toolbar
                        xToolbar.IsOpen = false;
                        //update margin
                        Thickness margin = xSubtoolbarStackPanel.Margin;
	                    margin.Top = 7;
	                    xSubtoolbarStackPanel.Margin = margin;
                    }

                }
                else if (docs.Count<DocumentView>() > 1)
                {
					// TODO: multi select
					subtoolbarElement = null;
				}
                else
                {
                    subtoolbarElement = null;
                    xGroupToolbar.TryMakeGroupEditable(false);
                }

                //Displays the subtoolbar element only if it corresponds to a valid subtoolbar and if the menu isn't collapsed
                if (subtoolbarElement != null && state == State.Expanded) subtoolbarElement.Visibility = Visibility.Visible;
            }
            else if (docs.Count<DocumentView>() > 1)
            {
                // TODO: multi select
	            subtoolbarElement = null;
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

        // copy btn
        private void Copy(object sender, RoutedEventArgs e)
        {
            foreach (DocumentView d in SelectionManager.GetSelectedDocs())
            {
                d.CopyDocument();
            }
        }
        // copy btn
        private void FitWidth(object sender, RoutedEventArgs e)
        {
            foreach (var d in SelectionManager.GetSelectedDocs())
            {
                if (d.ViewModel.LayoutDocument.GetHorizontalAlignment() == HorizontalAlignment.Stretch)
                    d.ViewModel.LayoutDocument.SetHorizontalAlignment(HorizontalAlignment.Left);
                else d.ViewModel.LayoutDocument.SetHorizontalAlignment(HorizontalAlignment.Stretch);
                d.GetFirstAncestorOfType<CollectionView>().ViewModel.FitContents(null);
            }
        }
        // copy btn
        private void FitHeight(object sender, RoutedEventArgs e)
        {
            foreach (var d in SelectionManager.GetSelectedDocs())
            {
                if (d.ViewModel.LayoutDocument.GetVerticalAlignment() == VerticalAlignment.Stretch)
                    d.ViewModel.LayoutDocument.SetVerticalAlignment(VerticalAlignment.Top);
                else d.ViewModel.LayoutDocument.SetVerticalAlignment(VerticalAlignment.Stretch);
                d.GetFirstAncestorOfType<CollectionView>().ViewModel.FitContents(null);
            }
        }
        // copy btn
        private void MakeInstance(object sender, RoutedEventArgs e)
        {
            foreach (var d in SelectionManager.GetSelectedDocs())
            {
                d.MakeInstance();
            }
        }

        // delete btn
        private void Delete(object sender, RoutedEventArgs e)
        {
            using (UndoManager.GetBatchHandle())
            foreach (DocumentView d in SelectionManager.GetSelectedDocs())
            {
                d.DeleteDocument();
            }
        }
        
        // controls which MouseMode is currently activated
        private AppBarToggleButton _checkedButton;

        private void AppBarToggleButton_Checked(object sender, RoutedEventArgs e)
        {
	        //bool isReadyForOpen = (subtoolbarElement == null && !_selectedType.Equals(PdfBox.DocumentType) &&
	        //                       !_selectedType.Equals(WebBox.DocumentType));
			//xToolbar.IsOpen = isReadyForOpen ? true : IsAtTop();
            if (_checkedButton != sender as AppBarToggleButton)
            {
                AppBarToggleButton temp = _checkedButton;
                _checkedButton = sender as AppBarToggleButton;
                temp.IsChecked = false;
                if (_checkedButton == xTouch) mode = MouseMode.TakeNote;
                else if (_checkedButton == xInk) mode = MouseMode.Ink;
                else if (_checkedButton == xGroup) mode = MouseMode.PanFast;
            }

	        xToolbar.IsOpen = true;
        }

        private void AppBarToggleButton_UnChecked(object sender, RoutedEventArgs e)
        {
			//bool isReadyForOpen = (subtoolbarElement == null && !_selectedType.Equals(PdfBox.DocumentType) &&
			//                       !_selectedType.Equals(WebBox.DocumentType));
	       // xToolbar.IsOpen = isReadyForOpen ? true : IsAtTop();
		
			AppBarToggleButton toggle = sender as AppBarToggleButton;
            if (toggle == _checkedButton)
            {
                _checkedButton = xTouch;
                _checkedButton.IsChecked = true;
                mode = MouseMode.TakeNote;
            }

	        xToolbar.IsOpen = true;
        }

        /// <summary>
        /// When the "Add Image" btn is clicked, this launches an image file picker & adds selected video(s) to the workspace.
        /// </summary>
        private async void AddImage_OnTapped(object sender, TappedRoutedEventArgs e)
         {
			//opens file picker and limits search by listed image extensions
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

            //adds each image selected to Dash
            var imagesToAdd = await imagePicker.PickMultipleFilesAsync();

            //TODO just add new images to docs list instead of going through mainPageCollectionView
            //var docs = MainPage.Instance.MainDocument.GetDataDocument().GetDereferencedField<ListController<DocumentController>>(KeyStore.DataKey, null);
            if (imagesToAdd != null)
            {
                foreach (var thisImage in imagesToAdd)
                {
                    var parser = new ImageToDashUtil();
                    var docController = await parser.ParseFileAsync(thisImage);
                    if (docController == null) continue;

                    var mainPageCollectionView = SplitFrame.ActiveFrame.GetFirstDescendantOfType<CollectionView>();
                    var where = Util.GetCollectionFreeFormPoint(mainPageCollectionView.CurrentView as CollectionFreeformBase, new Point(500, 500));
                    docController.GetPositionField().Data = @where;
                    mainPageCollectionView.ViewModel.AddDocument(docController);
                }
            }
            else
            {
                //Flash an 'X' over the image selection button
            }

	        xToolbar.IsOpen = true;
		}

        /// <summary>
        /// Used to check if the toolbar is currently located at the top of screen, for UI purposes.
        /// </summary>
        public bool IsAtTop()
        {
            return (int)xFloating.GetCurrentTop() == 0;
        }

        /// <summary>
        /// When the "Add Video" btn is clicked, this launches a file picker & adds selected video(s) to the workspace.
        /// </summary>
        private async void Add_Video_On_Click(object sender, RoutedEventArgs e)
        {
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

            //TODO just add new images to docs list instead of going through mainPageCollectionView
            //var docs = MainPage.Instance.MainDocument.GetDataDocument().GetDereferencedField<ListController<DocumentController>>(KeyStore.DataKey, null);
            if (files != null)
            {
                foreach (var file in files)
                {
                    //create a doc controller for the video, set position, and add to canvas
                    var docController = await new VideoToDashUtil().ParseFileAsync(file);
                    var mainPageCollectionView = SplitFrame.ActiveFrame.GetFirstDescendantOfType<CollectionView>();
                    var where = Util.GetCollectionFreeFormPoint(mainPageCollectionView.CurrentView as CollectionFreeformBase, new Point(500, 500));
                    docController.GetPositionField().Data = where;
                    docController.GetDataDocument().SetTitle(file.Name);
                    mainPageCollectionView.ViewModel.AddDocument(docController);
                }

                //add error message for null file?
            }
	        xToolbar.IsOpen = true;
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

            //TODO just add new images to docs list instead of going through mainPageCollectionView
            //var docs = MainPage.Instance.MainDocument.GetDataDocument().GetDereferencedField<ListController<DocumentController>>(KeyStore.DataKey, null);
            if (files != null)
            {
                foreach (var file in files)
                {
                    //create a doc controller for the audio, set position, and add to canvas
                    var docController = await new AudioToDashUtil().ParseFileAsync(file);
                    var mainPageCollectionView = SplitFrame.ActiveFrame.GetFirstDescendantOfType<CollectionView>();
                    var where = Util.GetCollectionFreeFormPoint(mainPageCollectionView.CurrentView as CollectionFreeformView, new Point(500, 500));
                    docController.GetPositionField().Data = where;
                    docController.GetDataDocument().SetTitle(file.Name);
                    mainPageCollectionView.ViewModel.AddDocument(docController);
                }
                //add error message for null file?
            }

	        xToolbar.IsOpen = true;
		}

	    private void Add_Group_On_Click(object sender, RoutedEventArgs e)
	    {
			//create and add group to workspace
		    var mainPageCollectionView = SplitFrame.ActiveFrame.GetFirstDescendantOfType<CollectionView>();
			var where = Util.GetCollectionFreeFormPoint(mainPageCollectionView.CurrentView as CollectionFreeformView, new Point(500, 500));

			mainPageCollectionView.ViewModel.AddDocument(Util.AdornmentWithPosition(BackgroundShape.AdornmentShape.Rectangular, where, 500, 500));
		}



		/// <summary>
		/// Rotates the internal stack panel of the command bar. Currently inactive.
		/// </summary>
		private void RotateToolbar()
        {
            Orientation = (Orientation == Orientation.Vertical) ? Orientation.Horizontal : Orientation.Vertical;
            //Appropriately adds and removes the drop down menus (ComboBoxes) based on the updated orientation
            AdjustComboBoxes();
            //xToolbar.IsOpen = (subtoolbarElement == null) ? true : (Orientation == Orientation.Vertical);
        }

        /// <summary>
        /// Toggles state of toolbar between collapsed and visible.  
        /// </summary>
        private async void ToggleVisibilityAsync(Visibility status)
        {
            //xPadding.Visibility = (status == Visibility.Visible) ? ((subtoolbarElement is ICommandBarBased) ? Visibility.Visible : Visibility.Collapsed) : status;
            if (subtoolbarElement != null) subtoolbarElement.Visibility = status;

            //if toolbar is currently expanded, loop through each button to alter their visibility
            if (state == State.Expanded)
            {
				
                foreach (var b in allButtons.Except(new List<FrameworkElement>{ xTouch, xInk, xGroup, xAddGroup, xAddVideo, xAddAudio}))
                {
                    if (b != xPin)
                    {
                        b.Visibility = status;
                        //adds a slight delay to the addition of buttons to simulate an animation
                        if (Orientation == Orientation.Horizontal) await Task.Delay(ToolbarConstants.ExpansionDelay);
                    }
                } //do same for separators
                foreach (var s in allSeparators)
                {
                    s.Visibility = status;
                }
	            
	            // get proper subtoolbar
	            Update(SelectionManager.GetSelectedDocs());
			}
            else
            {
                //otherwise, it is about to expand. In this case, update visibility of separators before buttons
                foreach (var s in allSeparators)
                {
                    s.Visibility = status;
                }
                foreach (var b in allButtons.Except(new List<FrameworkElement>{ xTouch, xInk, xGroup, xAddGroup, xAddVideo, xAddAudio}))
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
        /// Inverts toolbar orientation, currently inactive.
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

        /// <summary>
        /// Toggles orientation of any combo boxes on any subtoolbar.
        /// </summary>
        private void AdjustComboBoxes()
        {
            if (subtoolbarElement is ICommandBarBased cmd) cmd.SetComboBoxVisibility(Orientation == Orientation.Horizontal ? Visibility.Visible : Visibility.Collapsed);
        }

        /// <summary>
        /// Opens toolbar when user's pointer hovers over it.
        /// </summary>
        private void XToolbar_OnPointerEntered(object sender, PointerRoutedEventArgs e)
        {
            if (state == State.Expanded && IsAtTop())
            {
                xToolbar.IsOpen = true;
                //if subtoolbar is a command bar, open it normally
                if (subtoolbarElement is ICommandBarBased toClose)
                {
                    toClose.CommandBarOpen(false);
                }
                //else, if it is a text toolbar (which doesn't expand), update margins accordingly
                else if (subtoolbarElement is RichTextSubtoolbar txt)
                {
                    var margin = txt.Margin;
                    margin.Top = 13;
                    txt.Margin = margin;
                }
            }
			xToolbar.IsOpen = true;
        }

        /// <summary>
        /// Closes toolbar when user's pointer leaves its area.
        /// </summary>
        private void XToolbar_OnPointerExited(object sender, PointerRoutedEventArgs e)
        {
            if (state == State.Expanded && IsAtTop())
            {
                //main toolbar should only be open when there is no currently active subtoolbar present
                xToolbar.IsOpen = subtoolbarElement == null;
                //close any command bar - based toolbar, and update margins for texttoolbar accordingly
                if (subtoolbarElement is ICommandBarBased toClose)
                {
                    toClose.CommandBarOpen(true);
                }
                else if (subtoolbarElement is RichTextSubtoolbar txt)
                {
                    var margin = txt.Margin;
                    margin.Top = 0;
                    txt.Margin = margin;
                }
            }
	        xToolbar.IsOpen = false;
        }

        /// <summary>
        /// When the Collapse button is pressed, the visibility of the buttons are toggled accordingly. This enables the user control over the size of the toolbar.
        /// </summary>
        private void XCollapse_OnTapped(object sender, TappedRoutedEventArgs e)
        {
            //toggle state enum and update label & icon
            state = (state == State.Expanded) ? State.Collapsed : State.Expanded;
            //xCollapse.Label = (state == State.Expanded) ? "Collapse" : "";
            //xCollapseIcon.Text = (state == State.Expanded) ? "&#xE1D8;" : "&#xE1D9;";

            if (xOpenIcon.Visibility == Visibility.Visible)
            {
                xOpenIcon.Visibility = Visibility.Collapsed;
                xCollapseIcon.Visibility = Visibility.Visible;
                if (ToolTipService.GetToolTip(xCollapse) is ToolTip tip) tip.Content = "Collapse Toolbar";
            }
            else
            {
                xOpenIcon.Visibility = Visibility.Visible;
                xCollapseIcon.Visibility = Visibility.Collapsed;
                if (ToolTipService.GetToolTip(xCollapse) is ToolTip tip) tip.Content = "Expand Toolbar";
            }
            
            var backgroundBinding = new Binding
            {
                Source = this,
                Mode = BindingMode.OneWay,
                Path = new PropertyPath(state == State.Expanded ? nameof(ExpandColor) : nameof(CollapseColor))
            };

            xCollapse.SetBinding(BackgroundProperty, backgroundBinding);

            //toggle visibility
            var visibility = (state == State.Expanded) ? Visibility.Visible : Visibility.Collapsed;
            ToggleVisibilityAsync(visibility);

            //set subtoolbar element to null if collapsing toolbar
            if (subtoolbarElement == xRichTextToolbar) xRichTextToolbar.CloseSubMenu();
            subtoolbarElement = null;
            //adjust toolbar's position to account for the change in size
            xFloating.AdjustPositionForExpansion(0, ToolbarConstants.ToolbarExpandedWidth - xToolbar.ActualWidth);

	        xToolbar.IsOpen = true;
		}

        /// <summary>
        /// Updates UI of toolbar when Night Mode is active in order to remain readable.
        /// </summary>
        public void SwitchTheme(bool nightModeOn)
        {
            //toggle night mode styles
            //xToolbar.Foreground = (nightModeOn) ? new SolidColorBrush(Colors.White) : new SolidColorBrush(Colors.Black);
            xToolbar.RequestedTheme = ElementTheme.Light;
        }

        /// <summary>
        /// Toggles toolbar locked-state when pin button is clicked.
        /// </summary>
        private void XPin_OnClick(object sender, RoutedEventArgs e)
        {
            pinned = (pinned == Pinned.Unpinned) ? Pinned.Pinned : Pinned.Unpinned;
            //disables movement by updating Floating's boolean
            xFloating.ShouldManipulateChild = (pinned == Pinned.Unpinned);
            //update label & icon accordingly
            //xPin.Label = (pinned == Pinned.Unpinned) ? "Floating" : "Anchored";
            
            // xToolbar.IsOpen = (state == State.Collapsed) ? false : (subtoolbarElement == null ? true : IsAtTop());

            if (pinned == Pinned.Pinned)
            {
                var centX = (float) xLockIcon.ActualWidth / 2;
                var centY = (float) xLockIcon.ActualHeight / 2;
                xLockIcon.Rotate(value: -45.0f, centerX: centX, centerY: centY, duration: 300, delay: 0,
                    easingType: EasingType.Default).Start();
            }
            else
            {
                var centX = (float)xLockIcon.ActualWidth / 2;
                var centY = (float)xLockIcon.ActualHeight / 2;
                xLockIcon.Rotate(value: 0.0f, centerX: centX, centerY: centY, duration: 300, delay: 0,
                    easingType: EasingType.Default).Start();
            }
            xToolbar.IsOpen = true;
        }

        public void TempFreeze(bool mobile) { xFloating.ShouldManipulateChild = (mobile) ? true : pinned == Pinned.Unpinned; }

        private ToolTip _pin;
        private ToolTip _collapse;
        private ToolTip _select;
        private ToolTip _ink;
        private ToolTip _quickPan;
        private ToolTip _addGroup;
        private ToolTip _addImage;
        private ToolTip _addVideo;
        private ToolTip _addAudio;
        private ToolTip _copy;
        private ToolTip _instance;
        private ToolTip _fitWidth;
        private ToolTip _fitHeight;
        private ToolTip _delete;
        private ToolTip _undo;
        private ToolTip _redo;
        private ToolTip _presentation;
        private ToolTip _export;

        private void SetUpToolTips()
        {
            var placementMode = PlacementMode.Top;
            const int offset = 5;

            _pin = new ToolTip()
            {
                Content = "Pin Toolbar",
                Placement = placementMode,
                VerticalOffset = offset
            };
            ToolTipService.SetToolTip(xPin, _pin);

            _collapse = new ToolTip()
            {
                Content = "Collapse Toolbar",
                Placement = placementMode,
                VerticalOffset = offset
            };
            ToolTipService.SetToolTip(xCollapse, _collapse);

            _select = new ToolTip()
            {
                Content = "Select",
                Placement = placementMode,
                VerticalOffset = offset
            };
            ToolTipService.SetToolTip(xTouch, _select);

            _ink = new ToolTip()
            {
                Content = "Ink",
                Placement = placementMode,
                VerticalOffset = offset
            };
            ToolTipService.SetToolTip(xInk, _ink);

            _quickPan = new ToolTip()
            {
                Content = "Quick Pan",
                Placement = placementMode,
                VerticalOffset = offset
            };
            ToolTipService.SetToolTip(xGroup, _quickPan);

            _addGroup = new ToolTip()
            {
                Content = "Add Group",
                Placement = placementMode,
                VerticalOffset = offset
            };
            ToolTipService.SetToolTip(xAddGroup, _addGroup);

            _addImage = new ToolTip()
            {
                Content = "Add Image",
                Placement = placementMode,
                VerticalOffset = offset
            };
            ToolTipService.SetToolTip(xAddImage, _addImage);

            _addVideo = new ToolTip()
            {
                Content = "Add Video",
                Placement = placementMode,
                VerticalOffset = offset
            };
            ToolTipService.SetToolTip(xAddVideo, _addVideo);

            _addAudio = new ToolTip()
            {
                Content = "Add Audio",
                Placement = placementMode,
                VerticalOffset = offset
            };
            ToolTipService.SetToolTip(xAddAudio, _addAudio);

            _instance = new ToolTip()
            {
                Content = "Instance",
                Placement = placementMode,
                VerticalOffset = offset
            };
            ToolTipService.SetToolTip(xMakeInstance, _instance);

            _fitWidth = new ToolTip()
            {
                Content = "Fit Width",
                Placement = placementMode,
                VerticalOffset = offset
            };

            ToolTipService.SetToolTip(xFitWidth, _fitWidth);
            _fitHeight = new ToolTip()
            {
                Content = "Fit Height",
                Placement = placementMode,
                VerticalOffset = offset
            };
            ToolTipService.SetToolTip(xFitHeight, _fitHeight);

            _copy = new ToolTip()
            {
                Content = "Copy",
                Placement = placementMode,
                VerticalOffset = offset
            };
            ToolTipService.SetToolTip(xCopy, _copy);

            _delete = new ToolTip()
            {
                Content = "Delete",
                Placement = placementMode,
                VerticalOffset = offset
            };
            ToolTipService.SetToolTip(xDelete, _delete);

            _undo = new ToolTip()
            {
                Content = "Undo",
                Placement = placementMode,
                VerticalOffset = offset
            };
            ToolTipService.SetToolTip(xUndo, _undo);

            _redo = new ToolTip()
            {
                Content = "Redo",
                Placement = placementMode,
                VerticalOffset = offset
            };
            ToolTipService.SetToolTip(xRedo, _redo);

            _export = new ToolTip()
            {
                Content = "Export Workspace",
                Placement = placementMode,
                VerticalOffset = offset
            };
            ToolTipService.SetToolTip(xExport, _export);

            _presentation = new ToolTip()
            {
                Content = "Presentation Mode",
                Placement = placementMode,
                VerticalOffset = offset
            };
            ToolTipService.SetToolTip(xPresentationMode, _presentation);
        }

        private void xRedo_Click(object sender, RoutedEventArgs e)
        {
            UndoManager.RedoOccured();
        }

        private void xUndo_Click(object sender, RoutedEventArgs e)
        {
            UndoManager.UndoOccured();
        }

        private void ShowAppBarToolTip(object sender, PointerRoutedEventArgs e)
        {
            if (sender is AppBarButton button && ToolTipService.GetToolTip(button) is ToolTip tip) tip.IsOpen = true;
            else if (sender is AppBarToggleButton toggleButton && ToolTipService.GetToolTip(toggleButton) is ToolTip toggleTip) toggleTip.IsOpen = true;
        }

        private void HideAppBarToolTip(object sender, PointerRoutedEventArgs e)
        {
            if (sender is AppBarButton button && ToolTipService.GetToolTip(button) is ToolTip tip) tip.IsOpen = false;
            else if (sender is AppBarToggleButton toggleButton && ToolTipService.GetToolTip(toggleButton) is ToolTip toggleTip) toggleTip.IsOpen = false;
        }

        private void XExport_OnClick(object sender, RoutedEventArgs e)
        {
           MainPage.Instance.xMainTreeView.MakePdf_OnTapped(sender, null);
        }

        private void XPresentationMode_OnClick(object sender, RoutedEventArgs e)
        {
           MainPage.Instance.xMainTreeView.TogglePresentationMode(sender, null);
        }

        private void XSplitVertical_OnClick(object sender, RoutedEventArgs e)
        {
            SplitFrame.ActiveFrame.TrySplit(SplitFrame.SplitDirection.Left, SplitFrame.ActiveFrame.DocumentController, true);
        }

        private void XSplitHorizontal_OnClick(object sender, RoutedEventArgs e)
        {
            SplitFrame.ActiveFrame.TrySplit(SplitFrame.SplitDirection.Down, SplitFrame.ActiveFrame.DocumentController, true);
        }

        private void XCloseSplit_OnClick(object sender, RoutedEventArgs e)
        {
            SplitFrame.ActiveFrame.Delete();
        }

        private void XGoBack_OnClick(object sender, RoutedEventArgs e)
        {
            SplitFrame.ActiveFrame.GoBack();
        }

        private void XGoForward_OnClick(object sender, RoutedEventArgs e)
        {
            SplitFrame.ActiveFrame.GoForward();
        }
    }
}
