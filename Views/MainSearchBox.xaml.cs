using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Windows.ApplicationModel.DataTransfer;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using DashShared;
using Visibility = Windows.UI.Xaml.Visibility;
using Dash.Models.DragModels;
using System;
using Windows.System;
using Windows.UI.Xaml.Input;

// The User Control item template is documented at https://go.microsoft.com/fwlink/?LinkId=234236

namespace Dash
{
    public sealed partial class MainSearchBox
    {
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
        private void AutoSuggestBox_TextChanged(AutoSuggestBox sender, AutoSuggestBoxTextChangedEventArgs args)
        {
            // Only get results when it was a user typing, 
            // otherwise assume the value got filled in by TextMemberPath 
            // or the handler for SuggestionChosen.
            if (args.Reason == AutoSuggestionBoxTextChangeReason.UserInput)
            {
                //Set the ItemsSource to be your filtered dataset
                //sender.ItemsSource = dataset;

                ExecuteDishSearch(sender);

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
            if (args.ChosenSuggestion != null)
            {
                if (!(args.ChosenSuggestion is SearchResultViewModel resultVm)) return;
                if (resultVm.DocumentCollection != null)
                {
                    var currentWorkspace = MainPage.Instance.MainDocument.GetField<DocumentController>(KeyStore.LastWorkspaceKey);
                    if (!currentWorkspace.GetDataDocument().Equals(resultVm.DocumentCollection.GetDataDocument()))
                    {
                        MainPage.Instance.SetCurrentWorkspaceAndNavigateToDocument(resultVm.DocumentCollection, resultVm.ViewDocument);
                    }
                }

                MainPage.Instance.NavigateToDocumentInWorkspace(resultVm.ViewDocument, true, false);
            }
            else
            {
                // Use args.QueryText to determine what to do.
            }
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

        #endregion

        #region Other Events

        private void Grid_PointerEntered(object sender, Windows.UI.Xaml.Input.PointerRoutedEventArgs e)
        {
            var viewModel = (sender as Grid)?.DataContext as SearchResultViewModel;
            DocumentController docTapped = viewModel?.ViewDocument;
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
            var docs = Search.Parse(xAutoSuggestBox.Text)
                .Select(sr => sr.ViewDocument.GetViewCopy());

            args.Data.Properties[nameof(DragCollectionFieldModel)] =
                new DragCollectionFieldModel(docs.ToList(), null, null, CollectionView.CollectionViewType.Grid);

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


        private void XArrowBlock_OnTapped(object sender, TappedRoutedEventArgs e)
        {
            if (xSearchCodeBox.Visibility == Visibility.Visible)
            {
                //collapse search bar
                xSearchCodeBox.Visibility = Visibility.Collapsed;
                xArrow.Glyph = "\uE937";
            }
            else
            {
                //open search bar
                xSearchCodeBox.Visibility = Visibility.Visible;
                xArrow.Glyph = "\uE936";
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
            var text = xAutoSuggestBox.Text.Replace("\"", "\\\"");

            //open DishScriptEditView with search text
            var script = "var docs = search(\"" + text + "\"); \r for (var doc in docs){ \r" + xSearchCode.Text + "\r }";

            var note = new DishScriptBox(0, 0, 300, 400, script);

            args.Data.Properties[nameof(DragDocumentModel)] = new DragDocumentModel(note.Document, true);

            args.AllowedOperations = DataPackageOperation.Link | DataPackageOperation.Move | DataPackageOperation.Copy;
            args.Data.RequestedOperation = DataPackageOperation.Move | DataPackageOperation.Copy | DataPackageOperation.Link;
        }

        private void XDragScript_OnTapped(object sender, TappedRoutedEventArgs e)
        {
            var text = xAutoSuggestBox.Text.Replace("\"", "\\\"");

            //open DishScriptEditView with search text
            var script = "var docs = search(\"" + text + "\"); \r for (var doc in docs){ \r" + xSearchCode.Text + "\r }";


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
            var docs = searchRes.Select(f => f.ViewDocument).ToList();
            if (string.IsNullOrWhiteSpace(text)) return;
            //highlight doc results
            HighlightSearchResults(docs);


            var searchTerms = Search.ConvertSearchTerms(text);
            var vmGroups = new List<SearchResultViewModel>();
            foreach (SearchResult res in searchRes)
            {
                if (res.ViewDocument.DocumentType.Equals(RichTextBox.DocumentType))
                {
                    res.DataDocument.SetField(CollectionDBView.SelectedKey, searchTerms, true);
                }
                SearchResultViewModel newVm = DocumentSearchResultToViewModel(res);
                DocumentController parent = res.Node.Parent?.ViewDocument;
                if (parent != null) newVm.DocumentCollection = parent;
                vmGroups.Add(newVm);
            }

            var first = vmGroups 
                .Where(doc => doc?.DocumentCollection != null && doc.DocumentCollection != MainPage.Instance.MainDocument)
                .Take(MaxSearchResultSize).ToArray();

            foreach (SearchResultViewModel searchResultViewModel in first) { itemsSource?.Add(searchResultViewModel); }
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

            //DocumentTree.MainPageTree.Select(node => node.DataDocument.SetField<TextController>(CollectionDBView.SelectedKey, "", true));
            foreach (var node in DocumentTree.MainPageTree)
            {
                var a = node.DataDocument;
                if (a.GetField(CollectionDBView.SelectedKey) != null)
                {
                    a.RemoveField(CollectionDBView.SelectedKey);
                }

            }
        }

        public static void UnHighlightDocs(DocumentController coll)
        {
            var colDocs = coll.GetDataDocument().GetDereferencedField<ListController<DocumentController>>(KeyStore.DataKey, null).TypedData;
            //unhighlight each doc in collection
            foreach (DocumentController doc in colDocs)
            {
                MainPage.Instance.HighlightDoc(doc, false, 2);
                if (doc.DocumentType.ToString() == "Collection Box")
                {
                    UnHighlightDocs(doc);
                }
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
    }
}

