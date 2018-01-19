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
using DashShared.Models;
using Windows.UI.Xaml.Controls;

// The User Control item template is documented at https://go.microsoft.com/fwlink/?LinkId=234236

namespace Dash
{
    public sealed partial class CollectionDBSchemaView : SelectionElement, ICollectionView
    {
        private DocumentController _parentDocument;

        private ObjectToStringConverter converter = new ObjectToStringConverter();
        public CollectionDBSchemaView()
        {
            this.InitializeComponent();
            Unloaded += CollectionDBSchemaView_Unloaded;
            Loaded += CollectionDBSchemaView_Loaded;
            MinWidth = MinHeight = 50;
            xHeaderView.ItemsSource = SchemaHeaders;
            xEditTextBox.AddHandler(KeyDownEvent, new KeyEventHandler( xEditTextBox_KeyDown), true);
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
            CollectionDBSchemaHeader.DragModel = null;
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
                _parentDocument = value;
                if (value != null)
                {
                    _parentDocument = _parentDocument.GetDataDocument(null);
                    ParentDocument.FieldModelUpdated -= ParentDocument_DocumentFieldUpdated;
                    if (ParentDocument.GetField(DBFilterOperatorController.FilterFieldKey) == null)
                        ParentDocument.SetField(DBFilterOperatorController.FilterFieldKey,
                            new TextController(""), true);
                    ParentDocument.FieldModelUpdated += ParentDocument_DocumentFieldUpdated;
                }
            }
        }

