using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using Windows.ApplicationModel.DataTransfer;
using Windows.Foundation;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Dash.Annotations;
using Windows.System;
using Windows.UI;
using Windows.UI.Xaml.Media.Animation;
using DashShared;
using Windows.UI;
using Windows.UI.Xaml.Shapes;

// The User Control item template is documented at https://go.microsoft.com/fwlink/?LinkId=234236

namespace Dash
{


    public sealed partial class DocumentDecorations : UserControl, INotifyPropertyChanged
    {
        private Visibility _visibilityState;
        private List<DocumentView> _selectedDocs;
        private bool _isMoving;
        public ObservableDictionary<string, Tag> _tagNameDict = new ObservableDictionary<string, Tag>();
        public Tag CurrEditTag;
        private DocumentController currEditLink;
        private ObservableCollection<string> currNames = new ObservableCollection<string>();

        public Dictionary<string, List<DocumentController>> tagMap = new Dictionary<string, List<DocumentController>>();

        public Visibility VisibilityState
        {
            get => _visibilityState;
            set
            {
                if (value != _visibilityState && !_visibilityLock)
                {
                    _visibilityState = value;
                    SuggestGrid.Visibility = CurrEditTag != null ? Visibility.Visible : Visibility.Collapsed;
                    SetPositionAndSize();
                    OnPropertyChanged(nameof(VisibilityState));
                }
            }
        }

        public double DocWidth
        {
            get => _docWidth;
            set => _docWidth = value;
        }


        public Queue<Tag> RecentTags
        {
            get => _recentTags;
            set { _recentTags = value; }
        }

        private Queue<Tag> _recentTags;
        public List<Tag> Tags;

        public ListController<DocumentController> RecentTagsSave;
        public ListController<DocumentController> TagsSave;

        private double _docWidth;
        private bool _visibilityLock;

        public List<DocumentView> SelectedDocs
        {
            get => _selectedDocs;
            set
            {
                foreach (var doc in _selectedDocs)
                {
                    doc.PointerEntered -= SelectedDocView_PointerEntered;
                    doc.PointerExited -= SelectedDocView_PointerExited;
                    doc.ViewModel?.DocumentController.RemoveFieldUpdatedListener(KeyStore.PositionFieldKey,
                        DocumentController_OnPositionFieldUpdated);
                    doc.SizeChanged -= DocView_OnSizeChanged;
                    if ((doc.ViewModel?.DocumentController.DocumentType.Equals(RichTextBox.DocumentType) ?? false) &&
                        doc.GetFirstDescendantOfType<RichTextView>() != null)
                    {
                        doc.GetFirstDescendantOfType<RichTextView>().OnManipulatorHelperStarted -= ManipulatorStarted;
                        doc.GetFirstDescendantOfType<RichTextView>().OnManipulatorHelperCompleted -=
                            ManipulatorCompleted;
                    }

                    //doc.ManipulationControls.OnManipulatorStarted -= ManipulatorStarted;
                    //doc.ManipulationControls.OnManipulatorCompleted -= ManipulatorCompleted;
                    //doc.ManipulationControls.OnManipulatorAborted -= ManipulationControls_OnManipulatorAborted;
                    doc.FadeOutBegin -= DocView_OnDeleted;
                }

                _visibilityLock = false;
                foreach (var doc in value)
                {
                    if (doc.ViewModel?.Undecorated ?? false)
                    {
                        _visibilityLock = true;
                        VisibilityState = Visibility.Collapsed;
                    }

                    doc.PointerEntered += SelectedDocView_PointerEntered;
                    doc.PointerExited += SelectedDocView_PointerExited;
                    doc.ViewModel?.DocumentController.AddFieldUpdatedListener(KeyStore.PositionFieldKey,
                        DocumentController_OnPositionFieldUpdated);
                    doc.SizeChanged += DocView_OnSizeChanged;

                    if (doc.ViewModel == null) return;

                    if (doc.ViewModel.DocumentController.DocumentType.Equals(RichTextBox.DocumentType) &&
                        doc.GetFirstDescendantOfType<RichTextView>() != null)
                    {
                        doc.GetFirstDescendantOfType<RichTextView>().OnManipulatorHelperStarted += ManipulatorStarted;
                        doc.GetFirstDescendantOfType<RichTextView>().OnManipulatorHelperCompleted +=
                            OnManipulatorHelperCompleted;
                    }

                    //doc.ManipulationControls.OnManipulatorStarted += ManipulatorStarted;
                    //doc.ManipulationControls.OnManipulatorTranslatedOrScaled += ManipulatorMoving;
                    //doc.ManipulationControls.OnManipulatorCompleted += ManipulatorCompleted;
                    //doc.ManipulationControls.OnManipulatorAborted += ManipulationControls_OnManipulatorAborted;
                    doc.FadeOutBegin += DocView_OnDeleted;
                }

                _selectedDocs = value;
            }
        }

