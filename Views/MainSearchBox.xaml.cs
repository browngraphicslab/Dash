using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Windows.ApplicationModel.DataTransfer;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using DashShared;
using Visibility = Windows.UI.Xaml.Visibility;
using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using Windows.System;
using Windows.UI;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Dash.Annotations;
using Microsoft.Toolkit.Uwp.UI.Animations;
using MyToolkit.UI;
using Telerik.UI.Xaml.Controls.Chart;
using static Dash.DataTransferTypeInfo;

// The User Control item template is documented at https://go.microsoft.com/fwlink/?LinkId=234236

namespace Dash
{
    public sealed partial class MainSearchBox 
    {
        private int _selectedIndex = -1;
        private bool _arrowBlock = false;

        private Dictionary<string, string> _filterDictionary;
        private Dictionary<string, MenuFlyoutItem> _filtertoMenuFlyout;
        private Dictionary<Button, string> _optiontofilter;
        private HashSet<string> _options;
        private HashSet<string> _documentFilters;
        private HashSet<string> _authorFilters;
        private bool _searchAll = false;


        #region Definition and Initilization
        public const int MaxSearchResultSize = 75;
        //private CancellationTokenSource _tokenSource = new CancellationTokenSource();

        public MainSearchBox()
        {
            InitializeComponent();
            xAutoSuggestBox.ItemsSource = new ObservableCollection<SearchResultViewModel>();
            InitializeFilters();

            _searchTimer.Interval = TimeSpan.FromMilliseconds(300);
            _searchTimer.Tick += SearchTimerOnTick;
        }

        private void SearchTimerOnTick(object sender, object o)
        {
            ExecuteDishSearch(xAutoSuggestBox);
            _searchTimer.Stop();
        }

        private void InitializeFilters()
        {
            _filterDictionary = new Dictionary<string, string>
            {
                ["Image"] = "Image Box",
                ["Text"] = "Rich Text Box",
                ["Audio"] = "Audio Box",
                ["Video"] = "Video Box",
                ["PDF"] = "Pdf Box",
                ["Collection"] = "Collection Box"
            };

            _filtertoMenuFlyout = new Dictionary<string, MenuFlyoutItem>
            {
                ["Image"] = xImageFilter,
                ["Text"] = xTextFilter,
                ["Audio"] = xAudioFilter,
                ["Video"] = xVideoFilter,
                ["PDF"] = xPDFFilter,
                ["Collection"] = xCollectionFilter
            };

            _optiontofilter = new Dictionary<Button, string>()
            {
                [XCaseSensButton] = "Case sensitive",
                [XMatchWordButton] = "Match whole word",
                [XSearchAllButton] = "Search all documents",
                [XRegexButton] = "Regex"
            };


            _options = new HashSet<string>();
            _documentFilters = new HashSet<string>();
            _authorFilters = new HashSet<string>();

            var placementMode = PlacementMode.Bottom;

            var t1 = new ToolTip
            {
                Content = "Case sensitive",
                Placement = placementMode
            };
            ToolTipService.SetToolTip(XCaseSensButton,t1);
            var t2 = new ToolTip
            {
                Content = "Match whole word",
                Placement = placementMode
            };
            ToolTipService.SetToolTip(XMatchWordButton,t2);
            var t3 = new ToolTip
            {
                Content = "Use Regex",
                Placement = placementMode
            };
            ToolTipService.SetToolTip(XRegexButton,t3);
            var t4 = new ToolTip
            {
                Content = "Clear all filters",
                Placement = placementMode
            };
            ToolTipService.SetToolTip(XClearFiltersButton,t4);
            var t5 = new ToolTip
            {
                Content = "Search all documents",
                Placement = placementMode
            };
            ToolTipService.SetToolTip(XSearchAllButton, t5);

        }

        private void ShowToolTip(object sender, PointerRoutedEventArgs e)
        {
            if (sender is Button button && ToolTipService.GetToolTip(button) is ToolTip tip)
            {
                tip.IsOpen = true;
            }
            
        }

        private void HideToolTip(object sender, PointerRoutedEventArgs e)
        {
            if (sender is Button button && ToolTipService.GetToolTip(button) is ToolTip tip)
            {
                tip.IsOpen = false;
            }
             
        }

        #endregion

