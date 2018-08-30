using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Windows.ApplicationModel.DataTransfer;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using DashShared;
using Visibility = Windows.UI.Xaml.Visibility;
using System;
using Windows.System;
using Windows.UI.Xaml.Input;
using Microsoft.Toolkit.Uwp.UI.Animations;
using System.Threading.Tasks;
using static Dash.DataTransferTypeInfo;

// The User Control item template is documented at https://go.microsoft.com/fwlink/?LinkId=234236

namespace Dash
{
    public sealed partial class MainSearchBox
    {
        private int _selectedIndex = -1;
        private bool _arrowBlock = false;

        #region Definition and Initilization
        public const int MaxSearchResultSize = 75;
        //private CancellationTokenSource _tokenSource = new CancellationTokenSource();

        public MainSearchBox()
        {
            InitializeComponent();
            xAutoSuggestBox.ItemsSource = new ObservableCollection<SearchResultViewModel>();
        }
        #endregion

        #region AutoSuggestBox Events
        private async void AutoSuggestBox_TextChanged(AutoSuggestBox sender, AutoSuggestBoxTextChangedEventArgs args)
        {
            // Only get results when it was a user typing, 
            // otherwise assume the value got filled in by TextMemberPath 
            // or the handler for SuggestionChosen.
            if (args.Reason == AutoSuggestionBoxTextChangeReason.UserInput)
            {
                //Set the ItemsSource to be your filtered dataset
                //sender.ItemsSource = dataset;

                int length = sender.Text.Length;
                // Delay so that the user won't be slowed down by unnecessary searches unless they've been idle
                await Task.Delay(300);
                if (length == sender.Text.Length)
                {
                    ExecuteDishSearch(sender);
                }

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

            MainPage.Instance.HighlightDoc(resultVm.ViewDocument, false);

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

        private void Grid_PointerEntered(object sender, Windows.UI.Xaml.Input.PointerRoutedEventArgs e)
        {
            var viewModel = (sender as Grid)?.DataContext as SearchResultViewModel;
            DocumentController docTapped = viewModel?.ViewDocument;

            foreach (object res in xAutoSuggestBox.Items)
            {
                MainPage.Instance.HighlightDoc(((SearchResultViewModel) res).ViewDocument, false);
            }

            MainPage.Instance.HighlightDoc(docTapped, true);
        }

        private void Grid_PointerExited(object sender, Windows.UI.Xaml.Input.PointerRoutedEventArgs e)
        {
            var viewModel = (sender as Grid)?.DataContext as SearchResultViewModel;
            DocumentController docTapped = viewModel?.ViewDocument;
            MainPage.Instance.HighlightDoc(docTapped, false);
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
            var docs = Search.Parse(xAutoSuggestBox.Text).Where(sr => !sr.Node.Parent?.ViewDocument.DocumentType.Equals(DashConstants.TypeStore.MainDocumentType) == true).Select(sr => sr.ViewDocument.GetViewCopy()).ToList();

            args.Data.AddDragModel(new DragDocumentModel(docs, CollectionView.CollectionViewType.Page));

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
            var dragModel = new DragDocumentModel(((sender as FrameworkElement)?.DataContext as SearchResultViewModel)?.ViewDocument, true);
            // get the sender's view docs and set the key for the drag to a static const
            args.Data.AddDragModel(dragModel);

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
               
            }
            else
            {
                var centX = (float)xArrow.ActualWidth / 2 + 1;
                var centY = (float) xArrow.ActualHeight / 2 + 1;
                //open search bar
                xArrow.Rotate(value: 90.0f, centerX: centX, centerY: centY, duration: 300, delay: 0,
                    easingType: EasingType.Default).Start();
                xSearchCodeBox.Visibility = Visibility.Visible;
                xFadeAnimationIn.Begin();
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
            return allDocs.Where(f => f != MainPage.Instance.MainDocument.GetField(KeyStore.LastWorkspaceKey));//TODO Have optional way to specify if we wan't workspace (really just search options/parameters in general)
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
                var scope = new OuterReplScope();
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

            args.Data.AddDragModel(new DragDocumentModel(note.Document, true));

            args.AllowedOperations = DataPackageOperation.Link | DataPackageOperation.Move | DataPackageOperation.Copy;
            args.Data.RequestedOperation = DataPackageOperation.Move | DataPackageOperation.Copy | DataPackageOperation.Link;
        }

        private void XDragScript_OnTapped(object sender, TappedRoutedEventArgs e)
        {
            string text = xAutoSuggestBox.Text.Replace("\"", "\\\"");

            //open DishScriptEditView with search text
            string script = "var docs = search(\"" + text + "\"); \r for (var doc in docs){ \r" + xSearchCode.Text + "\r }";


            var collection = MainPage.Instance.MainDocument.GetField<DocumentController>(KeyStore.LastWorkspaceKey);
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
        private static void ExecuteDishSearch(AutoSuggestBox searchBox)
        {
            if (searchBox == null) return;

            UnHighlightAllDocs();
            
            //TODO This is going to screw up regex by making it impossible to specify regex with capital letters
            string text = searchBox.Text; //.ToLower();

            var itemsSource = (ObservableCollection<SearchResultViewModel>) searchBox.ItemsSource;
            itemsSource?.Clear();

            IEnumerable<SearchResult> searchRes;
            try
            {
                searchRes = Search.Parse(text).ToList();
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
            var docs = searchRes.Select(f => f.ViewDocument).ToList();
            if (string.IsNullOrWhiteSpace(text)) return;
            //highlight doc results
            HighlightSearchResults(docs);

            var vmGroups = new List<SearchResultViewModel>();
            foreach (SearchResult res in searchRes)
            {
                if (res.ViewDocument.DocumentType.Equals(RichTextBox.DocumentType))
                {
                    res.DataDocument.SetField(CollectionDBView.SelectedKey, Search.SearchTerm.ConvertSearchTerms(res.RtfHighlight), true);
                }
                SearchResultViewModel newVm = DocumentSearchResultToViewModel(res);
                DocumentController parent = res.Node.Parent?.ViewDocument;
                if (parent != null) newVm.DocumentCollection = parent;
                vmGroups.Add(newVm);
            }

            var first = vmGroups 
                .Where(doc => /*doc?.DocumentCollection != null && */!doc.DocumentCollection?.DocumentType.Equals(DashConstants.TypeStore.MainDocumentType) == true)
                .Take(MaxSearchResultSize).ToArray();

            foreach (SearchResultViewModel searchResultViewModel in first) { itemsSource?.Add(searchResultViewModel); }
        }

        public static void HighlightSearchResults(List<DocumentController> docs, bool animate = false)
        {
            //highlight new search results
            foreach (var doc in docs)
            {
                //var id = doc.GetField<TextController>(KeyStore.SearchResultDocumentOutline.SearchResultIdKey).Data;
                var id = doc.Id;
                DocumentController resultDoc = ContentController<FieldModel>.GetController<DocumentController>(id);

                //make border thickness of DocHighlight for each doc 8
                MainPage.Instance.HighlightDoc(resultDoc, false, 1, animate);
            }
        }

        public static void UnHighlightAllDocs()
        {
            //TODO:call this when search is unfocused

            //list of all collections
            var allCollections = MainPage.Instance.MainDocument.GetField<ListController<DocumentController>>(KeyStore.DataKey).TypedData;

            foreach (var coll in allCollections)
            {
                UnHighlightDocs(coll);
            }

            //DocumentTree.MainPageTree.Select(node => node.DataDocument.SetField<TextController>(CollectionDBView.SelectedKey, "", true));
            foreach (var node in DocumentTree.MainPageTree)
            {
                var a = node.DataDocument;
                if (a.GetField(CollectionDBView.SelectedKey) != null)
                {
                    a.SetField(CollectionDBView.SelectedKey, new ListController<TextController>(new TextController("")), true);
                }
            }
        }

        public static void UnHighlightDocs(DocumentController coll)
        {
            var colDocs = coll.GetDataDocument().GetDereferencedField<ListController<DocumentController>>(KeyStore.DataKey, null);
            //unhighlight each doc in collection
            MainPage.Instance.HighlightDoc(coll, false, 2);
            if (colDocs != null)
                foreach (DocumentController doc in colDocs)
                {
                   UnHighlightDocs(doc);
                }
        }

        private static SearchResultViewModel DocumentSearchResultToViewModel(SearchResult result)
        {
            string docTitle = result.ViewDocument.ToString();
            int len = docTitle.Length > 10 ? 10 : docTitle.Length - 1;
            string suffix = len < docTitle.Length - 1 ? "..." : "";
            docTitle = docTitle.Substring(1, len) + suffix;
            var titles = result.FormattedKeyRef.Select(key => docTitle + key).ToList();
            return new SearchResultViewModel(titles, result.RelevantText, result.ViewDocument, null, true);
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
            MainPage.Instance.HighlightDoc(resultVm.ViewDocument, false);
            NavigateToSearchResult(resultVm);
        }

        private void NavigateToSearchResult(SearchResultViewModel resultVm)
        {
            var navigated = false;
            resultVm.ViewDocument.SetHidden(false);
            if (resultVm.DocumentCollection != null)
            {
                var currentWorkspace = MainPage.Instance.MainDocument.GetField<DocumentController>(KeyStore.LastWorkspaceKey);
                if (!currentWorkspace.GetDataDocument().Equals(resultVm.DocumentCollection.GetDataDocument()))
                {
                    MainPage.Instance.SetCurrentWorkspaceAndNavigateToDocument(resultVm.DocumentCollection, resultVm.ViewDocument);
                }
                else
                {
                    navigated = MainPage.Instance.NavigateToDocumentInWorkspace(resultVm.ViewDocument, true, false);
                }
            }
            else
            {
                navigated = MainPage.Instance.NavigateToDocumentInWorkspace(resultVm.ViewDocument, true, false);
            }

            if (!navigated)
            {
                MainPage.Instance.DockManager.Dock(resultVm.ViewDocument, DockDirection.Right);
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
                    foreach (object res in xAutoSuggestBox.Items)
                    {
                        MainPage.Instance.HighlightDoc(((SearchResultViewModel)res).ViewDocument, false);
                    }

                    if (_selectedIndex + 1 == xAutoSuggestBox.Items?.Count) _selectedIndex = -1;
                    else _selectedIndex++;

                    if (_selectedIndex != -1)
                    {
                        DocumentController docTappedDown = (xAutoSuggestBox.Items?[_selectedIndex] as SearchResultViewModel)?.ViewDocument;
                        if (docTappedDown == null) return;

                        MainPage.Instance.HighlightDoc(docTappedDown, true);
                    }

                    break;
                case VirtualKey.Up:
                    foreach (object res in xAutoSuggestBox.Items)
                    {
                        MainPage.Instance.HighlightDoc(((SearchResultViewModel)res).ViewDocument, false);
                    }

                    if (_selectedIndex == -1) _selectedIndex = xAutoSuggestBox.Items.Count - 1;
                    else _selectedIndex--;

                    if (_selectedIndex != -1)
                    {
                        DocumentController docTappedUp = (xAutoSuggestBox.Items?[_selectedIndex] as SearchResultViewModel)?.ViewDocument;
                        if (docTappedUp == null) return;

                        MainPage.Instance.HighlightDoc(docTappedUp, true);
                    }

                    break;
            }
        }
    }
}

