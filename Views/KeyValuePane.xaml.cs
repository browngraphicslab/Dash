using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using Windows.ApplicationModel.DataTransfer;
using Windows.System;
using Windows.UI;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Point = Windows.Foundation.Point;

// The User Control item template is documented at https://go.microsoft.com/fwlink/?LinkId=234236

namespace Dash
{
    public sealed partial class KeyValuePane : UserControl
    {
        private bool _showDataDoc = true;

        private DocumentController activeContextDoc { get => _showDataDoc ? _dataContextDocument : _layoutContextDocument; }

        /// <summary>
        /// This is a local reference to the DataContext and the Document we render fields for
        /// </summary>
        private DocumentController _dataContextDocument;
        private DocumentController _layoutContextDocument;
        private DocumentController _contextDocument;

        public class KeyValueItemViewModel : EditableScriptViewModel {
            private bool _isSelected = false;
            public KeyValueItemViewModel(FieldReference reference) : base(reference) { }
            public bool IsSelected
            {
                get => _isSelected;
                set => SetProperty<bool>(ref _isSelected, value);
            }
        };

        /// <summary>
        ///     The list of fields displayed on the key value pane
        /// </summary>
        private ObservableCollection<KeyValueItemViewModel> ListItemSource { get; }

        public GridLength TypeColumnWidth { get; set; } = GridLength.Auto;

        public KeyValuePane(bool AllowClose = false)
        {
            InitializeComponent();

            ListItemSource = new ObservableCollection<KeyValueItemViewModel>();

            DataContextChanged += KeyValuePane_DataContextChanged;

            Loaded += KeyValuePane_Loaded;
            Unloaded += KeyValuePane_Unloaded;
        }

        private void KeyValuePane_Unloaded(object sender, RoutedEventArgs e)
        {
            Loaded -= KeyValuePane_Loaded;
            DataContextChanged -= KeyValuePane_DataContextChanged;
            _dataContextDocument.FieldModelUpdated -= ViewDocumentFieldUpdated;

        }

        private void KeyValuePane_Loaded(object sender, RoutedEventArgs e)
        {
            var currPageBinding = new FieldBinding<TextController>
            {
                Mode = BindingMode.TwoWay,
                Document = this.GetFirstAncestorOfType<DocumentView>().ViewModel.DataDocument,
                Key = KeyStore.TitleKey
            };
            xTitleBlock.AddFieldBinding(TextBlock.TextProperty, currPageBinding);
        }

        /// <summary>
        /// Called whenever the datacontext changes
        /// </summary>
        private void KeyValuePane_DataContextChanged(FrameworkElement sender, DataContextChangedEventArgs args)
        {
            // if the datacontext is a document controller
            if (DataContext is DocumentController dc)
            {
                if (dc.Equals(_contextDocument))
                {
                    return;
                }
                _contextDocument = dc;
                // remove old events from the previous datacontext
                if (_dataContextDocument != null)
                {
                    _dataContextDocument.FieldModelUpdated -= ViewDocumentFieldUpdated;
                    _layoutContextDocument.FieldModelUpdated -= ViewDocumentFieldUpdated;
                }

                // assign the new datacontext to a variable, and add events
                _dataContextDocument = dc.GetDataDocument();
                _layoutContextDocument = dc;
                _dataContextDocument.FieldModelUpdated -= ViewDocumentFieldUpdated;
                _dataContextDocument.FieldModelUpdated += ViewDocumentFieldUpdated;
                _layoutContextDocument.FieldModelUpdated -= ViewDocumentFieldUpdated;
                _layoutContextDocument.FieldModelUpdated += ViewDocumentFieldUpdated;

                // set the field list item source to the new datacontext
                SetListItemSourceToCurrentDataContext();
            }
        }

        /// <summary>
        ///     Resets the ListItemSource to fields in the current datacontext (this can be thought of as rebuilding the entire
        ///     list)
        /// </summary>
        private void SetListItemSourceToCurrentDataContext()
        {
            ListItemSource.Clear();
            if (activeContextDoc != null)
            {
                foreach (var keyFieldPair in activeContextDoc.EnumDisplayableFields())
                    ListItemSource.Add(
                        new KeyValueItemViewModel(
                            new DocumentFieldReference(activeContextDoc, keyFieldPair.Key)));
            }
        }

        /// <summary>
        /// Called whenever the list of fields attached to the document changes
        /// </summary>
        private void ViewDocumentFieldUpdated(FieldControllerBase sender, FieldUpdatedEventArgs args, Context context)
        {
            // if a field has been replaced or updated then set it's source to be the new element
            // otherwise replace the entire data source to reflect the new set of fields (due to add or remove)
            var dargs = (DocumentController.DocumentFieldUpdatedEventArgs) args;
            if (args.Action == DocumentController.FieldUpdatedAction.Add)
            {
                addField(dargs);
            }
            else if (args.Action == DocumentController.FieldUpdatedAction.Remove)
            {
                removeField(dargs);
            }
        }