        #region AutoSuggestBox Events
        private DispatcherTimer _searchTimer = new DispatcherTimer();
        private void AutoSuggestBox_TextChanged(AutoSuggestBox sender, AutoSuggestBoxTextChangedEventArgs args)
        {
            // Only get results when it was a user typing, 
            // otherwise assume the value got filled in by TextMemberPath 
            // or the handler for SuggestionChosen.
            if (args.Reason == AutoSuggestionBoxTextChangeReason.UserInput)
            {
                //Set the ItemsSource to be your filtered dataset
                //sender.ItemsSource = dataset;

                _searchTimer.Start();

            }
        }

        private void AutoSuggestBox_SuggestionChosen(AutoSuggestBox sender, AutoSuggestBoxSuggestionChosenEventArgs args)
        {
            // Set sender.Text. You can use args.SelectedItem to build your text string.
            if (args.SelectedItem is SearchResultViewModel resultVM)
            {

            }
        }


        private void AutoSuggestBox_QuerySubmitted(AutoSuggestBox sender, AutoSuggestBoxQuerySubmittedEventArgs args)
        {
            if (!(args.ChosenSuggestion is SearchResultViewModel resultVm)) return;

            SplitFrame.HighlightDoc(resultVm.ViewDocument, SplitFrame.HighlightMode.Highlight, false);

            NavigateToSearchResult(resultVm);
            MainPage.Instance.Focus(FocusState.Programmatic);
            xAutoSuggestBox.Focus(FocusState.Programmatic);
            _selectedIndex = -1;
        }

        private void XAutoSuggestBox_OnGotFocus(object sender, RoutedEventArgs e)
        {
            if (!string.IsNullOrEmpty(xAutoSuggestBox.Text))
            {
                ExecuteDishSearch(sender as AutoSuggestBox);
            }
        }

        private void XAutoSuggestBox_OnLostFocus(object sender, RoutedEventArgs e)
        {
            //xAutoSuggestBox.Text = "";
            UnHighlightAllDocs();
            _selectedIndex = -1;
            if (!_arrowBlock)
            {
                MainPage.Instance.CollapseSearch();
                _arrowBlock = false;
            }
        }

        private void XAutoSuggestBox_OnDragEnter(object sender, DragEventArgs e)
        {
            if (e.DataView.HasDataOfType(Internal))
            {
                e.AcceptedOperation = DataPackageOperation.Link;
            }
        }

        // Changed AutoSuggestBox so that dragging in the document shows the id, rather than the typeinfo
        private void XAutoSuggestBox_OnDrop(object sender, DragEventArgs e)
        {
            if (e.DataView.TryGetLoneDocument(out DocumentController dragDoc))
            {
                xAutoSuggestBox.Text = xAutoSuggestBox.Text + dragDoc.Id;
            }

            e.Handled = true;
        }

        #endregion

        #region Other Events

        private void Grid_PointerEntered(object sender, PointerRoutedEventArgs e)
        {
            if (!(sender is Grid outerGrid && outerGrid.DataContext is SearchResultViewModel srvm)) return;
            var tip = new ToolTip()
            {
                Content = srvm.Path,
                Placement = PlacementMode.Left,
                VerticalOffset = 400
            };
            ToolTipService.SetToolTip(outerGrid, tip);
            tip.IsOpen = true;
            SplitFrame.HighlightDoc(srvm.ViewDocument, SplitFrame.HighlightMode.Highlight);
        }

        private void Grid_PointerExited(object sender, PointerRoutedEventArgs e)
        {
            if (!(sender is Grid outerGrid && outerGrid.DataContext is SearchResultViewModel srvm)) return;
            if (ToolTipService.GetToolTip(outerGrid) is ToolTip tip) tip.IsOpen = false;
            SplitFrame.HighlightDoc(srvm.ViewDocument, SplitFrame.HighlightMode.Unhighlight);
        }

        public DocumentController SearchForFirstMatchingDocument(string text, DocumentController thisController = null)
        {
            //var vms = SearchHelper.SearchOverCollection(text.ToLower(), thisController: thisController);

            //var first = vms
            //    .Where(doc =>
            //        doc?.DocumentCollection != null && doc.DocumentCollection != MainPage.Instance.MainDocument)
            //    .Take(maxSearchResultSize).ToArray();
            //foreach (var searchResultViewModel in first)
            //{
            //    return searchResultViewModel.ViewDocument;
            //}

            return null;

        }

