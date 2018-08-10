using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.ApplicationModel.DataTransfer;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using Dash.Annotations;
using Dash.Models.DragModels;
using System.Diagnostics;
using Windows.System;
using Windows.UI;
using Windows.UI.Xaml.Media.Animation;
using DashShared;
using Windows.UI;

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
        private ObservableCollection<string> currNames = new ObservableCollection<string>();

        private Dictionary<string, List<DocumentController>> tagMap = new Dictionary<string, List<DocumentController>>();

        public Visibility VisibilityState
        {
            get => _visibilityState;
            set
            {
                if (value != _visibilityState && !_visibilityLock)
                {
                    _visibilityState = value;
                    //if (value == Visibility.Collapsed) SuggestGrid.Visibility = Visibility.Collapsed;
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

                    doc.ManipulationControls.OnManipulatorStarted -= ManipulatorStarted;
                    doc.ManipulationControls.OnManipulatorCompleted -= ManipulatorCompleted;
                    doc.ManipulationControls.OnManipulatorAborted -= ManipulationControls_OnManipulatorAborted;
                    doc.FadeOutBegin -= DocView_OnDeleted;
                }

                _visibilityLock = false;
                foreach (var doc in value)
                {
                    if (doc.ViewModel.Undecorated)
                    {
                        _visibilityLock = true;
                        VisibilityState = Visibility.Collapsed;
                    }

                    doc.PointerEntered += SelectedDocView_PointerEntered;
                    doc.PointerExited += SelectedDocView_PointerExited;
                    doc.ViewModel?.DocumentController.AddFieldUpdatedListener(KeyStore.PositionFieldKey,
                        DocumentController_OnPositionFieldUpdated);
                    doc.SizeChanged += DocView_OnSizeChanged;
                    if (doc.ViewModel.DocumentController.DocumentType.Equals(RichTextBox.DocumentType) &&
                        doc.GetFirstDescendantOfType<RichTextView>() != null)
                    {
                        doc.GetFirstDescendantOfType<RichTextView>().OnManipulatorHelperStarted += ManipulatorStarted;
                        doc.GetFirstDescendantOfType<RichTextView>().OnManipulatorHelperCompleted +=
                            OnManipulatorHelperCompleted;
                    }

                    doc.ManipulationControls.OnManipulatorStarted += ManipulatorStarted;
                    doc.ManipulationControls.OnManipulatorTranslatedOrScaled += ManipulatorMoving;
                    doc.ManipulationControls.OnManipulatorCompleted += ManipulatorCompleted;
                    doc.ManipulationControls.OnManipulatorAborted += ManipulationControls_OnManipulatorAborted;
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
        }

        private void ManipulatorCompleted()
        {
            VisibilityState = Visibility.Visible;
            _isMoving = false;
        }

        private void ManipulatorStarted()
        {
            VisibilityState = Visibility.Collapsed;
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
            foreach (var item in xButtonsPanel.Children.OfType<Button>())
            {
                var target = SelectedDocs.FirstOrDefault()?.ViewModel.DataDocument;
                //tagMap.Clear();
                //GetLinkTypes(target, tagMap);
                var menuLinkName = (item.Tag as Tuple<DocumentView, string>).Item2;
                //item.Background = map.ContainsKey(menuLinkName)
                //	? new SolidColorBrush(new Windows.UI.Color() {A = 0x10, R = 0, G = 0xff, B = 0})
                //	: null;
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
                X = topLeft.X,
                Y = topLeft.Y
            };

            ContentColumn.Width = new GridLength(botRight.X - topLeft.X);
            xRow.Height = new GridLength(botRight.Y - topLeft.Y);

            if (_recentTags.Count == 0) xRecentTagsDivider.Visibility = Visibility.Visible;
        }

        private void AddLinkTypeButton(string linkName)
        {
            //check if link type button already exists 
            if (tagMap[linkName] == null)
            {

            }
            var tb = new TextBlock()
            {
                Text = linkName.Substring(0, 1),
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                Foreground = new SolidColorBrush(Colors.White)
            };
            var g = new Grid()
            {
                Background = new SolidColorBrush(Colors.Transparent)
            };
            //set button color to tag color
            var btnColorOrig = _tagNameDict.ContainsKey(linkName) ? _tagNameDict[linkName]?.Color : null;
            var btnColorFinal = btnColorOrig != null
                ? Color.FromArgb(200, btnColorOrig.Value.R, btnColorOrig.Value.G, btnColorOrig.Value.B)
                : Color.FromArgb(255, 64, 123, 177);
            g.Children.Add(new Windows.UI.Xaml.Shapes.Ellipse()
            {
                Width = 22,
                Height = 22,
                Fill = new SolidColorBrush(btnColorFinal)
            });
            g.Children.Add(tb);
            var button = new Button()
            {
                Content = g,
                Width = 22,
                Height = 22,
                CanDrag = true,
                HorizontalAlignment = HorizontalAlignment.Center,
            };
            button.DragStarting += (s, args) =>
            {
                var doq = ((s as FrameworkElement).Tag as Tuple<DocumentView, string>).Item1;
                if (doq != null)
                {
                    args.Data.Properties[nameof(DragDocumentModel)] =
                        new DragDocumentModel(doq.ViewModel.DocumentController, false, doq) { LinkType = linkName };
                    args.AllowedOperations =
                        DataPackageOperation.Link | DataPackageOperation.Move | DataPackageOperation.Copy;
                    args.Data.RequestedOperation =
                        DataPackageOperation.Move | DataPackageOperation.Copy | DataPackageOperation.Link;
                    doq.ViewModel.DecorationState = false;
                }
            };

            ToolTip toolTip = new ToolTip();
            toolTip.Content = linkName;
            toolTip.HorizontalOffset = 5;
            toolTip.Placement = PlacementMode.Right;
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
                ToggleTagEditor(_tagNameDict[linkName]);
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
                /*
				var oldTag = _tagNameDict[linkName];
				hexColor = oldTag.Color;
				Tags.Remove(oldTag);
				DocumentController oldTagDoc = null;
				foreach (var docC in TagsSave)
				{
					if (docC.GetField<TextController>(KeyStore.DataKey, true).Data.Equals(linkName))
					{
						oldTagDoc = docC;
						break;
					}
				}

				if (oldTagDoc != null)
				{
					if (RecentTagsSave.Contains(oldTagDoc))
					{
						RecentTagsSave.Remove(oldTagDoc);
						//check if this tag is in recent tags & replace
						int index;
						Queue<Tag> catchQueue = new Queue<Tag>();
						foreach (var recent in _recentTags)
						{

						}
					}
					TagsSave.Remove(oldTagDoc);
					hexColor = oldTagDoc.GetField<ColorController>(KeyStore.BackgroundColorKey, true).Data;
				}
				*/
                tag = _tagNameDict[linkName];
                //hexColor = tag.Color;
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
            //_tagNameDict.Clear();
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
            var linkedTo = doc.GetLinks(KeyStore.LinkToKey)?.TypedData;
            if (linkedTo != null)
                //for each link
                foreach (var l in linkedTo)
                {
                    var tagNames = l.GetDataDocument().GetField<ListController<TextController>>(KeyStore.LinkTagKey).Data;
                    //for each tag name of this link
                    foreach (TextController name in tagNames)
                    {
                        var str = name.Data;
                        //tag name could already exist in side panel, in which case we need to add it to the list of dcs that are related to this tag 
                        if (map.ContainsKey(str))
                        {
                            if (!map[str].Contains(l.GetDataDocument())) map[str].Add(l.GetDataDocument());
                        }
                        else //create new list containing link doc
                        {
                            map.Add(str, new List<DocumentController> { l.GetDataDocument() });
                        }
                    }
                    //linknames.Add(string.Join(", ", tags?.Select(tc => tc.Data) ?? new string[0]));

                }

            var linkedFrom = doc.GetLinks(KeyStore.LinkFromKey)?.TypedData;
            if (linkedFrom != null)
                foreach (var l in linkedFrom)
                {
                    var tagNames = l.GetDataDocument().GetField<ListController<TextController>>(KeyStore.LinkTagKey).Data;

                    //linknames.Add(string.Join(", ", tags?.Select(tc => tc.Data) ?? new string[0]));

                    //for each tag name of this link
                    foreach (TextController name in tagNames)
                    {
                        var str = name.Data;
                        //tag name could already exist in side panel, in which case we need to add it to the list of dcs that are related to this tag 
                        if (map.ContainsKey(str))
                        {
                            map[str].Add(l.GetDataDocument());
                        }
                        else //create new list containing link doc
                        {
                            map.Add(str, new List<DocumentController> { l.GetDataDocument() });
                        }
                    }

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
                //SuggestGrid.Visibility = Visibility.Collapsed;
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
            foreach (var doc in SelectedDocs)
            {
                args.Data.Properties[nameof(DragDocumentModel)] =
                    new DragDocumentModel(doc.ViewModel.DocumentController, false, doc);
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
            //ShowTags();
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        private void DocumentDecorations_OnPointerEntered(object sender, PointerRoutedEventArgs e)
        {
            VisibilityState = Visibility.Visible;
            // ShowTags();
        }

        private void DocumentDecorations_OnPointerExited(object sender, PointerRoutedEventArgs e)
        {
            VisibilityState = Visibility.Collapsed;
            //SuggestGrid.Visibility = Visibility.Collapsed;
            //xAddLinkTypeBorder.Visibility = Visibility.Collapsed;
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
            if (!MainPage.Instance.IsShiftPressed())
            {
                MainPage.Instance.ActivationManager.DeactivateAllExcept(SelectedDocs);
            }

            foreach (var doc in SelectedDocs)
            {
                MainPage.Instance.ActivationManager.ToggleActivation(doc);
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

        private void ToggleTagEditor(Tag tagPressed = null)
        {
            if (tagPressed == CurrEditTag)
            {
                if (SuggestGrid.Visibility == Visibility.Collapsed)
                {
                    OpenTagEditor(tagPressed);
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
                OpenTagEditor(tagPressed);
            }

        }

        private void OpenTagEditor(Tag currTag)
        {
            CurrEditTag = currTag;
            //TODO: Update selected tags based on currtag (CHECK MORE THAN JUST RECENT TAGS)
            foreach (var tag in _recentTags)
            {
                tag.RidSelectionBorder();
                if (tag == currTag) tag.AddSelectionBorder();
            }

            SuggestGrid.Visibility = Visibility.Visible;
            xFadeAnimationIn.Begin();
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
