using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Dash.Controllers.Operators;
using Dash.Views;
using Windows.System;
using Windows.UI.Core;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using DashShared;
using System.Diagnostics;
using Windows.ApplicationModel.DataTransfer;
using Windows.UI.Xaml.Controls;
using static Dash.CollectionDBSchemaHeader;
using Dash.Models.DragModels;

// The User Control item template is documented at https://go.microsoft.com/fwlink/?LinkId=234236

namespace Dash
{
    public sealed partial class CollectionDBSchemaView : ICollectionView
    {
        private DocumentController _parentDocument;

        private ObjectToStringConverter converter = new ObjectToStringConverter();

        // This list stores the fields added in the schema view (not originally in the documents)
        private List<KeyController> _schemaFieldsNotInDocs;


        public CollectionDBSchemaView()
        {
            this.InitializeComponent();
            Unloaded += CollectionDBSchemaView_Unloaded;
            Loaded += CollectionDBSchemaView_Loaded;
            MinWidth = MinHeight = 50;
            xHeaderView.ItemsSource = SchemaHeaders;
            xEditTextBox.AddHandler(KeyDownEvent, new KeyEventHandler( xEditTextBox_KeyDown), true);
            Drop += CollectionDBSchemaView_Drop;

            _schemaFieldsNotInDocs = new List<KeyController>();
        }

        private void CollectionDBSchemaView_Drop(object sender, DragEventArgs e)
        {
            e.Handled = true;
        }

        private void SchemaHeaders_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            var recordSource = xRecordsView.ItemsSource as ObservableCollection<CollectionDBSchemaRecordViewModel>;
            var slist = SchemaHeaders.ToArray().ToList();
            foreach (var r in recordSource)
                if (r.RecordFields.Count == slist.Count)
                {
                    UpdateRecords((xRecordsView.ItemsSource as ObservableCollection<CollectionDBSchemaRecordViewModel>).ToArray().Select((xr)=> xr.Document));
                    var stuff = new ListController<TextController>();
                    foreach (var s in SchemaHeaders)
                        stuff.Add(new TextController(s.FieldKey.Id));
                    ParentDocument.SetField(HeaderListKey, stuff, true);
                    break;
                }
        }

        //bcz: this field isn't used, but if it's not here Field items won't be updated when they're changed.  Why???????
        public ObservableCollection<CollectionDBSchemaRecordViewModel> Records { get; set; } =
            new ObservableCollection<CollectionDBSchemaRecordViewModel>();

        public ObservableCollection<CollectionDBSchemaHeader.HeaderViewModel> SchemaHeaders { get; set; } =
            new ObservableCollection<CollectionDBSchemaHeader.HeaderViewModel>();

        public DocumentController ParentDocument
        {
            get => _parentDocument;
            set
            {
                if (ParentDocument != null)
                    ParentDocument.FieldModelUpdated -= ParentDocument_DocumentFieldUpdated;
                _parentDocument = value;
                if (value != null)
                {
                    if (ParentDocument.GetField(CollectionDBView.FilterFieldKey) == null)
                        ParentDocument.SetField(CollectionDBView.FilterFieldKey, new KeyController(), true);
                    ParentDocument.FieldModelUpdated += ParentDocument_DocumentFieldUpdated;
                }
            }
        }

        public CollectionViewModel ViewModel { get => DataContext as CollectionViewModel; }

        #region ItemSelection

        public void ToggleSelectAllItems()
        {
        }

        #endregion

        private void CollectionDBSchemaRecordField_FieldTappedEvent(CollectionDBSchemaRecordField fieldView)
        {
            try
            {
                var dc = fieldView.DataContext as CollectionDBSchemaRecordFieldViewModel;
                var recordCollection = (xRecordsView.Items[dc.Row] as CollectionDBSchemaRecordViewModel).RecordFields;
                if (recordCollection.Contains(dc))
                {
                    var column = recordCollection.IndexOf(dc);
                    if (column != -1)
                    {
                        FlyoutBase.SetAttachedFlyout(fieldView, xEditField);
                        updateEditBox(dc);
                        xEditField.ShowAt(this);
                    }

                }
            }
            catch (Exception e)
            {
                Debug.WriteLine(e);
            }


        }