        /// <summary>
        /// Called when we drag the entire search collection
        /// </summary>
        private void XCollDragIcon_OnDragStarting(UIElement sender, DragStartingEventArgs args)
        {
            // the drag contains an IEnumberable of view documents, we add it as a collection note displayed as a grid
            var docs = Search.Parse(xAutoSuggestBox.Text).Where(sr => !sr.Node.Parent?.ViewDocument.DocumentType.Equals(DashConstants.TypeStore.MainDocumentType) == true).Select(sr => sr.ViewDocument).ToList();

            var searchString = xAutoSuggestBox.Text;
            args.Data.SetDragModel(new DragDocumentModel(docs, CollectionViewType.Page, collection =>
            {
                collection.GetDataDocument().SetField<TextController>(KeyStore.SearchStringKey, searchString, true);
                var fields = collection.GetDereferencedField<ListController<DocumentController>>(KeyStore.DataKey, null);
                for (var index = 0; index < fields.Count; index++)
                {
                    var doc = fields[index];
                    doc.SetField(KeyStore.SearchOriginKey, docs[index], true);
                }
            }, true));

            // set the allowed operations
            args.AllowedOperations = DataPackageOperation.Link | DataPackageOperation.Copy;
            args.Data.RequestedOperation = DataPackageOperation.Link;

        }

        /// <summary>
        /// Called when we drag a single result from search
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="args"></param>
        private void SearchResult_OnDragStarting(UIElement sender, DragStartingEventArgs args)
        {
            Debug.Write("Dragging a copy into workspace...");
            var svm = (sender as FrameworkElement)?.DataContext as SearchResultViewModel;
            var dragModel = new DragDocumentModel(svm?.ViewDocument);
            // get the sender's view docs and set the key for the drag to a static const
            args.Data.SetDragModel(dragModel);

            // set the allowed operations
            args.AllowedOperations = DataPackageOperation.Link | DataPackageOperation.Copy;
            args.Data.RequestedOperation = DataPackageOperation.Copy;
        }

        public void ShowCollectionDrag(bool show)
        {
            if (show)
            {
                xCollectionDragBorder.Visibility = Visibility.Visible;

            }
            else
            {
                xCollectionDragBorder.Visibility = Visibility.Collapsed;

            }
        }


        private void XArrowBlock_OnTapped(object sender, TappedRoutedEventArgs e)
        {
            _arrowBlock = true;
            if (xSearchCodeBox.Visibility == Visibility.Visible)
            {
                var centX = (float)xArrow.ActualWidth / 2;
                var centY = (float)xArrow.ActualHeight / 2;
                //open search bar
                xArrow.Rotate(value: 0.0f, centerX: centX, centerY: centY, duration: 300, delay: 0,
                    easingType: EasingType.Default).Start();
                //collapse search bar
                xFadeAnimationOut.Begin();
                xSearchCodeBox.Visibility = Visibility.Collapsed;
                xFilterButton.Visibility = Visibility.Collapsed;
                XOptionsGrid.Visibility = Visibility.Collapsed;

            }
            else
            {
                var centX = (float)xArrow.ActualWidth / 2 + 1;
                var centY = (float)xArrow.ActualHeight / 2 + 1;
                //open search bar
                xArrow.Rotate(value: 90.0f, centerX: centX, centerY: centY, duration: 300, delay: 0,
                    easingType: EasingType.Default).Start();
                xSearchCodeBox.Visibility = Visibility.Visible;
                xFadeAnimationIn.Begin();
                xFilterButton.Visibility = Visibility.Visible;
                XOptionsGrid.Visibility = Visibility.Visible;
            }
        }

        private IEnumerable<DocumentController> runSearch()
        {
            string text = xAutoSuggestBox.Text;

            var itemsSource = (ObservableCollection<SearchResultViewModel>)xAutoSuggestBox.ItemsSource;
            itemsSource?.Clear();

            IEnumerable<SearchResult> searchRes;
            try
            {
                searchRes = Search.Parse(text).ToList();
            }
            catch (Exception e2)
            {
                searchRes = new List<SearchResult>();
            }
            var allDocs = searchRes.Select(f => f.ViewDocument).ToList();
            if (string.IsNullOrWhiteSpace(text)) return null;
            return allDocs.Where(f => f != MainPage.Instance.MainDocument.GetDataDocument().GetField(KeyStore.LastWorkspaceKey));//TODO Have optional way to specify if we wan't workspace (really just search options/parameters in general)
        }

