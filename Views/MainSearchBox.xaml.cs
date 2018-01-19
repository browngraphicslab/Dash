using DashShared.Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

// The User Control item template is documented at https://go.microsoft.com/fwlink/?LinkId=234236

namespace Dash
{
    public sealed partial class MainSearchBox : UserControl
    {
        //private CancellationTokenSource _tokenSource = new CancellationTokenSource();
        private string _currentSearch = "";

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

            //_tokenSource?.Cancel();
            //Debug.WriteLine("Task canceled");
            //_tokenSource = new CancellationTokenSource();
            var text = searchBox.Text.ToLower();
            //Task.Factory.StartNew(async () =>
            //{
            //Search(sender, sender.Text.ToLower());
            (searchBox.ItemsSource as ObservableCollection<SearchResultViewModel>).Clear();

            var maxSearchResultSize = 75;

            var vms = SearchByParts(text);

            var first = vms.Where(doc => doc?.DocumentCollection != null && doc.DocumentCollection != MainPage.Instance.MainDocument).Take(maxSearchResultSize).ToArray();
            Debug.WriteLine("Search Results: " + first.Length);
            foreach (var searchResultViewModel in first)
            {
                (searchBox.ItemsSource as ObservableCollection<SearchResultViewModel>).Add(searchResultViewModel);
            }

            //}, _tokenSource.Token);

        }

        /// <summary>
        /// returns a list of result view models based upon a textual search that looks at all the parts of the input text
        /// </summary>
        /// <param name="text"></param>
        /// <returns></returns>
        private List<SearchResultViewModel> SearchByParts(string text)
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
        private IEnumerable<SearchResultViewModel> SpecialSearch(SpecialSearchCriteria criteria)
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

        private IEnumerable<SearchResultViewModel> CollectionMembershipSearch(SpecialSearchCriteria criteria)
        {
            var tree = DocumentTree.MainPageTree;
            return LocalSearch("").Where(vm => vm?.DocumentCollection != null && (GetTitleOfCollection(tree, vm.DocumentCollection) ?? "").ToLower().Contains(criteria.SearchText));
        }

        /// <summary>
        /// Get the search results for a part of search trying to specify keys/value pairs
        /// </summary>
        /// <param name="criteria"></param>
        /// <returns></returns>
        private IEnumerable<SearchResultViewModel> GenericSpecialSearch(SpecialSearchCriteria criteria)
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
                    title = GetTitleOfCollection(documentTree,docController) ?? "?" ;
                }
                var url = docController.GetLongestViewedContextUrl();
                url = url == null ? "" : (Uri.IsWellFormedUriString(url, UriKind.RelativeOrAbsolute) ? new Uri(url).LocalPath : url);
                yield return CreateSearchResult(documentTree, docController, url ?? docController.DocumentType.Type, title);
            }
        }

        private string GetTitleOfCollection(DocumentTree tree, DocumentController collection)
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
        private IEnumerable<SearchResultViewModel> HandleTypeSearch(SpecialSearchCriteria criteria)
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
        private SpecialSearchCriteria GetSpecialSearchCriteria (string searchText)
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

        /// <summary>
        /// searches but only through the content controller
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="searchString"></param>
        /// <returns></returns>
        private IEnumerable<SearchResultViewModel> LocalSearch(string searchString)
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
                        topText = "Name Of Key: "+keySearch.RelatedString;
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
        private SearchResultViewModel CreateSearchResult(DocumentTree documentTree, DocumentController dataDocumentController, string bottomText, string titleText)
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

        private void XAutoSuggestBox_OnGotFocus(object sender, RoutedEventArgs e)
        {
            if (!string.IsNullOrEmpty(_currentSearch))
            {
                ExecuteSearch(sender as AutoSuggestBox);
            }
        }

        /// <summary>
        /// returns the current document controllers for the data documents of the search results
        /// </summary>
        /// <param name="maxSearchResultSize"></param>
        /// <param name="filterFunc"></param>
        /// <returns></returns>
        public IEnumerable<DocumentController> GetDocumentsForCurrentSearch(int maxSearchResultSize = 75, Func<SearchResultViewModel, bool> filterFunc = null)
        {
            IEnumerable<SearchResultViewModel> vms = GetSearchViewModelsForCurrentSearch(maxSearchResultSize, filterFunc);
            return vms.Select(i => i.ViewDocument.GetDataDocument());
        }

        /// <summary>
        /// returns to you the search view models for the current search
        /// </summary>
        /// <param name="maxSearchResultSize"></param>
        /// <param name="filterFunc"></param>
        /// <returns></returns>
        public IEnumerable<SearchResultViewModel> GetSearchViewModelsForCurrentSearch(int maxSearchResultSize = 75, Func<SearchResultViewModel, bool> filterFunc = null)
        {
            var text = _currentSearch;
            IEnumerable<SearchResultViewModel> vms = filterFunc == null ? SearchByParts(text) : SearchByParts(text).Where(filterFunc);
            return vms.Take(maxSearchResultSize);
        }

        /*
        /// <summary>
        /// the method in which we actually process the search and perform the db query
        /// </summary>
        /// <param name="searchString"></param>
        /// <returns></returns>
        private void Search(AutoSuggestBox sender, string searchString)
        {
            for (var i = 0; i < 10; i++)
            {
                //results.Add(new SearchResultViewModel("Title" + i, "id " + i));
            }

            RESTClient.Instance.Fields.GetDocumentsByQuery(new SearchQuery(GetQueryFunc(searchString)),
                async (RestRequestReturnArgs args) =>
                {
                    var results = new ObservableCollection<SearchResultViewModel>(args.ReturnedObjects.OfType<DocumentModel>().Select(DocumentToSearchResult));
                    sender.ItemsSource = results;
                }, null);
        }

        private SearchResultViewModel DocumentToSearchResult(DocumentModel doc)
        {
            if (doc == null)
            {
                return null;
            }
            return new SearchResultViewModel((ContentController<FieldModel>.GetController<DocumentController>(doc.Id)?.GetField(KeyStore.TitleKey) as TextController)?.Data ?? "", doc.Id, doc.Id);
        }

        private bool TextFieldContains(TextController field, string searchString)
        {
            if (field == null)
            {
                return false;
            }
            return field.Data.ToLower().Contains(searchString);
        }


        private bool KeyContains(KeyController key, string searchString)
        {
            if (key == null)
            {
                return false;
            }
            return key.Name.ToLower().Contains(searchString);
        }

        private bool SearchKeyFieldIdPair(KeyValuePair<string, string> keyFieldPair, string searchString)
        {
            return (ContentController<FieldModel>.GetController<FieldControllerBase>(keyFieldPair.Value).SearchForString(searchString)?.StringFound == true ||
                ContentController<FieldModel>.GetController<FieldControllerBase>(keyFieldPair.Key).SearchForString(searchString)?.StringFound == true);
        }

        private Func<FieldModel, bool> GetQueryFunc(string searchString)
        {
            return(fieldModel) =>
            {
                if (!(fieldModel is DocumentModel))
                {
                    return false;
                }
                var doc = (DocumentModel) fieldModel;
                //var docController = ContentController<FieldModel>.GetController<DocumentController>(doc.Id);
                //return docController.
                return doc.Fields.Any(i => SearchKeyFieldIdPair(i, searchString) != null);
            };
        }*/
    }
}