        public BaseCollectionViewModel ViewModel { get; private set; }

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
            var field = dc.Document.GetDataDocument(null).GetDereferencedField(dc.HeaderViewModel.FieldKey, null);
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
                if (e.Key == Windows.System.VirtualKey.Enter)
                {
                    if (xEditTextBox.Text == "\r")
                    {
                        var dc = xEditTextBox.Tag as CollectionDBSchemaRecordFieldViewModel;
                        var field = dc.Document.GetDereferencedField(dc.HeaderViewModel.FieldKey, null);
                        xEditTextBox.Text = field?.GetValue(null)?.ToString() ?? "<null>";
                        dc.Selected = false;
                        var direction = (Window.Current.CoreWindow.GetKeyState(VirtualKey.Shift).HasFlag(CoreVirtualKeyStates.Down)) ? -1 : 1;
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
                var direction = (Window.Current.CoreWindow.GetKeyState(VirtualKey.Shift).HasFlag(CoreVirtualKeyStates.Down)) ? -1 : 1;
                if (e.Key == Windows.System.VirtualKey.Down || e.Key == Windows.System.VirtualKey.Up)
                {
                    direction = e.Key == Windows.System.VirtualKey.Down ? 1 : e.Key == Windows.System.VirtualKey.Up ? -1 : direction;
                    var dc = xEditTextBox.Tag as CollectionDBSchemaRecordFieldViewModel;
                    SetFieldValue(dc);
                    var column = (xRecordsView.Items[dc.Row] as CollectionDBSchemaRecordViewModel).RecordFields.IndexOf(dc);
                    var recordViewModel = xRecordsView.Items[Math.Max(0, Math.Min(xRecordsView.Items.Count - 1, dc.Row + direction))] as CollectionDBSchemaRecordViewModel;
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
            dc.Document.GetDataDocument(null).ParseDocField(dc.HeaderViewModel.FieldKey, xEditTextBox.Text, dc.Document.GetDataDocument(null).GetDereferencedField(dc.HeaderViewModel.FieldKey,null));
            dc.DataReference = new DocumentReferenceController(dc.Document.GetDataDocument(null).GetId(), dc.HeaderViewModel.FieldKey);
            dc.Selected = false;
        }

        private void CollectionDBSchemaView_Unloaded(object sender, RoutedEventArgs e)
        {
            DataContextChanged -= CollectionDBView_DataContextChanged;
            if (ParentDocument != null)
                ParentDocument.FieldModelUpdated -= ParentDocument_DocumentFieldUpdated;
            ParentDocument = null;

            CollectionDBSchemaRecordField.FieldTappedEvent -= CollectionDBSchemaRecordField_FieldTappedEvent;
        }


        private void CollectionDBSchemaView_Loaded(object sender, RoutedEventArgs e)
        {
            DataContextChanged += CollectionDBView_DataContextChanged;
            ViewModel = DataContext as BaseCollectionViewModel;
            ParentDocument = this.GetFirstAncestorOfType<DocumentView>().ViewModel.DocumentController;
            if (ViewModel != null)
                UpdateFields(new Context(ParentDocument));

            CollectionDBSchemaRecordField.FieldTappedEvent += CollectionDBSchemaRecordField_FieldTappedEvent;
        }

        private void CollectionDBView_DataContextChanged(FrameworkElement sender, DataContextChangedEventArgs args)
        {
            ViewModel = DataContext as BaseCollectionViewModel;
            ViewModel.OutputKey = KeyStore.CollectionOutputKey;
            if (ParentDocument != null)
                ParentDocument.FieldModelUpdated -= ParentDocument_DocumentFieldUpdated;
            ParentDocument = this.GetFirstAncestorOfType<DocumentView>()?.ViewModel?.DocumentController;
            if (ParentDocument != null)
                UpdateFields(new Context(ParentDocument));
        }


        private void ParentDocument_DocumentFieldUpdated(FieldControllerBase sender,
            FieldUpdatedEventArgs args, Context context)
        {
            if (((DocumentController.DocumentFieldUpdatedEventArgs) args).Reference.FieldKey.Equals(ViewModel.CollectionKey))
                UpdateFields(new Context(ParentDocument));
        }

        /// </summary>
        public static KeyController HeaderListKey = new KeyController("7C3F0C3F-F065-4094-8802-F572B35C4D42", "HeaderList");
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
            var dbDocs = ParentDocument
                   .GetDereferencedField<ListController<DocumentController>>(ViewModel.CollectionKey, null)?.TypedData?.Select((d) => d.GetDereferencedField<DocumentController>(KeyStore.DocumentContextKey, null) ?? d);

            var records = new SortedList<string, DocumentController>();
            foreach (var d in dbDocs)
            {
                var str = d.GetDereferencedField(viewModel.FieldKey, null)?.GetValue(new Context(d))?.ToString() ?? "{}";
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
            var dbDocs = ParentDocument.GetDereferencedField<ListController<DocumentController>>(ViewModel.CollectionKey, context)?.TypedData;
            var headerList = ParentDocument
                .GetDereferencedField<ListController<TextController>>(HeaderListKey, context)?.Data ?? new List<FieldControllerBase>();
            if (dbDocs != null)
            {
                SchemaHeaders.CollectionChanged -= SchemaHeaders_CollectionChanged;
                SchemaHeaders.Clear();
                foreach (var h in headerList)
                { 
                    SchemaHeaders.Add(new CollectionDBSchemaHeader.HeaderViewModel() { SchemaView = this, SchemaDocument = ParentDocument, Width = 70, 
                                                     FieldKey = ContentController<FieldModel>.GetController<KeyController>((h as TextController).Data)  });
                }
                // for each document we add any header we find with a name not matching a current name. This is the UNION of all fields *assuming no collisions
                foreach (var d in dbDocs.Select((db)=> db.GetDereferencedField<DocumentController>(KeyStore.DocumentContextKey, null) ?? db))
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
                            SchemaHeaders.Add(new CollectionDBSchemaHeader.HeaderViewModel() { SchemaView = this, SchemaDocument = ParentDocument, Width = 70, FieldKey = f.Key });
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
                if (pfield.Value is DocumentController)
                {
                    var nestedDoc = pfield.Value as DocumentController;
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

        private void CollectionViewOnDragEnter(object sender, DragEventArgs e)
        {
            ViewModel.CollectionViewOnDragEnter(sender, e);
        }

        private void CollectionViewOnDrop(object sender, DragEventArgs e)
        {
            Debug.WriteLine("drop event from collection");

            ViewModel.CollectionViewOnDrop(sender, e);
        }

        private void CollectionViewOnDragLeave(object sender, DragEventArgs e)
        {
            ViewModel.CollectionViewOnDragLeave(sender, e);
        }

        public void SetDropIndicationFill(Brush fill)
        {
        }

        #endregion

        #region Activation

        protected override void OnActivated(bool isSelected)
        {
            ViewModel.SetSelected(this, isSelected);
        }

        protected override void OnLowestActivated(bool isLowestSelected)
        {
            ViewModel.SetLowestSelected(this, isLowestSelected);
        }

        private void OnTapped(object sender, TappedRoutedEventArgs e)
        {
            e.Handled = true;
            if (ViewModel.IsInterfaceBuilder)
                return;
            OnSelected();
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
            var recordDoc = GetLayoutFromDataDocAndSetDefaultLayout(vm.Document);
            this.GetFirstAncestorOfType<DocumentView>().ViewModel.DocumentController.SetField(KeyStore.SelectedSchemaRow, recordDoc, true);
        }

        // TODO lsm wrote this here it's a hack we should definitely remove this
        private static DocumentController GetLayoutFromDataDocAndSetDefaultLayout(DocumentController dataDoc)
        {
            var isLayout = dataDoc.GetField(KeyStore.DocumentContextKey) != null;
            var layoutDocType = (dataDoc.GetField(KeyStore.ActiveLayoutKey) as DocumentController)
                ?.DocumentType;
            if (!isLayout && (layoutDocType == null || layoutDocType.Equals(DefaultLayout.DocumentType)))
            {
                if (dataDoc.GetField(KeyStore.ThisKey) == null)
                    dataDoc.SetField(KeyStore.ThisKey, dataDoc, true);
                var layoutDoc =
                    new KeyValueDocumentBox(new DocumentReferenceController(dataDoc.GetId(), KeyStore.ThisKey));

                layoutDoc.Document.SetField(KeyStore.WidthFieldKey, new NumberController(300), true);
                layoutDoc.Document.SetField(KeyStore.HeightFieldKey, new NumberController(100), true);
                dataDoc.SetActiveLayout(layoutDoc.Document, forceMask: true, addToLayoutList: false);
            }

            return isLayout ? dataDoc : dataDoc.GetActiveLayout(null);
        }
        private void XRecordsView_OnDragItemsStarting(object sender, DragItemsStartingEventArgs args)
        {
            List<CollectionDBSchemaRecordViewModel> recordVMs =
                xRecordsView.SelectedItems.OfType<CollectionDBSchemaRecordViewModel>().ToList();
            var docControllerList = new List<DocumentController>();
            foreach (var vm in recordVMs)
            {
                docControllerList.Add(vm.Document);
                GetLayoutFromDataDocAndSetDefaultLayout(vm.Document);
            }
            args.Data.Properties.Add("DocumentControllerList", docControllerList);
            args.Data.Properties.Add("View", true);
            args.Data.RequestedOperation = DataPackageOperation.Link;
        }
    }
}