        private void updateEditBox(CollectionDBSchemaRecordFieldViewModel dc)
        {
            xEditTextBox.Tag = dc;
            var field = dc.Document.GetDataDocument().GetDereferencedField(dc.HeaderViewModel.FieldKey, null);
            xEditTextBox.Text = converter.ConvertDataToXaml(field?.GetValue(null));
            var numReturns = xEditTextBox.Text.Count((c) => c == '\r');
            xEditTextBox.Height = Math.Min(250, 50 + numReturns * 15);
            dc.Selected = true;
            xEditTextBox.SelectAll();
        }
        
        private void xEditTextBox_KeyDown(object sender, KeyRoutedEventArgs e)
        {
            try
            {
                if (e.Key == VirtualKey.Enter)
                {
                    if (xEditTextBox.Text == "\r")
                    {
                        var dc = xEditTextBox.Tag as CollectionDBSchemaRecordFieldViewModel;
                        var field = dc.Document.GetDereferencedField(dc.HeaderViewModel.FieldKey, null);
                        xEditTextBox.Text = field?.GetValue(null)?.ToString() ?? "<null>";
                        dc.Selected = false;
                        var direction = this.IsShiftPressed() ? -1 : 1;
                        var column = (xRecordsView.Items[dc.Row] as CollectionDBSchemaRecordViewModel).RecordFields.IndexOf(dc);
                        var recordViewModel = xRecordsView.Items[Math.Max(0, Math.Min(xRecordsView.Items.Count - 1, dc.Row + direction))] as CollectionDBSchemaRecordViewModel;
                        updateEditBox(recordViewModel.RecordFields[column]);
                    }
                    e.Handled = true;
                }
                if (e.Key == Windows.System.VirtualKey.Tab)
                {
                    e.Handled = true;
                }
            }
            catch (Exception exception)
            {
                e.Handled = true;

            }

        }

        private void xEditTextBox_KeyUp(object sender, KeyRoutedEventArgs e)
        {
            try
            {
                var direction = this.IsShiftPressed() ? -1 : 1;
                if (e.Key == Windows.System.VirtualKey.Down || e.Key == Windows.System.VirtualKey.Up)
                {
                    direction = e.Key == Windows.System.VirtualKey.Down ? 1 : e.Key == Windows.System.VirtualKey.Up ? -1 : direction;
                    var dc = xEditTextBox.Tag as CollectionDBSchemaRecordFieldViewModel;
                    SetFieldValue(dc);
                    var column = (xRecordsView.Items[dc.Row] as CollectionDBSchemaRecordViewModel).RecordFields.IndexOf(dc);
                    if (column < 0) return;
                    var recordViewModel = xRecordsView.Items[Math.Max(0, Math.Min(xRecordsView.Items.Count - 1, dc.Row + direction))] as CollectionDBSchemaRecordViewModel;
                    this.xRecordsView.SelectedItem = recordViewModel;
                    updateEditBox(recordViewModel.RecordFields[column]);
                }

                if (e.Key == Windows.System.VirtualKey.Tab || e.Key == Windows.System.VirtualKey.Right || e.Key == Windows.System.VirtualKey.Left)
                {
                    direction = e.Key == Windows.System.VirtualKey.Right ? 1 : e.Key == Windows.System.VirtualKey.Left ? -1 : direction;
                    var dc = xEditTextBox.Tag as CollectionDBSchemaRecordFieldViewModel;
                    SetFieldValue(dc);
                    var column = (xRecordsView.Items[dc.Row] as CollectionDBSchemaRecordViewModel).RecordFields.IndexOf(dc);
                    var recordViewModel = xRecordsView.Items[dc.Row] as CollectionDBSchemaRecordViewModel;
                    updateEditBox(recordViewModel.RecordFields[Math.Max(0, Math.Min(recordViewModel.RecordFields.Count - 1, column + direction))]);
                }
            }
            catch (Exception exception)
            {
                Debug.WriteLine(exception);
            }
            e.Handled = true;
        }


        private void xEditField_Closing(FlyoutBase sender, FlyoutBaseClosingEventArgs args)
        {
            var dc = xEditTextBox.Tag as CollectionDBSchemaRecordFieldViewModel;
            SetFieldValue(dc);
        }

