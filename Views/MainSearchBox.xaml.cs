using DashShared.Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.ApplicationModel.DataTransfer;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Newtonsoft.Json.Linq;

// The User Control item template is documented at https://go.microsoft.com/fwlink/?LinkId=234236

namespace Dash
{
    public sealed partial class MainSearchBox : UserControl
    {
        //private CancellationTokenSource _tokenSource = new CancellationTokenSource();
        private string _currentSearch = "";

        public const string SearchResultDragKey = "Search Result";
        public const string SearchCollectionDragKey = "Search Collection";

        public MainSearchBox()
        {
            this.InitializeComponent();
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

                ExecuteSearch(sender);

            }
            _currentSearch = sender.Text.ToLower(); ;
        }

        private void ExecuteSearch(AutoSuggestBox searchBox)
        {
            if (searchBox == null)
            {
                return;
            }

            var text = searchBox.Text.ToLower();
            (searchBox.ItemsSource as ObservableCollection<SearchResultViewModel>).Clear();

            var maxSearchResultSize = 75;

            var vms = SearchHelper.SearchOverCollection(text);

            var first = vms.Where(doc => doc?.DocumentCollection != null && doc.DocumentCollection != MainPage.Instance.MainDocument).Take(maxSearchResultSize).ToArray();
            Debug.WriteLine("Search Results: " + first.Length);
            foreach (var searchResultViewModel in first)
            {
                (searchBox.ItemsSource as ObservableCollection<SearchResultViewModel>).Add(searchResultViewModel);
            }
        }

        

        private void AutoSuggestBox_SuggestionChosen(AutoSuggestBox sender, AutoSuggestBoxSuggestionChosenEventArgs args)
        {
            // Set sender.Text. You can use args.SelectedItem to build your text string.
            if (args.SelectedItem is SearchResultViewModel resultVM)
            {

            }
        }


        private async void AutoSuggestBox_QuerySubmitted(AutoSuggestBox sender, AutoSuggestBoxQuerySubmittedEventArgs args)
        {
            if (args.ChosenSuggestion != null)
            {
                sender.Text = _currentSearch;
                if (args.ChosenSuggestion is SearchResultViewModel resultVM)
                {
                    if (resultVM?.DocumentCollection != null)
                    {
                        var currentWorkspace = MainPage.Instance.MainDocument.GetField<DocumentController>(KeyStore.LastWorkspaceKey);
                        if (!currentWorkspace.GetDataDocument().Equals(resultVM.DocumentCollection.GetDataDocument()))
                        {
                            MainPage.Instance.SetCurrentWorkspaceAndNavigateToDocument(resultVM.DocumentCollection, resultVM.ViewDocument);
                        }
                    }
                    MainPage.Instance.NavigateToDocumentInWorkspace(resultVM.ViewDocument);
                }
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
                ExecuteSearch(sender as AutoSuggestBox);
            }
        }

        /// <summary>
        /// Gets the specified number of view documents for the current search
        /// </summary>
        /// <param name="maxSearchResultSize">The maximum number of results to return</param>
        /// <param name="filterFunc">A filtering function to filter the type of view models returned</param>
        /// <returns></returns>
        public IEnumerable<DocumentController> GetViewDocumentsForCurrentSearch(int maxSearchResultSize = 75, Func<SearchResultViewModel, bool> filterFunc = null)
        {
            var vms = GetSearchViewModelsForCurrentSearch(maxSearchResultSize, filterFunc);
            return vms.Select(i => i.ViewDocument);
        }

        /// <summary>
        /// returns to you the search view models for the current search
        /// </summary>
        /// <param name="maxSearchResultSize"></param>
        /// <param name="filterFunc"></param>
        /// <returns></returns>
        public IEnumerable<SearchResultViewModel> GetSearchViewModelsForCurrentSearch(int maxSearchResultSize, Func<SearchResultViewModel, bool> filterFunc = null)
        {
            var text = _currentSearch;
            var vms = filterFunc == null ? SearchHelper.SearchOverCollection(text) : SearchHelper.SearchOverCollection(text).Where(filterFunc);
            return vms.Take(maxSearchResultSize);
        }

