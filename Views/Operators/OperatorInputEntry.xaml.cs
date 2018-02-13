using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.ApplicationModel.DataTransfer;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using DashShared;
using Visibility = Windows.UI.Xaml.Visibility;

// The User Control item template is documented at https://go.microsoft.com/fwlink/?LinkId=234236

namespace Dash
{
    public sealed partial class OperatorInputEntry : UserControl
    {
        public static readonly DependencyProperty OperatorFieldReferenceProperty = DependencyProperty.Register(
            "OperatorFieldReference", typeof(FieldReference), typeof(OperatorInputEntry), new PropertyMetadata(default(FieldReference)));

        public FieldReference OperatorFieldReference
        {
            get { return (FieldReference)GetValue(OperatorFieldReferenceProperty); }
            set { xFieldLabel.Foreground = new SolidColorBrush(Colors.Red); SetValue(OperatorFieldReferenceProperty, value); }
        }

        private DocumentController _refDoc;

        /// <summary>
        /// The type of this input
        /// </summary>
        private TypeInfo _inputType; // TODO currently this only is initialized in dragevents someone else should make this initialized in other places (datacontext changed) if they want...

        public OperatorInputEntry()
        {
            this.InitializeComponent();
            DataContextChanged += OperatorInputEntry_DataContextChanged;
        }

        private void OperatorInputEntry_DataContextChanged(FrameworkElement sender, DataContextChangedEventArgs args)
        {

            var key = ((DictionaryEntry?)xHandle.DataContext)?.Key as KeyController;
            
            var opDoc = OperatorFieldReference?.GetDocumentController(null);
            if (opDoc == null)
            {
                ToggleInputUI(false);
                return;
            }

            var inputRef = opDoc.GetField(key);
            if (inputRef != null)
            {
                ToggleInputUI(true);
            }
        }
        

