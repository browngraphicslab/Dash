using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using Windows.ApplicationModel.DataTransfer;
using Windows.Foundation;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Dash.Annotations;
using Windows.System;
using Windows.UI;
using Windows.UI.Core;
using Windows.UI.Xaml.Shapes;
using Microsoft.Toolkit.Uwp.UI.Controls;
using Windows.UI.Xaml.Documents;
using Windows.UI.Xaml.Media.Animation;
using DashShared;

// The User Control item template is documented at https://go.microsoft.com/fwlink/?LinkId=234236

namespace Dash
{
    public sealed partial class DocumentDecorations : UserControl, INotifyPropertyChanged
    {
        private Visibility _resizerVisibilityState = Visibility.Collapsed;
        private Visibility _visibilityState;
        private List<DocumentView> _selectedDocs;

        //_tagNameDict is used for the actual tags graphically added into the tag/link pane. it contains a list of names of the tags paired with the tags themselves.
        public ObservableDictionary<string, Tag> _tagNameDict = new ObservableDictionary<string, Tag>();
        //TagMap is used to keep track of the different activated tags displayed underneath the link button. it contains a list of names of tags paired with a list of all of the links tagged with that specific tag.
        public Dictionary<string, List<DocumentController>> TagMap = new Dictionary<string, List<DocumentController>>();
        public List<DocumentController> CurrentLinks;
        public Tag CurrEditTag;
        private DocumentController currEditLink;
        public WrapPanel XTagContainer => xTagContainer;
        private DocumentController _currentLink;
        public bool touchActivated = false;
      

        private bool optionClick;

        public Visibility VisibilityState
        {
            get => _visibilityState;
            set
            {
                if (value != _visibilityState && !_visibilityLock)
                {
                    _visibilityState = value;
                    SuggestGrid.Visibility = CurrEditTag != null ? Visibility.Visible : Visibility.Collapsed;
                    OnPropertyChanged(nameof(VisibilityState));
                }
            }
        }
        public Visibility ResizerVisibilityState
        {
            get => _resizerVisibilityState;
            set
            {
                if (_resizerVisibilityState != value)
                {
                    _resizerVisibilityState = value;
                    if (value == Visibility.Visible)
                        SetPositionAndSize();
                    OnPropertyChanged(nameof(ResizerVisibilityState));
                }
            }
        }

        public double DocWidth
        {
            get => _docWidth;
            set => _docWidth = value;
        }

        //RecentTags keeps track of the 5 most recently-used tags that will be displayed graphically as a default
        public Queue<Tag> RecentTags
        {
            get => _recentTags;
            set { _recentTags = value; }
        }

        private Queue<Tag> _recentTags;

        private Stack<Tag> _inLineTags;

        public Stack<Tag> InLineTags
        {
            get => _inLineTags;
            set { _inLineTags = value; }
        }

        //Tags keeps track of all of the availble tags a user has created and that can be used
        public List<Tag> Tags;


        //these lists save the RecentTags and Tags in between refreshes/restarts so that they are preserved for the user
        public ListController<DocumentController> RecentTagsSave;
        public ListController<DocumentController> TagsSave;

        private double _docWidth;
        private bool _visibilityLock;

        public List<DocumentView> SelectedDocs
        {
            get => _selectedDocs;
            set
            {
                foreach (var docView in _selectedDocs)
                {
                    docView.PointerEntered -= SelectedDocView_PointerEntered;
                    docView.PointerExited -= SelectedDocView_PointerExited;
                    docView.SizeChanged -= DocView_OnSizeChanged;
                    docView.FadeOutBegin -= DocView_OnDeleted;
                }

                _visibilityLock = false;
                foreach (var docView in value)
                {
                    if (docView.ViewModel?.Undecorated == true)
                    {
                        _visibilityLock = true;
                        VisibilityState = Visibility.Collapsed;
                    }

                    docView.PointerEntered += SelectedDocView_PointerEntered;
                    docView.PointerExited += SelectedDocView_PointerExited;
                    docView.SizeChanged += DocView_OnSizeChanged;
                    docView.FadeOutBegin += DocView_OnDeleted;
                }

                _selectedDocs = value;
            }
        }
        private void DocView_OnDeleted()
        {
            VisibilityState = Visibility.Collapsed;
            SuggestGrid.Visibility = Visibility.Collapsed;
        }

        private void DocView_OnSizeChanged(object sender, SizeChangedEventArgs e)
        {
            SetPositionAndSize();
        }

