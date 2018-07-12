using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Windows.ApplicationModel.DataTransfer;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using DashShared;
using Visibility = Windows.UI.Xaml.Visibility;
using Dash.Models.DragModels;
using System.Diagnostics;

// The User Control item template is documented at https://go.microsoft.com/fwlink/?LinkId=234236

namespace Dash
{
    public sealed partial class MainSearchBox
    {
        public const int MaxSearchResultSize = 75;
        //private CancellationTokenSource _tokenSource = new CancellationTokenSource();
        private string _currentSearch = "";
        public const string SearchCollectionDragKey = "Search Collection";


        public MainSearchBox()
        {
            InitializeComponent();
            xAutoSuggestBox.ItemsSource = new ObservableCollection<SearchResultViewModel>();
        }

        private void AutoSuggestBox_TextChanged(AutoSuggestBox sender, AutoSuggestBoxTextChangedEventArgs args)
        {
            if (sender.Text.Equals("Dash.SearchResultViewModel"))
            {
                sender.Text = _currentSearch;
                return;
            }

            // Only get results when it was a user typing, 
            // otherwise assume the value got filled in by TextMemberPath 
            // or the handler for SuggestionChosen.
            if (args.Reason == AutoSuggestionBoxTextChangeReason.UserInput)
            {
                //Set the ItemsSource to be your filtered dataset
                //sender.ItemsSource = dataset;

                ExecuteDishSearch(sender);

            }

            _currentSearch = sender.Text.ToLower();
        }

        private static void ExecuteDishSearch(AutoSuggestBox searchBox)
        {
            if (searchBox == null) return;

            UnHighlightAllDocs();

            //TODO This is going to screw up regex by making it impossible to specify regex with capital letters
            string text = searchBox.Text; //.ToLower();

            var itemsSource = (ObservableCollection<SearchResultViewModel>)searchBox.ItemsSource;
            itemsSource?.Clear();

            if (string.IsNullOrWhiteSpace(text)) return;

            var searchRes = Search.Parse(text).ToList();
            var docs = searchRes.Select(f => f.ViewDocument).ToList();

            //highlight doc results
            HighlightSearchResults(docs);

            var vms = new List<SearchResultViewModel>();
            foreach (var res in searchRes)
            {
                var newVm = SearchHelper.DocumentSearchResultToViewModel(res);
                var parent = res.Node.Parent?.ViewDocument;
                if (parent != null) newVm.DocumentCollection = parent;
                vms.Add(newVm);
            }

            var first = vms
                .Where(doc => doc?.DocumentCollection != null && doc.DocumentCollection != MainPage.Instance.MainDocument)
                .Take(MaxSearchResultSize).ToArray();

            foreach (var searchResultViewModel in first) { itemsSource?.Add(searchResultViewModel); }
        }

        public static void HighlightSearchResults(List<DocumentController> docs)
        {
            //highlight new search results
            foreach (var doc in docs)
            {
                //var id = doc.GetField<TextController>(KeyStore.SearchResultDocumentOutline.SearchResultIdKey).Data;
                var id = doc.Id;
                DocumentController resultDoc = ContentController<FieldModel>.GetController<DocumentController>(id);

                //make border thickness of DocHighlight for each doc 8
                MainPage.Instance.HighlightDoc(resultDoc, false, 1);
            }
        }

        public static void UnHighlightAllDocs()
        {

            //TODO:call this when search is unfocused

            //list of all collections
            var allCollections =
                MainPage.Instance.MainDocument.GetField<ListController<DocumentController>>(KeyStore.DataKey).TypedData;

            foreach (var coll in allCollections)
            {
                UnHighlightDocs(coll);
            }
        }

        public static void UnHighlightDocs(DocumentController coll)
        {
            var colDocs = coll.GetDataDocument().GetDereferencedField<ListController<DocumentController>>(KeyStore.DataKey, null).TypedData;
            //unhighlight each doc in collection
            foreach (var doc in colDocs)
            {
                MainPage.Instance.HighlightDoc(doc, false, 2);
                if (doc.DocumentType.ToString() == "Collection Box")
                {
                    UnHighlightDocs(doc);
                }
            }
        }

