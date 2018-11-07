using System.Collections.Generic;
using System.Linq;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using DashShared;

// The User Control item template is documented at https://go.microsoft.com/fwlink/?LinkId=234236

namespace Dash
{
    public sealed partial class ExtractSentencesOperatorView : UserControl
    {

        /// <summary>
        /// The document containing the Extract Sentences Operator that this view is associated with
        /// </summary>
        private DocumentController _operatorDoc;

        /// <summary>
        /// Typed dictionary of all the headers in the input collection
        /// </summary>
        private Dictionary<KeyController, HashSet<TypeInfo>> _allHeaders;

        /// <summary>
        /// List of the documents in the input collection, set when the datacontext is changed
        /// </summary>
        public ListController<DocumentController> InputCollection { get; set; }

        public ExtractSentencesOperatorView()
        {
            this.InitializeComponent();
            DataContextChanged += OnDataContextChanged;

        }

        private void OnDataContextChanged(FrameworkElement sender, DataContextChangedEventArgs args)
        {
            // datacontext is a reference to the operator field
            var refToOp = DataContext as FieldReference;

            // get the document containing the operator
            _operatorDoc = refToOp?.GetDocumentController(null);

            // listen for when the input collection is changed
            _operatorDoc?.AddFieldUpdatedListener(ExtractSentencesOperatorController.InputCollection, OnInputCollectionChanged);
            _operatorDoc?.AddFieldUpdatedListener(ExtractSentencesOperatorController.TextField, OnTextFieldChanged);

            var keyId = _operatorDoc
                ?.GetDereferencedField<TextController>(ExtractSentencesOperatorController.TextField, null)?.Data;
            if (keyId != null)
            {
                XTextFieldBox.Text = RESTClient.Instance.Fields.GetController<KeyController>(keyId)?.Name ?? string.Empty;
            }
        }

        private void OnTextFieldChanged(DocumentController sender, DocumentController.DocumentFieldUpdatedEventArgs args)
        {
            var tfmc = args.NewValue.DereferenceToRoot<TextController>(null);
            XTextFieldBox.Text = RESTClient.Instance.Fields.GetController<KeyController>(tfmc.Data).Name;

        }

        private void OnInputCollectionChanged(DocumentController sender, DocumentController.DocumentFieldUpdatedEventArgs args)
        {
            InputCollection = args.NewValue.DereferenceToRoot<ListController<DocumentController>>(null);
            _allHeaders = Util.GetDisplayableTypedHeaders(InputCollection); // TODO update the headers when a document is added to the input collection!
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
                var userInput = sender.Text;
                sender.ItemsSource = _allHeaders?.Where(kvp => kvp.Key.Name.ToLower().Contains(userInput.ToLower())).Select(kvp => kvp.Key);
            }
        }


        private void AutoSuggestBox_SuggestionChosen(AutoSuggestBox sender, AutoSuggestBoxSuggestionChosenEventArgs args)
        {
            // Set sender.Text. You can use args.SelectedItem to build your text string.

            sender.Text = (args.SelectedItem as KeyController).Name;
        }


        private void AutoSuggestBox_QuerySubmitted(AutoSuggestBox sender, AutoSuggestBoxQuerySubmittedEventArgs args)
        {
            if (args.ChosenSuggestion != null)
            {
                // User selected an item from the suggestion list, take an action on it here.
                _operatorDoc.SetField(ExtractSentencesOperatorController.TextField,
                    new TextController((args.ChosenSuggestion as KeyController).Id), true);

            }
            else
            {
                // Use args.QueryText to determine what to do.
                // TODO maybe get the first thing in the list
            }
        }
    }
}