        private void XSearchCode_OnKeyUp(object sender, KeyRoutedEventArgs e)
        {
            if (e.Key == VirtualKey.Enter)
            {
                //excute code on each result               
                var docs = runSearch();

                //create script
                var code = xSearchCode.Text;
                var script = "for (var doc in docs){ \r" + code + "\r }";

                //run script
                var scope = new DocumentScope();
                scope.DeclareVariable("docs", new ListController<DocumentController>(docs));
                var dsl = new DSL(scope);
                dsl.Run(script, true);
            }
        }

        private void XSearchCode_OnDragStarting(UIElement sender, DragStartingEventArgs args)
        {
            string text = xAutoSuggestBox.Text.Replace("\"", "\\\"");

            //open DishScriptEditView with search text
            string script = "var docs = search(\"" + text + "\"); \r for (var doc in docs){ \r" + xSearchCode.Text + "\r }";

            var note = new DishScriptBox(0, 0, 300, 400, script);

            args.Data.SetDragModel(new DragDocumentModel(note.Document));

            args.AllowedOperations = DataPackageOperation.Link | DataPackageOperation.Move | DataPackageOperation.Copy;
            args.Data.RequestedOperation = DataPackageOperation.Move | DataPackageOperation.Copy | DataPackageOperation.Link;
        }

        private void XDragScript_OnTapped(object sender, TappedRoutedEventArgs e)
        {
            string text = xAutoSuggestBox.Text.Replace("\"", "\\\"");

            //open DishScriptEditView with search text
            string script = "var docs = search(\"" + text + "\"); \r for (var doc in docs){ \r" + xSearchCode.Text + "\r }";


            var collection = MainPage.Instance.MainDocument.GetDataDocument().GetField<DocumentController>(KeyStore.LastWorkspaceKey);
            DishScriptBox note;
            if (collection.GetField<PointController>(KeyStore.PanPositionKey) == null)
            {
                note = new DishScriptBox(0, 0, 300, 4000, script);
            }
            else
            {
                var panPos = collection.GetField<PointController>(KeyStore.PanPositionKey).Data;
                var zoom = collection.GetField<PointController>(KeyStore.PanZoomKey).Data;
                note = new DishScriptBox((800 - panPos.X) / zoom.X, (500 - panPos.Y) / zoom.Y, 300, 400, script);//TODO this position should be based on the main doc views size

            }

            collection.AddToListField(KeyStore.DataKey, note.Document);
        }


        #endregion