        private ToolTip _titleTip = new ToolTip() { Placement = PlacementMode.Top };
        public DocumentDecorations()
        {
            this.InitializeComponent();
            _visibilityState = Visibility.Collapsed;
            SuggestGrid.Visibility = Visibility.Collapsed;
            _selectedDocs = new List<DocumentView>();
            _titleTip.Content = HeaderFieldKey.Name;
            ToolTipService.SetToolTip(xHeaderText, _titleTip);
            xHeaderText.PointerEntered += (s, e) => _titleTip.IsOpen = true;
            xHeaderText.PointerExited  += (s, e) => _titleTip.IsOpen = false;
            xHeaderText.GotFocus += (s, e) =>
            {
                if (xHeaderText.Text == "<empty>") xHeaderText.SelectAll();
            };
            //Tags = new List<SuggestViewModel>();
            //Recents = new Queue<SuggestViewModel>();
            Tags = new List<Tag>();
            _recentTags = new Queue<Tag>();
            _inLineTags = new Stack<Tag>();
            Loaded += DocumentDecorations_Loaded;
            Unloaded += DocumentDecorations_Unloaded;
            // setup ResizeHandles
            void ResizeHandles_OnManipulationStarted(object sender, ManipulationStartedRoutedEventArgs e)
            {
                if (this.IsRightBtnPressed())
                {
                    SelectionManager.InitiateDragDrop(_selectedDocs.First(), null, null);
                }
                else
                {
                    (sender as FrameworkElement).ManipulationCompleted -= ResizeHandles_OnManipulationCompleted;
                    (sender as FrameworkElement).ManipulationCompleted += ResizeHandles_OnManipulationCompleted;
                    
                    UndoManager.StartBatch();

                    MainPage.Instance.Focus(FocusState.Programmatic);
                    e.Handled = true;
                }
            }

            void ResizeHandles_OnManipulationCompleted(object sender, ManipulationCompletedRoutedEventArgs e)
            {
                UndoManager.EndBatch();
                (sender as FrameworkElement).ManipulationCompleted -= ResizeHandles_OnManipulationCompleted;
                e.Handled = true;
            }

            foreach (var handle in new Rectangle[] {
                xTopLeftResizeControl, xTopResizeControl, xTopRightResizeControl,
                xLeftResizeControl, xRightResizeControl,
                xBottomLeftResizeControl, xBottomRightResizeControl, xBottomResizeControl })
            {
                handle.ManipulationStarted += ResizeHandles_OnManipulationStarted;
                handle.PointerReleased += (s, e) => {
                    handle.ReleasePointerCapture(e.Pointer);
                    e.Handled = true;
                };
                handle.PointerPressed += (s, e) =>
                {
                    ManipulationMode = ManipulationModes.None;
                    if (!e.GetCurrentPoint(this).Properties.IsRightButtonPressed)
                    {
                        handle.CapturePointer(e.Pointer);
                        e.Handled = true;
                    }
                };
            }
            SelectionManager.DragManipulationStarted += (s,e) =>  ResizerVisibilityState = Visibility.Collapsed;
            SelectionManager.DragManipulationCompleted += (s,e) => 
                 ResizerVisibilityState = _selectedDocs.FirstOrDefault()?.GetFirstAncestorOfType<CollectionFreeformView>() == null ? Visibility.Collapsed : Visibility.Visible;

        }


        private void DocumentDecorations_Unloaded(object sender, RoutedEventArgs e)
        {
            SelectionManager.SelectionChanged -= SelectionManager_SelectionChanged;
        }

        private void DocumentDecorations_Loaded(object sender, RoutedEventArgs e)
        {
            SelectionManager.SelectionChanged += SelectionManager_SelectionChanged;

        }

        //this method retrieves the saved recent tags and saved tags from their respective keys and repopulates the RecentTags and Tags lists 
        public void LoadTags(DocumentController settingsdoc)
        {
            RecentTagsSave =
                settingsdoc.GetFieldOrCreateDefault<ListController<DocumentController>>(KeyStore.RecentTagsKey);
            TagsSave = settingsdoc.GetFieldOrCreateDefault<ListController<DocumentController>>(KeyStore.TagsKey);
            foreach (var documentController in RecentTagsSave)
            {
                RecentTags.Enqueue(new Tag(this, documentController.GetField<TextController>(KeyStore.DataKey).Data,
                    documentController.GetField<ColorController>(KeyStore.BackgroundColorKey).Data));
                xRecentTagsDivider.Visibility = Visibility.Visible;
            }

            foreach (var documentController in TagsSave)
            {
                var tag = new Tag(this, documentController.GetField<TextController>(KeyStore.DataKey).Data,
                    documentController.GetField<ColorController>(KeyStore.BackgroundColorKey).Data);
                Tags.Add(tag);
                _tagNameDict.Add(tag.Text, tag);
                //possibly repopulate the TagMap here??
                xRecentTagsDivider.Visibility = Visibility.Visible;
            }

            //graphically displays the reloaded recent tags
            foreach (var tag in RecentTags)
            {
                xTagContainer.Children.Add(tag);
            }
        }

