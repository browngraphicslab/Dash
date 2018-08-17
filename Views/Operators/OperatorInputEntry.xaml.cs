using System.Collections.Generic;
using System.Linq;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;
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
            set { SetValue(OperatorFieldReferenceProperty, value); }
        }

        private DocumentController _refDoc;

        /// <summary>
        /// The type of this input
        /// </summary>
        private TypeInfo _inputType; // TODO currently this only is initialized in dragevents someone else should make this initialized in other places (datacontext changed) if they want...

        public OperatorInputEntry()
        {
            this.InitializeComponent();
            
        }

        /// <summary>
        /// Handles dropping data onto the input operator ellipse.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void UIElement_OnDrop(object sender, DragEventArgs e)
        {
            //// make sure drop data is a document
            //if (e.DataView.Properties.ContainsKey(nameof(DragDocumentModel)))
            //{
            //    var dragData = e.DataView.Properties[nameof(DragDocumentModel)] as DragDocumentModel;
            //    // we pass a view document, so we get the data document
            //    _refDoc = dragData.DraggedDocument?.GetDataDocument();
            //    var opDoc = OperatorFieldReference.GetDocumentController(null);
            //    var el = sender as FrameworkElement;
                

            //    var key = ((KeyValuePair<KeyController, IOInfo>?)el?.DataContext)?.Key as KeyController;
            //    xEllipse.Stroke = new SolidColorBrush(Colors.Red);
            //    if (dragData.DraggedKey != null)
            //    {
            //        opDoc.SetField(key, new DocumentReferenceController(_refDoc, dragData.DraggedKey), true);
            //    }
            //    else
            //    {
            //        // if only one field on the input has the correct type then connect that field
            //        var fieldsWithCorrectType = _refDoc.EnumDisplayableFields().Where(kv => _inputType.HasFlag(_refDoc.GetRootFieldType(kv.Key)) || _refDoc.GetRootFieldType(kv.Key).HasFlag(TypeInfo.List)).Select(kv => kv.Key).ToList();
            //        if (fieldsWithCorrectType.Count == 1)
            //        {
            //            var refKey = fieldsWithCorrectType[0];
            //            opDoc.SetField(key, new DocumentReferenceController(_refDoc, refKey), true);
            //        }
            //        else // otherwise display the autosuggest box
            //        {
            //            SuggestBox.Visibility = Visibility.Visible;
            //            SuggestBox.Focus(FocusState.Programmatic);
            //        }
            //    }
            //}
            //// if the user dragged from the header of a schema view
            //else if (e.DataView.Properties.ContainsKey(nameof(DragCollectionFieldModel)))
            //{
            //    var opDoc = OperatorFieldReference.GetDocumentController(null);
            //    var el = sender as FrameworkElement;
            //    var key = ((KeyValuePair<KeyController, IOInfo>?)el?.DataContext)?.Key as KeyController;
            //    var dragData = e.DataView.Properties[nameof(DragCollectionFieldModel)] as DragCollectionFieldModel;
            //    var fieldKey = dragData.FieldKey;
            //    opDoc.SetField<TextController>(key, fieldKey.Id, true);
            //}


            e.Handled = true; // have to hit handled otherwise the event bubbles to the collection
        }


        private void Ellipse_DragOver(object sender, DragEventArgs e)
        {
            UIElement_OnDragEnter(sender, e);
        }

        /// <summary>
        /// For hovering over operator ellipse.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void UIElement_OnDragEnter(object sender, DragEventArgs e)
        {
            //// set the types of data this operator input can handle
            //var el = sender as FrameworkElement;
            //var opField = OperatorFieldReference.DereferenceToRoot<ListController<OperatorController>>(null).TypedData.First();
            //var key = ((KeyValuePair<KeyController, IOInfo>?)el?.DataContext)?.Key;
            //_inputType = opField.Inputs.First(i => i.Key.Equals(key)).Value.Type;


            //if (e.DataView.Properties.ContainsKey(nameof(DragDocumentModel)))
            //{
            //    var dragData = e.DataView.Properties[nameof(DragDocumentModel)] as DragDocumentModel;
            //    _refDoc = dragData.DraggedDocument.GetDataDocument();
            //    if (dragData.DraggedKey != null) //There is a specified key, so check if it's the right type
            //    {
            //        // the operator controller the input is going to
            //        // the key we're dropping on
            //        // the type of the field we're dragging
            //        var fieldType = _refDoc.GetRootFieldType(dragData.DraggedKey);
            //        // if the field we're dragging from and the field we're dragging too are the same (todo: or convertible)
            //        // then let the user link otherwise don't let them do anything
            //        e.AcceptedOperation = _inputType.HasFlag(fieldType) ? DataPackageOperation.Link : DataPackageOperation.None;
            //    }
            //    else //There's just a document, and a key needs to be chosen later, so accept for now
            //    {
            //        var fieldsWithCorrectType = _refDoc.EnumDisplayableFields().Where(kv => _inputType.HasFlag(_refDoc.GetRootFieldType(kv.Key))).Select(kv => kv.Key).ToList();
            //        e.AcceptedOperation = fieldsWithCorrectType.Count == 0 ? DataPackageOperation.None : DataPackageOperation.Link;
            //    }
            //}

            //// if the user dragged from the header of a schema view
            //else if (e.DataView.Properties.ContainsKey(nameof(DragCollectionFieldModel)))
            //{
            //    e.AcceptedOperation = DataPackageOperation.Link;
            //}
            //System.Diagnostics.Debug.WriteLine("Accpeted = " + e.AcceptedOperation);
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
                var fieldsWithCorrectType = _refDoc.EnumDisplayableFields().Where(kv => _inputType.HasFlag(_refDoc.GetRootFieldType(kv.Key))).Select(kv => kv.Key).ToList();

                // add all the fields with the correct type to the list of suggestions
                var suggestions = fieldsWithCorrectType.Select(keyController => new CollectionKeyPair(keyController)).ToList();

                // get all the fields from the connecting document that are collections
                var collections = _refDoc.EnumDisplayableFields()
                    .Where(kv => _refDoc.GetRootFieldType(kv.Key).HasFlag(TypeInfo.List)).Select(kv => kv.Key).ToList();

                // if the user has entered dot syntax we want to parse that as <collection>.<field>
                // unfortunatley we don't parse anything nested beyond that
                if (queryText.Contains("."))
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
                                /*.Where(kt => kt.Value.Any(ti => _inputType.HasFlag(ti)))*/.Select(kt => kt.Key);
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
            if (_refDoc == null)
            {
                return;
            }
            var key = ((KeyValuePair<KeyController, IOInfo>?)DataContext)?.Key as KeyController;
            if (args.ChosenSuggestion is CollectionKeyPair chosen)
            {
                if (chosen.CollectionKey == null)
                {
                    OperatorFieldReference.GetDocumentController(null).SetField(key,
                        new DocumentReferenceController(_refDoc, chosen.FieldKey), true);
                }
                else
                {
                    OperatorFieldReference.GetDocumentController(null)
                        .SetField<TextController>(key, chosen.FieldKey.Id, true);
                }
            }
            else
            {
                // TODO LSM made it so the user has to unambiguously select a key. this can be changed
                // TODO when we have more robust key parsing


                //KeyController refKey = _refDoc.EnumDisplayableFields()
                //    .Select(kv => kv.Key).FirstOrDefault(k => k.Name == args.QueryText);
                //if (refKey != null)
                //{
                //    OperatorFieldReference.GetDocumentController(null).SetField(key,
                //        new DocumentReferenceController(_refDoc.Id, refKey), true);
                //}
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
                if (collectionString != null)
                {
                    return collectionString + "." + FieldKey;
                }
                return FieldKey.ToString();
            }
        }
    }
}
