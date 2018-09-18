using DashShared;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using Windows.ApplicationModel.DataTransfer;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;
using static Dash.CollectionDBSchemaHeader;
using Windows.UI.Xaml.Media;

// The User Control item template is documented at https://go.microsoft.com/fwlink/?LinkId=234236

namespace Dash
{
    public sealed partial class CollectionDBSchemaView : ICollectionView
    {
        private DocumentController _parentDocument;

        private Dictionary<KeyController, HashSet<TypeInfo>> _typedHeaders;

        /// <summary>
        ///     Each element in the list renders a column in the records list view
        /// </summary>
        private ObservableCollection<CollectionDBSchemaColumnViewModel> ColumnViewModels { get; }

        /// <summary>
        ///     The observable collection of all documents displayed in the schema, a pointer to this is held by every
        ///     columnviewmodel so be very careful about changing it (i.e. don't add a setter)
        /// </summary>
        private ObservableCollection<DocumentController> CollectionDocuments { get; }

        ////bcz: this field isn't used, but if it's not here Field items won't be updated when they're changed.  Why???????
        //public ObservableCollection<CollectionDBSchemaRecordViewModel> Records { get; set; } =
        //    new ObservableCollection<CollectionDBSchemaRecordViewModel>();

        public ObservableCollection<HeaderViewModel> SchemaHeaders { get; }

        public CollectionViewModel ViewModel { get; set; }

        public DocumentController ParentDocument
        {
            get => _parentDocument;
            set
            {
                _parentDocument = value;
                if (value != null)
                    if (ParentDocument.GetField(CollectionDBView.FilterFieldKey) == null)
                        ParentDocument.SetField(CollectionDBView.FilterFieldKey, new KeyController(), true);
            }
        }

        public UserControl UserControl => this;

        public CollectionDBSchemaView()
        {
            InitializeComponent();
            Unloaded += CollectionDBSchemaView_Unloaded;
            Loaded += CollectionDBSchemaView_Loaded;
            MinWidth = MinHeight = 50;
            Drop += CollectionDBSchemaView_Drop;
            ColumnViewModels = new ObservableCollection<CollectionDBSchemaColumnViewModel>();
            CollectionDocuments = new ObservableCollection<DocumentController>();
            SchemaHeaders = new ObservableCollection<HeaderViewModel>();
            xHeaderView.ItemsSource = SchemaHeaders;
        }

        private void SchemaHeaders_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            switch (e.Action)
            {
                case System.Collections.Specialized.NotifyCollectionChangedAction.Add:
                    for (var i = 0; i < e.NewItems.Count; i++)
                    {
                        var headerViewModel = e.NewItems[i] as HeaderViewModel;
                        Debug.Assert(headerViewModel != null);
                        ColumnViewModels.Insert(e.NewStartingIndex + i,
                            new CollectionDBSchemaColumnViewModel(headerViewModel.FieldKey, CollectionDocuments,
                                headerViewModel));
                    }
                    break;
                case System.Collections.Specialized.NotifyCollectionChangedAction.Move:
                    
                    break;
                case System.Collections.Specialized.NotifyCollectionChangedAction.Remove:
                    for (var i = 0; i < e.OldItems.Count; i++)
                    {
                        ColumnViewModels.RemoveAt(e.OldStartingIndex);
                    }

                    break;
                case System.Collections.Specialized.NotifyCollectionChangedAction.Replace:
                    break;
                case System.Collections.Specialized.NotifyCollectionChangedAction.Reset:
                    break;
            }
        }

        private void AddViewChanged()
        {
            foreach (var scrollViewer in ColumnViewModels.Select(colvm => colvm.ViewModelScrollViewer))
            {
                if (scrollViewer != null)
                {
                    scrollViewer.ViewChanged += ScrollViewer_ViewChanged;
                }
            }
        }
        
        private void ScrollViewer_ViewChanged(object sender, ScrollViewerViewChangedEventArgs scrollViewerViewChangedEventArgs)
        {
            foreach (var scrollViewer in ColumnViewModels.Select(cvm => cvm.ViewModelScrollViewer))//TODO calling change view in the view changed method causes this to get called way more than it should
            {
                if (scrollViewer == null) continue;

                if (!scrollViewer.Equals(sender as ScrollViewer))
                    scrollViewer.ChangeView(null, (sender as ScrollViewer).VerticalOffset, null, true);
            }

        }

