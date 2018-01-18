using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using DashShared;
using DashShared.Models;

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
            // Only get results when it was a user typing, 
            // otherwise assume the value got filled in by TextMemberPath 
            // or the handler for SuggestionChosen.
            if (args.Reason == AutoSuggestionBoxTextChangeReason.UserInput)
            {
                //Set the ItemsSource to be your filtered dataset
                //sender.ItemsSource = dataset;

                //_tokenSource?.Cancel();
                //Debug.WriteLine("Task canceled");
                //_tokenSource = new CancellationTokenSource();
                var text = sender.Text.ToLower();
                _currentSearch = text;
                //Task.Factory.StartNew(async () =>
                //{
                    //Search(sender, sender.Text.ToLower());
                (sender.ItemsSource as ObservableCollection<SearchResultViewModel>).Clear();

                var vms = GetSpecialSearchCriteria(text) != null ? SpecialSearch(GetSpecialSearchCriteria(text)) : LocalSearch(text);
                var first = vms.Take(75).ToArray();
                Debug.WriteLine("Search Results: "+first.Length);
                foreach (var searchResultViewModel in first) 
                {
                    (sender.ItemsSource as ObservableCollection<SearchResultViewModel>).Add(searchResultViewModel);
                }
                    
                //}, _tokenSource.Token);

                

            }
        }

        private IEnumerable<SearchResultViewModel> SpecialSearch(SpecialSearchCriteria criteria)
        {
            if (criteria.SearchCategory == "type")
            {
                return HandleTypeSearch(criteria);
            }
            return GenericSpecialSearch(criteria);
        }

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
                yield return CreateSearchResult(documentTree, docController, (docController.GetField(KeyStore.SourecUriKey) as ImageController)?.Data?.AbsoluteUri ?? "", docController.Title);
            }
        }

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
                yield return CreateSearchResult(documentTree, docController, (docController.GetField(KeyStore.SourecUriKey) as ImageController)?.Data?.AbsoluteUri ?? "", docController.Title);
            }
        }

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


        private void AutoSuggestBox_QuerySubmitted(AutoSuggestBox sender, AutoSuggestBoxQuerySubmittedEventArgs args)
        {
            if (args.ChosenSuggestion != null)
            {
                sender.Text = _currentSearch;
                if (args.ChosenSuggestion is SearchResultViewModel resultVM)
                {
                    if (resultVM?.DocumentCollection != null)
                    {
                        MainPage.Instance.SetCurrentWorkspace(resultVM.DocumentCollection);
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
                if (searchString != _currentSearch)
                {
                    Debug.WriteLine("ended early-------------------------");
                    return new List<SearchResultViewModel>();
                }

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

        private class SpecialSearchCriteria
        {
            public string SearchText { get; set; }
            public string SearchCategory { get; set; }
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