        private void ManipulationControls_OnManipulatorAborted()
        {
            VisibilityState = Visibility.Collapsed;
        }

        private void OnManipulatorHelperCompleted()
        {
            if (!_isMoving)
            {
                VisibilityState = Visibility.Visible;
            }
        }

        private void ManipulatorMoving(TransformGroupData transformationDelta)
        {
            if (!_isMoving)
            {
                _isMoving = true;
            }
        }

        private void DocView_OnDeleted()
        {
            VisibilityState = Visibility.Collapsed;
            SuggestGrid.Visibility = Visibility.Collapsed;
        }

        private void ManipulatorCompleted()
        {
            VisibilityState = Visibility.Visible;
            _isMoving = false;
        }

        private void ManipulatorStarted()
        {
            VisibilityState = Visibility.Collapsed;
            SuggestGrid.Visibility = VisibilityState;
            _isMoving = true;

        }

        private void DocView_OnSizeChanged(object sender, SizeChangedEventArgs e)
        {
            SetPositionAndSize();
        }

        private void DocumentController_OnPositionFieldUpdated(DocumentController sender,
            DocumentController.DocumentFieldUpdatedEventArgs args, Context context)
        {
            SetPositionAndSize();
        }

        public DocumentDecorations()
        {
            this.InitializeComponent();
            _visibilityState = Visibility.Collapsed;
            SuggestGrid.Visibility = Visibility.Collapsed;
            _selectedDocs = new List<DocumentView>();
            //Tags = new List<SuggestViewModel>();
            //Recents = new Queue<SuggestViewModel>();
            Tags = new List<Tag>();
            _recentTags = new Queue<Tag>();
            Loaded += DocumentDecorations_Loaded;
            Unloaded += DocumentDecorations_Unloaded;
        }

        private void DocumentDecorations_Unloaded(object sender, RoutedEventArgs e)
        {
            SelectionManager.SelectionChanged -= SelectionManager_SelectionChanged;
        }

        private void DocumentDecorations_Loaded(object sender, RoutedEventArgs e)
        {
            SelectionManager.SelectionChanged += SelectionManager_SelectionChanged;

        }

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
                xRecentTagsDivider.Visibility = Visibility.Visible;
            }