        private void CollectionDBSchemaView_Drop(object sender, DragEventArgs e)
        {
            e.Handled = true;
        }

        private void CollectionDBSchemaView_Unloaded(object sender, RoutedEventArgs e)
        {
            DataContextChanged -= CollectionDBView_DataContextChanged;
            if (ViewModel != null)
                ViewModel.CollectionController.FieldModelUpdated -= CollectionController_FieldModelUpdated;
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
                if (ViewModel != null && ViewModel.CollectionController.Equals(cvm.CollectionController)) return;

                // remove events from previous datacontext
                if (ViewModel != null)
                    ViewModel.CollectionController.FieldModelUpdated -= CollectionController_FieldModelUpdated;

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

        private void CollectionController_FieldModelUpdated(FieldControllerBase sender, FieldUpdatedEventArgs args,
            Context context)
        {
            // if a field has been replaced or updated then set it's source to be the new element
            // otherwise replace the entire data source to reflect the new set of fields (due to add or remove)
            var dargs = (ListController<DocumentController>.ListFieldUpdatedEventArgs) args;

            UpdateHeaders(dargs.NewItems); // TODO find a better way to update this


            if (dargs.ListAction == ListController<DocumentController>.ListFieldUpdatedEventArgs.ListChangedAction.Add)
                AddRows(dargs);
            else if (dargs.ListAction ==
                     ListController<DocumentController>.ListFieldUpdatedEventArgs.ListChangedAction.Remove)
                RemoveRows(dargs);
            else if (dargs.ListAction ==
                     ListController<DocumentController>.ListFieldUpdatedEventArgs.ListChangedAction.Clear)
                ColumnViewModels.Clear();
            else if (dargs.ListAction ==
                     ListController<DocumentController>.ListFieldUpdatedEventArgs.ListChangedAction.Replace)
                throw new NotImplementedException();
        }

        private void UpdateHeaders(List<DocumentController> changedDocuments)
        {
            // TODO logic for adding and removing old and new columns using ColumnViewModels
            var context = new Context(ParentDocument);
            _typedHeaders = Util.GetDisplayableTypedHeaders(ParentDocument.GetDataDocument()
                .GetDereferencedField<ListController<DocumentController>>(KeyStore.DataKey, context));
            foreach (var doc in changedDocuments.Select(doc =>
                doc.GetDereferencedField<DocumentController>(KeyStore.DocumentContextKey, null) ?? doc))
            foreach (var keyFieldPair in doc.EnumDisplayableFields())
                if (!_typedHeaders.ContainsKey(keyFieldPair.Key))
                {
                    _typedHeaders.Add(keyFieldPair.Key, new HashSet<TypeInfo> {keyFieldPair.Value.TypeInfo});
                    SchemaHeaders.Add(new HeaderViewModel
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

        private void AddRows(ListController<DocumentController>.ListFieldUpdatedEventArgs dargs)
        {
            foreach (var doc in dargs.NewItems.Select(doc => doc.GetDataDocument()))
                CollectionDocuments.Add(doc);
        }

        private void RemoveRows(ListController<DocumentController>.ListFieldUpdatedEventArgs dargs)
        {
            // TODO
            throw new NotImplementedException();
        }

        public void Sort(HeaderViewModel viewModel)
        {
            // TODO: ACV might not work?
            /*
            var acv = new AdvancedCollectionView(CollectionDocuments, true);

            acv.Filter = x => x != null;
            acv.SortDescriptions.Add(new SortDescription("Title", SortDirection.Ascending));

            xRecordsView.ItemsSource = acv;
            */
        }

        /// <summary>
        ///     Updates all the fields in the schema view
        /// </summary>
        public void ResetHeaders()
        {
            // TODO why is this called about 4 times on start up
            var context = new Context(ParentDocument);
            _typedHeaders = Util.GetDisplayableTypedHeaders(ParentDocument.GetDataDocument()
                .GetDereferencedField<ListController<DocumentController>>(KeyStore.DataKey, context));
            SchemaHeaders.CollectionChanged -= SchemaHeaders_CollectionChanged;

            SchemaHeaders.Clear();
            foreach (var typedHeader in _typedHeaders)
                SchemaHeaders.Add(new HeaderViewModel
                {
                    SchemaView = this,
                    SchemaDocument = ParentDocument,
                    Width = 150,
                    FieldKey = typedHeader.Key
                });
            SchemaHeaders.CollectionChanged += SchemaHeaders_CollectionChanged;
        }

        private void ResetRecords()
        {
            var dbDocs = ParentDocument.GetDataDocument()
                .GetDereferencedField<ListController<DocumentController>>(KeyStore.DataKey, new Context(ParentDocument))
                ?.TypedData;
            dbDocs = dbDocs.Select(db => db.GetDataDocument()).ToList();
            foreach (var documentController in dbDocs) CollectionDocuments.Add(documentController);

            foreach (var typedHeader in _typedHeaders)
            {
                var cvm = new CollectionDBSchemaColumnViewModel(typedHeader.Key, CollectionDocuments,
                    SchemaHeaders.First(hvm => hvm.FieldKey.Equals(typedHeader.Key)));
                ColumnViewModels.Add(cvm);
                cvm.PropertyChanged += Cvm_PropertyChanged;
            }
        }

        private void Cvm_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName.Equals("ViewModelScrollViewer"))
            {
                AddViewChanged();
            }
        }

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
            var recordDoc = vm.Document.GetLayoutFromDataDocAndSetDefaultLayout();
            this.GetFirstAncestorOfType<DocumentView>().ViewModel.DataDocument.SetField(KeyStore.SelectedSchemaRow, recordDoc, true);
        }
        
        private void XRecordsView_OnDragItemsStarting(object sender, DragItemsStartingEventArgs args)
        {
            foreach (var vm in args.Items.Select(item => item as CollectionDBSchemaRecordViewModel))
            {
                vm.Document.GetLayoutFromDataDocAndSetDefaultLayout();
                // bcz: this ends up dragging only the last document -- next to extend DragDocumentModel to support collections of documents
                args.Data.AddDragModel(new DragDocumentModel(vm.Document));
                args.Data.RequestedOperation = DataPackageOperation.Move | DataPackageOperation.Copy | DataPackageOperation.Link;
            }
        }

        private void xHeaderView_DragItemsStarting(object sender, DragItemsStartingEventArgs e)
        {
            //foreach (var m in e.Items)
            //{
            //    var viewModel = m as HeaderViewModel;
            //    var collectionViewModel = viewModel.SchemaView.DataContext as CollectionViewModel;
            //    var collectionReference =
            //        new DocumentReferenceController(viewModel.SchemaDocument.GetDataDocument().GetId(),
            //            collectionViewModel.CollectionKey);
            //    var collectionData = collectionReference.DereferenceToRoot<ListController<DocumentController>>(null)
            //        .TypedData;
            //    e.Data.Properties.Add(nameof(DragCollectionFieldModel),
            //        new DragCollectionFieldModel(
            //            collectionData,
            //            collectionReference,
            //            viewModel.FieldKey,
            //            CollectionView.CollectionViewType.DB
            //        ));
            //}
        }

        private void xRecordsView_PointerPressed(object sender, PointerRoutedEventArgs e)
        {
            this.GetFirstAncestorOfType<DocumentView>().ManipulationMode =
                e.GetCurrentPoint(this).Properties.IsRightButtonPressed
                    ? ManipulationModes.All
                    : ManipulationModes.None;
        }

        private void AddRow_OnTapped(object sender, TappedRoutedEventArgs e)
        {
            // Add a new document to the schema view
            ViewModel.AddDocument(Util.BlankNote());
            e.Handled = true;
        }

        private void AddColumn_OnTapped(object sender, TappedRoutedEventArgs e)
        {
            var newHvm = new HeaderViewModel
            {
                SchemaView = this,
                SchemaDocument = ParentDocument,
                Width = 150,
                FieldKey = new KeyController("New Field", Guid.NewGuid().ToString())
            };
            SchemaHeaders.Add(newHvm);

            //var cvm = new CollectionDBSchemaColumnViewModel(newHvm.FieldKey, CollectionDocuments, newHvm);
            //ColumnViewModels.Add(cvm);
            //cvm.PropertyChanged += Cvm_PropertyChanged;

            e.Handled = true;
        }

        public void SetDropIndicationFill(Brush fill)
        {
        }
    }
}