        #region Search
        private void ExecuteDishSearch(AutoSuggestBox searchBox)
        {
            if (searchBox == null) return;

            UnHighlightAllDocs();

            //TODO This is going to screw up regex by making it impossible to specify regex with capital letters
            string text = searchBox.Text; //.ToLower();
            if (string.IsNullOrWhiteSpace(text)) return;

            var itemsSource = (ObservableCollection<SearchResultViewModel>)searchBox.ItemsSource;
            itemsSource?.Clear();

            IEnumerable<SearchResult> searchRes;
            try
            {
                searchRes = Search.Parse(text, useAll:_searchAll, options:_options).ToList();
            }
            catch (Exception)
            {
                searchRes = new List<SearchResult>();
            }
            foreach (var res in searchRes)
            {
                for (int i = 0; i < res.FormattedKeyRef.Count; i++)
                {
                    for (int j = 0; j < res.FormattedKeyRef.Count; j++)
                    {
                        if (i != j && res.FormattedKeyRef.ElementAt(i) == res.FormattedKeyRef.ElementAt(j) && res.RelevantText.ElementAt(i) == res.RelevantText.ElementAt(j))
                        {
                            res.FormattedKeyRef.RemoveAt(j);
                            res.RelevantText.RemoveAt(j);
                            j--;
                        }
                    }
                }
            }

            var map = new Dictionary<DocumentController, List<SearchResult>>();
            foreach (var searchResult in searchRes)
            {
                if (map.TryGetValue(searchResult.DataDocument, out var results))
                {
                    results.Add(searchResult);
                }
                else
                {
                    map[searchResult.DataDocument] = new List<SearchResult>() { searchResult };
                }
            }
            var docs = searchRes.Select(f => f.ViewDocument).ToList();

            //highlight doc results
            HighlightSearchResults(docs);
            foreach (var doc in docs)
            {
                doc.SetField<TextController>(KeyStore.SearchStringKey, text, true);
            }

            var vmGroups = new List<SearchResultViewModel>();

            foreach (var resList in map)
            {
                var res = resList.Value.First();
                if (res.ViewDocument.DocumentType.Equals(RichTextBox.DocumentType))
                {
                    res.DataDocument.SetField(CollectionDBView.SelectedKey, Search.SearchTerm.ConvertSearchTerms(res.RtfHighlight), true);
                }

                var docAuthor = res.DataDocument.GetAuthor();
                var docType = res.ViewDocument.GetDocType();

                if (_authorFilters.Count > 0)
                {
                    if (!_authorFilters.Contains(docAuthor))
                    {
                        continue;
                    }
                }

                if (_documentFilters.Count > 0)
                {
                    var trueDocumentFilters = new HashSet<string>();
                    foreach (var docFilter in _documentFilters)
                    {
                        trueDocumentFilters.Add(_filterDictionary[docFilter]);
                    }
                    if (!trueDocumentFilters.Contains(docType))
                    {
                        continue;
                    }
                }

                var newVm = DocumentSearchResultToViewModel(res);

                // removing copies 

                var numOfCopies = resList.Value.Count;
                newVm.Copies = numOfCopies;
                if (numOfCopies > 1)
                {
                    newVm.DropDownVisibility = "Visible";
                    foreach (var sr in resList.Value)
                    {
                        newVm.svmCopies.Add(DocumentSearchResultToViewModel(sr));
                    }
                }

                vmGroups.Add(newVm);
            }

            var first = vmGroups
                .Where(doc => doc.DocumentCollection?.DocumentType.Equals(DashConstants.TypeStore.MainDocumentType) != true)
                .Take(MaxSearchResultSize).ToArray();

            var docsToHighlight = new List<DocumentController>();

            foreach (var searchResultViewModel in first)
            {
                itemsSource?.Add(searchResultViewModel);
                docsToHighlight.Add(searchResultViewModel.ViewDocument);
            }

            HighlightSearchResults(docsToHighlight);

        }

        public static void HighlightSearchResults(List<DocumentController> docs, bool animate = false)
        {
            //highlight new search results
            foreach (var doc in docs)
            {
                //var id = doc.GetField<TextController>(KeyStore.SearchResultDocumentOutline.SearchResultIdKey).Data;

                //make border thickness of DocHighlight for each doc 8
                SplitFrame.HighlightDoc(doc, SplitFrame.HighlightMode.Highlight, unhighlightOthers: false);
            }
        }

        public static void UnHighlightAllDocs()
        {
            //TODO:call this when search is unfocused

            //list of all collections
            SplitFrame.UnhighlightAllDocs();

            //DocumentTree.MainPageTree.Select(node => node.DataDocument.SetField<TextController>(CollectionDBView.SelectedKey, "", true));
            foreach (var node in DocumentTree.MainPageTree)
            {
                var a = node.DataDocument;
                a.RemoveField(CollectionDBView.SelectedKey);
            }
        }

        private static SearchResultViewModel DocumentSearchResultToViewModel(SearchResult result)
        {
            var view = result.ViewDocument;
            string docTitle = view.ToString();
            int len = docTitle.Length > 10 ? 10 : docTitle.Length - 1;
            string suffix = len < docTitle.Length - 1 ? "..." : "";
            docTitle = docTitle.Substring(1, len) + suffix;
            var titles = result.FormattedKeyRef.Select(key => "Title:" + docTitle + ", Key:" + key).ToList();
            var svm = new SearchResultViewModel(result.Path, titles, result.RelevantText, result.ViewDocument, result.Node.Parent?.ViewDocument, true);
            return svm;
        }

        #endregion