        private void removeField(DocumentController.DocumentFieldUpdatedEventArgs dargs)
        {
            foreach (var editableScriptViewModel in ListItemSource.Where(esvm => esvm.Reference.Equals(dargs.Reference)).ToList())
            {
                ListItemSource.Remove(editableScriptViewModel);
            }
        }

        private void addField(DocumentController.DocumentFieldUpdatedEventArgs dargs)
        {
            if (!dargs.Reference.FieldKey.Name.StartsWith("_"))
            {
                ListItemSource.Add(new KeyValueItemViewModel(dargs.Reference));
            }
        }


        /// <summary>
        ///     Adds a new row to the KeyValuePane, using user inputed values, returning a boolean depending on whether it is
        ///     successful in adding the pair.
        ///     
        /// </summary>
        private async Task AddKeyValuePair()
        {
            using (UndoManager.GetBatchHandle())
            {
                var key = KeyController.Get(xNewKeyText.Text);
                var stringValue = xNewValueText.Text;

                FieldControllerBase fmController = null;

                try
                {
                    fmController = await DSL.InterpretUserInput(stringValue, scope: Scope.CreateStateWithThisDocument(activeContextDoc));
                }
                catch (DSLException e) { }

                activeContextDoc.SetField(key, fmController, true);

                // reset the fields to the empty values
                xNewKeyText.Text = "";
                xNewValueText.Text = "";
                xFieldsScroller.ChangeView(null, xFieldsScroller.ScrollableHeight, null);
            }
        }

        /// <summary>
        /// add new key value pair on enter in list view of key value grid
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void AddKeyValueFieldOnEnter(object sender, KeyRoutedEventArgs e)
        {
            if (e.Key == VirtualKey.Enter)
            {
                // check key field is filled in
                if (xNewKeyText.Text != "")
                {
                    await AddKeyValuePair();
                    xNewKeyText.Focus(FocusState.Programmatic);
                }
                xFieldsScroller.ChangeView(0.0, xFieldsScroller.ScrollableHeight, 1);
            }
        }

        /// <summary>
        /// hides tab menu on tab up, and focuses on value field
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void TextFocus_KeyUp(object sender, KeyRoutedEventArgs e)
        {
            if (e.Key == VirtualKey.Tab)
            {
                e.Handled = true;
            }
        }

        private void XFieldListView_DragItemsStarting(object sender, DragItemsStartingEventArgs args)
        {
            foreach (object m in args.Items)
            {
                // if the field is a document, then drag out the document
                // otherwise drag out a databox containing the field value.
                var docField = _dataContextDocument.GetField<DocumentController>((m as EditableScriptViewModel)?.Key);
                args.Data.SetDragModel(docField != null ? (DragModelBase)new DragDocumentModel(docField) : new DragFieldModel(new DocumentFieldReference(activeContextDoc, (m as EditableScriptViewModel)?.Key)));
                // args.AllowedOperations = DataPackageOperation.Link | DataPackageOperation.Move | DataPackageOperation.Copy;
                args.Data.RequestedOperation = DataPackageOperation.Move | DataPackageOperation.Copy | DataPackageOperation.Link;
                break;
            }
        }
        private bool _deselectOnTap = false;
        private void KeyValueScriptView_PointerPressed(object sender, PointerRoutedEventArgs e)
        {
            _deselectOnTap = (sender as KVPRow).IsSelected;
            this.GetFirstAncestorOfType<DocumentView>().ManipulationMode = e.GetCurrentPoint(this).Properties.IsRightButtonPressed ? ManipulationModes.All : ManipulationModes.None;
        }

        private void SwitchButton_Tapped(object sender, RoutedEventArgs e)
        {
            _showDataDoc = !_showDataDoc;
            xDocBlock.Text = _showDataDoc ? "Data" : "Layout";

            SetListItemSourceToCurrentDataContext();
        }

        private void xFieldListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            e.AddedItems.ToList().ForEach((ai) => (ai as KeyValueItemViewModel).IsSelected = true);
            e.RemovedItems.ToList().ForEach((ai) => (ai as KeyValueItemViewModel).IsSelected = false);
        }

        private void xText_GotFocus(object sender, RoutedEventArgs e)
        {
            xFieldListView.SelectedIndex = -1; 
        }

        private void KVPRow_Tapped(object sender, TappedRoutedEventArgs e)
        {
            if (_deselectOnTap)
            {
                xFieldListView.SelectedIndex = -1;
                MainPage.Instance.Focus(FocusState.Programmatic);
            }
        }
    }
}