        private void SelectionManager_SelectionChanged(DocumentSelectionChangedEventArgs args)
        {
            SuggestGrid.Visibility = Visibility.Collapsed;
            CurrEditTag = null;

            SelectedDocs = SelectionManager.GetSelectedDocs().ToList();
            xMultiSelectBorder.BorderThickness = new Thickness(SelectedDocs.Count > 1 ? 2 : 0);
            SetPositionAndSize();
            VisibilityState = (SelectedDocs.Any() && !this.IsRightBtnPressed()) ? Visibility.Visible : Visibility.Collapsed;
        }

        public void SetPositionAndSize()
        {
            var topLeft = new Point(double.PositiveInfinity, double.PositiveInfinity);
            var botRight = new Point(double.NegativeInfinity, double.NegativeInfinity);

            foreach (var doc in SelectedDocs)
            {
                var viewModelBounds = doc.TransformToVisual(MainPage.Instance.xCanvas).TransformBounds(new Rect(new Point(), new Size(doc.ActualWidth, doc.ActualHeight)));

                topLeft.X = Math.Min(viewModelBounds.Left, topLeft.X);
                topLeft.Y = Math.Min(viewModelBounds.Top, topLeft.Y);

                botRight.X = Math.Max(viewModelBounds.Right, botRight.X);
                botRight.Y = Math.Max(viewModelBounds.Bottom, botRight.Y);

                if (doc.ViewModel != null)
                {
                    TagMap.Clear();
                    GetLinkTypes(doc.ViewModel.DataDocument, TagMap); // make sure all of this documents link types have been added to the menu of link types
                }
            }

            ResizerVisibilityState = _selectedDocs.FirstOrDefault()?.GetFirstAncestorOfType<CollectionFreeformView>() == null ? Visibility.Collapsed : Visibility.Visible;

            rebuildMenuIfNeeded();

            if (!double.IsPositiveInfinity(topLeft.X) && !double.IsPositiveInfinity(topLeft.Y) &&
                !double.IsNegativeInfinity(botRight.X) && !double.IsNegativeInfinity(botRight.Y))
            {
                if (botRight.X > MainPage.Instance.ActualWidth - xAnnotationButtonsStack.ActualWidth - MainPage.Instance.xLeftGrid.ActualWidth)
                {
                    botRight = new Point(MainPage.Instance.ActualWidth - xAnnotationButtonsStack.ActualWidth - MainPage.Instance.xLeftGrid.ActualWidth, botRight.Y);
                }

                RenderTransform = new TranslateTransform
                {
                    X = topLeft.X,
                    Y = topLeft.Y
                };

                ContentColumn.Width = new GridLength(Math.Max(0, botRight.X - topLeft.X));
                ContentRow.Height = new GridLength(botRight.Y - topLeft.Y);

                if (_recentTags.Count == 0)
                {
                    xRecentTagsDivider.Visibility = Visibility.Visible;
                }
            }
        }

        //adds a button for a link type to appear underneath the link button
        private void AddLinkTypeButton(string linkName)
        {
            //set button color to tag color
            var btnColorOrig = _tagNameDict.ContainsKey(linkName) ? _tagNameDict[linkName]?.Color : null;
            var btnColorFinal = btnColorOrig != null
                ? Color.FromArgb(200, btnColorOrig.Value.R, btnColorOrig.Value.G, btnColorOrig.Value.B)
                : Color.FromArgb(255, 64, 123, 177);

            var toolTip = new ToolTip
            {
                Content = linkName,
                HorizontalOffset = 5,
                Placement = PlacementMode.Right
            };
            
            var button = new LinkButton(this, btnColorFinal, linkName, toolTip, SelectedDocs.FirstOrDefault());
            xButtonsPanel.Children.Add(button);

            //adds tooltip with link tag name inside
            ToolTipService.SetToolTip(button, toolTip);
        }
        

        //checks to see if a tag with the same name has already been created. if not, then a new tag is created
        public Tag AddTagIfUnique(string name)
        {
            foreach (var comp in Tags)
            {
                if (name == comp.Text)
                {
                    return comp;
                }
            }

            return AddTag(name);
        }

