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

        // This list stores the fields added in the schema view (not originally in the documents)
        private List<KeyController> _schemaFieldsNotInDocs;

        private ObservableCollection<CollectionDBSchemaRecordViewModel> ListItemSource { get; }

        //bcz: this field isn't used, but if it's not here Field items won't be updated when they're changed.  Why???????
        public ObservableCollection<CollectionDBSchemaRecordViewModel> Records { get; set; } =
            new ObservableCollection<CollectionDBSchemaRecordViewModel>();

        public ObservableCollection<HeaderViewModel> SchemaHeaders { get; set; } =
            new ObservableCollection<HeaderViewModel>();

        public DocumentController ParentDocument
        {
            get => _parentDocument;
            set
            {
                _parentDocument = value;
                if (value != null)
                {
                    if (ParentDocument.GetField(CollectionDBView.FilterFieldKey) == null)
                        ParentDocument.SetField(CollectionDBView.FilterFieldKey, new KeyController(), true);
                }
            }
        }

        public CollectionViewModel ViewModel { get; set; }

        private Dictionary<KeyController, HashSet<TypeInfo>> _typedHeaders;

        public CollectionDBSchemaView()
        {
            InitializeComponent();
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

        private void CollectionDBSchemaView_Unloaded(object sender, RoutedEventArgs e)
        {
            DataContextChanged -= CollectionDBView_DataContextChanged;
            if (ViewModel != null)
            {
                ViewModel.CollectionController.FieldModelUpdated -= CollectionController_FieldModelUpdated;
            }
            ParentDocument = null;
        }


        private void CollectionDBSchemaView_Loaded(object sender, RoutedEventArgs e)
        {
            DataContextChanged += CollectionDBView_DataContextChanged;
            ParentDocument = this.GetFirstAncestorOfType<DocumentView>().ViewModel.DocumentController;
            CollectionDBView_DataContextChanged(null, null);
        }

        private void CollectionDBView_DataContextChanged(FrameworkElement sender, DataContextChangedEventArgs args)
        {
            if (DataContext is CollectionViewModel cvm)
            {
                // if datacontext hasn't actually changed just return
                if (ViewModel == cvm)
                {
                    return;
                }

                // remove events from previous datacontext
                if (ViewModel != null)
                {
                    ViewModel.CollectionController.FieldModelUpdated -= CollectionController_FieldModelUpdated;
                }

                // add events to new datacontext and set it
                cvm.CollectionController.FieldModelUpdated += CollectionController_FieldModelUpdated;
                ViewModel = cvm;

                // set the parentDocument which is the document holding this collection
                ParentDocument = this.GetFirstAncestorOfType<DocumentView>()?.ViewModel?.DocumentController;
                if (ParentDocument != null)
                {
                    ResetHeaders();
                    ResetRecords();
                }
            }
        }

        private void CollectionController_FieldModelUpdated(FieldControllerBase sender, FieldUpdatedEventArgs args, Context context)
        {
            // if a field has been replaced or updated then set it's source to be the new element
            // otherwise replcae the entire data source to reflect the new set of fields (due to add or remove)
            var dargs = (ListController<DocumentController>.ListFieldUpdatedEventArgs)args;
            if (dargs.ListAction == ListController<DocumentController>.ListFieldUpdatedEventArgs.ListChangedAction.Add)
            {
                AddRows(dargs);
            }
            else if (dargs.ListAction == ListController<DocumentController>.ListFieldUpdatedEventArgs.ListChangedAction.Remove)
            {
                RemoveRows(dargs);
            } else if (dargs.ListAction ==
                       ListController<DocumentController>.ListFieldUpdatedEventArgs.ListChangedAction.Clear)
            {
                ListItemSource.Clear();
            }
            else if (dargs.ListAction ==
                       ListController<DocumentController>.ListFieldUpdatedEventArgs.ListChangedAction.Replace)
            {
                //TODO
                throw new NotImplementedException();
            }

            UpdateHeaders(dargs.ChangedDocuments); // TODO find a better way to update this
        }

        private void UpdateHeaders(List<DocumentController> changedDocuments)
        {
            var context = new Context(ParentDocument);
            _typedHeaders = Util.GetDisplayableTypedHeaders(ParentDocument.GetDataDocument()
                .GetDereferencedField<ListController<DocumentController>>(KeyStore.DataKey, context));
            foreach (var doc in changedDocuments.Select(doc => doc.GetDereferencedField<DocumentController>(KeyStore.DocumentContextKey, null) ?? doc))
            {
                foreach (var keyFieldPair in doc.EnumDisplayableFields())
                {
                    if (!_typedHeaders.ContainsKey(keyFieldPair.Key))
                    {
                        _typedHeaders.Add(keyFieldPair.Key, new HashSet<TypeInfo> { keyFieldPair.Value.TypeInfo });
                        SchemaHeaders.Add(new HeaderViewModel()
                        {
                            SchemaView = this,
                            SchemaDocument = ParentDocument,
                            Width = 150,
                            FieldKey = keyFieldPair.Key
                        });
                    }
                    else
                    {
                        if (!_typedHeaders[keyFieldPair.Key].Contains(keyFieldPair.Value.TypeInfo))
                            _typedHeaders[keyFieldPair.Key].Add(keyFieldPair.Value.TypeInfo);
                    }
                }
            }
        }

        private void AddRows(ListController<DocumentController>.ListFieldUpdatedEventArgs dargs)
        {
            foreach (var doc in dargs.ChangedDocuments.Select(doc => doc.GetDereferencedField<DocumentController>(KeyStore.DocumentContextKey, null) ?? doc))
            {
                var recordFields = CreateNewRecord(doc);
                ListItemSource.Add(new CollectionDBSchemaRecordViewModel(
                    ParentDocument,
                    doc,
                    recordFields
                ));
            }
        }

        private void RemoveRows(ListController<DocumentController>.ListFieldUpdatedEventArgs dargs)
        {
            //TODO
            throw new NotImplementedException();
        }

        KeyController _lastFieldSortKey = null;

        public void Sort(HeaderViewModel viewModel)
        {
            // TODO reimplement this with advanced collection view
            //var dbDocs = ParentDocument.GetDataDocument()
            //       .GetDereferencedField<ListController<DocumentController>>(ViewModel.CollectionKey, null)?.TypedData;

            //var records = new SortedList<string, DocumentController>();
            //foreach (var d in dbDocs)
            //{
            //    var str = d.GetDataDocument().GetDereferencedField(viewModel.FieldKey, null)?.GetValue(new Context(d))?.ToString() ?? "{}";
            //    if (records.ContainsKey(str))
            //        records.Add(str + Guid.NewGuid(), d);
            //    else records.Add(str, d);
            //}
            //if (_lastFieldSortKey != null && _lastFieldSortKey.Equals(viewModel.FieldKey))
            //    ResetRecords(records.Select((r) => r.Value).Reverse());
            //else ResetRecords(records.Select((r) => r.Value));
            //_lastFieldSortKey = viewModel.FieldKey;
        }

        /// <summary>
        ///     Updates all the fields in the schema view
        /// </summary>
        public void ResetHeaders()
        {
            var context = new Context(ParentDocument);
            _typedHeaders = Util.GetDisplayableTypedHeaders(ParentDocument.GetDataDocument()
                .GetDereferencedField<ListController<DocumentController>>(KeyStore.DataKey, context));
            SchemaHeaders.Clear();
            foreach (var typedHeader in _typedHeaders)
            {
                SchemaHeaders.Add(new HeaderViewModel()
                {
                    SchemaView = this,
                    SchemaDocument = ParentDocument,
                    Width = 150,
                    FieldKey = typedHeader.Key
                });
            }
        }

        private void ResetRecords()
        {
            var dbDocs = ParentDocument.GetDataDocument().GetDereferencedField<ListController<DocumentController>>(KeyStore.DataKey, new Context(ParentDocument))?.TypedData;         
            ListItemSource.Clear();
            foreach (var d in dbDocs.Select(db => db.GetDereferencedField<DocumentController>(KeyStore.DocumentContextKey, null) ?? db))
            {
                var recordFields = CreateNewRecord(d);
                ListItemSource.Add(new CollectionDBSchemaRecordViewModel(
                    ParentDocument,
                    d,
                    recordFields
                ));
            }
        }

        private ObservableCollection<EditableScriptViewModel> CreateNewRecord(DocumentController doc)
        {
            var newRecord = new ObservableCollection<EditableScriptViewModel>();
            foreach (var keyTypePair in _typedHeaders)
            {
                newRecord.Add(
                    new EditableScriptViewModel(
                        new DocumentFieldReference(doc.Id, keyTypePair.Key)));
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
            xRecordsView.Height = xOuterGrid.ActualHeight - xHeaderArea.ActualHeight;
        }

        private void xOuterGrid_Loaded(object sender, RoutedEventArgs e)
        {
            // TODO see if this is still necessary
            xRecordsView.Height = xOuterGrid.ActualHeight - xHeaderArea.ActualHeight;
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
            ResetHeaders();
        }
    }
}