        private void DocIcon_OnPointerWheelChanged(object sender, PointerRoutedEventArgs e)
        {
            var viewModel = ((sender as TextBlock)?.DataContext as SearchResultViewModel);
            bool forward = e.GetCurrentPoint(this).Properties.MouseWheelDelta > 0;
            if (forward) viewModel?.NextField();
            else viewModel?.PreviousField();

            e.Handled = true;
        }

        private void UIElement_OnTapped(object sender, TappedRoutedEventArgs e)
        {
            if (!((sender as Grid)?.DataContext is SearchResultViewModel resultVm)) return;
            SplitFrame.HighlightDoc(resultVm.ViewDocument, SplitFrame.HighlightMode.Highlight);
            NavigateToSearchResult(resultVm);
        }

        private void NavigateToSearchResult(SearchResultViewModel resultVm)
        {
            resultVm.ViewDocument.SetHidden(false);
            if (resultVm.DocumentCollection != null)
            {
                SplitFrame.OpenDocumentInWorkspace(resultVm.ViewDocument, resultVm.DocumentCollection);
            }
            else
            {
                SplitFrame.OpenInInactiveFrame(resultVm.ViewDocument);
            }
        }

        private void XAutoSuggestBox_OnKeyDown(object sender, KeyRoutedEventArgs e)
        {

        }

        private void XAutoSuggestBox_OnKeyUp(object sender, KeyRoutedEventArgs e)
        {
            switch (e.Key)
            {
            case VirtualKey.Down:
                if (_selectedIndex + 1 == xAutoSuggestBox.Items?.Count) _selectedIndex = -1;
                else _selectedIndex++;

                if (_selectedIndex != -1)
                {
                    DocumentController docTappedDown = (xAutoSuggestBox.Items?[_selectedIndex] as SearchResultViewModel)?.ViewDocument;
                    if (docTappedDown == null) return;

                    SplitFrame.HighlightDoc(docTappedDown, SplitFrame.HighlightMode.Highlight);
                }

                break;
            case VirtualKey.Up:
                if (_selectedIndex == -1) _selectedIndex = xAutoSuggestBox.Items.Count - 1;
                else _selectedIndex--;

                if (_selectedIndex != -1)
                {
                    DocumentController docTappedUp = (xAutoSuggestBox.Items?[_selectedIndex] as SearchResultViewModel)?.ViewDocument;
                    if (docTappedUp == null) return;

                    SplitFrame.HighlightDoc(docTappedUp, SplitFrame.HighlightMode.Highlight);
                }

                break;
            }
        }

        private void XDropDown_OnPointerPressed(object sender, TappedRoutedEventArgs e)
        {
            var viewModel = ((sender as TextBlock)?.DataContext as SearchResultViewModel);
            var itemsSource = (ObservableCollection<SearchResultViewModel>)xAutoSuggestBox.ItemsSource;
            var index = itemsSource.IndexOf(viewModel);
            var count = itemsSource.Count;
            var numCopies = viewModel.Copies;
            if (viewModel?.DropDownText == ">")
            {
                viewModel.DropDownText = "v";
                int counter = 0;
                foreach (var svm in viewModel.svmCopies)
                {
                    if (index == count)
                    {
                        itemsSource?.Add(svm);
                    }
                    else
                    {
                        itemsSource.Insert(index + 1, svm);
                    }

                    if (counter == 0)
                    {
                        svm.BorderThickness = "0 0 0 1";
                    }
                    if (counter == numCopies - 1)
                    {
                        svm.BorderThickness = "0 1 0 0";
                    }

                    counter++;
                    //svm.BorderThickness = "0 0 0 1";
                }
            }
            else if (viewModel?.DropDownText == "v")
            {
                viewModel.DropDownText = ">";
                foreach (var svm in viewModel.svmCopies)
                {
                    itemsSource?.Remove(svm);
                }
            }

            e.Handled = true;

        }

        private void Filter_Tapped(object sender, RoutedEventArgs e)
        {
            FlyoutBase.ShowAttachedFlyout((FrameworkElement)sender);
        }

        #region Clean Filters

        private void SetFilterText()
        {
            int numFilters = _authorFilters.Count + _documentFilters.Count;
            if (numFilters == 0)
            {
                xFilterButton.Content = "Filter by:";
            }
            else
            {
                xFilterButton.Content = "Filter by: (" + numFilters + ")";
            }

        }

