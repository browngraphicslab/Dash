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
        private ObservableCollection<CollectionDBSchemaRecordViewModel> ListItemSource { get; }

        public CollectionDBSchemaView()
        {
            this.InitializeComponent();
            Unloaded += CollectionDBSchemaView_Unloaded;
            Loaded += CollectionDBSchemaView_Loaded;
            MinWidth = MinHeight = 50;
            xHeaderView.ItemsSource = SchemaHeaders;
            Drop += CollectionDBSchemaView_Drop;
            ListItemSource = new ObservableCollection<CollectionDBSchemaRecordViewModel>();

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

        private void CollectionDBSchemaView_Unloaded(object sender, RoutedEventArgs e)
        {
            DataContextChanged -= CollectionDBView_DataContextChanged;
            ParentDocument = null;
        }


        private void CollectionDBSchemaView_Loaded(object sender, RoutedEventArgs e)
        {
            DataContextChanged += CollectionDBView_DataContextChanged;
            ParentDocument = this.GetFirstAncestorOfType<DocumentView>().ViewModel.DocumentController;
            CollectionDBView_DataContextChanged(null, null);

            var newDataDoc = ParentDocument.GetDataDocument().GetDereferencedField<ListController<DocumentController>>(ViewModel.CollectionKey, new Context(ParentDocument))?.TypedData ??
                       ParentDocument.GetDataDocument().GetDereferencedField<ListController<DocumentController>>(KeyStore.DataKey, new Context(ParentDocument))?.TypedData;
            UpdateRecords(newDataDoc);
        }

        private void CollectionDBView_DataContextChanged(FrameworkElement sender, DataContextChangedEventArgs args)
        {
            ViewModel.OutputKey = KeyStore.CollectionOutputKey;
            ParentDocument = this.GetFirstAncestorOfType<DocumentView>()?.ViewModel?.DocumentController;
            if (ParentDocument != null)
                UpdateFields(new Context(ParentDocument)); 
        }


        private void ParentDocument_DocumentFieldUpdated(FieldControllerBase sender, FieldUpdatedEventArgs args, Context context)
        {
            if (((DocumentController.DocumentFieldUpdatedEventArgs) args).Reference.FieldKey.Equals(ViewModel?.CollectionKey))
                UpdateRecords(ParentDocument.GetDataDocument().GetDereferencedField<ListController<DocumentController>>(ViewModel.CollectionKey, context)?.TypedData ??
                             ParentDocument.GetDataDocument().GetDereferencedField<ListController<DocumentController>>(KeyStore.DataKey, context)?.TypedData);
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
            // ParentDoc is collection layout, the datadoc has a List of Layouot Documents which are displayed by the parentdoc
            // this makes dbdocs a list of layout docs in the collection
            var dbDocs = ParentDocument.GetDataDocument().GetDereferencedField<ListController<DocumentController>>(ViewModel.CollectionKey, context)?.TypedData ??
                         ParentDocument.GetDataDocument().GetDereferencedField<ListController<DocumentController>>(KeyStore.DataKey, context)?.TypedData;
            var headerList = ParentDocument
                .GetDereferencedField<ListController<TextController>>(HeaderListKey, context)?.Data ?? new List<FieldControllerBase>();
            if (dbDocs != null)
            {
                SchemaHeaders.CollectionChanged -= SchemaHeaders_CollectionChanged;
                SchemaHeaders.Clear();
                foreach (var h in headerList)
                {
                    SchemaHeaders.Add(new CollectionDBSchemaHeader.HeaderViewModel()
                    {
                        SchemaView = this,
                        SchemaDocument = ParentDocument,
                        Width = 150,
                        FieldKey = ContentController<FieldModel>.GetController<KeyController>((h as TextController).Data)
                    });
                }
                // for each document we add any header we find with a name not matching a current name. This is the UNION of all fields *assuming no collisions
                foreach (var d in dbDocs.Select(db => db.GetDereferencedField<DocumentController>(KeyStore.DocumentContextKey, null) ?? db))
                {
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
            // update records only happen when 
            if (dbDocs.Count() == ListItemSource.Count) return;
            ListItemSource.Clear();
            int recordCount = 0;

            foreach (var d in dbDocs.Select(db => db.GetDereferencedField<DocumentController>(KeyStore.DocumentContextKey, null) ?? db))
            {
                var recordFields = CreateNewRecord(d, recordCount);
                ListItemSource.Add(new CollectionDBSchemaRecordViewModel(
                    ParentDocument,
                    d,
                    recordFields
                ));
                recordCount++;
            }
        }

        private ObservableCollection<EditableScriptViewModel> CreateNewRecord(DocumentController doc, int row)
        {
            var newRecord = new ObservableCollection<EditableScriptViewModel>();
            foreach (var keyFieldPair in doc.EnumFields())
            {
                if (!keyFieldPair.Key.Name.StartsWith("_"))
                    newRecord.Add(
                        new EditableScriptViewModel(
                            new DocumentFieldReference(doc.Id, keyFieldPair.Key))
                        {
                            Row = row
                        });
            }

            return newRecord;
        }
        
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
            var recordDoc = GetLayoutFromDataDocAndSetDefaultLayout(vm.Document);
            this.GetFirstAncestorOfType<DocumentView>().ViewModel.DataDocument.SetField(KeyStore.SelectedSchemaRow, recordDoc, true);
        }

        // TODO lsm wrote this here it's a hack we should definitely remove this
        private static DocumentController GetLayoutFromDataDocAndSetDefaultLayout(DocumentController dataDoc)
        {
            var isLayout = dataDoc.GetField(KeyStore.DocumentContextKey) != null;
            var layoutDocType = (dataDoc.GetField(KeyStore.ActiveLayoutKey) as DocumentController)
                ?.DocumentType;
            if (!isLayout && (layoutDocType == null || layoutDocType.Equals(DefaultLayout.DocumentType)))
            {
                var layoutDoc = new KeyValueDocumentBox(dataDoc);

                layoutDoc.Document.SetField(KeyStore.WidthFieldKey, new NumberController(300), true);
                layoutDoc.Document.SetField(KeyStore.HeightFieldKey, new NumberController(100), true);
                dataDoc.SetActiveLayout(layoutDoc.Document, forceMask: true, addToLayoutList: false);
            }

            return isLayout ? dataDoc : dataDoc.GetActiveLayout(null);
        }
        private void XRecordsView_OnDragItemsStarting(object sender, DragItemsStartingEventArgs args)
        {
            foreach (var vm in args.Items.Select((item) => item as CollectionDBSchemaRecordViewModel))
            {
                GetLayoutFromDataDocAndSetDefaultLayout(vm.Document);
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
                var collectionReference = new DocumentReferenceController(viewModel.SchemaDocument.GetDataDocument().GetId(), collectionViewModel.CollectionKey);
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
            ViewModel.AddDocument(Util.BlankNote(), null);
            CollectionDBView_DataContextChanged(null, null);
            e.Handled = true;
        }

        private void AddColumn_OnTapped(object sender, TappedRoutedEventArgs e)
        {
            // Add a new field to the schema view
            _schemaFieldsNotInDocs.Add(new KeyController());
            UpdateFields(new Context(ParentDocument));
        }
    }
}