        private void Grid_PointerEntered(object sender, Windows.UI.Xaml.Input.PointerRoutedEventArgs e)
        {
            var docTapped = ((sender as Grid)?.DataContext as SearchResultViewModel)?.ViewDocument;
            MainPage.Instance.HighlightDoc(docTapped, true);
        }

        private void Grid_PointerExited(object sender, Windows.UI.Xaml.Input.PointerRoutedEventArgs e)
        {
            var docTapped = ((sender as Grid)?.DataContext as SearchResultViewModel)?.ViewDocument;
            MainPage.Instance.HighlightDoc(docTapped, false);
        }

        public DocumentController SearchForFirstMatchingDocument(string text, DocumentController thisController = null)
        {
            var maxSearchResultSize = 75;
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

        private void AutoSuggestBox_SuggestionChosen(AutoSuggestBox sender, AutoSuggestBoxSuggestionChosenEventArgs args)
        {
            // Set sender.Text. You can use args.SelectedItem to build your text string.
            if (args.SelectedItem is SearchResultViewModel resultVM)
            {

            }
        }


        private void AutoSuggestBox_QuerySubmitted(AutoSuggestBox sender, AutoSuggestBoxQuerySubmittedEventArgs args)
        {
            if (args.ChosenSuggestion != null)
            {
                sender.Text = _currentSearch;
                if (!(args.ChosenSuggestion is SearchResultViewModel resultVm)) return;
                if (resultVm.DocumentCollection != null)
                {
                    var currentWorkspace = MainPage.Instance.MainDocument.GetField<DocumentController>(KeyStore.LastWorkspaceKey);
                    if (!currentWorkspace.GetDataDocument().Equals(resultVm.DocumentCollection.GetDataDocument()))
                    {
                        MainPage.Instance.SetCurrentWorkspaceAndNavigateToDocument(resultVm.DocumentCollection, resultVm.ViewDocument);
                    }
                }

                MainPage.Instance.NavigateToDocumentInWorkspaceAnimated(resultVm.ViewDocument);
            }
            else
            {
                sender.Text = _currentSearch;
                // Use args.QueryText to determine what to do.
            }
        }

        private void XAutoSuggestBox_OnGotFocus(object sender, RoutedEventArgs e)
        {
            if (!string.IsNullOrEmpty(_currentSearch))
            {
                ExecuteDishSearch(sender as AutoSuggestBox);
            }
        }

        /// <summary>
        /// Called when we drag the entire search collection
        /// </summary>
        private void XCollDragIcon_OnDragStarting(UIElement sender, DragStartingEventArgs args)
        {
            //    // the drag contains an IEnumberable of view documents, we add it as a collection note displayed as a grid
            //    var docs = SearchHelper.SearchOverCollection(_currentSearch)
            //        .Select((srvm) => srvm.ViewDocument.GetViewCopy());

            //    args.Data.Properties[nameof(DragCollectionFieldModel)] =
            //        new DragCollectionFieldModel(docs.ToList(), null, null, CollectionView.CollectionViewType.Grid);

            //    // set the allowed operations
            //    args.AllowedOperations = DataPackageOperation.Link | DataPackageOperation.Copy;
            //    args.Data.RequestedOperation = DataPackageOperation.Copy;

        }

    /// <summary>
    /// Called when we drag a single result from search
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="args"></param>
    private void SearchResult_OnDragStarting(UIElement sender, DragStartingEventArgs args)
        {
            var dragModel =
                new DragDocumentModel(
                    ((sender as FrameworkElement)?.DataContext as SearchResultViewModel)?.ViewDocument, true);
            // get the sender's view docs and set the key for the drag to a static const
            args.Data.Properties[nameof(DragDocumentModel)] = dragModel;

            // set the allowed operations
            args.AllowedOperations = DataPackageOperation.Link | DataPackageOperation.Copy;
            args.Data.RequestedOperation = DataPackageOperation.Copy;
        }

        public void ShowCollectionDrag(bool show)
        {
            if (show)
            {
                xCollectionDragBorder.Visibility = Visibility.Visible;
                ;
            }
            else
            {
                xCollectionDragBorder.Visibility = Visibility.Collapsed;
                ;
            }
        }

        private void XAutoSuggestBox_OnDragEnter(object sender, DragEventArgs e)
        {
            if (e.DataView.Properties.ContainsKey(nameof(DragDocumentModel)))
            {
                e.AcceptedOperation = DataPackageOperation.Link;
            }
        }

        //// Changed AutoSuggestBox so that dragging in the document shows the id, rather than the typeinfo
        private void XAutoSuggestBox_OnDrop(object sender, DragEventArgs e)
        {
            if (e.DataView.Properties.ContainsKey(nameof(DragDocumentModel)))
            {
                var dragData = (DragDocumentModel)e.DataView.Properties[nameof(DragDocumentModel)];
                var doc = dragData.DraggedDocument;
                xAutoSuggestBox.Text = xAutoSuggestBox.Text + doc.Id;
                /*
                var listKeys = doc.EnumDisplayableFields()
                    .Where(kv => doc.GetRootFieldType(kv.Key).HasFlag(TypeInfo.List)).Select(kv => kv.Key).ToList();
                if (listKeys.Count == 1)
                {
                    var currText = xAutoSuggestBox.Text;
                    xAutoSuggestBox.Text = "in:" + doc.Title.Split()[0];
                    if (!string.IsNullOrWhiteSpace(currText))
                    {
                        xAutoSuggestBox.Text = xAutoSuggestBox.Text + "  " + currText;
                    }
                }
                */
            }

            e.Handled = true;
        }


        /// <summary>
        /// public static class for encapsulating all the search code
        /// </summary>
        public static class SearchHelper
        {
            /// <summary>
            /// this criteria simple tells us which key and value pair to look at
            /// </summary>
            private class SpecialSearchCriteria
            {
                public string SearchCategory { get; set; }
                public string SearchText { get; set; }
            }

            private class SearchCriteria : EntityBase
            {
                public string SearchText { get; set; }
            }

            public static SearchResultViewModel DocumentSearchResultToViewModel(SearchResult res)
            {
                string title = res.ViewDocument.ToString().Substring(1) + res.TitleAppendix; // .GetField<TextController>(KeyStore.SearchResultDocumentOutline.SearchResultTitleKey)?.Data;
                string helpText = res.RelevantText;

                return new SearchResultViewModel(title, helpText, res.ViewDocument, null, true);
            }

            ///// <summary>
            ///// Supposed to handle all searches that are for key-value specified searches.   currenly just returns the generic special search.
            ///// If more search capabilities are desired, probably should put them in here.
            ///// </summary>
            ///// <param name="criteria"></param>
            ///// <returns></returns>
            //private static IEnumerable<SearchResultViewModel> SpecialSearch(SpecialSearchCriteria criteria)
            //{
            //    if (criteria.SearchCategory == "in")
            //    {
            //        return CollectionMembershipSearch(criteria);
            //    }
            //    if (criteria.SearchCategory == "near")
            //    {
            //        return GroupMembershipSearch(criteria);
            //    }
            //    if (criteria.SearchCategory == "rtf" ||
            //        criteria.SearchCategory == "rt" ||
            //        criteria.SearchCategory == "richtext" ||
            //        criteria.SearchCategory == "richtextformat")
            //    {
            //        return RichTextContains(criteria);
            //    }
            //    return GenericSpecialSearch(criteria);
            //}

            //private static IEnumerable<SearchResultViewModel> RichTextContains(SpecialSearchCriteria criteria)
            //{
            //    var tree = DocumentTree.MainPageTree;
            //    return LocalSearch("").Where(vm => tree.GetNodeFromViewId(vm?.ViewDocument?.Id) != null &&
            //                                       (tree.GetNodeFromViewId(vm.ViewDocument.Id).DataDocument
            //                                           .EnumFields(false)
            //                                           .Any(f => (f.Value is RichTextController) && !
            //                                                         ((RichTextController)f.Value)
            //                                                         .SearchForStringInRichText(criteria.SearchText)
            //                                                         .StringFound)));
            //}

            //private static IEnumerable<SearchResultViewModel> GroupMembershipSearch(SpecialSearchCriteria criteria)
            //{
            //    var tree = DocumentTree.MainPageTree;
            //    var local = LocalSearch(criteria.SearchText).ToArray();
            //    return local
            //        .SelectMany(i => (tree.GetNodeFromViewId(i.ViewDocument.Id)?.GroupPeers ?? new DocumentNode[0]).Concat(tree.GetNodesFromDataDocumentId(i.ViewDocument.GetDataDocument().Id)?.SelectMany(k => k.GroupPeers) ?? new DocumentNode[0]))
            //        .DistinctBy(d => d.Id).SelectMany(i => MakeAdjacentSearchResultViewModels(i, criteria, tree, null));
            //    /*
            //    var tree = DocumentTree.MainPageTree;
            //    var localSearch = LocalSearch(criteria.SearchText).Where(vm => tree[vm?.ViewDocument?.Id] != null).ToArray();
            //    var map = new Dictionary<DocumentNode, SearchResultViewModel>();
            //    foreach (var vm in localSearch)
            //    {
            //        foreach(var peer in tree[vm.ViewDocument.Id].GroupPeers)
            //        {
            //            map[peer] = vm;
            //        }
            //    }
            //    var allPeers = localSearch.SelectMany(vm => tree[vm.ViewDocument.Id].GroupPeers).DistinctBy(i => i.Id).ToArray();

            //    return allPeers.Select(node => MakeAdjacentSearchResultViewModel(node, criteria, tree, map[node]));*/
            //}

            //private static SearchResultViewModel[] MakeAdjacentSearchResultViewModels(DocumentNode node,
            //    SpecialSearchCriteria criteria, DocumentTree tree, SearchResultViewModel foundVm)
            //{
            //    return CreateSearchResults(tree, node.DataDocument,
            //        "Found near: " + (foundVm?.Title ?? criteria.SearchText),
            //        node.DataDocument.GetDereferencedField<TextController>(KeyStore.TitleKey, null)?.Data);
            //}

            //private static IEnumerable<SearchResultViewModel> CollectionMembershipSearch(SpecialSearchCriteria criteria)
            //{
            //    return CollectionMembershipSearch(criteria.SearchText);
            //}

            //private static IEnumerable<SearchResultViewModel> CollectionMembershipSearch(string collectionNameToFind)
            //{
            //    var tree = DocumentTree.MainPageTree;
            //    return LocalSearch("").Where(vm => vm?.DocumentCollection != null &&
            //                                       (GetTitleOfCollection(tree, vm.DocumentCollection) ?? "").ToLower()
            //                                       .Contains(collectionNameToFind));
            //}

            ///// <summary>
            ///// Get the search results for a part of search trying to specify keys/value pairs
            ///// </summary>
            ///// <param name="criteria"></param>
            ///// <returns></returns>
            //private static IEnumerable<SearchResultViewModel> GenericSpecialSearch(SpecialSearchCriteria criteria)
            //{
            //    var documentTree = DocumentTree.MainPageTree;

            //    var negateCategory = criteria.SearchCategory.StartsWith('!');
            //    var searchCategory = criteria.SearchCategory.TrimStart('!');

            //    List<DocumentController> docControllers = new List<DocumentController>();
            //    foreach (var documentController in ContentController<FieldModel>.GetControllers<DocumentController>())
            //    {
            //        var hasField = false;
            //        foreach (var kvp in documentController.EnumFields())
            //        {
            //            var contains = kvp.Key.Name.ToLower().Contains(searchCategory);
            //            if (!contains) continue;
            //            hasField = true;
            //            var stringSearch = kvp.Value.SearchForString(criteria.SearchText);
            //            if ((stringSearch.StringFound && !negateCategory) || (!stringSearch.StringFound && negateCategory))
            //            {
            //                docControllers.Add(documentController);
            //            }
            //        }
            //        if (negateCategory && string.IsNullOrEmpty(criteria.SearchText) && !hasField)
            //        {
            //            foreach (var kvp in documentController.GetDataDocument().EnumFields())
            //            {
            //                var contains = kvp.Key.Name.ToLower().Contains(searchCategory);
            //                if (contains)
            //                {
            //                    hasField = true;
            //                }
            //            }
            //            if (!hasField)
            //                docControllers.Add(documentController);
            //        }
            //    }

            //    var results = new List<SearchResultViewModel>();
            //    foreach (var docController in docControllers)
            //    {
            //        var title = docController.Title;

            //        if (documentTree.GetNodeFromViewId(docController.Id) != null && documentTree.GetNodeFromViewId(docController.Id).DataDocument
            //                .GetField<ListController<DocumentController>>(KeyStore.DataKey) != null)
            //        {
            //            title = GetTitleOfCollection(documentTree, docController) ?? "?";
            //        }
            //        var url = docController.GetLongestViewedContextUrl();
            //        url = url == null
            //            ? ""
            //            : (Uri.IsWellFormedUriString(url, UriKind.RelativeOrAbsolute) ? new Uri(url).LocalPath : url);
            //        url = url == null ? url : "Context: " + url;
            //        results.AddRange(CreateSearchResults(documentTree, docController.GetDataDocument(), url ?? docController.DocumentType.Type, title));
            //    }
            //    return results;
            //}

            //private static string GetTitleOfCollection(DocumentTree tree, DocumentController collection)
            //{
            //    if (tree == null || collection == null)
            //    {
            //        return null;
            //    }
            //    return tree.GetNodeFromViewId(collection.Id)?.DataDocument?.GetDereferencedField<TextController>(KeyStore.TitleKey, null)?.Data;
            //}

            ///// <summary>
            ///// More direct search for types.  not currently used since we put the type of documents in their fields
            ///// </summary>
            ///// <param name="criteria"></param>
            ///// <returns></returns>
            //private static IEnumerable<SearchResultViewModel> HandleTypeSearch(SpecialSearchCriteria criteria)
            //{
            //    var documentTree = DocumentTree.MainPageTree;
            //    List<DocumentController> docControllers = new List<DocumentController>();
            //    foreach (var documentController in ContentController<FieldModel>.GetControllers<DocumentController>())
            //    {
            //        if (documentController.DocumentType.Type.ToLower().Contains(criteria.SearchText))
            //        {
            //            docControllers.Add(documentController);
            //        }
            //    }
            //    var results = new List<SearchResultViewModel>();
            //    foreach (var docController in docControllers)
            //    {
            //        var field = docController.GetDereferencedField<ImageController>(AnnotatedImage.ImageFieldKey,
            //            null);
            //        var imageUrl = (field as ImageController)?.Data?.AbsoluteUri ?? "";
            //        results.AddRange(CreateSearchResults(documentTree, docController, imageUrl, docController.Title));
            //    }
            //    return results;
            //}



            ///// <summary>
            ///// searches but only through the content controller
            ///// </summary>
            ///// <param name="sender"></param>
            ///// <param name="searchString"></param>
            ///// <returns></returns>
            //private static IEnumerable<SearchResultViewModel> LocalSearch(string searchString)
            //{
            //    var documentTree = DocumentTree.MainPageTree;
            //    var countToResults = new Dictionary<int, List<SearchResultViewModel>>();
            //    var controllers = ContentController<FieldModel>.GetControllers<DocumentController>().ToArray();
            //    foreach (var documentController in ContentController<FieldModel>.GetControllers<DocumentController>())
            //    {
            //        var foundCount = 0;
            //        var lastTopText = "";
            //        StringSearchModel lastKeySearch = null;
            //        StringSearchModel lastFieldSearch = null;

            //        foreach (var kvp in documentController.EnumDisplayableFields())
            //        {
            //            var keySearch = StringSearchModel.False;//kvp.Key.SearchForString(searchString);
            //            var fieldSearch = kvp.Value.Dereference(new Context(documentController))?.SearchForString(searchString) ?? StringSearchModel.False;

            //            string topText = null;
            //            if (fieldSearch.StringFound)
            //            {
            //                topText = kvp.Key.Name;
            //            }
            //            else if (keySearch.StringFound)
            //            {
            //                topText = "Name Of Key: " + keySearch.RelatedString;
            //            }

            //            if (keySearch.StringFound || fieldSearch.StringFound)
            //            {
            //                foundCount++;

            //                //compare old search models to current one, trying to predict which would be better for the user to see
            //                var newIsBetter = lastFieldSearch == null ||
            //                                  (lastFieldSearch.RelatedString?.Length ?? 0) <
            //                                  (fieldSearch.RelatedString?.Length ?? 0);
            //                newIsBetter |= (lastFieldSearch?.RelatedString?.ToCharArray()?.Take(50)
            //                                    ?.Where(c => c == ' ')?.Count() ?? 0) <
            //                               (fieldSearch?.RelatedString?.ToCharArray()?.Take(50)?.Where(c => c == ' ')
            //                                    ?.Count() ?? 0);

            //                if (newIsBetter)
            //                {
            //                    lastTopText = topText;
            //                    lastKeySearch = keySearch;
            //                    lastFieldSearch = fieldSearch;
            //                }
            //            }
            //        }

            //        if (foundCount > 0)
            //        {
            //            var bottomText = (lastFieldSearch?.RelatedString ?? lastKeySearch?.RelatedString)
            //                ?.Replace('\n', ' ').Replace('\t', ' ').Replace('\r', ' ');
            //            var title = string.IsNullOrEmpty(documentController.Title)
            //                ? lastTopText
            //                : documentController.Title;

            //            var vm = CreateSearchResults(documentTree, documentController, bottomText, title,
            //                lastFieldSearch.IsUseFullRelatedString);

            //            if (!countToResults.ContainsKey(foundCount))
            //            {
            //                countToResults.Add(foundCount, new List<SearchResultViewModel>());
            //            }
            //            countToResults[foundCount].AddRange(vm);
            //        }
            //        else if (documentController.Id.ToLower() == searchString)
            //        {
            //            if (!countToResults.ContainsKey(1))
            //            {
            //                countToResults[1] = new List<SearchResultViewModel>();
            //            }
            //            countToResults[1].AddRange(CreateSearchResults(documentTree, documentController, "test", "test", true));
            //        }
            //    }

            //    return countToResults.OrderBy(kvp => -kvp.Key).SelectMany(i => i.Value);
            //    //ContentController<FieldModel>.GetControllers<DocumentController>().Where(doc => SearchKeyFieldIdPair(doc.DocumentModel.Fields, searchString))
            //}

            ///// <summary>
            ///// creates a SearchResultViewModel and correctly fills in fields to help the user understand the search result
            ///// </summary>
            ///// <param name = "documentTree" ></ param >
            ///// < param name="dataDocumentController"></param>
            ///// <param name = "bottomText" ></ param >
            ///// < param name="titleText"></param>
            //    /// <returns></returns>
            //    private static SearchResultViewModel[] CreateSearchResults(DocumentTree documentTree,
            //        DocumentController dataDocumentController, string bottomText, string titleText,
            //        bool isLikelyUsefulContextText = false)
            //{
            //    var vms = new List<SearchResultViewModel>();
            //    var preTitle = "";

            //    var documentNodes = documentTree.GetNodesFromDataDocumentId(dataDocumentController.Id);
            //    foreach (var documentNode in documentNodes ?? new DocumentNode[0])
            //    {
            //        if (documentNode?.Parents?.FirstOrDefault() != null)
            //        {
            //            preTitle = " >  " +
            //                ((string.IsNullOrEmpty(documentNode.Parents.First().DataDocument
            //                           .GetDereferencedField<TextController>(KeyStore.TitleKey, null)?.Data)
            //                           ? "?"
            //                           : documentNode.Parents.First().DataDocument
            //                               .GetDereferencedField<TextController>(KeyStore.TitleKey, null)?.Data))
            //                     ;
            //        }

            //        var vm = new SearchResultViewModel(titleText + preTitle, bottomText ?? "",
            //            dataDocumentController.Id,
            //            documentNode?.ViewDocument ?? dataDocumentController,
            //            documentNode?.Parents?.FirstOrDefault()?.ViewDocument, isLikelyUsefulContextText);
            //        vms.Add(vm);
            //    }

            //    return vms.ToArray();
            //}
        //}
    }
    }
}