        private void SetFieldValue(CollectionDBSchemaRecordFieldViewModel dc)
        {
            //TODO tfs: on my branch we used new Context(dc.Document) for context instead of null?
            var field = dc.Document.GetDataDocument().GetDereferencedField(dc.HeaderViewModel.FieldKey, null);
            if (field == null)
            {


                var key = dc.HeaderViewModel.FieldKey;
                FieldControllerBase fmController = new TextController("something went wrong");
                var stringValue = xEditTextBox.Text;

                dc.Document.GetDataDocument().ParseDocField(key, xEditTextBox.Text);
                dc.Document.GetDataDocument().ParseDocField(key, xEditTextBox.Text);

                fmController = dc.Document.GetDataDocument().GetField(key);

                // If this field does not yet exist for the document, make one with the inputted text
                if (fmController == null)
                {
                    //TODO make this create the correct field type (with text parsing?)
                    dc.Document.GetDataDocument().SetField(key, new TextController(xEditTextBox.Text), true);
                }
            }
            field = dc.Document.GetDataDocument().GetDereferencedField(dc.HeaderViewModel.FieldKey, null);

            dc.Document.GetDataDocument().ParseDocField(dc.HeaderViewModel.FieldKey, xEditTextBox.Text, field);
            dc.DataReference = new DocumentReferenceController(dc.Document.GetDataDocument(), dc.HeaderViewModel.FieldKey);
            dc.Selected = false;
        }

        private void CollectionDBSchemaView_Unloaded(object sender, RoutedEventArgs e)
        {
            DataContextChanged -= CollectionDBView_DataContextChanged;
            ParentDocument = null;

            CollectionDBSchemaRecordField.FieldTappedEvent -= CollectionDBSchemaRecordField_FieldTappedEvent;
        }


        private void CollectionDBSchemaView_Loaded(object sender, RoutedEventArgs e)
        {
            DataContextChanged += CollectionDBView_DataContextChanged;
            ParentDocument = this.GetFirstAncestorOfType<DocumentView>().ViewModel.DocumentController;
            CollectionDBView_DataContextChanged(null, null);

            CollectionDBSchemaRecordField.FieldTappedEvent -= CollectionDBSchemaRecordField_FieldTappedEvent;
            CollectionDBSchemaRecordField.FieldTappedEvent += CollectionDBSchemaRecordField_FieldTappedEvent;
        }

        private void CollectionDBView_DataContextChanged(FrameworkElement sender, DataContextChangedEventArgs args)
        {
            ParentDocument = this.GetFirstAncestorOfType<DocumentView>()?.ViewModel?.DocumentController;
            if (ParentDocument != null)
                UpdateFields(new Context(ParentDocument)); 
        }


        private void ParentDocument_DocumentFieldUpdated(FieldControllerBase sender, FieldUpdatedEventArgs args, Context context)
        {
            if (((DocumentController.DocumentFieldUpdatedEventArgs) args).Reference.FieldKey.Equals(ViewModel?.CollectionKey))
                UpdateFields(new Context(ParentDocument));
        }

        /// </summary>
        public static KeyController HeaderListKey = new KeyController("HeaderList", "7C3F0C3F-F065-4094-8802-F572B35C4D42");
        private bool SchemaHeadersContains(KeyController field)
        {
            foreach (var s in SchemaHeaders)
                if (s.FieldKey.Equals(field))
                    return true;
            return false;
        }