        /// <summary>
        /// Called when we drag the entire search collection
        /// </summary>
        private void XCollDragIcon_OnDragStarting(UIElement sender, DragStartingEventArgs args)
        {
            // get all the view docs for the search and set the key for the drag to a static const
            args.Data.Properties[SearchCollectionDragKey] = GetViewDocumentsForCurrentSearch();

            // set the allowed operations
            args.AllowedOperations = DataPackageOperation.Link | DataPackageOperation.Copy;
            args.Data.RequestedOperation = DataPackageOperation.Copy;

        }

        /// <summary>
        /// Called when we drag a single result from search
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="args"></param>
        private void SearchResult_OnDragStarting(UIElement sender, DragStartingEventArgs args)
        {
            // get the sender's view docs and set the key for the drag to a static const
            args.Data.Properties[SearchResultDragKey] = ((sender as FrameworkElement)?.DataContext as SearchResultViewModel)?.ViewDocument;

            // set the allowed operations
            args.AllowedOperations = DataPackageOperation.Link | DataPackageOperation.Copy;
            args.Data.RequestedOperation = DataPackageOperation.Copy;
        }

        /// <summary>
        /// public static class for encapsulating all the search code
        /// </summary>
        public static class SearchHelper
        {
            public static IEnumerable<SearchResultViewModel> SearchOverCollection(string[] searchParts, DocumentController collectionDocument)
            {
                return SearchOverCollection(string.Join(' ', searchParts), collectionDocument);
            }

            public static IEnumerable<SearchResultViewModel> SearchOverCollection(string searchString, DocumentController collectionDocument = null)
            {
                return LocalSearch(searchString).Where(vm => collectionDocument == null || (vm?.DocumentCollection != null && vm.DocumentCollection.Equals(collectionDocument)));
            }

            /// <summary>
            /// returns a list of result view models based upon a textual search that looks at all the parts of the input text
            /// </summary>
            /// <param name="text"></param>
            /// <returns></returns>
            private static List<SearchResultViewModel> SearchByParts(string text)
            {
                List<SearchResultViewModel> mainList = null;
                foreach (var searchPart in text.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries))
                {
                    var criteria = GetSpecialSearchCriteria(searchPart);
                    var searchResult = (criteria != null ? SpecialSearch(criteria) : LocalSearch(searchPart)).ToList();
                    mainList = mainList ?? searchResult;
                    if (criteria == null)
                    {
                        var temp = mainList;
                        mainList = searchResult;
                        searchResult = temp;
                    }
                    foreach (var existingVm in mainList.ToArray())
                    {
                        var valid = false;
                        foreach (var vm in searchResult)
                        {
                            if (existingVm.ViewDocument.Equals(vm.ViewDocument))
                            {
                                valid = true;
                                break;
                            }
                        }
                        if (!valid)
                        {
                            mainList.Remove(existingVm);
                        }
                    }
                }
                return mainList ?? new List<SearchResultViewModel>();
            }

            /// <summary>
            /// Supposed to handle all searches that are for key-value specified searches.   currenly just returns the generic special search.
            /// If more search capabilities are desired, probably should put them in here.
            /// </summary>
            /// <param name="criteria"></param>
            /// <returns></returns>
            private static IEnumerable<SearchResultViewModel> SpecialSearch(SpecialSearchCriteria criteria)
            {
                if (criteria.SearchCategory == "type" && false)
                {
                    return HandleTypeSearch(criteria);
                }
                if (criteria.SearchCategory == "in")
                {
                    return CollectionMembershipSearch(criteria);
                }
                return GenericSpecialSearch(criteria);
            }

            private static IEnumerable<SearchResultViewModel> CollectionMembershipSearch(SpecialSearchCriteria criteria)
            {
                return CollectionMembershipSearch(criteria.SearchText);
            }

