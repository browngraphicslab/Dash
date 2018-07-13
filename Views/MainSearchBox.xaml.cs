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
using System;
using Windows.System;
using Windows.UI.Xaml.Input;

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

            IEnumerable<SearchResult> searchRes;
            try
            {
                searchRes = Search.Parse(text).ToList();
            } catch(Exception e)
            {
                searchRes = new List<SearchResult>();
            }
            var docs = searchRes.Select(f => f.ViewDocument).ToList();
                if (string.IsNullOrWhiteSpace(text)) return;
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
            }



        }
}