        //adds a new tag both graphically and to the dictionary
        public Tag AddTag(string linkName, List<DocumentController> links = null)
        {
            xRecentTagsDivider.Visibility = Visibility.Visible;

            var r = new Random();
            var hexColor = Color.FromArgb(150, (byte)r.Next(256), (byte)r.Next(256), (byte)r.Next(256));

            Tag tag = null;

            //removes an old tag if one already exists and redoes it
            if (_tagNameDict.ContainsKey(linkName))
            {
                tag = _tagNameDict[linkName];
            }
            else
            {
                //otherwise a new tag is created and is added to the tag dictionary and the list of tags
                tag = new Tag(this, linkName, hexColor);

                Tags.Add(tag);
                _tagNameDict.Add(linkName, tag);

                //creates a new document controller out of the tag details to save into the database via tagssave
                var doc = new DocumentController();
                doc.SetField<TextController>(KeyStore.DataKey, linkName, true);
                doc.SetField<ColorController>(KeyStore.BackgroundColorKey, hexColor, true);
                TagsSave.Add(doc);

                //if there are currently less than 5 recent tags (aka less than 5 tags currently exist), add the new tag to the recent tags
                if (_recentTags.Count < 5)
                {
                    _recentTags.Enqueue(tag);
                    RecentTagsSave.Add(doc);
                }
                //otherwise, get rid of the oldest recent tag and add the new tag to recent tags, as well as update the recenttagssave
                else
                {
                    var deq = _recentTags.Dequeue();
                    RecentTagsSave.RemoveAt(0);
                    _inLineTags.Push(deq);
                    _recentTags.Enqueue(tag);
                    RecentTagsSave.Add(doc);
                }

                //replace the default recent tags to include the newest tag
                xTagContainer.Children.Clear();
                foreach (var recent in _recentTags.Reverse())
                {
                    xTagContainer.Children.Add(recent);
                }
            }

            //if (links != null)
            //{
            //    //connect link to tag
            //    foreach (DocumentController link in links)
            //    {
            //        tag.AddLink(link);
            //    }
            //}
            return tag;
        }
        static public KeyController HeaderFieldKey = KeyStore.TitleKey;
        //rebuilds the different link dots when the menu is refreshed or one is added
        public void rebuildMenuIfNeeded()
        {
            xButtonsPanel.Children.Clear();
            //check each relevant tag name & create the tag graphic & button for it
            foreach (var name in TagMap.Keys.Where((k) => k != null))
            {
                    //adds the tag box & link button that connects the name of the tag to all link docs included in the list
                AddLinkTypeButton(name);
                AddTag(name, TagMap[name]);
            }
            xButtonsCanvas.Height = xButtonsPanel.Children.Aggregate(xAnnotateEllipseBorder.ActualHeight, (hgt, child) => hgt += (child as FrameworkElement).Height);

            ResetHeader(); // force header field to update

            var htmlAddress = SelectedDocs.FirstOrDefault()?.ViewModel?.DataDocument.GetDereferencedField<TextController>(KeyStore.SourceUriKey,null)?.Data;
            if (!string.IsNullOrEmpty(htmlAddress))
            {// add a hyperlink that points to the source webpage.

                xURISource.Text = "From:";
                try
                {
                    var hyperlink = new Hyperlink() { NavigateUri = new System.Uri(htmlAddress) };
                    hyperlink.Inlines.Add(new Run() { Text = " " + HtmlToDashUtil.GetTitlesUrl(htmlAddress) });

                    xURISource.Inlines.Add(hyperlink);
                }
                catch (Exception)
                {
                    var theDoc = RESTClient.Instance.Fields.GetController<DocumentController>(htmlAddress);
                    if (theDoc != null)
                    {
                        var regDef = theDoc.GetDataDocument().GetRegionDefinition() ?? theDoc;
                        xURISource.Text += " " + regDef?.Title;
                        //var hyperlink = new Hyperlink() { NavigateUri = new System.Uri(htmlAddress) };
                        //hyperlink.Inlines.Add(new Run() { Text = " " + HtmlToDashUtil.GetTitlesUrl(htmlAddress) });

                        //xURISource.Inlines.Add(hyperlink);
                    }
                }
                xURISource.Visibility = Visibility.Visible;
            }
            else
            {
                var author = SelectedDocs.FirstOrDefault()?.ViewModel?.DataDocument.GetDereferencedField<TextController>(KeyStore.AuthorKey,null)?.Data;
                if (!string.IsNullOrEmpty(author))
                {// add a hyperlink that points to the source webpage.

                    xURISource.Text = "Authored by: " + author;
                    xURISource.Visibility = Visibility.Visible;
                }
                else xURISource.Visibility = Visibility.Collapsed;
            }
        }

        private Dictionary<string, List<DocumentController>> UpdateTags()
        {
            return null;
            //TODO: IMPLEMENT
        }

        private static void GetLinkTypes(DocumentController doc, Dictionary<string, List<DocumentController>> map)
        {
            if (doc == null)
                return;
            //ADDED: cleared linknames
            //linknames.Clear();
            //for each link
            foreach (var l in doc.GetLinks(null))
            {
                //for each tag name of this link
                
                    var str = l.GetDataDocument().GetLinkTag().Data;
                    //tag name could already exist in side panel, in which case we need to add it to the list of dcs that are related to this tag 
                    if (map.ContainsKey(str))
                    {
                        if (!map[str].Contains(l))
                            map[str].Add(l);
                    }
                    else //create new list containing link doc
                    {
                        map.Add(str, new List<DocumentController> { l });
                    }
              
                //linknames.Add(string.Join(", ", tags?.Select(tc => tc.Data) ?? new string[0]));
            }

            var regions = doc.GetDataDocument().GetRegions();
            if (regions != null)
                foreach (var region in regions.TypedData)
                {
                    GetLinkTypes(region.GetDataDocument(), map);
                }
        }