            private static IEnumerable<SearchResultViewModel> CollectionMembershipSearch(string collectionNameToFind)
            {
                var tree = DocumentTree.MainPageTree;
                return LocalSearch("").Where(vm => vm?.DocumentCollection != null && (GetTitleOfCollection(tree, vm.DocumentCollection) ?? "").ToLower().Contains(collectionNameToFind));
            }

            /// <summary>
            /// Get the search results for a part of search trying to specify keys/value pairs
            /// </summary>
            /// <param name="criteria"></param>
            /// <returns></returns>
            private static IEnumerable<SearchResultViewModel> GenericSpecialSearch(SpecialSearchCriteria criteria)
            {
                var documentTree = DocumentTree.MainPageTree;
                List<DocumentController> docControllers = new List<DocumentController>();
                foreach (var documentController in ContentController<FieldModel>.GetControllers<DocumentController>())
                {
                    foreach (var kvp in documentController.EnumFields())
                    {
                        if (kvp.Key.Name.ToLower().Contains(criteria.SearchCategory))
                        {
                            var stringSearch = kvp.Value.SearchForString(criteria.SearchText);
                            if (stringSearch.StringFound)
                            {
                                docControllers.Add(documentController);
                            }
                        }
                    }
                }
                foreach (var docController in docControllers)
                {
                    var title = docController.Title;

                    if (documentTree[docController.Id] != null && documentTree[docController.Id].DataDocument.GetField<ListController<DocumentController>>(KeyStore.CollectionKey) != null)
                    {
                        title = GetTitleOfCollection(documentTree, docController) ?? "?";
                    }
                    var url = docController.GetLongestViewedContextUrl();
                    url = url == null ? "" : (Uri.IsWellFormedUriString(url, UriKind.RelativeOrAbsolute) ? new Uri(url).LocalPath : url);
                    yield return CreateSearchResult(documentTree, docController, url ?? docController.DocumentType.Type, title);
                }
            }

            private static string GetTitleOfCollection(DocumentTree tree, DocumentController collection)
            {
                if (tree == null || collection == null)
                {
                    return null;
                }
                return tree[collection.Id]?.DataDocument?.GetDereferencedField<TextController>(KeyStore.TitleKey, null)?.Data;
            }

            /// <summary>
            /// More direct search for types.  not currently used since we put the type of documents in their fields
            /// </summary>
            /// <param name="criteria"></param>
            /// <returns></returns>
            private static IEnumerable<SearchResultViewModel> HandleTypeSearch(SpecialSearchCriteria criteria)
            {
                var documentTree = DocumentTree.MainPageTree;
                List<DocumentController> docControllers = new List<DocumentController>();
                foreach (var documentController in ContentController<FieldModel>.GetControllers<DocumentController>())
                {
                    if (documentController.DocumentType.Type.ToLower().Contains(criteria.SearchText))
                    {
                        docControllers.Add(documentController);
                    }
                }
                foreach (var docController in docControllers)
                {
                    var field = docController.GetDereferencedField<ImageController>(AnnotatedImage.Image1FieldKey, null);
                    var imageUrl = (field as ImageController)?.Data?.AbsoluteUri ?? "";
                    yield return CreateSearchResult(documentTree, docController, imageUrl, docController.Title);
                }
            }


            /// <summary>
            /// returns the criteria object for kvp special search specification, null if not a request for a special search
            /// </summary>
            /// <param name="searchText"></param>
            /// <returns></returns>
            private static SpecialSearchCriteria GetSpecialSearchCriteria(string searchText)
            {
                searchText = searchText.Replace(" ", "");
                var split = searchText.Split(':');
                if (split.Count() == 2)
                {
                    return new SpecialSearchCriteria()
                    {
                        SearchCategory = split[0],
                        SearchText = split[1]
                    };
                }
                return null;
            }