            foreach (var tag in RecentTags)
            {
                xTest.Children.Add(tag);
            }
        }

        private void SelectionManager_SelectionChanged(DocumentSelectionChangedEventArgs args)
        {
            SuggestGrid.Visibility = Visibility.Collapsed;
            CurrEditTag = null;

            SelectedDocs = SelectionManager.GetSelectedDocs().ToList();
            if (SelectedDocs.Count > 1)
            {
                xMultiSelectBorder.BorderThickness = new Thickness(2);
            }
            else
            {
                xMultiSelectBorder.BorderThickness = new Thickness(0);
            }

            SetPositionAndSize();
            if (SelectedDocs.Any() && !this.IsRightBtnPressed())
            {
                VisibilityState = Visibility.Visible;
            }
            else
            {
                VisibilityState = Visibility.Collapsed;

            }
        }

        static HashSet<string> LinkNames = new HashSet<string>();

        private void SetPositionAndSize()
        {
            var topLeft = new Point(double.PositiveInfinity, double.PositiveInfinity);
            var botRight = new Point(double.NegativeInfinity, double.NegativeInfinity);

            foreach (var doc in SelectedDocs)
            {
                var viewModelBounds = doc.TransformToVisual(MainPage.Instance.xCanvas)
                    .TransformBounds(new Rect(new Point(), new Size(doc.ActualWidth, doc.ActualHeight)));

                topLeft.X = Math.Min(viewModelBounds.Left - doc.xTargetBorder.BorderThickness.Left, topLeft.X);
                topLeft.Y = Math.Min(viewModelBounds.Top - doc.xTargetBorder.BorderThickness.Top, topLeft.Y);

                botRight.X = Math.Max(viewModelBounds.Right + doc.xTargetBorder.BorderThickness.Right, botRight.X);
                botRight.Y = Math.Max(viewModelBounds.Bottom + doc.xTargetBorder.BorderThickness.Bottom, botRight.Y);

                if (doc.ViewModel != null)
                {
                    tagMap.Clear();
                    GetLinkTypes(doc.ViewModel.DataDocument,
                        tagMap); // make sure all of this documents link types have been added to the menu of link types
                }
            }


            rebuildMenuIfNeeded();

            //TODO: DO WE NEED THIS STILL?
            // update menu items to point to the currently selected document
            foreach (var item in xButtonsPanel.Children.OfType<Grid>())
            {
                var menuLinkName = (item.Tag as Tuple<DocumentView, string>).Item2;
                item.Tag = new Tuple<DocumentView, string>(SelectedDocs.FirstOrDefault(), menuLinkName);
            }

            if (double.IsPositiveInfinity(topLeft.X) || double.IsPositiveInfinity(topLeft.Y) ||
                double.IsNegativeInfinity(botRight.X) || double.IsNegativeInfinity(botRight.Y))
            {
                return;
            }

            if (botRight.X > MainPage.Instance.ActualWidth - xStackPanel.ActualWidth - MainPage.Instance.xLeftGrid.ActualWidth)
                botRight = new Point(MainPage.Instance.ActualWidth - xStackPanel.ActualWidth - MainPage.Instance.xLeftGrid.ActualWidth, botRight.Y);
            this.RenderTransform = new TranslateTransform
            {
                X = topLeft.X - 3,
                Y = topLeft.Y
            };

            ContentColumn.Width = new GridLength(botRight.X - topLeft.X);
            xRow.Height = new GridLength(botRight.Y - topLeft.Y);

            if (_recentTags.Count == 0) xRecentTagsDivider.Visibility = Visibility.Visible;
        }

        private void AddLinkTypeButton(string linkName)
        {
            //button formatting
            var tb = new TextBlock()
            {
                Text = linkName.Substring(0, 1),
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                Foreground = new SolidColorBrush(Colors.White)
            };
            var button = new Grid()
            {
                Background = new SolidColorBrush(Colors.Transparent),
                CanDrag = true,
                Width = 22,
                Height = 22,
            };
            //set button color to tag color
            var btnColorOrig = _tagNameDict.ContainsKey(linkName) ? _tagNameDict[linkName]?.Color : null;
            var btnColorFinal = btnColorOrig != null
                ? Color.FromArgb(200, btnColorOrig.Value.R, btnColorOrig.Value.G, btnColorOrig.Value.B)
                : Color.FromArgb(255, 64, 123, 177);
            var ellipse = new Ellipse()
            {
                Width = 22,
                Height = 22,
                Fill = new SolidColorBrush(btnColorFinal),
                CanDrag = true,
                HorizontalAlignment = HorizontalAlignment.Center,
            };
            button.Children.Add(ellipse);
            button.Children.Add(tb);
            button.DragStarting += (s, args) =>
            {
                DocumentView doq = ((s as FrameworkElement)?.Tag as Tuple<DocumentView, string>)?.Item1;
                if (doq == null) return;

                args.Data.AddDragModel(new DragDocumentModel(doq.ViewModel.DocumentController, false, doq) { LinkType = linkName });
                args.AllowedOperations = DataPackageOperation.Link | DataPackageOperation.Move | DataPackageOperation.Copy;
                args.Data.RequestedOperation = DataPackageOperation.Move | DataPackageOperation.Copy | DataPackageOperation.Link;
                doq.ViewModel.DecorationState = false;
            };

            ToolTip toolTip = new ToolTip
            {
                Content = linkName,
                HorizontalOffset = 5,
                Placement = PlacementMode.Right
            };
            ToolTipService.SetToolTip(button, toolTip);
            xButtonsPanel.Children.Add(button);
            button.PointerEntered += (s, e) => toolTip.IsOpen = true;
            button.PointerExited += (s, e) => toolTip.IsOpen = false;

            button.Tapped += (s, e) =>
            {
                if (ToolTipService.GetToolTip(button) is ToolTip tip) tip.IsOpen = false;
                var doq = ((s as FrameworkElement).Tag as Tuple<DocumentView, string>).Item1;
                if (doq != null)
                {
                    new AnnotationManager(doq).FollowRegion(doq.ViewModel.DocumentController,
                        doq.GetAncestorsOfType<ILinkHandler>(), e.GetPosition(doq), linkName);
                }

            };
            button.Tag = new Tuple<DocumentView, string>(null, linkName);

            //allow users to change default tag titles by right click
            button.RightTapped += (s, e) =>
            {
                e.Handled = true;
                ToggleTagEditor(_tagNameDict[linkName], s as FrameworkElement);

            };
            button.PointerPressed += (s, e) =>
            {
                foreach (var doc in SelectedDocs)
                {
                    doc.ManipulationMode = ManipulationModes.None;
                }
            };
        }

        /*
		private void LaunchLinkTypeInputBox(Point where)
		{
			ActionTextBox inputBox = MainPage.Instance.xLinkInputBox;
			Storyboard fadeIn = MainPage.Instance.xLinkInputIn;
			Storyboard fadeOut = MainPage.Instance.xLinkInputOut;

			var moveTransform = new TranslateTransform {X = where.X, Y = where.Y};
			inputBox.RenderTransform = moveTransform;

			inputBox.AddKeyHandler(VirtualKey.Enter, args =>
			{
				string entry = inputBox.Text.Trim();
				if (string.IsNullOrEmpty(entry)) return;

				inputBox.ClearHandlers(VirtualKey.Enter);

				fadeOut.Completed += FadeOutOnCompleted;
				fadeOut.Begin();

				args.Handled = true;

				void FadeOutOnCompleted(object sender2, object o1)
				{
					fadeOut.Completed -= FadeOutOnCompleted;

					LinkNames.Add(entry);
					
					var color = AddTag(entry);
					//_tagNameDict.Add(entry)
					//AddLinkTypeButton(, color);
					//rebuildMenuIfNeeded();

					//SELECT LINK TYPE 

					inputBox.Visibility = Visibility.Collapsed;
				}
			});
			
			inputBox.Visibility = Visibility.Visible;
			fadeIn.Begin();
			inputBox.Focus(FocusState.Programmatic);
		}
		*/

        private Tag AddTagIfUnique(string name)
        {
            foreach (var comp in Tags)
            {
                if (name == comp.Text)
                {
                    return null;
                }
            }

            return AddTag(name);
        }

        private Tag AddTag(string linkName, List<DocumentController> links = null)
        {
            xRecentTagsDivider.Visibility = Visibility.Visible;

            var r = new Random();
            var hexColor = Color.FromArgb(150, (byte)r.Next(256), (byte)r.Next(256), (byte)r.Next(256));

            Tag tag = null;

            //REMOVE OLD TAG
            if (_tagNameDict.ContainsKey(linkName))
            {
                tag = _tagNameDict[linkName];
            }
            else
            {
                tag = new Tag(this, linkName, hexColor);

                Tags.Add(tag);
                _tagNameDict.Remove(linkName);
                _tagNameDict.Add(linkName, tag);

                var doc = new DocumentController();
                doc.SetField<TextController>(KeyStore.DataKey, linkName, true);
                doc.SetField<ColorController>(KeyStore.BackgroundColorKey, hexColor, true);
                TagsSave.Add(doc);

                if (_recentTags.Count < 5)
                {
                    _recentTags.Enqueue(tag);
                    RecentTagsSave.Add(doc);
                }
                else
                {
                    _recentTags.Dequeue();
                    RecentTagsSave.RemoveAt(0);
                    _recentTags.Enqueue(tag);
                    RecentTagsSave.Add(doc);
                }

                xTest.Children.Clear();
                foreach (var recent in _recentTags.Reverse())
                {
                    xTest.Children.Add(recent);
                }
            }

            if (links != null)
            {
                //connect link to tag
                foreach (DocumentController link in links)
                {
                    tag.AddLink(link);
                }
            }
            return tag;
        }

        //adds the tag box & link button that connexts the name of the tag to all link docs included in the list
        private void AddTagGraphic(string name, List<DocumentController> linkList)
        {
            //maybe call this
            AddTag(name, tagMap[name]);
            AddLinkTypeButton(name);
        }


        private void rebuildMenuIfNeeded()
        {
            if (SuggestGrid.Visibility == Visibility.Visible) return;
            xButtonsPanel.Children.Clear();
            //check each relevant tag name & create the tag graphic & button for it!
            foreach (var name in tagMap.Keys)
            {
                if (name != "")
                {
                    AddTagGraphic(name, tagMap[name]);
                }
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
                foreach (var name in l.GetDataDocument().GetLinkTags()?.TypedData ?? new List<TextController>())
                {
                    var str = name.Data;
                    //tag name could already exist in side panel, in which case we need to add it to the list of dcs that are related to this tag 
                    if (map.ContainsKey(str))
                    {
                        if (!map[str].Contains(l.GetDataDocument()))
                            map[str].Add(l.GetDataDocument());
                    }
                    else //create new list containing link doc
                    {
                        map.Add(str, new List<DocumentController> { l.GetDataDocument() });
                    }
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
            if (doc.ViewModel != null)
            {
                if ((doc.StandardViewLevel.Equals(CollectionViewModel.StandardViewLevel.None) ||
                     doc.StandardViewLevel.Equals(CollectionViewModel.StandardViewLevel.Detail)) &&
                    doc.ViewModel != null &&
                    !e.GetCurrentPoint(doc).Properties.IsLeftButtonPressed &&
                    !e.GetCurrentPoint(doc).Properties.IsRightButtonPressed)
                {
                    VisibilityState = Visibility.Visible;
                }

                MainPage.Instance.HighlightTreeView(doc.ViewModel.DocumentController, true);
            }

            Window.Current.CoreWindow.PointerCursor = new CoreCursor(CoreCursorType.Arrow, 1);
            if (MainPage.Instance.MainDocView == doc && MainPage.Instance.MainDocView.ViewModel != null)
            {
                var level = MainPage.Instance.MainDocView.ViewModel.ViewLevel;
                if (level.Equals(CollectionViewModel.StandardViewLevel.Overview) ||
                    level.Equals(CollectionViewModel.StandardViewLevel.Region))
                    Window.Current.CoreWindow.PointerCursor = new CoreCursor(CoreCursorType.SizeAll, 0);
                else if (level.Equals(CollectionViewModel.StandardViewLevel.Detail))
                    Window.Current.CoreWindow.PointerCursor = new CoreCursor(CoreCursorType.IBeam, 0);
            }
        }

        private void SelectedDocView_PointerExited(object sender, PointerRoutedEventArgs e)
        {
            var doc = sender as DocumentView;
            if (doc.StandardViewLevel.Equals(CollectionViewModel.StandardViewLevel.None) ||
                doc.StandardViewLevel.Equals(CollectionViewModel.StandardViewLevel.Detail))
            {
                if (e == null ||
                    (!e.GetCurrentPoint(doc).Properties.IsRightButtonPressed &&
                     !e.GetCurrentPoint(doc).Properties.IsLeftButtonPressed) && doc.ViewModel != null)
                    VisibilityState = Visibility.Collapsed;
                //xAddLinkTypeBorder.Visibility = Visibility.Collapsed;
                SuggestGrid.Visibility = Visibility.Collapsed;
            }

            MainPage.Instance.HighlightTreeView(doc.ViewModel.DocumentController, false);
            if (MainPage.Instance.MainDocView != doc)
            {
                var viewlevel = MainPage.Instance.MainDocView.ViewModel.ViewLevel;
                if (viewlevel.Equals(CollectionViewModel.StandardViewLevel.Overview) ||
                    viewlevel.Equals(CollectionViewModel.StandardViewLevel.Region))
                    Window.Current.CoreWindow.PointerCursor = new CoreCursor(CoreCursorType.SizeAll, 0);
                else if (viewlevel.Equals(CollectionViewModel.StandardViewLevel.Detail))
                    Window.Current.CoreWindow.PointerCursor = new CoreCursor(CoreCursorType.IBeam, 0);
            }
        }

        private void XAnnotateEllipseBorder_OnTapped(object sender, TappedRoutedEventArgs e)
        {
            foreach (var doc in SelectedDocs)
            {
                var ann = new AnnotationManager(doc);
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
            foreach (DocumentView doc in SelectedDocs)
            {
                args.Data.AddDragModel(new DragDocumentModel(doc.ViewModel.DocumentController, false, doc));
                args.AllowedOperations =
                    DataPackageOperation.Link | DataPackageOperation.Move | DataPackageOperation.Copy;
                args.Data.RequestedOperation =
                    DataPackageOperation.Move | DataPackageOperation.Copy | DataPackageOperation.Link;
                doc.ViewModel.DecorationState = false;
            }
        }

        private void XTemplateEditorEllipseBorder_OnPointerPressed(object sender, PointerRoutedEventArgs e)
        {
            foreach (var doc in SelectedDocs)
            {
                doc.ManipulationMode = ManipulationModes.None;
                doc.ToggleTemplateEditor();
            }
        }

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
            VisibilityState = Visibility.Collapsed;
        }

        private void AutoSuggestBox_TextChanged(AutoSuggestBox sender, AutoSuggestBoxTextChangedEventArgs args)
        {
            var results = new List<Tag>();
            if (args.Reason == AutoSuggestionBoxTextChangeReason.UserInput)
            {
                xTest.Children.Clear();
                string search = sender.Text;

                if (search == "")
                {
                    foreach (var recent in _recentTags.Reverse())
                    {
                        xTest.Children.Add(recent);
                    }
                }
                else
                {
                    foreach (var tag in Tags)
                    {
                        if (tag.Text.StartsWith(search))
                        {
                            results.Add(tag);
                        }
                    }

                    var temp = new List<Tag>();
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

                    temp.Sort();
                    results.AddRange(temp);

                    foreach (var result in results)
                    {
                        xTest.Children.Add(result);
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
                    MainPage.Instance.ActivationManager.DeactivateAllExcept(SelectedDocs);
                }

                foreach (var doc in SelectedDocs)
                {
                    MainPage.Instance.ActivationManager.ToggleActivation(doc);
                }
            }


        }

        private void XAutoSuggestBox_OnKeyUp(object sender, KeyRoutedEventArgs e)
        {
            if (e.Key == VirtualKey.Enter)
            {
                var box = sender as AutoSuggestBox;
                string entry = box.Text.Trim();
                if (string.IsNullOrEmpty(entry)) return;


                AddTagIfUnique(entry).Select();

                box.Text = "";
            }
        }

        private void ToggleTagEditor(Tag tagPressed, FrameworkElement button)
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
                    xFadeAnimationOut.Completed += (s, en) => { SuggestGrid.Visibility = Visibility.Collapsed; };
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
            CurrEditTag = currTag;
            //TODO: Update selected tags based on currtag (CHECK MORE THAN JUST RECENT TAGS)

            //if one link has this tag, open tag editor for that link
            if (tagMap[currTag.Text].Count == 1)
            {
                //update selected recent tag
                foreach (var tag in _recentTags)
                {
                    tag.RidSelectionBorder();
                    if (tag.Text.Equals(currTag.Text)) tag.AddSelectionBorder();
                }
                currEditLink = tagMap[currTag.Text].First();
                SuggestGrid.Visibility = Visibility.Visible;
                xFadeAnimationIn.Begin();
            }
            else if (chosenLink != null)
            {
                currEditLink = chosenLink;
                //update selected recent tag
                foreach (var tag in _recentTags)
                {
                    tag.RidSelectionBorder();
                    if (chosenLink.GetField<ListController<TextController>>(KeyStore.LinkTagKey)?.Select(tc => tc.Data).Contains(tag.Text) ?? false) tag.AddSelectionBorder();
                }
                SuggestGrid.Visibility = Visibility.Visible;
                xFadeAnimationIn.Begin();
            }
            else //open context menu to let user decide which link to edit
            {
                var flyout = new MenuFlyout();

                foreach (DocumentController link in tagMap[currTag.Text])
                {
                    if (link.GetField<ListController<TextController>>(KeyStore.LinkTagKey)?.Select(tc => tc.Data).Contains(currTag.Text) ?? false)
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
                //show flyout @ correct point
                flyout.ShowAt(button);
            }


        }

        //temporary method for telling all links associated with this tag that an additional tag has been added
        public void UpdateAllTags(Tag selected)
        {
            //get active links from last-pressed btn & add this tag to them

            foreach (var link in tagMap[CurrEditTag.Text])
            {
                selected.AddLink(link);
            }

        }

    }
}
