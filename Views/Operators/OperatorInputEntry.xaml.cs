using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Windows.ApplicationModel.DataTransfer;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;
using Dash.Models.DragModels;
using DashShared;
using Visibility = Windows.UI.Xaml.Visibility;

// The User Control item template is documented at https://go.microsoft.com/fwlink/?LinkId=234236

namespace Dash
{
    public sealed partial class OperatorInputEntry : UserControl
    {
        public static readonly DependencyProperty OperatorFieldReferenceProperty = DependencyProperty.Register(
            "OperatorFieldReference", typeof(FieldReference), typeof(OperatorInputEntry),
            new PropertyMetadata(default(FieldReference)));


        /// <summary>
        ///     The key this input is associated with on the OperatorController
        /// </summary>
        private KeyController _inputKey;

        /// <summary>
        ///     The data type of this input
        /// </summary>
        private TypeInfo _inputType;

        /// <summary>
        ///     The document the input is linked to (not the operator, the input doc)
        /// </summary>
        private DocumentController _refDoc;


        /// <summary>
        ///     A reference to the field on the operator document this input is associated with
        /// </summary>
        public FieldReference OperatorFieldReference
        {
            get => (FieldReference) GetValue(OperatorFieldReferenceProperty);
            set
            {
                SetValue(OperatorFieldReferenceProperty, value);
                TrySetInputTypeAndKey();
            }
        }

        public OperatorInputEntry()
        {
            InitializeComponent();
            DataContextChanged += OperatorInputEntry_DataContextChanged;
        }

        /// <summary>
        ///     Try to set the input type and input key based on the DataContext and OperatorFieldReference
        /// </summary>
        private void TrySetInputTypeAndKey()
        {
            var opField = OperatorFieldReference?.DereferenceToRoot<OperatorController>(null);
            _inputKey = ((DictionaryEntry?) DataContext)?.Key as KeyController;

            if (opField != null && _inputKey != null) _inputType = opField.Inputs[_inputKey].Type;
        }

        private void OperatorInputEntry_DataContextChanged(FrameworkElement sender, DataContextChangedEventArgs args)
        {
            TrySetInputTypeAndKey();
        }

        private void UIElement_OnDragEnter(object sender, DragEventArgs e)
        {
            // if the user dragged from the orange dot, or a search result, basically most cases
            if (e.DataView.Properties.ContainsKey(nameof(DragDocumentModel)))
            {
                if (e.DataView.Properties[nameof(DragDocumentModel)] is DragDocumentModel dragData)
                {
                    _refDoc = dragData.DraggedDocument.GetDataDocument();

                    // if there is a key check to see if it has the same type as the input type
                    if (dragData.DraggedKey != null)
                    {
                        var fieldType = _refDoc.GetRootFieldType(dragData.DraggedKey);
                        e.AcceptedOperation = fieldType.HasFlag(_inputType)
                            ? DataPackageOperation.Link
                            : DataPackageOperation.None;
                    }

                    // if there is no key check to see if any of the fields have the correct type
                    else
                    {
                        var anyFieldsWithCorrectType = _refDoc.EnumDisplayableFields()
                            .Any(kv => _refDoc.GetRootFieldType(kv.Key).HasFlag(_inputType));
                        e.AcceptedOperation = anyFieldsWithCorrectType
                            ? DataPackageOperation.None
                            : DataPackageOperation.Link;
                    }
                }
            }

            // if the user dragged from the header of a schema view
            else if (e.DataView.Properties.ContainsKey(nameof(DragCollectionFieldModel)))
            {
                e.AcceptedOperation = DataPackageOperation.Link;
            }

            e.Handled = true;
        }

        private void UIElement_OnDrop(object sender, DragEventArgs e)
        {
            if (e.DataView.Properties.ContainsKey(nameof(DragDocumentModel)))
            {
                if (e.DataView.Properties[nameof(DragDocumentModel)] is DragDocumentModel dragData)
                    if (dragData.DraggedKey != null)
                    {
                        OperatorFieldReference.GetDocumentController(null)?.SetField(_inputKey,
                            new DocumentReferenceController(_refDoc.Id, dragData.DraggedKey), true);
                    }
                    else
                    {
                        // if only one field on the input has the correct type then connect that field
                        var fieldsWithCorrectType = _refDoc.EnumDisplayableFields()
                            .Where(kv =>
                                _refDoc.GetRootFieldType(kv.Key).HasFlag(_inputType) ||
                                _refDoc.GetRootFieldType(kv.Key).HasFlag(TypeInfo.List)).Select(kv => kv.Key).ToList();
                        if (fieldsWithCorrectType.Count == 1)
                        {
                            var refKey = fieldsWithCorrectType[0];
                            OperatorFieldReference.GetDocumentController(null)?.SetField(_inputKey,
                                new DocumentReferenceController(_refDoc.Id, refKey), true);
                        }
                        else // otherwise display the autosuggest box
                        {
                            SuggestBox.Visibility = Visibility.Visible;
                            SuggestBox.Focus(FocusState.Programmatic);
                            SuggestBox.Text = string.Empty;
                        }
                    }
            }
            // if the user dragged from the header of a schema view
            else if (e.DataView.Properties.ContainsKey(nameof(DragCollectionFieldModel)))
            {
                if (e.DataView.Properties[nameof(DragCollectionFieldModel)] is DragCollectionFieldModel dragData)
                {
                    var fieldKey = dragData.FieldKey;
                    OperatorFieldReference.GetDocumentController(null)
                        ?.SetField(_inputKey, new TextController(fieldKey.Id), true);
                }
            }

            e.Handled = true; // have to hit handled otherwise the event bubbles to the collection
        }