            /// <summary>
            /// searches but only through the content controller
            /// </summary>
            /// <param name="sender"></param>
            /// <param name="searchString"></param>
            /// <returns></returns>
            private static IEnumerable<SearchResultViewModel> LocalSearch(string searchString)
            {
                var documentTree = DocumentTree.MainPageTree;
                var countToResults = new Dictionary<int, List<SearchResultViewModel>>();
                foreach (var documentController in ContentController<FieldModel>.GetControllers<DocumentController>())
                {
                    int foundCount = 0;
                    string lastTopText = "";
                    StringSearchModel lastKeySearch = null;
                    StringSearchModel lastFieldSearch = null;

                    foreach (var kvp in documentController.EnumDisplayableFields())
                    {
                        var keySearch = kvp.Key.SearchForString(searchString);
                        var fieldSearch = kvp.Value.SearchForString(searchString);

                        string topText = null;
                        if (fieldSearch.StringFound)
                        {
                            topText = kvp.Key.Name;
                        }
                        else if (keySearch.StringFound)
                        {
                            topText = "Name Of Key: " + keySearch.RelatedString;
                        }

                        if (keySearch.StringFound || fieldSearch.StringFound)
                        {
                            foundCount++;

                            //compare old search models to current one, trying to predict which would be better for the user to see
                            var newIsBetter = lastFieldSearch == null || (lastFieldSearch.RelatedString?.Length ?? 0) < (fieldSearch.RelatedString?.Length ?? 0);
                            newIsBetter |= (lastFieldSearch?.RelatedString?.ToCharArray()?.Take(50)?.Where(c => c == ' ')?.Count() ?? 0) <
                                           (fieldSearch?.RelatedString?.ToCharArray()?.Take(50)?.Where(c => c == ' ')?.Count() ?? 0);

                            if (newIsBetter)
                            {
                                lastTopText = topText;
                                lastKeySearch = keySearch;
                                lastFieldSearch = fieldSearch;
                            }
                        }
                    }

                    if (foundCount > 0)
                    {
                        var bottomText = (lastFieldSearch?.RelatedString ?? lastKeySearch?.RelatedString)?.Replace('\n', ' ').Replace('\t', ' ').Replace('\r', ' ');
                        var title = string.IsNullOrEmpty(documentController.Title) ? lastTopText : documentController.Title;

                        var vm = CreateSearchResult(documentTree, documentController, bottomText, title);

                        if (!countToResults.ContainsKey(foundCount))
                        {
                            countToResults.Add(foundCount, new List<SearchResultViewModel>());
                        }
                        countToResults[foundCount].Add(vm);
                    }
                }
                return countToResults.OrderBy(kvp => -kvp.Key).SelectMany(i => i.Value);
                //ContentController<FieldModel>.GetControllers<DocumentController>().Where(doc => SearchKeyFieldIdPair(doc.DocumentModel.Fields, searchString))
            }

            /// <summary>
            /// creates a SearchResultViewModel and correctly fills in fields to help the user understand the search result
            /// </summary>
            /// <param name="documentTree"></param>
            /// <param name="dataDocumentController"></param>
            /// <param name="bottomText"></param>
            /// <param name="titleText"></param>
            /// <returns></returns>
            private static SearchResultViewModel CreateSearchResult(DocumentTree documentTree, DocumentController dataDocumentController, string bottomText, string titleText)
            {
                string preTitle = "";

                var documentNode = documentTree[dataDocumentController.Id];
                if (documentNode?.Parents?.FirstOrDefault() != null)
                {
                    preTitle = (string.IsNullOrEmpty(documentNode.Parents.First().DataDocument.GetDereferencedField<TextController>(KeyStore.TitleKey, null)?.Data) ? "?" :
                                   documentNode.Parents.First().DataDocument.GetDereferencedField<TextController>(KeyStore.TitleKey, null)?.Data) + " >  ";
                }

                var vm = new SearchResultViewModel(preTitle + titleText, bottomText ?? "", dataDocumentController.Id, documentNode?.ViewDocument ?? dataDocumentController, documentNode?.Parents?.FirstOrDefault()?.ViewDocument);
                return vm;
            }

            /// <summary>
            /// this criteria simple tells us which key and value pair to look at
            /// </summary>
            private class SpecialSearchCriteria
            {
                public string SearchText { get; set; }
                public string SearchCategory { get; set; }
            }
        }
    }
}