        private void SelectedDocView_PointerEntered(object sender, PointerRoutedEventArgs e)
        {
            var doc = sender as DocumentView;
            if (e.Pointer.PointerDeviceType.Equals(Windows.Devices.Input.PointerDeviceType.Touch))
                touchActivated = true;
            if (doc.ViewModel != null)
            {
                VisibilityState = Visibility.Visible;
            }
        }

        private void SelectedDocView_PointerExited(object sender, PointerRoutedEventArgs e)
        {
            var doc = sender as DocumentView;
            if (e == null || (!e.IsRightPressed() && !e.IsRightPressed() && !e.Pointer.PointerDeviceType.Equals(Windows.Devices.Input.PointerDeviceType.Touch)))
            {
                VisibilityState = Visibility.Collapsed;
                SuggestGrid.Visibility = Visibility.Collapsed;
            }

            touchActivated = false;
        }

        private void XAnnotateEllipseBorder_OnTapped(object sender, TappedRoutedEventArgs e)
        {
            foreach (var doc in SelectedDocs)
            {
                var ann = new AnnotationManager(doc);
                if (doc.ViewModel != null)
                    ann.FollowRegion(doc.ViewModel.DocumentController, doc.GetAncestorsOfType<ILinkHandler>(),
                        e.GetPosition(doc));
            }
        }


        private void AllEllipses_OnPointerReleased(object sender, PointerRoutedEventArgs e)
        {
            foreach (var doc in SelectedDocs)
            {
                doc.ManipulationMode = ManipulationModes.All;
            }
        }

        private void XAnnotateEllipseBorder_OnPointerPressed(object sender, PointerRoutedEventArgs e)
        {
            foreach (var doc in SelectedDocs)
            {
                doc.ManipulationMode = ManipulationModes.None;
            }
        }

        private void XAnnotateEllipseBorder_OnDragStarting(UIElement sender, DragStartingEventArgs args)
        {
            foreach (var docView in SelectedDocs)
            {
                var docCollectionView = docView.GetFirstAncestorOfType<AnnotationOverlay>() == null ? docView.ParentCollection : null;
                args.Data.SetDragModel(new DragDocumentModel(docView) { DraggingLinkButton = true, DraggedDocCollectionViews = new List<CollectionViewModel>(new[] { docCollectionView.ViewModel } ) });
                args.AllowedOperations =
                    DataPackageOperation.Link | DataPackageOperation.Move | DataPackageOperation.Copy;
                args.Data.RequestedOperation =
                    DataPackageOperation.Move | DataPackageOperation.Copy | DataPackageOperation.Link;
            }

            //touchActivated = false;
        }

        //private void XTemplateEditorEllipseBorder_OnPointerPressed(object sender, PointerRoutedEventArgs e)
        //{
        //    foreach (var doc in SelectedDocs)
        //    {
        //        doc.ManipulationMode = ManipulationModes.None;
        //        doc.ToggleTemplateEditor();
        //    }
        //}

        private void XTitleBorder_OnPointerPressed(object sender, PointerRoutedEventArgs e)
        {
            foreach (var doc in SelectedDocs)
            {
                CapturePointer(e.Pointer);
                doc.ManipulationMode = e.GetCurrentPoint(doc).Properties.IsRightButtonPressed
                    ? ManipulationModes.None
                    : ManipulationModes.All;
                e.Handled = doc.ManipulationMode == ManipulationModes.All;
            }
        }

