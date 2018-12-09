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
using Windows.UI.Xaml.Media;

// The User Control item template is documented at https://go.microsoft.com/fwlink/?LinkId=234236

namespace Dash
{
    public sealed partial class KeyValuePane : UserControl
    {

        private bool _showDataDoc = true;

        DocumentController activeContextDoc { get => _showDataDoc ? _dataContextDocument : _layoutContextDocument; }

        /// <summary>
        /// This is a local reference to the DataContext and the Document we render fields for
        /// </summary>
        private DocumentController _dataContextDocument;
        private DocumentController _layoutContextDocument;
        private DocumentController _contextDocument;

        /// <summary>
        ///     The list of fields displayed on the key value pane
        /// </summary>
        private ObservableCollection<EditableScriptViewModel> ListItemSource { get; }

        public GridLength TypeColumnWidth { get; set; } = GridLength.Auto;

        public KeyValuePane(bool AllowClose = false)
        {
            InitializeComponent();

            ListItemSource = new ObservableCollection<EditableScriptViewModel>();

            DataContextChanged += KeyValuePane_DataContextChanged;
            PointerPressed += (sender, e) =>
                this.GetFirstAncestorOfType<DocumentView>().ManipulationMode = e.GetCurrentPoint(this).Properties.IsRightButtonPressed ? ManipulationModes.All : ManipulationModes.None;

            if (AllowClose)
            {
                xCloseButton.Visibility = Visibility.Collapsed;
            }

            Loaded += KeyValuePane_Loaded;
            Unloaded += KeyValuePane_Unloaded;
        }

        void FontIcon_PointerPressed(object sender, PointerRoutedEventArgs e)
        {
            this.GetFirstAncestorOfType<DocumentView>().ManipulationMode = e.GetCurrentPoint(this).Properties.IsRightButtonPressed ? ManipulationModes.All : ManipulationModes.None;
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
                //TODO This probably leaks events/memory
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
                        new EditableScriptViewModel(
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
                ListItemSource.Add(new EditableScriptViewModel(dargs.Reference));
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

                FieldControllerBase fmController;

                try
                {
                    //fmController = DSL.InterpretUserInput(stringValue, true);
                    fmController = await DSL.InterpretUserInput(stringValue, scope: Scope.CreateStateWithThisDocument(activeContextDoc));
                }
                catch (DSLException e)
                {
                    fmController = null;
                }

                activeContextDoc.SetField(key, fmController, true);

                // reset the fields to the empty values
                xNewKeyText.Text = "";
                xNewValueText.Text = "";
                xFieldsScroller.ChangeView(null, xFieldsScroller.ScrollableHeight, null);
            }
        }

        private void CloseButton_Tapped(object sender, TappedRoutedEventArgs e)
        {
            var docView = this.GetFirstAncestorOfType<DocumentView>();
            using (UndoManager.GetBatchHandle())
                docView.DeleteDocument();
            e.Handled = true;
        }

        private void XNewKeyField_OnKeyUp(object sender, KeyRoutedEventArgs e)
        {
            // focus on the value field if the user hits the tab key
            if (e.Key == VirtualKey.Tab)
            {
                e.Handled = true; // stop the operator menu frum shoing up
            }
        }

        private async void XNewValueField_OnKeyUp(object sender, KeyRoutedEventArgs e)
        {
            // focus on the button if the user hits the tab key
            if (e.Key == VirtualKey.Tab)
            {
                e.Handled = true;
            }

            // add the field if the user hits enter
            if (e.Key == VirtualKey.Enter)
            {
                await AddKeyValuePair();
            }
        }


        /// <summary>
        /// changing background color slightly to show that you've moused over this element
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ListItemPointerEntered(object sender, PointerRoutedEventArgs e)
        {
            var container = (Panel)sender;
            container.Background = new SolidColorBrush(Color.FromArgb(80, 180, 180, 180));
        }
        /// <summary>
        /// changes bg color back
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ListItemPointerExited(object sender, PointerRoutedEventArgs e)
        {
            var container = (Panel)sender;
            container.Background = new SolidColorBrush(Color.FromArgb(0, 255, 255, 255));
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
                var docField = _dataContextDocument.GetField<DocumentController>((m as EditableScriptViewModel)?.Key);
                args.Data.SetDragModel(docField != null ? (DragModelBase)new DragDocumentModel(docField) : new DragFieldModel(new DocumentFieldReference(activeContextDoc, (m as EditableScriptViewModel)?.Key)));
                // args.AllowedOperations = DataPackageOperation.Link | DataPackageOperation.Move | DataPackageOperation.Copy;
                args.Data.RequestedOperation = DataPackageOperation.Move | DataPackageOperation.Copy | DataPackageOperation.Link;
                break;
            }
        }

        private void KeyValueScriptView_PointerPressed(object sender, PointerRoutedEventArgs e)
        {
            this.GetFirstAncestorOfType<DocumentView>().ManipulationMode = e.GetCurrentPoint(this).Properties.IsRightButtonPressed ? ManipulationModes.All : ManipulationModes.None;
        }

        private void xKeyListView_DragItemsStarting(object sender, DragItemsStartingEventArgs args)
        {
            foreach (var m in args.Items)
            {
                args.Data.SetDragModel(new DragFieldModel(new DocumentFieldReference(activeContextDoc, (m as EditableScriptViewModel).Key)));
                // args.AllowedOperations = DataPackageOperation.Link | DataPackageOperation.Move | DataPackageOperation.Copy;
                args.Data.RequestedOperation = DataPackageOperation.Move | DataPackageOperation.Copy | DataPackageOperation.Link;
                break;
            }
        }

        private void SwitchButton_Tapped(object sender, TappedRoutedEventArgs e)
        {
            _showDataDoc = !_showDataDoc;
            xDocBlock.Text = _showDataDoc ? "Data" : "Layout";

            OffsetMarginOnToggle();

            SetListItemSourceToCurrentDataContext();
        }

        private void xDocBlock_Tapped(object sender, TappedRoutedEventArgs e)
        {
            _showDataDoc = !_showDataDoc;
            xDocBlock.Text = _showDataDoc ? "Data" : "Layout";

            OffsetMarginOnToggle();

            SetListItemSourceToCurrentDataContext();
        }

        private void OffsetMarginOnToggle()
        {
            var margin = new Thickness
            {
                Top = -4,
                Left = xDocBlock.Text.Equals("Data") ? -12 : -30
            };
            xDocBlock.Margin = margin;
        }
    }
}