        private void ToggleInputUI(bool hasInput)
        {
            if (hasInput)
                xFieldLabel.Foreground = new SolidColorBrush(Colors.Red);
            else
                xFieldLabel.Foreground = new SolidColorBrush(Colors.Black);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void InputLinkHandle_OnDrop(object sender, DragEventArgs e)
        {
            if (e.DataView.Properties.ContainsKey("Operator Document"))
            {
                // we pass a view document, so we get the data document, this is the doc we dragged from
                var refDoc = (e.DataView.Properties["Operator Document"] as DocumentController)?.GetDataDocument();         
                var opDoc = OperatorFieldReference.GetDocumentController(null);
                var el = sender as FrameworkElement;
                var key = ((DictionaryEntry?)el?.DataContext)?.Key as KeyController;
                _refDoc = refDoc;

                // if the drag event is associated with a key then set the input using the key
                if (e.DataView.Properties.ContainsKey("Operator Key"))
                {
                    var refKey = (KeyController)e.DataView.Properties["Operator Key"];
                    opDoc.SetField(key, new DocumentReferenceController(refDoc.Id, refKey), true);
                    ToggleInputUI(true); // assume setting key works so show input ui
                }
                else
                {
                    // user can manually input a key
                    SuggestBox.Visibility = Visibility.Visible;
                    SuggestBox.Focus(FocusState.Programmatic);
                }
            }
            // if the user dragged from the header of a schema view
            else if (CollectionDBSchemaHeader.DragModel != null)
            {
                var opDoc = OperatorFieldReference.GetDocumentController(null);
                var el = sender as FrameworkElement;
                var key = ((DictionaryEntry?)el?.DataContext)?.Key as KeyController;
                opDoc.SetField(key, new TextController(CollectionDBSchemaHeader.DragModel.FieldKey.Id), true);


            }
            e.Handled = true;
        }
        
        /// <summary>
        /// When the pointer is hovering over the input handle. TODO: should also have this
        /// when you're just hovering over the label as well, makes it easier on user / for touch
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void InputLinkHandle_OnDragOver(object sender, DragEventArgs e)
        {
            // set the input type
            var el = sender as FrameworkElement;
            var opField = OperatorFieldReference.DereferenceToRoot<OperatorController>(null);
            var key = ((DictionaryEntry?)el?.DataContext)?.Key as KeyController;
            _inputType = opField.Inputs[key].Type;

            if (e.DataView.Properties.ContainsKey("Operator Document"))
            {
                var refDoc = (DocumentController)e.DataView.Properties["Operator Document"];
                if (e.DataView.Properties.ContainsKey("Operator Key")) //There is a specified key, so check if it's the right type
                {
                    // the key we're dragging from
                    var refKey = (KeyController)e.DataView.Properties["Operator Key"];
                    // the operator controller the input is going to
                    // the key we're dropping on
                    // the type of the field we're dragging
                    var fieldType = refDoc.GetRootFieldType(refKey);
                    // if the field we're dragging from and the field we're dragging too are the same then let the user link otherwise don't let them do anything
                    e.AcceptedOperation = _inputType.HasFlag(fieldType) ? DataPackageOperation.Link : DataPackageOperation.None;
                }
                else //There's just a document, and a key needs to be chosen later, so accept for now
                {
                    e.AcceptedOperation = DataPackageOperation.Link;
                }
            }

            // if the user dragged from the header of a schema view
            else if (CollectionDBSchemaHeader.DragModel != null)
            {
                e.AcceptedOperation = DataPackageOperation.Link;
            }
            e.Handled = true;
        }

        private void AutoSuggestBox_OnSuggestionChosen(AutoSuggestBox sender, AutoSuggestBoxSuggestionChosenEventArgs args)
        {
            sender.Text = args.SelectedItem.ToString();
        }

        private void AutoSuggestBox_OnTextChanged(AutoSuggestBox sender, AutoSuggestBoxTextChangedEventArgs args)
        {
            if (args.Reason == AutoSuggestionBoxTextChangeReason.UserInput)
            {
                var queryText = sender.Text;

                // get the fields that have the same type as the key the user is suggesting for
                var fieldsWithCorrectType = _refDoc.EnumDisplayableFields().Where(kv => _inputType.HasFlag(_refDoc.GetRootFieldType(kv.Key))).Select(kv => kv.Key);

                // add all the fields with the correct type to the list of suggestions
                var suggestions = fieldsWithCorrectType.Select(keyController => new CollectionKeyPair(keyController)).ToList();

                // get all the fields from the connecting document that are collections
                var collections = _refDoc.EnumDisplayableFields()
                    .Where(kv => _refDoc.GetRootFieldType(kv.Key).HasFlag(TypeInfo.List)).Select(kv => kv.Key);

                // if the user has entered dot syntax we want to parse that as <collection>.<field>
                // unfortunatley we don't parse anything nested beyond that
                if (queryText.EndsWith("."))
                {
                    // iterate over all collections
                    foreach (var collectionKey in collections)
                    {
                        // make sure it is a collection of documents (not numbers or strings)
                        var docCollection =
                            _refDoc.GetDereferencedField<ListController<DocumentController>>(collectionKey, null);
                        if (docCollection != null)
                        {
                            // get the keys for fields that are associated with types we have as input
                            var validHeaderKeys = Util.GetTypedHeaders(docCollection)
                                .Where(kt => kt.Value.Any(ti => _inputType.HasFlag(ti))).Select(kt => kt.Key);
                            // add the keys as collection field pairs
                            suggestions.AddRange(
                                validHeaderKeys.Select(fieldKey => new CollectionKeyPair(fieldKey, collectionKey)));
                        }
                    }
                }
                // if the user hasn't used a "." then we at least let them autocomplete collections of documents
                else
                {
                    suggestions.AddRange(collections.Where(collectionKey => _refDoc.GetDereferencedField<ListController<DocumentController>>(collectionKey, null) != null).Select(collectionKey => new CollectionKeyPair(collectionKey)));
                }
                
                // set the itemsource to either filtered or unfiltered suggestions
                if (queryText == string.Empty)
                {
                    sender.ItemsSource = suggestions;
                }
                else
                {
                    suggestions = suggestions.Where(s => s.ToString().ToLower().Contains(queryText.ToLower())).ToList();
                    sender.ItemsSource = suggestions;
                }
            }
        }

        private void AutoSuggestBox_OnQuerySubmitted(AutoSuggestBox sender, AutoSuggestBoxQuerySubmittedEventArgs args)
        {
            if (_refDoc == null)
            {
                return;
            }
            var key = ((DictionaryEntry?)DataContext)?.Key as KeyController;
            if (args.ChosenSuggestion is CollectionKeyPair chosen)
            {
                if (chosen.CollectionKey == null)
                {
                    OperatorFieldReference.GetDocumentController(null).SetField(key,
                        new DocumentReferenceController(_refDoc.Id, chosen.FieldKey), true);
                }
                else
                {
                    OperatorFieldReference.GetDocumentController(null)
                        .SetField(key, new TextController(chosen.FieldKey.Id), true);
                }
            }
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

            public override string ToString()
            {
                var collectionString = CollectionKey?.ToString();
                if (collectionString != null)
                {
                    return collectionString + "." + FieldKey;
                }
                return FieldKey.ToString();
            }
        }
    }
}
