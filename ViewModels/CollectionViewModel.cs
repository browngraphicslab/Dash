using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using Windows.UI;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using DashShared;
using Windows.Foundation;
using Visibility = Windows.UI.Xaml.Visibility;
using System.Linq;

namespace Dash
{
    public class CollectionViewModel : ViewModelBase, IFreeFormCollectionViewModel
    {

        #region Properties
        public DocumentCollectionFieldModelController CollectionFieldModelController { get; }

        public bool IsInterfaceBuilder { get; set; }

        /// <summary>
        /// The DocumentViewModels that the CollectionView actually binds to.
        /// </summary>
        public ObservableCollection<DocumentViewModel> DataBindingSource
        {
            get { return _dataBindingSource; }
            set
            {
                SetProperty(ref _dataBindingSource, value);
            }
        }
        private ObservableCollection<DocumentViewModel> _dataBindingSource;

        public bool KeepItemsOnMove { get; set; } = true;

        private bool _canDragItems;

        public bool CanDragItems
        {
            get { return _canDragItems; }
            set { SetProperty(ref _canDragItems, value); }
        }
        /// <summary>
        /// Determines the selection mode of the control currently displaying the documents
        /// </summary>
        public ListViewSelectionMode ItemSelectionMode
        {
            get { return _itemSelectionMode; }
            set { SetProperty(ref _itemSelectionMode, value); }
        }
        private ListViewSelectionMode _itemSelectionMode;

        /// <summary>
        /// The size of each cell in the GridView.
        /// </summary>
        public double CellSize { get; set; }
        #endregion

        //Not backing variable; used to keep track of which items selected in view
        private ObservableCollection<DocumentViewModel> _selectedItems;


        public CollectionViewModel(FieldModelController collection, bool IsInInterfaceBuilder, Context context = null)
        {
            _selectedItems = new ObservableCollection<DocumentViewModel>();
            DataBindingSource = new ObservableCollection<DocumentViewModel>();
            CollectionFieldModelController =
                collection.DereferenceToRoot<DocumentCollectionFieldModelController>(context);
            AddViewModels(CollectionFieldModelController.GetDocuments(), context);
            var copiedContext = new Context(context);

            if (collection is ReferenceFieldModelController)
            {
                var reference = collection as ReferenceFieldModelController;
                reference.GetDocumentController(null).AddFieldUpdatedListener(reference.FieldKey,
                    delegate (DocumentController sender, DocumentController.DocumentFieldUpdatedEventArgs args)
                    {
                        if (args.Action == DocumentController.FieldUpdatedAction.Update)
                        {
                            var cargs = args.FieldArgs as DocumentCollectionFieldModelController.CollectionFieldUpdatedEventArgs;
                            UpdateViewModels(cargs, copiedContext);
                        }
                        else
                        {
                            DataBindingSource.Clear();
                            AddViewModels(args.NewValue.DereferenceToRoot<DocumentCollectionFieldModelController>(args.Context).GetDocuments(), copiedContext);
                        }
                    });
            }
            else
            {
                collection.FieldModelUpdated += delegate (FieldModelController sender, FieldUpdatedEventArgs args, Context context1)
                {
                    UpdateViewModels(args as DocumentCollectionFieldModelController.CollectionFieldUpdatedEventArgs, 
                        copiedContext);
                };
            }
            CellSize = 250;
        }


        #region Event Handlers

        /// <summary>
        /// Deletes all of the Documents selected in the CollectionView by removing their DocumentViewModels from the data binding source. 
        /// **Note that this removes the DocumentModel as well, and any other associated DocumentViewModels.
        /// </summary>
        /// <param name="sender">The "Delete" menu option</param>
        /// <param name="e"></param>
        public void DeleteSelected_Tapped(object sender, TappedRoutedEventArgs e)
        {
            List<DocumentViewModel> itemsToDelete = new List<DocumentViewModel>();
            foreach (var vm in _selectedItems)
            {
                itemsToDelete.Add(vm);
            }
            _selectedItems.Clear();
            foreach (var vm in itemsToDelete)
            {
                //DataBindingSource.Remove(vm);
                CollectionFieldModelController.RemoveDocument(vm.DocumentController);
            }
        }


        /// <summary>
        /// Updates an ObservableCollection of DocumentViewModels to contain 
        /// only those currently selected whenever the user changes the selection.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            foreach (object item in e.AddedItems)
            {
                var dvm = item as DocumentViewModel;
                if (dvm != null)
                {
                    _selectedItems.Add(dvm);
                }
            }
            foreach (object item in e.RemovedItems)
            {
                var dvm = item as DocumentViewModel;
                if (dvm != null)
                {
                    _selectedItems.Remove(dvm);
                }
            }
        }

        #endregion

        #region DocumentModel and DocumentViewModel Data Changes

        private void UpdateViewModels(DocumentCollectionFieldModelController.CollectionFieldUpdatedEventArgs args, Context c)
        {
            switch (args.CollectionAction)
            {
                case DocumentCollectionFieldModelController.CollectionFieldUpdatedEventArgs.CollectionChangedAction.Add:
                    AddViewModels(args.ChangedDocuments, c);
                    break;
                case DocumentCollectionFieldModelController.CollectionFieldUpdatedEventArgs.CollectionChangedAction.Clear:
                    DataBindingSource.Clear();
                    break;
                case DocumentCollectionFieldModelController.CollectionFieldUpdatedEventArgs.CollectionChangedAction.Remove:
                    RemoveViewModels(args.ChangedDocuments);
                    break;
                case DocumentCollectionFieldModelController.CollectionFieldUpdatedEventArgs.CollectionChangedAction.Replace:
                    DataBindingSource.Clear();
                    AddViewModels(args.ChangedDocuments, c);
                    break;
            }
        }

        public void AddViewModels(List<DocumentController> documents, Context context)
        {
            foreach (var doc in documents)
            {
                if (context.DocContextList.Contains(doc) || doc.DocumentType.Type.Contains("Box"))
                {
                    continue;
                }
                var viewModel = new DocumentViewModel(doc, false, context);
                viewModel.DoubleTapEnabled = false;
                DataBindingSource.Add(viewModel);
            }
        }

        public void RemoveViewModels(List<DocumentController> documents)
        {
            var ids = documents.Select(doc => doc.GetId());
            var vms = DataBindingSource.Where(vm => ids.Contains(vm.DocumentController.GetId())).ToList();
            foreach (var vm in vms)
            {
                DataBindingSource.Remove(vm);
            }
        }

        #endregion


    }
}
