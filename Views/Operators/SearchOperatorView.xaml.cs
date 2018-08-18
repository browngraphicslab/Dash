using System.Collections.ObjectModel;
using System.Linq;
using Windows.ApplicationModel.DataTransfer;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using DashShared;

// The User Control item template is documented at https://go.microsoft.com/fwlink/?LinkId=234236

namespace Dash
{
    public sealed partial class SearchOperatorView : UserControl
    {

        /// <summary>
        /// The document which has a field containing the SearchOperator that this view is associated with
        /// </summary>
        private DocumentController _operatorDoc;

        /// <summary>
        /// Observable collection of the search results for the autosuggestbox
        /// </summary>
        private readonly ObservableCollection<SearchResultViewModel> _searchResultViewModels = new ObservableCollection<SearchResultViewModel>();

        /// <summary>
        /// The current search string that the user is inputing
        /// </summary>
        private string _currentSearch;

        public SearchOperatorView()
        {
            this.InitializeComponent();
            DataContextChanged += OnDataContextChanged;
            xAutoSuggestBox.ItemsSource = _searchResultViewModels;
        }

        private void OnDataContextChanged(FrameworkElement sender, DataContextChangedEventArgs args)
        {
            // datacontext is a reference to the operator field
            var refToOp = DataContext as FieldReference;

            // get the document containing the operator
            _operatorDoc = refToOp?.GetDocumentController(null);

            // listen for when the input text is changed
            _operatorDoc?.AddFieldUpdatedListener(SearchOperatorController.TextKey, OnTextFieldChanged);

            // set the initial autosuggest text to the current field value if it exists
            xAutoSuggestBox.Text = _operatorDoc.GetDereferencedField<TextController>(SearchOperatorController.TextKey, null)?.Data ?? string.Empty;
        }

        /// <summary>
        /// Update the text in the autosuggestbox when the input text changes
        /// </summary>
        private void OnTextFieldChanged(DocumentController sender, DocumentController.DocumentFieldUpdatedEventArgs args, Context context)
        {
            var tfmc = args.NewValue.DereferenceToRoot<TextController>(null);
            if (xAutoSuggestBox.Text != tfmc.Data)
            {
                xAutoSuggestBox.Text = tfmc.Data;
            }
        }

        /// <summary>
        /// Update the input to the collection when the user types something in the autosuggest box
        /// </summary>
        private void AutoSuggestBox_QuerySubmitted(AutoSuggestBox sender, AutoSuggestBoxQuerySubmittedEventArgs args)
        {
            sender.Text = _currentSearch;
            _operatorDoc.SetField(SearchOperatorController.TextKey, new TextController(sender.Text), true);
        }

        private void XAutoSuggestBox_OnTextChanged(AutoSuggestBox sender, AutoSuggestBoxTextChangedEventArgs args)
        {
            // Only get results when it was a user typing,
            // otherwise assume the value got filled in by TextMemberPath
            // or the handler for SuggestionChosen.
            if (args.Reason == AutoSuggestionBoxTextChangeReason.UserInput)
            {
                var searchCollection = _operatorDoc.GetDereferencedField<ListController<DocumentController>>(SearchOperatorController.InputCollection, null)?.TypedData;
                var searchText = sender.Text;
                _searchResultViewModels.Clear();
                //foreach (var searchResultViewModel in MainSearchBox.SearchHelper.SearchOverCollectionList(searchText, searchCollection))
                //{
                //    _searchResultViewModels.Add(searchResultViewModel);
                //}
            }

            // update the current search
            _currentSearch = sender.Text;
        }

        private void XAutoSuggestBox_OnSuggestionChosen(AutoSuggestBox sender, AutoSuggestBoxSuggestionChosenEventArgs args)
        {
            // Set sender.Text. You can use args.SelectedItem to build your text string.
            sender.Text = _currentSearch;
        }


        private void XAutoSuggestBox_OnDragEnter(object sender, DragEventArgs e)
        {
            if (e.DataView.Properties.ContainsKey(nameof(DragDocumentModel)))
            {
                e.AcceptedOperation = DataPackageOperation.Link;
            }
        }

        private void XAutoSuggestBox_OnDrop(object sender, DragEventArgs e)
        {
            if (e.DataView.TryGetLoneDocument(out DocumentController dragDoc))
            {
                var listKeys = dragDoc.EnumDisplayableFields().Where(kv => dragDoc.GetRootFieldType(kv.Key).HasFlag(TypeInfo.List)).Select(kv => kv.Key).ToList();
                if (listKeys.Count == 1)
                {
                    string currText = xAutoSuggestBox.Text;
                    xAutoSuggestBox.Text = "in:" + dragDoc.Title.Split()[0];
                    if (!string.IsNullOrWhiteSpace(currText))
                    {
                        xAutoSuggestBox.Text = xAutoSuggestBox.Text + "  " + currText;
                    }
                }
            }

            e.Handled = true;
        }

    }
}