        private void AutoSuggestBox_OnSuggestionChosen(AutoSuggestBox sender,
            AutoSuggestBoxSuggestionChosenEventArgs args)
        {
            sender.Text = args.SelectedItem.ToString();
        }

        private void AutoSuggestBox_OnTextChanged(AutoSuggestBox sender, AutoSuggestBoxTextChangedEventArgs args)
        {
            if (args.Reason == AutoSuggestionBoxTextChangeReason.UserInput || args.Reason == AutoSuggestionBoxTextChangeReason.ProgrammaticChange)
            {
                var queryText = sender.Text;

                // get the fields that have the same type as the key the user is suggesting for
                var fieldsWithCorrectType = _refDoc.EnumDisplayableFields()
                    .Where(kv => _inputType.HasFlag(_refDoc.GetRootFieldType(kv.Key))).Select(kv => kv.Key).ToList();

                // add all the fields with the correct type to the list of suggestions
                var suggestions = fieldsWithCorrectType.Select(keyController => new CollectionKeyPair(keyController))
                    .ToList();

                // get all the fields from the connecting document that are collections
                var collections = _refDoc.EnumDisplayableFields()
                    .Where(kv => _refDoc.GetRootFieldType(kv.Key).HasFlag(TypeInfo.List)).Select(kv => kv.Key).ToList();

                // if the user has entered dot syntax we want to parse that as <collection>.<field>
                // unfortunatley we don't parse anything nested beyond that
                if (queryText.Contains("."))
                    foreach (var collectionKey in collections)
                    {
                        // make sure it is a collection of documents (not numbers or strings)
                        var docCollection =
                            _refDoc.GetDereferencedField<ListController<DocumentController>>(collectionKey, null);
                        if (docCollection != null)
                        {
                            // get the keys for fields that are associated with types we have as input
                            var validHeaderKeys = Util.GetTypedHeaders(docCollection)
                                /*.Where(kt => kt.Value.Any(ti => _inputType.HasFlag(ti)))*/.Select(kt => kt.Key);
                            // add the keys as collection field pairs
                            suggestions.AddRange(
                                validHeaderKeys.Select(fieldKey => new CollectionKeyPair(fieldKey, collectionKey)));
                        }
                    }
                // if the user hasn't used a "." then we at least let them autocomplete collections of documents
                else
                    suggestions.AddRange(collections
                        .Where(collectionKey =>
                            _refDoc.GetDereferencedField<ListController<DocumentController>>(collectionKey, null) !=
                            null).Select(collectionKey => new CollectionKeyPair(collectionKey)));

                // set the itemsource to either filtered or unfiltered suggestions
                if (queryText == string.Empty)
                {
                    sender.ItemsSource = suggestions.ToHashSet();
                }
                else
                {
                    suggestions = suggestions.Where(s => s.ToString().ToLower().Contains(queryText.ToLower())).ToList();
                    sender.ItemsSource = suggestions.ToHashSet();
                }
            }
        }

        private void AutoSuggestBox_OnQuerySubmitted(AutoSuggestBox sender, AutoSuggestBoxQuerySubmittedEventArgs args)
        {
            if (_refDoc == null) return;
            var key = ((DictionaryEntry?) DataContext)?.Key as KeyController;
            if (args.ChosenSuggestion is CollectionKeyPair chosen)
            {
                if (chosen.CollectionKey == null)
                    OperatorFieldReference.GetDocumentController(null).SetField(key,
                        new DocumentReferenceController(_refDoc.Id, chosen.FieldKey), true);
                else
                    OperatorFieldReference.GetDocumentController(null)
                        .SetField(key, new TextController(chosen.FieldKey.Id), true);
            }

            // hide the suggestion box
            SuggestBox.Visibility = Visibility.Collapsed;
        }

        private void UIElement_OnDoubleTapped(object sender, DoubleTappedRoutedEventArgs e)
        {
            if (_refDoc != null)
            {
                SuggestBox.Visibility = Visibility.Visible;
                SuggestBox.Focus(FocusState.Programmatic);
            }
        }

        private void SuggestBox_OnLostFocus(object sender, RoutedEventArgs e)
        {
            SuggestBox.Visibility = Visibility.Collapsed;
        }

        private class CollectionKeyPair
        {
            public readonly KeyController CollectionKey;
            public readonly KeyController FieldKey;

            public CollectionKeyPair(KeyController fieldKey, KeyController collectionKey = null)
            {
                CollectionKey = collectionKey;
                FieldKey = fieldKey;
            }

            public override bool Equals(object obj)
            {
                var pair = obj as CollectionKeyPair;
                return pair != null &&
                       EqualityComparer<KeyController>.Default.Equals(CollectionKey, pair.CollectionKey) &&
                       EqualityComparer<KeyController>.Default.Equals(FieldKey, pair.FieldKey);
            }

            public override int GetHashCode()
            {
                var hashCode = 1443636342;
                hashCode = hashCode * -1521134295 + EqualityComparer<KeyController>.Default.GetHashCode(CollectionKey);
                hashCode = hashCode * -1521134295 + EqualityComparer<KeyController>.Default.GetHashCode(FieldKey);
                return hashCode;
            }

            public override string ToString()
            {
                var collectionString = CollectionKey?.ToString();
                if (collectionString != null) return collectionString + "." + FieldKey;
                return FieldKey.ToString();
            }
        }
    }
}