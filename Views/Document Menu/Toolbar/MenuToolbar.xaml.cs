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
            "Orientation", typeof(Orientation), typeof(MenuToolbar), new PropertyMetadata(Orientation.Horizontal));

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
            Visibility = isVisible ? Visibility.Visible : Visibility.Collapsed;
        }

        public SolidColorBrush CollapseColor
        {
            get => (SolidColorBrush)GetValue(CollapseColorProperty);
            set => SetValue(CollapseColorProperty, value);
        }


        public void SetUndoEnabled(bool enabled)
        {
            //xUndo.IsEnabled = enabled;
            //xUndo.Opacity = enabled ? 1.0 : 0.5;
        }

        public void SetRedoEnabled(bool enabled)
        {
            //xRedo.IsEnabled = enabled;
            //xRedo.Opacity = enabled ? 1.0 : 0.5;
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

        // == FIELDS == 
        private UIElement subtoolbarElement = null; // currently active submenu, if null, nothing is selected
        private UIElement baseLevelContentToolbar;
        private State state;
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
            //pinnedIcon = new BitmapImage(new Uri("ms-appx:///Assets/pinned.png"));
            //unpinnedIcon = new BitmapImage(new Uri("ms-appx:///Assets/unpinned.png"));

            //xPadding.Width = ToolbarConstants.PaddingLong;
            //xPadding.Height = ToolbarConstants.PaddingShort;

            SelectionManager.SelectionChanged += (sender) => { Update(SelectionManager.GetSelectedDocs()); };

            //move toolbar to ideal location on start-up
            Loaded += (sender, args) =>
            {
                var ele = (FrameworkElement)Window.Current.Content;
                xFloating.ManipulateControlPosition(ele.ActualWidth - XDocumentView.ActualWidth,
                    ele.ActualHeight - XDocumentView.ActualHeight,
                    XDocumentView.ActualHeight, XDocumentView.ActualWidth);
                SelectionManager.SelectionChanged += SelectionManager_SelectionChanged;
            };
        }

        public void SetCollection(DocumentController collection)
        {
            XDocumentView.DataContext = new DocumentViewModel(collection)
            {
                //IsDimensionless = true,
                //ResizersVisible = false,
                //Undecorated = true,
            };
        }

        private void SelectionManager_SelectionChanged(DocumentSelectionChangedEventArgs args)
        {
            //xAreContentsHitTestVisibleIcon.Text = ((char)0xE840).ToString();
            //foreach (var d in args.SelectedViews)
            //{
            //    xAreContentsHitTestVisibleIcon.Text = (!d.AreContentsHitTestVisible ? (char)0xE77A : (char)0xE840).ToString();
            //}
        }

        /// <summary>
        /// Updates the toolbar with the data from the current selected. TODO: bindings with this to MainPage.SelectedDocs?
        /// </summary>
        /// <param name="docs"></param>
        public void Update(IEnumerable<DocumentView> docs)
        {
            if (subtoolbarElement != null) subtoolbarElement.Visibility = Visibility.Collapsed;

            subtoolbarElement = null;
            docs = docs.ToList();

            // just single select
            if (docs.Count() == 1)
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
                        xRichTextToolbar.SetSelectedDocumentView(selection);
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
                        if (MainPage.Instance.IsCtrlPressed())
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
                        xRichTextToolbar.SetSelectedDocumentView(selection);
                        subtoolbarElement = xRichTextToolbar;
                        xGroupToolbar.TryMakeGroupEditable(false);
                    }
                    else if (data is TextController || data is DateTimeController || data is NumberController)
                    {
                        containsInternalContent = true;
                        baseLevelContentToolbar = xPlainTextToolbar;
                        xPlainTextToolbar.SetMenuToolBarBinding(selection.GetFirstDescendantOfType<EditableTextBlock>());
                        //give toolbar access to the most recently selected text box for editing purposes
                        xPlainTextToolbar.SetCurrTextBox(selection.GetFirstDescendantOfType<EditableTextBlock>());
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
            if (subtoolbarElement != null) subtoolbarElement.Visibility = Visibility.Visible;
            //set proper subtoolbar to visible
            if (subtoolbarElement != null)
            {
                //if vertical, adjust position for additional height
                if (Orientation == Orientation.Vertical)
                {
                    xFloating.AdjustPositionForExpansion(0, XDocumentView.ActualWidth);
                }
                //otherwise, adjust position for additional width
                else
                {
                    xFloating.AdjustPositionForExpansion(XDocumentView.ActualHeight, 0);
                }

                subtoolbarElement.Visibility = Visibility.Visible;
                //xFloating.Floating_SizeChanged(null, null);
            }
        }

        private void FitWidth(object sender, RoutedEventArgs e)
        {
            foreach (var d in SelectionManager.GetSelectedDocs())
            {
                if (d.ViewModel.LayoutDocument.GetHorizontalAlignment() == HorizontalAlignment.Stretch)
                {
                    d.ViewModel.LayoutDocument.SetWidth(d.ViewModel.LayoutDocument.GetDereferencedField<NumberController>(KeyStore.CollectionOpenWidthKey, null)?.Data ??
                        (!double.IsNaN(d.ViewModel.LayoutDocument.GetWidth()) ? d.ViewModel.LayoutDocument.GetWidth() :
                           d.ViewModel.LayoutDocument.GetActualSize().Value.X));
                    d.ViewModel.LayoutDocument.SetHorizontalAlignment(HorizontalAlignment.Left);
                }
                else if (!(d.GetFirstAncestorOfType<CollectionView>()?.CurrentView is CollectionFreeformView) || d.ViewModel.LayoutDocument.DocumentType.Equals(RichTextBox.DocumentType))
                {
                    d.ViewModel.LayoutDocument.SetField<NumberController>(KeyStore.CollectionOpenWidthKey, d.ViewModel.LayoutDocument.GetWidth(), true);
                    d.ViewModel.LayoutDocument.SetWidth(double.NaN);
                    d.ViewModel.LayoutDocument.SetHorizontalAlignment(HorizontalAlignment.Stretch);
                }
            }
        }
        private void FitHeight(object sender, RoutedEventArgs e)
        {
            foreach (var d in SelectionManager.GetSelectedDocs())
            {
                if (d.ViewModel.LayoutDocument.GetVerticalAlignment() == VerticalAlignment.Stretch)
                {
                    d.ViewModel.LayoutDocument.SetHeight(d.ViewModel.LayoutDocument.GetDereferencedField<NumberController>(KeyStore.CollectionOpenHeightKey, null)?.Data ??
                        (!double.IsNaN(d.ViewModel.LayoutDocument.GetHeight()) ? d.ViewModel.LayoutDocument.GetHeight() :
                           d.ViewModel.LayoutDocument.GetActualSize().Value.Y));
                    d.ViewModel.LayoutDocument.SetVerticalAlignment(VerticalAlignment.Top);
                }
                else if (!(d.GetFirstAncestorOfType<CollectionView>()?.CurrentView is CollectionFreeformView) || d.ViewModel.LayoutDocument.DocumentType.Equals(RichTextBox.DocumentType))
                {
                    d.ViewModel.LayoutDocument.SetField<NumberController>(KeyStore.CollectionOpenHeightKey, d.ViewModel.LayoutDocument.GetHeight(), true);
                    d.ViewModel.LayoutDocument.SetHeight(double.NaN);
                    d.ViewModel.LayoutDocument.SetVerticalAlignment(VerticalAlignment.Stretch);
                }
            }
        }
        private void FreezeContents(object sender, RoutedEventArgs e)
        {
            using (UndoManager.GetBatchHandle())
            {
                foreach (var d in SelectionManager.GetSelectedDocs())
                {
                    d.AreContentsHitTestVisible = !d.AreContentsHitTestVisible;
                    //xAreContentsHitTestVisibleIcon.Text = (!d.AreContentsHitTestVisible ? (char)0xE77A : (char)0xE840).ToString();
                }
            }
        }
        private void Copy(object sender, RoutedEventArgs e)
        {
            using (UndoManager.GetBatchHandle())
            {
                SelectionManager.GetSelectedDocs().ForEach((d) => d.CopyDocument());
            }
        }
        private void MakeInstance(object sender, RoutedEventArgs e)
        {
            using (UndoManager.GetBatchHandle())
            {
                foreach (var d in SelectionManager.GetSelectedDocs())
                {
                    d.MakeInstance();
                }
            }
        }

        private void Delete(object sender, RoutedEventArgs e)
        {
            using (UndoManager.GetBatchHandle())
            {
                foreach (DocumentView d in SelectionManager.GetSelectedDocs())
                {
                    d.DeleteDocument();
                }
            }
        }

        // controls which MouseMode is currently activated
        private AppBarToggleButton _checkedButton;

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

        }

        private void Add_Group_On_Click(object sender, RoutedEventArgs e)
        {
            //create and add group to workspace
            var mainPageCollectionView = SplitFrame.ActiveFrame.GetFirstDescendantOfType<CollectionView>();
            var where = Util.GetCollectionFreeFormPoint(mainPageCollectionView.CurrentView as CollectionFreeformView, new Point(500, 500));

            mainPageCollectionView.ViewModel.AddDocument(Util.AdornmentWithPosition(BackgroundShape.AdornmentShape.Rectangular, where, 500, 500));
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


        //private ToolTip _pin;
        //private ToolTip _collapse;
        //private ToolTip _select;
        //private ToolTip _ink;
        //private ToolTip _quickPan;
        //private ToolTip _addGroup;
        //private ToolTip _addImage;
        //private ToolTip _addVideo;
        //private ToolTip _addAudio;
        //private ToolTip _copy;
        //private ToolTip _instance;
        //private ToolTip _freeze;
        //private ToolTip _fitWidth;
        //private ToolTip _fitHeight;
        //private ToolTip _delete;
        //private ToolTip _undo;
        //private ToolTip _redo;
        //private ToolTip _presentation;
        //private ToolTip _export;

        private void SetUpToolTips()
        {
            //var placementMode = PlacementMode.Top;
            //const int offset = 5;

            //_pin = new ToolTip()
            //{
            //    Content = "Pin Toolbar",
            //    Placement = placementMode,
            //    VerticalOffset = offset
            //};
            //ToolTipService.SetToolTip(xPin, _pin);

            //_collapse = new ToolTip()
            //{
            //    Content = "Collapse Toolbar",
            //    Placement = placementMode,
            //    VerticalOffset = offset
            //};
            //ToolTipService.SetToolTip(xCollapse, _collapse);

            //_select = new ToolTip()
            //{
            //    Content = "Select",
            //    Placement = placementMode,
            //    VerticalOffset = offset
            //};
            //ToolTipService.SetToolTip(xTouch, _select);

            //_ink = new ToolTip()
            //{
            //    Content = "Ink",
            //    Placement = placementMode,
            //    VerticalOffset = offset
            //};
            //ToolTipService.SetToolTip(xInk, _ink);

            //_quickPan = new ToolTip()
            //{
            //    Content = "Quick Pan",
            //    Placement = placementMode,
            //    VerticalOffset = offset
            //};
            //ToolTipService.SetToolTip(xGroup, _quickPan);

            //_addGroup = new ToolTip()
            //{
            //    Content = "Add Group",
            //    Placement = placementMode,
            //    VerticalOffset = offset
            //};
            //ToolTipService.SetToolTip(xAddGroup, _addGroup);

            //_addImage = new ToolTip()
            //{
            //    Content = "Add Image",
            //    Placement = placementMode,
            //    VerticalOffset = offset
            //};
            //ToolTipService.SetToolTip(xAddImage, _addImage);

            //_addVideo = new ToolTip()
            //{
            //    Content = "Add Video",
            //    Placement = placementMode,
            //    VerticalOffset = offset
            //};
            //ToolTipService.SetToolTip(xAddVideo, _addVideo);

            //_addAudio = new ToolTip()
            //{
            //    Content = "Add Audio",
            //    Placement = placementMode,
            //    VerticalOffset = offset
            //};
            //ToolTipService.SetToolTip(xAddAudio, _addAudio);

            //_instance = new ToolTip()
            //{
            //    Content = "Instance",
            //    Placement = placementMode,
            //    VerticalOffset = offset
            //};
            //ToolTipService.SetToolTip(xMakeInstance, _instance);

            //_freeze = new ToolTip()
            //{
            //    Content = "Freeze Contents",
            //    Placement = placementMode,
            //    VerticalOffset = offset
            //};
            //ToolTipService.SetToolTip(xAreContentsHitTestVisibleIcon, _freeze); 

            // _fitWidth = new ToolTip()
            //{
            //    Content = "Fit Width",
            //    Placement = placementMode,
            //    VerticalOffset = offset
            //};

            //ToolTipService.SetToolTip(xFitWidth, _fitWidth);
            //_fitHeight = new ToolTip()
            //{
            //    Content = "Fit Height",
            //    Placement = placementMode,
            //    VerticalOffset = offset
            //};
            //ToolTipService.SetToolTip(xFitHeight, _fitHeight);

            //_copy = new ToolTip()
            //{
            //    Content = "Copy",
            //    Placement = placementMode,
            //    VerticalOffset = offset
            //};
            //ToolTipService.SetToolTip(xCopy, _copy);

            //_delete = new ToolTip()
            //{
            //    Content = "Delete",
            //    Placement = placementMode,
            //    VerticalOffset = offset
            //};
            //ToolTipService.SetToolTip(xDelete, _delete);

            //_undo = new ToolTip()
            //{
            //    Content = "Undo",
            //    Placement = placementMode,
            //    VerticalOffset = offset
            //};
            //ToolTipService.SetToolTip(xUndo, _undo);

            //_redo = new ToolTip()
            //{
            //    Content = "Redo",
            //    Placement = placementMode,
            //    VerticalOffset = offset
            //};
            //ToolTipService.SetToolTip(xRedo, _redo);

            //_export = new ToolTip()
            //{
            //    Content = "Export Workspace",
            //    Placement = placementMode,
            //    VerticalOffset = offset
            //};
            //ToolTipService.SetToolTip(xExport, _export);

            //_presentation = new ToolTip()
            //{
            //    Content = "Presentation Mode",
            //    Placement = placementMode,
            //    VerticalOffset = offset
            //};
            //ToolTipService.SetToolTip(xPresentationMode, _presentation);
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
            MainPage.Instance.Publish_OnTapped(sender, null);
        }

        private void XPresentationMode_OnClick(object sender, RoutedEventArgs e)
        {
            MainPage.Instance.SetPresentationState(MainPage.Instance.CurrPresViewState == MainPage.PresentationViewState.Collapsed);
        }

        private void XSplitVertical_OnClick(object sender, RoutedEventArgs e)
        {
            SplitFrame.ActiveFrame.Split(SplitDirection.Right, autosize: true);
        }

        private void XSplitHorizontal_OnClick(object sender, RoutedEventArgs e)
        {
            SplitFrame.ActiveFrame.Split(SplitDirection.Down, autosize: true);
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

        private void XEnableInk_OnChecked(object sender, RoutedEventArgs e)
        {
            MainPage.Instance.InkManager.ShowToolbar();
        }

        private void XEnableInk_OnUnchecked(object sender, RoutedEventArgs e)
        {
            MainPage.Instance.InkManager.HideToolbar();
        }
    }
}