        KeyController _lastFieldSortKey = null;
        public void Sort(CollectionDBSchemaHeader.HeaderViewModel viewModel)
        {
            var dbDocs = ParentDocument.GetDataDocument()
                   .GetDereferencedField<ListController<DocumentController>>(ViewModel.CollectionKey, null)?.TypedData;

            var records = new SortedList<string, DocumentController>();
            foreach (var d in dbDocs)
            {
                var str = d.GetDataDocument().GetDereferencedField(viewModel.FieldKey, null)?.GetValue(new Context(d))?.ToString() ?? "{}";
                if (records.ContainsKey(str))
                    records.Add(str + Guid.NewGuid(), d);
                else records.Add(str, d);
            }
            if (_lastFieldSortKey != null && _lastFieldSortKey.Equals(viewModel.FieldKey))
                UpdateRecords(records.Select((r) => r.Value).Reverse());
            else UpdateRecords(records.Select((r) => r.Value));
            _lastFieldSortKey = viewModel.FieldKey;
        }
        /// <summary>
        ///     Updates all the fields in the schema view
        /// </summary>
        /// <param name="context"></param>
        public void UpdateFields(Context context)
        {
            var dbDocs = ParentDocument.GetDataDocument().GetDereferencedField<ListController<DocumentController>>(ViewModel.CollectionKey, context)?.TypedData ??
                         ParentDocument.GetDataDocument().GetDereferencedField<ListController<DocumentController>>(KeyStore.DataKey, context)?.TypedData;
            //TODO This can be a list of keys, it doesn't have to be a list of TextControllers anymore
            var headerList = ParentDocument
                .GetDereferencedField<ListController<TextController>>(HeaderListKey, context)?.TypedData ?? new List<TextController>();
            if (dbDocs != null)
            {
                SchemaHeaders.CollectionChanged -= SchemaHeaders_CollectionChanged;
                SchemaHeaders.Clear();
                foreach (var h in headerList)
                {
                    var fieldKey = ContentController<FieldModel>.GetController<KeyController>(h.Data);
                    var hvm = new CollectionDBSchemaHeader.HeaderViewModel()
                    {
                        SchemaView = this,
                        SchemaDocument = ParentDocument,
                        // TODO: Allow width to carry over when regenerating header list
                        Width = 150,
                        FieldKey = fieldKey
                    };
                    SchemaHeaders.Add(hvm);
                }
                // for each document we add any header we find with a name not matching a current name. This is the UNION of all fields *assuming no collisions
                foreach (var d in dbDocs.Select((db) => db.GetDataDocument()))
                {
                    //if (d.GetField(RegexOperatorController.TextKey) == null &&
                    //    d.GetField(KeyStore.DocumentTextKey) != null)
                    //{
                    //    var rd = OperatorDocumentFactory.CreateOperatorDocument(new RegexOperatorController());
                    //    rd.SetField(RegexOperatorController.ExpressionKey, new TextController("^\\$[0-9.]+$"), true);
                    //    rd.SetField(RegexOperatorController.SplitExpressionKey, new TextController(" "), true);
                    //    rd.SetField(RegexOperatorController.ExpressionKey, new TextController(".*"), true);
                    //    rd.SetField(RegexOperatorController.SplitExpressionKey, new TextController("\\."), true);
                    //    rd.SetField(RegexOperatorController.TextKey, new DocumentReferenceFieldController(d.GetId(), KeyStore.DocumentTextKey), true);
                    //    d.SetField(RegexOperatorController.MatchesKey, new DocumentReferenceFieldController(rd.GetId(), RegexOperatorController.MatchesKey), true);
                    //}
                    foreach (var f in d.EnumFields())
                        if (!f.Key.Name.StartsWith("_") && !SchemaHeadersContains(f.Key))
                            SchemaHeaders.Add(new CollectionDBSchemaHeader.HeaderViewModel() { SchemaView = this, SchemaDocument = ParentDocument, Width = 150, FieldKey = f.Key });
                }

                // Add to the header the fields that are not in the documents but has been added to the schema view by the user
                // if the field is already being displayed (the user has added this field to a document by entered a value), remove it from the list
                foreach (var f in _schemaFieldsNotInDocs)
                    if (!f.Name.StartsWith("_") && !SchemaHeadersContains(f))
                    {
                        SchemaHeaders.Add(new CollectionDBSchemaHeader.HeaderViewModel() { SchemaView = this, SchemaDocument = ParentDocument, Width = 150, FieldKey = f });
                    }
                    else
                    {
                        _schemaFieldsNotInDocs.Remove(f);
                    }

                SchemaHeaders.CollectionChanged += SchemaHeaders_CollectionChanged;

                // add all the records
                UpdateRecords(dbDocs);
            }
        }

        private void UpdateRecords(IEnumerable<DocumentController> dbDocs)
        {
            var records = new List<CollectionDBSchemaRecordViewModel>();
            int recordCount = 0;
            foreach (var d in dbDocs)
            {
                records.Add(new CollectionDBSchemaRecordViewModel(
                    ParentDocument,
                    d,
                    SchemaHeaders.Select(f => new CollectionDBSchemaRecordFieldViewModel(d, f, HeaderBorderThickness, recordCount))
                    ));
                recordCount++;
            }
            xRecordsView.ItemsSource = new ObservableCollection<CollectionDBSchemaRecordViewModel>(records);
        }

        /// <summary>
        ///     removes any documents which would lead to infinite loops from dbDocs
        /// </summary>
        /// <param name="dbDocs"></param>
        /// <param name="selectedBars"></param>
        public void filterDocuments(List<DocumentController> dbDocs, List<string> selectedBars)
        {
            var keepAll = selectedBars.Count == 0;

            var collection = new List<DocumentController>();

            foreach (var dmc in dbDocs.ToArray())
            {
                var visited = new List<DocumentController>();
                visited.Add(dmc);

                if (SearchInDocumentForNamedField(dmc, selectedBars, visited))
                    collection.Add(dmc);
            }
            ParentDocument.SetField(KeyStore.CollectionOutputKey, new ListController<DocumentController>(collection), true);
        }

