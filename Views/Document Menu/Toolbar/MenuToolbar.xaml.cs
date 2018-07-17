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
using Windows.System;
using Windows.UI.Core;
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

        public static readonly DependencyProperty ExpandColorProperty = DependencyProperty.Register(
            "ExpandColor", typeof(SolidColorBrush), typeof(MenuToolbar), new PropertyMetadata(default(SolidColorBrush)));

        public SolidColorBrush ExpandColor
        {
            get { return (SolidColorBrush)GetValue(ExpandColorProperty); }
            set { SetValue(ExpandColorProperty, value); }
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
                xToolbar.IsOpen = true;
            }
        }

        public SolidColorBrush CollapseColor
        {
            get { return (SolidColorBrush)GetValue(CollapseColorProperty); }
            set { SetValue(CollapseColorProperty, value); }
        }


        public void SetUndoEnabled(bool enabled)
        {
            xUndo.IsEnabled = enabled;
            if (enabled)
            {
                xUndo.Opacity = 1.0;
            }
            else
            {
                xUndo.Opacity = 0.5;
            }
        }

        public void SetRedoEnabled(bool enabled)
        {
            xRedo.IsEnabled = enabled;
            if (enabled)
            {
                xRedo.Opacity = 1.0;
            }
            else
            {
                xRedo.Opacity = 0.5;
            }
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

        // == CONSTRUCTORS ==
        /// <summary>
        /// Creates a new Toolbar with the given canvas as reference.
        /// </summary>
        /// <param name="canvas"></param>
        public MenuToolbar()
        {
            InitializeComponent();

            MenuToolbar.Instance = this;
            //set enum defaults
            mode = MouseMode.TakeNote;
            state = State.Expanded;
            pinned = Pinned.Unpinned;
            checkedButton = xTouch;

            pinnedIcon = new BitmapImage(new Uri("ms-appx:///Assets/pinned.png"));
            unpinnedIcon = new BitmapImage(new Uri("ms-appx:///Assets/unpinned.png"));

            //xPadding.Width = ToolbarConstants.PaddingLong;
            //xPadding.Height = ToolbarConstants.PaddingShort;

            xToolbar.Loaded += (sender, e) => { SetUpOrientationBindings(); };
            SelectionManager.SelectionChanged += (sender) => { Update(SelectionManager.SelectedDocs); };

            //move toolbar to ideal location on start-up
            Loaded += (sender, args) =>
            {
                xFloating.ManipulateControlPosition(ToolbarConstants.DefaultXOnLoaded, ToolbarConstants.DefaultYOnLoaded, xToolbar.ActualWidth, xToolbar.ActualHeight);
            };

            // list of buttons that are enabled only if there is 1 or more selected documents
            AppBarButton[] buttons =
            {
                xCopy,
                xDelete
            };
            docSpecificButtons = buttons;

            //List of all buttons on main menu toolbar - used for collapsing and event handler assignment
            //ADD NEW BUTTONS HERE!!!
            ButtonBase[] tempButtons =
            {
                xCopy,
                xDelete,
                xAddGroup,
                xAddImage,
                xAddVideo,
                xAddAudio,
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
            if (IsAtTop()) xToolbar.IsOpen = (subtoolbarElement == null);
            if (subtoolbarElement is ICommandBarBased toOpen && state == State.Expanded)
            {
                toOpen.CommandBarOpen(true);
            }
            else if (subtoolbarElement is TextSubtoolbar txt)
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
        public MouseMode GetMouseMode()
        {
            return mode;
        }

        /// <summary>
        /// Gets the current State of the toolbar.
        /// </summary>
        public State GetState()
        {
            return state;
        }

        /// <summary>
        /// Disables or enables toolbar level document specific icons.
        /// </summary>
        /// <param name="hasDocuments"></param>
        private void ToggleSelectOptions(Boolean hasDocuments)
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

                ToggleSelectOptions(docs.Count() > 0);

                // just single select
                if (docs.Count() == 1)
                {
                    var selection = docs.First();

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
                        baseLevelContentToolbar = xTextToolbar;
                        if (FocusManager.GetFocusedElement() is RichEditBox reb)
                        {
                            xTextToolbar.SetMenuToolBarBinding(reb);
                            //give toolbar access to the most recently selected text box for editing purposes
                            xTextToolbar.SetCurrTextBox(reb);
                            xTextToolbar.SetDocs(selection);
                            subtoolbarElement = xTextToolbar;
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
                                var thisCollection = VisualTreeHelperExtensions.GetFirstDescendantOfType<CollectionView>(selection);
                                xCollectionToolbar.SetCollectionBinding(thisCollection);
                                subtoolbarElement = xCollectionToolbar;
                            }
                            else
                            {
                                subtoolbarElement = baseLevelContentToolbar;
                            }
                        }
                        else
                        {
                            var thisCollection = VisualTreeHelperExtensions.GetFirstDescendantOfType<CollectionView>(selection);
                            xCollectionToolbar.SetCollectionBinding(thisCollection);
                            subtoolbarElement = xCollectionToolbar;
                        }
                        xGroupToolbar.TryMakeGroupEditable(false);
                    }

                    //Annnotation controls
                    var annot = VisualTreeHelperExtensions.GetFirstDescendantOfType<RegionBox>(selection);
                    if (annot != null)
                    {
                        System.Diagnostics.Debug.WriteLine("IMAGEBOX IS SELECTED");

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
                    xGroupToolbar.TryMakeGroupEditable(false);
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

        // copy btn
        private void Copy(object sender, RoutedEventArgs e)
        {
            foreach (DocumentView d in SelectionManager.SelectedDocs)
            {
                d.CopyDocument();
            }
        }

        // delete btn
        private void Delete(object sender, RoutedEventArgs e)
        {
            foreach (DocumentView d in SelectionManager.SelectedDocs)
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

        /// <summary>
        /// When the "Add Image" btn is clicked, this launches an image file picker & adds selected video(s) to the workspace.
        /// </summary>
        private async void AddImage_OnTapped(object sender, TappedRoutedEventArgs e)
        {
            xToolbar.IsOpen = (subtoolbarElement == null) ? true : IsAtTop();
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

                    var mainPageCollectionView = MainPage.Instance.MainDocView.GetFirstDescendantOfType<CollectionView>();
                    var where = Util.GetCollectionFreeFormPoint(mainPageCollectionView.CurrentView as CollectionFreeformBase, new Point(500, 500));
                    docController.GetPositionField().Data = @where;
                    mainPageCollectionView.ViewModel.AddDocument(docController);
                }
            }
            else
            {
                //Flash an 'X' over the image selection button
            }
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

            //TODO just add new images to docs list instead of going through mainPageCollectionView
            //var docs = MainPage.Instance.MainDocument.GetDataDocument().GetDereferencedField<ListController<DocumentController>>(KeyStore.DataKey, null);
            if (files != null)
            {
                foreach (var file in files)
                {
                    //create a doc controller for the video, set position, and add to canvas
                    var docController = await new VideoToDashUtil().ParseFileAsync(file);
                    var mainPageCollectionView = MainPage.Instance.MainDocView.GetFirstDescendantOfType<CollectionView>();
                    var where = Util.GetCollectionFreeFormPoint(mainPageCollectionView.CurrentView as CollectionFreeformBase, new Point(500, 500));
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
            xToolbar.IsOpen = (subtoolbarElement == null) ? true : IsAtTop();

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
                    var mainPageCollectionView = MainPage.Instance.MainDocView.GetFirstDescendantOfType<CollectionView>();
                    var where = Util.GetCollectionFreeFormPoint(mainPageCollectionView.CurrentView as CollectionFreeformView, new Point(500, 500));
                    docController.GetPositionField().Data = where;
                    MainPage.Instance.MainDocView.GetFirstDescendantOfType<CollectionView>().ViewModel.AddDocument(docController);
                }
                //add error message for null file?
            }
        }

        /// <summary>
        /// Rotates the internal stack panel of the command bar. Currently inactive.
        /// </summary>
        private void RotateToolbar()
        {
            Orientation = (Orientation == Orientation.Vertical) ? Orientation.Horizontal : Orientation.Vertical;
            //Appropriately adds and removes the drop down menus (ComboBoxes) based on the updated orientation
            AdjustComboBoxes();
            xToolbar.IsOpen = (subtoolbarElement == null) ? true : (Orientation == Orientation.Vertical);
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
                foreach (var b in allButtons)
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
            }
            else
            {
                //otherwise, it is about to expand. In this case, update visisbility of separators before buttons
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
        /// If toolbar is expanded & at top of screen, it will open when user's pointer hovers over it.
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
                else if (subtoolbarElement is TextSubtoolbar txt)
                {
                    var margin = txt.Margin;
                    margin.Top = 13;
                    txt.Margin = margin;
                }
            }
        }

        /// <summary>
        /// If toolbar is expanded & at top of screen, it will close when user's pointer leaves its area.
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
                else if (subtoolbarElement is TextSubtoolbar txt)
                {
                    var margin = txt.Margin;
                    margin.Top = 0;
                    txt.Margin = margin;
                }
            }
        }

        /// <summary>
        /// When the Collapse button is pressed, the visibility of the buttons are toggled accordingly. This enables the user control over the size of the toolbar.
        /// </summary>
        private void XCollapse_OnTapped(object sender, TappedRoutedEventArgs e)
        {
            //toggle state enum and update label & icon
            state = (state == State.Expanded) ? State.Collapsed : State.Expanded;
            xCollapse.Label = (state == State.Expanded) ? "Collapse" : "";
            xCollapse.Icon = (state == State.Expanded) ? new SymbolIcon(Symbol.BackToWindow) : new SymbolIcon(Symbol.FullScreen);

            //
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
            if (subtoolbarElement == xTextToolbar) xTextToolbar.CloseSubMenu();
            subtoolbarElement = null;
            xToolbar.IsOpen = (state == State.Expanded);

            //adjust toolbar's position to account for the change in size
            xFloating.AdjustPositionForExpansion(0, ToolbarConstants.ToolbarExpandedWidth - xToolbar.ActualWidth);
        }

        /// <summary>
        /// Updates UI of toolbar when Night Mode is active in order to remain readable.
        /// </summary>
        public void SwitchTheme(bool nightModeOn)
        {
            //toggle night mode styles
            xToolbar.Foreground = (nightModeOn) ? new SolidColorBrush(Colors.White) : new SolidColorBrush(Colors.Black);
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
            xPin.Label = (pinned == Pinned.Unpinned) ? "Floating" : "Anchored";
            xLockIcon.Source = (pinned == Pinned.Unpinned) ? unpinnedIcon : pinnedIcon;
            xToolbar.IsOpen = (state == State.Collapsed) ? false : (subtoolbarElement == null ? true : IsAtTop());
        }

        public void TempFreeze(bool mobile) { xFloating.ShouldManipulateChild = (mobile) ? true : pinned == Pinned.Unpinned; }

        private void xRedo_Click(object sender, RoutedEventArgs e)
        {
            UndoManager.RedoOccured();
        }

        private void xUndo_Click(object sender, RoutedEventArgs e)
        {
            UndoManager.UndoOccured();
        }
    }
}
