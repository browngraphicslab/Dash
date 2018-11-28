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
                IsDimensionless = true,
                ResizersVisible = false,
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
        private void Update(IEnumerable<DocumentView> docs)
        {
            if (subtoolbarElement != null) subtoolbarElement.Visibility = Visibility.Collapsed;

            subtoolbarElement = null;

            //if (!ToolbarColumn.Width.IsStar)
            //{
            //    return;
            //}

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
                        if (thisCollection != null)
                        {
                            xCollectionToolbar.SetCollectionBinding(thisCollection, selection.ViewModel.DocumentController);
                            subtoolbarElement = xCollectionToolbar;
                        }
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
                    //Thickness margin = xSubtoolbarStackPanel.Margin;
                    //margin.Top = 7;
                    //xSubtoolbarStackPanel.Margin = margin;
                    //xPadding.Visibility = Visibility.Collapsed;

                }
                else
                {
                    //If nothing is selected, open/label the main menu toolbar
                    //update margin
                    //Thickness margin = xSubtoolbarStackPanel.Margin;
                    //margin.Top = 7;
                    //xSubtoolbarStackPanel.Margin = margin;
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
        /// Toggles orientation of any combo boxes on any subtoolbar.
        /// </summary>
        private void AdjustComboBoxes()
        {
            if (subtoolbarElement is ICommandBarBased cmd) cmd.SetComboBoxVisibility(Orientation == Orientation.Horizontal ? Visibility.Visible : Visibility.Collapsed);
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

        private void XEnableInk_OnChecked(object sender, RoutedEventArgs e)
        {
            MainPage.Instance.InkManager.ShowToolbar();
        }

        private void XEnableInk_OnUnchecked(object sender, RoutedEventArgs e)
        {
            MainPage.Instance.InkManager.HideToolbar();
        }

        private void XCollapseButton_OnTapped(object sender, TappedRoutedEventArgs e)
        {
            if (ToolbarColumn.Width.IsStar)
            {
                ToolbarColumn.Width = new GridLength(0);
                XDocumentView.Visibility = Visibility.Collapsed;
                XCollapseBox.Text = "\uE740";
            }
            else
            {
                ToolbarColumn.Width = new GridLength(1, GridUnitType.Star);
                XDocumentView.Visibility = Visibility.Visible;
                XCollapseBox.Text = "\uE73F";
            }

            Update(SelectionManager.GetSelectedDocs());
        }
    }
}