        private void Document_OnClick(object sender, TappedRoutedEventArgs e)
        {

            if (sender is MenuFlyoutItem mf)
            {
                var document = mf.Text;
                if (_documentFilters.Contains(document))
                {
                    _documentFilters.Remove(document);
                    mf.FontWeight = Windows.UI.Text.FontWeights.Normal;
                }
                else
                {
                    _documentFilters.Add(document);
                    mf.FontWeight = Windows.UI.Text.FontWeights.Bold;
                }
                SetFilterText();
            }
        }
        private void Author_OnClick(object sender, RoutedEventArgs routedEventArgs)
        {
            if (sender is MenuFlyoutItem mf)
            {
                string author = mf.Text;
                if (_authorFilters.Contains(author))
                {
                    _authorFilters.Remove(author);
                    mf.FontWeight = Windows.UI.Text.FontWeights.Normal;
                }
                else
                {
                    _authorFilters.Add(author);
                    mf.FontWeight = Windows.UI.Text.FontWeights.Bold;
                }
                SetFilterText();
            }
        }

        private MenuFlyoutSubItem _authorItem;

        private void FlyoutBase_OnOpen(object sender, object e)
        {
            if (sender is MenuFlyout flyout)
            {
                if (_authorItem != null)
                {
                    flyout.Items.Remove(_authorItem);
                }
                _authorItem = new MenuFlyoutSubItem() { Text = "Author" };
                var nodes = DocumentTree.MainPageTree;
                var currentAuthors = new HashSet<string>();
                // add authors to current authors
                foreach (var node in nodes)
                {
                    if (!node.ViewDocument.DocumentType.Equals(DashConstants.TypeStore.MainDocumentType) == true)
                    {
                        if (!node.Parent.ViewDocument.DocumentType.Equals(DashConstants.TypeStore.MainDocumentType) ==
                            true)
                        {
                            var author = node.DataDocument.GetAuthor();
                            if (!currentAuthors.Contains(author) && author != null)
                            {
                                currentAuthors.Add(author);
                            }
                        }
                    }
                }
                foreach (var auth in currentAuthors)
                {
                    var authorItem = new MenuFlyoutItem
                    {
                        Text = auth,
                    };
                    _authorItem?.Items?.Add(authorItem);
                    if (_authorFilters.Contains(auth))
                    {
                        authorItem.FontWeight = Windows.UI.Text.FontWeights.Bold;
                    }
                    authorItem.Click += Author_OnClick;
                }
                flyout.Items.Add(_authorItem);
            }
        }

        private void Clear_Filters()
        {
            _authorFilters.Clear();
            foreach (var filter in _documentFilters)
            {
                var mfi = _filtertoMenuFlyout[filter];
                mfi.FontWeight = Windows.UI.Text.FontWeights.Normal;
            }
            _documentFilters.Clear();
            SetFilterText();
        }

        private void Clear_Options()
        {
            foreach (var option in _options)
            {
                var bt = _optiontofilter.FirstOrDefault(x => x.Value == option).Key;
                bt.BorderBrush = new SolidColorBrush(Colors.Transparent);

            }
            _options.Clear();
            _searchAll = false;
        }

        #endregion


        private void XRegexButton_OnClick(object sender, RoutedEventArgs e)
        {
            if (sender is Button bt)
            {
                XOptionButton_OnClick(sender,e);
            }
        }

        private void XSearchAllButton_OnClick(object sender, RoutedEventArgs e)
        {
                _searchAll = !_searchAll;
                XOptionButton_OnClick(sender, e);
        }

        private void XClearFiltersButton_OnClick(object sender, RoutedEventArgs e)
        {
            Clear_Filters();
            Clear_Options();
        }

        private void XOptionButton_OnClick(object sender, RoutedEventArgs e)
        {
            if (sender is Button bt)
            {
                var option = _optiontofilter[bt];
                if (_options.Contains(option))
                {
                    _options.Remove(option);
                    bt.BorderBrush = new SolidColorBrush(Colors.Transparent);
                }
                else
                {
                    _options.Add(option);
                    bt.BorderBrush = new SolidColorBrush(Colors.DarkOrange);
                }
            }
        }


    }
}