        private void XTitleBorder_OnTapped(object sender, TappedRoutedEventArgs e)
        {
            foreach (var doc in SelectedDocs)
            {
                doc.ShowContext();
                e.Handled = true;
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        [NotifyPropertyChangedInvocator]
        private void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        private void DocumentDecorations_OnPointerEntered(object sender, PointerRoutedEventArgs e)
        {
            VisibilityState = Visibility.Visible;
        }

        private void DocumentDecorations_OnPointerExited(object sender, PointerRoutedEventArgs e)
        {
            var doc = sender as DocumentDecorations;
            if (e == null ||
                (!e.GetCurrentPoint(doc).Properties.IsRightButtonPressed &&
                 !e.GetCurrentPoint(doc).Properties.IsLeftButtonPressed) && !optionClick)
            {
                SuggestGrid.Visibility = Visibility.Collapsed;
            }

            optionClick = false;

            if (!this.IsLeftBtnPressed() && touchActivated == false)
                VisibilityState = Visibility.Collapsed;
        }

        private void AutoSuggestBox_TextChanged(AutoSuggestBox sender, AutoSuggestBoxTextChangedEventArgs args)
        {
            var results = new List<Tag>();
            if (args.Reason == AutoSuggestionBoxTextChangeReason.UserInput)
            {
                xTagContainer.Children.Clear();
                string search = sender.Text;

                //if nothing is changed, keep the results as the default recent tags
                if (search == "")
                {
                    foreach (var recent in _recentTags.Reverse())
                    {
                        if (!xTagContainer.Children.Contains(recent))
                        {
                            xTagContainer.Children.Add(recent);
                        }
                        
                    }
                }
                else
                {
                    //first gather the tags that start with the search input, as they are more relevant than others
                    foreach (var tag in Tags)
                    {
                        if (tag.Text.StartsWith(search))
                        {
                            results.Add(tag);
                        }
                    }

                    var temp = new List<Tag>();
                    //then gather the tags that contain the search input anywhere, and add them to the results if they have not already been added
                    foreach (var tag in Tags)
                    {
                        if (tag.Text.Contains(search))
                        {
                            bool unique = true;
                            foreach (var result in results)
                            {
                                if (result.Text == tag.Text)
                                {
                                    unique = false;
                                }
                            }
                            if (unique)
                            {
                                temp.Add(tag);
                            }
                        }
                    }

                    //sort and add them to the results
                    temp.Sort();
                    results.AddRange(temp);

                    //add all relevant results to be graphically displayed in the tag container
                    foreach (var result in results)
                    {
                        xTagContainer.Children.Add(result);
                    }
                }
            }
        }

        private void XAnnotateEllipseBorder_OnRightTapped(object sender, RightTappedRoutedEventArgs e)
        {
            //activation mode only relevant for images & pdfs now
            var shouldActivate = true;
            foreach (DocumentView doc in SelectedDocs)
            {
                var docType = doc.ViewModel.DocumentController.DocumentType;
                if (!docType.Equals(ImageBox.DocumentType) && !docType.Equals(PdfBox.DocumentType)) shouldActivate = false;
            }

            if (shouldActivate == false) return;

            using (UndoManager.GetBatchHandle())
            {
                if (!MainPage.Instance.IsShiftPressed())
                {
                    LinkActivationManager.DeactivateAllExcept(SelectedDocs);
                }

                foreach (var doc in SelectedDocs)
                {
                    LinkActivationManager.ToggleActivation(doc);
                }
            }


        }

        private void XAutoSuggestBox_OnKeyUp(object sender, KeyRoutedEventArgs e)
        {
            //if enter is pressed, the text in the search box will be made into a new tag 
            if (e.Key == VirtualKey.Enter)
            {
                var box = sender as AutoSuggestBox;
                string entry = box.Text.Trim();
                if (string.IsNullOrEmpty(entry)) return;


                var newtag = AddTagIfUnique(entry);
                if (!TagMap.ContainsKey(entry))
                    TagMap.Add(entry, new List<DocumentController>());
                
                newtag.Select();

                box.Text = "";
            }
        }

        //opens or closes the tag editor box
        public void ToggleTagEditor(Tag tagPressed, FrameworkElement button)
        {
            if (tagPressed == CurrEditTag)
            {
                if (SuggestGrid.Visibility == Visibility.Collapsed)
                {
                    OpenTagEditor(tagPressed, button);
                }
                else
                {
                    xFadeAnimationOut.Begin();
                    CurrEditTag = null;
                }
            }
            else
            {
                OpenTagEditor(tagPressed, button);
            }
        }

        /// <summary>
        /// Opens the editor beneath the document to edit the tags of the selected links. This is called when the user right clicks a link bubble.
        /// </summary>
        /// <param name="currTag"></param>
        private void OpenTagEditor(Tag currTag, FrameworkElement button, DocumentController chosenLink = null)
        {
            //TODO: DO I NEED THIS?
            //TODO: Update selected tags based on currtag (CHECK MORE THAN JUST RECENT TAGS)

            

            //if one link has this tag, open tag editor for that link
            if (TagMap[currTag.Text].Count == 1)
            {
                CurrEditTag = currTag;
                //update selected recent tag
                //foreach (var tag in _recentTags)
                //{
                //    tag.RidSelectionBorder();
                //    if (tag.Text.Equals(currTag.Text)) tag.AddSelectionBorder();
                //}
                currEditLink = TagMap[currTag.Text].First();
                SuggestGrid.Visibility = Visibility.Visible;
                xFadeAnimationIn.Begin();
            }
            else if (chosenLink != null)
            {
                CurrEditTag = currTag;
                currEditLink = chosenLink;
                //update selected recent tag
                //foreach (var tag in _recentTags)
                //{
                //    tag.RidSelectionBorder();
                //    if (chosenLink.GetField<ListController<TextController>>(KeyStore.LinkTagKey)?.Select(tc => tc.Data).Contains(tag.Text) ?? false) tag.AddSelectionBorder();
                //}
                SuggestGrid.Visibility = Visibility.Visible;
                xFadeAnimationIn.Begin();
            }
            else //open context menu to let user decide which link to edit
            {
                var flyout = new MenuFlyout();

                foreach (var link in TagMap[currTag.Text])
                {
                    if (link.GetDataDocument().GetField<TextController>(KeyStore.LinkTagKey)?.Data.Equals(currTag.Text) ?? false)
                    {
                        //get title of target
                        var targetTitle = link.GetLinkedDocument(LinkDirection.ToDestination)?
                            .Title ?? link.GetLinkedDocument(LinkDirection.ToSource)
                                              .Title;

                        var item = new MenuFlyoutItem
                        {
                            Text = targetTitle,
                            DataContext = link
                        };
                        //clicking menu item should open the editor with the chosen, affected doc as the chosen item
                        var itemHdlr = new RoutedEventHandler((s, e) =>
                            OpenTagEditor(currTag, button, (s as MenuFlyoutItem)?.DataContext as DocumentController));

                        item.Click += itemHdlr;
                        flyout.Items?.Add(item);

                    }
                }

                _visibilityLock = true;
                flyout.Closed += (sender, o) => _visibilityLock = false;
                //show flyout @ correct point
                flyout.ShowAt(button);
            }

            _currentLink = currEditLink;

            //select saved link options
            xInContext.IsOn = currEditLink?.GetDataDocument()?.GetField<BoolController>(KeyStore.LinkContextKey)?.Data ?? true;
            switch (currEditLink?.GetDataDocument().GetLinkBehavior())
            {
                case LinkBehavior.Follow:
                    xTypeZoom.IsSelected = true;
                    break;
                case LinkBehavior.Annotate:
                    xTypeAnnotation.IsSelected = true;
                    break;
                case LinkBehavior.Dock:
                    xTypeDock.IsSelected = true;
                    break;
                case LinkBehavior.Overlay:
                    break;
                case LinkBehavior.Float:
                    xTypeFloat.IsSelected = true;
                    break;
            }

        }

      

        private void XInContext_OnToggled(object sender, RoutedEventArgs e)
        {
            //save if in context toggle is on or off
            var toggled = (sender as ToggleSwitch)?.IsOn;
            currEditLink?.GetDataDocument().SetField<BoolController>(KeyStore.LinkContextKey, toggled, true);
        }


        private void XLinkTypeBox_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
        {

            //save field for what link behavior is selected
            var selected = ((sender as ComboBox)?.SelectedItem as ComboBoxItem)?.Content;

            switch (selected)
            {
                case "Zoom":
                    currEditLink?.GetDataDocument().SetLinkBehavior(LinkBehavior.Follow);
                    //set in context toggle based on saved info before making area visible 
                    if (xInContext != null && xInContextGrid != null)
                    {
                        xInContext.IsOn = currEditLink?.GetDataDocument()?.GetField<BoolController>(KeyStore.LinkContextKey)?.Data ?? true;
                        xInContextGrid.Visibility = Visibility.Visible;
                    }
                    
                    break;
                case "Annotation":
                    currEditLink?.GetDataDocument().SetLinkBehavior(LinkBehavior.Annotate);
                    xInContextGrid.Visibility = Visibility.Collapsed;
                    break;
                case "Dock":
                    currEditLink?.GetDataDocument().SetLinkBehavior(LinkBehavior.Dock);
                    //set in context toggle based on saved info before making area visible 
                    if (xInContext != null && xInContextGrid != null)
                    {
                        xInContext.IsOn = currEditLink?.GetDataDocument()?.GetField<BoolController>(KeyStore.LinkContextKey)?.Data ?? true;
                        xInContextGrid.Visibility = Visibility.Visible;
                    }
                    
                    break;
                case "Float":
                    currEditLink?.GetDataDocument().SetLinkBehavior(LinkBehavior.Float);
                    xInContextGrid.Visibility = Visibility.Collapsed;
                    break;
                default:
                    break;
            }
        }

        private void XLinkTypeBox_OnDropDownOpened(object sender, object e)
        {
            optionClick = true;
        }
        private void XFadeAnimationOut_OnCompleted(object sender, object e)
        {
            SuggestGrid.Visibility = Visibility.Collapsed;
        }

        private void DeleteButton_OnTapped(object sender, TappedRoutedEventArgs e)
        {
            var source = _currentLink.GetDataDocument().GetField<DocumentController>(KeyStore.LinkSourceKey);
            var dest = _currentLink.GetDataDocument().GetField<DocumentController>(KeyStore.LinkDestinationKey);

            var to = source.GetDataDocument().GetField<ListController<DocumentController>>(KeyStore.LinkToKey);
            var from = dest.GetDataDocument().GetField<ListController<DocumentController>>(KeyStore.LinkFromKey);

            to.Remove(_currentLink);
            from.Remove(_currentLink);

            xFadeAnimationOut.Begin();
            CurrEditTag = null;
        }
        void ResizeTLaspect(object sender, ManipulationDeltaRoutedEventArgs e) { _selectedDocs.ForEach((dv) => dv.Resize(sender as FrameworkElement, e, true, true, true)); }
        void ResizeRTaspect(object sender, ManipulationDeltaRoutedEventArgs e) { _selectedDocs.ForEach((dv) => dv.Resize(sender as FrameworkElement, e, true, false, true)); }
        void ResizeBLaspect(object sender, ManipulationDeltaRoutedEventArgs e) { _selectedDocs.ForEach((dv) => dv.Resize(sender as FrameworkElement, e, false, true, true)); }
        void ResizeBRaspect(object sender, ManipulationDeltaRoutedEventArgs e) { _selectedDocs.ForEach((dv) => dv.Resize(sender as FrameworkElement, e, false, false, true)); }
        void ResizeRTunconstrained(object sender, ManipulationDeltaRoutedEventArgs e) { _selectedDocs.ForEach((dv) => dv.Resize(sender as FrameworkElement, e, true, false, false)); }
        void ResizeBLunconstrained(object sender, ManipulationDeltaRoutedEventArgs e) { _selectedDocs.ForEach((dv) => dv.Resize(sender as FrameworkElement, e, false, true, false)); }
        void ResizeBRunconstrained(object sender, ManipulationDeltaRoutedEventArgs e) { _selectedDocs.ForEach((dv) => dv.Resize(sender as FrameworkElement, e, false, false, false)); }

        private void xTitle_KeyUp(object sender, KeyRoutedEventArgs e)
        {
            switch (e.Key)
            {
                case VirtualKey.Enter:
                    if (xHeaderText.Text.StartsWith("#"))
                    {
                        ResetHeader(xHeaderText.Text.Substring(1));
                    }
                    else
                    {
                        CommitHeaderText();
                    }
                    break;
                case VirtualKey.Down:
                case VirtualKey.Up:
                    ChooseNextHeaderKey(e.Key == VirtualKey.Up);
                    break;
                default :
                    xHeaderText.Foreground = new SolidColorBrush(Colors.Red);
                    break;
            }
            e.Handled = true;
        }

        private void ChooseNextHeaderKey(bool prev=false)
        {
            var keys = new List<KeyController>();
            foreach (var d in SelectedDocs.Select((sd) => sd.ViewModel?.DataDocument))
            {
                keys.AddRange(d.EnumDisplayableFields().Select((pair) => pair.Key));
            }
            keys = keys.ToHashSet().ToList();
            keys.Sort((dv1, dv2) => string.Compare(dv1.Name, dv2.Name));
            var ind = keys.IndexOf(HeaderFieldKey);
            do
            {
                ind = prev ? (ind > 0 ? ind - 1 : keys.Count - 1) : (ind < keys.Count - 1 ? ind + 1 : 0);
                ResetHeader(keys[ind].Name);
            } while (xHeaderText.Text == "<empty>");
        }
        private void CommitHeaderText()
        {
            foreach (var doc in SelectedDocs.Select((sd) => sd.ViewModel?.DocumentController))
            {
                var targetDoc = doc.GetField<TextController>(HeaderFieldKey)?.Data != null ? doc : doc.GetDataDocument();

                targetDoc.SetField<TextController>(HeaderFieldKey, xHeaderText.Text, true);
            }
            xHeaderText.Background = new SolidColorBrush(Colors.LightBlue);
            ResetHeader();
        }
        private void ResetHeader(string newkey = null)
        {
            if (SelectedDocs.Count > 0)
            {
                if (newkey != null)
                {
                    HeaderFieldKey = new KeyController(newkey);
                }
                var layoutHeader = SelectedDocs.First().ViewModel?.DocumentController.GetField<TextController>(HeaderFieldKey)?.Data;
                xHeaderText.Text = layoutHeader ?? SelectedDocs.First().ViewModel?.DataDocument.GetDereferencedField<TextController>(HeaderFieldKey, null)?.Data ?? "<empty>";
                if (SelectedDocs.Count > 1)
                {
                    foreach (var d in SelectedDocs.Where(sd => sd.ViewModel != null).Select(sd => sd.ViewModel.DataDocument))
                    {
                        var dvalue = d?.GetDereferencedField<TextController>(HeaderFieldKey, null)?.Data ?? "<empty>";
                        if (dvalue != xHeaderText.Text)
                        {
                            xHeaderText.Text = "...";
                            break;
                        }
                    }
                }
                xHeaderText.Foreground = new SolidColorBrush(Colors.Black);
                _titleTip.Content = HeaderFieldKey.Name;
                xHeaderText.Background = new SolidColorBrush(xHeaderText.Text == "<empty>" ? Colors.Pink : Colors.LightBlue);
            }
        }
    }
}