        private static bool SearchInDocumentForNamedField(DocumentController dmc, List<string> selectedBars,
            List<DocumentController> visited)
        {
            if (dmc == null)
                return false;
            // loop through each field to find on that matches the field name pattern 
            foreach (var pfield in dmc.EnumFields()
                .Where(pf => selectedBars.Contains(pf.Key.Name) || pf.Value is DocumentController))
                if (pfield.Value is DocumentController nestedDoc)
                {
                    if (!visited.Contains(nestedDoc))
                    {
                        visited.Add(nestedDoc);
                        var field = SearchInDocumentForNamedField(nestedDoc, selectedBars, visited);
                        if (field)
                            return true;
                    }
                }
                else
                {
                    return true;
                }
            return false;
        }

        #region DragAndDrop
        

        public void SetDropIndicationFill(Brush fill)
        {
        }

        #endregion

        #region Activation
        

        private void OnTapped(object sender, TappedRoutedEventArgs e)
        {
            e.Handled = true;
        }

        #endregion

        private void xOuterGrid_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            this.xRecordsView.Height = xOuterGrid.ActualHeight - xHeaderArea.ActualHeight;
        }

        private void xOuterGrid_Loaded(object sender, RoutedEventArgs e)
        {
            this.xRecordsView.Height = xOuterGrid.ActualHeight - xHeaderArea.ActualHeight;
        }

        private void XRecordsView_OnLoaded(object sender, RoutedEventArgs e)
        {
            Util.FixListViewBaseManipulationDeltaPropagation(xRecordsView);
        }

        private void XRecordsView_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var vm = e.AddedItems.FirstOrDefault() as CollectionDBSchemaRecordViewModel;
            if (vm == null) return;
            var recordDoc = vm.Document.GetLayoutFromDataDocAndSetDefaultLayout();
            this.GetFirstAncestorOfType<DocumentView>().ViewModel.DataDocument.SetField(KeyStore.SelectedSchemaRow, recordDoc, true);
        }
        
        private void XRecordsView_OnDragItemsStarting(object sender, DragItemsStartingEventArgs args)
        {
            foreach (var vm in args.Items.Select((item) => item as CollectionDBSchemaRecordViewModel))
            {
                vm.Document.GetLayoutFromDataDocAndSetDefaultLayout();
                // bcz: this ends up dragging only the last document -- next to extend DragDocumentModel to support collections of documents
                args.Data.Properties[nameof(DragDocumentModel)] = new DragDocumentModel(vm.Document, true);
                args.Data.RequestedOperation = DataPackageOperation.Move | DataPackageOperation.Copy | DataPackageOperation.Link;
            }
        }

        private void xHeaderView_DragItemsStarting(object sender, DragItemsStartingEventArgs e)
        {
            foreach (var m in e.Items)
            {
                var viewModel = m as HeaderViewModel;
                var collectionViewModel = (viewModel.SchemaView.DataContext as CollectionViewModel);
                var collectionReference = new DocumentReferenceController(viewModel.SchemaDocument.GetDataDocument(), collectionViewModel.CollectionKey);
                var collectionData = collectionReference.DereferenceToRoot<ListController<DocumentController>>(null).TypedData;
                e.Data.Properties.Add(nameof(DragCollectionFieldModel),
                    new DragCollectionFieldModel(
                        collectionData,
                        collectionReference,
                        viewModel.FieldKey,
                        CollectionView.CollectionViewType.DB
                    ));
            }
        }

        private void xRecordsView_PointerPressed(object sender, PointerRoutedEventArgs e)
        {
           this.GetFirstAncestorOfType<DocumentView>().ManipulationMode = e.GetCurrentPoint(this).Properties.IsRightButtonPressed ? ManipulationModes.All : ManipulationModes.None;
        }

        private void AddRow_OnTapped(object sender, TappedRoutedEventArgs e)
        {
            // Add a new document to the schema view
            ViewModel.AddDocument(Util.BlankNote());
            e.Handled = true;
        }

        private void AddColumn_OnTapped(object sender, TappedRoutedEventArgs e)
        {
            // Add a new field to the schema view
            _schemaFieldsNotInDocs.Add(new KeyController("New Field", UtilShared.GenerateNewId()));
            UpdateFields(new Context(ParentDocument));
        }
    }
}