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
        private DocumentCollectionFieldModelController _collectionFieldModelController { get; }

        public bool IsInterfaceBuilder { get; set; }

        /// <summary>
        /// The DocumentViewModels that the CollectionView actually binds to.
        /// </summary>
        public ObservableCollection<DocumentViewModel> DocumentViewModels
        {
            get { return _documentViewModels; }
            set
            {
                SetProperty(ref _documentViewModels, value);
            }
        }
        private ObservableCollection<DocumentViewModel> _documentViewModels;

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
            DocumentViewModels = new ObservableCollection<DocumentViewModel>();
            _collectionFieldModelController =
                collection.DereferenceToRoot<DocumentCollectionFieldModelController>(context);
            AddDocuments(_collectionFieldModelController.GetDocuments(), context);
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
                            DocumentViewModels.Clear();
                            AddDocuments(args.NewValue.DereferenceToRoot<DocumentCollectionFieldModelController>(args.Context).GetDocuments(), copiedContext);
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
                //DocumentViewModels.Remove(vm);
                _collectionFieldModelController.RemoveDocument(vm.DocumentController);
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
                    AddDocumentsCollectionIsCaller(args.ChangedDocuments, c);
                    break;
                case DocumentCollectionFieldModelController.CollectionFieldUpdatedEventArgs.CollectionChangedAction.Clear:
                    DocumentViewModels.Clear();
                    break;
                case DocumentCollectionFieldModelController.CollectionFieldUpdatedEventArgs.CollectionChangedAction.Remove:
                    RemoveDocumentsCollectionIsCaller(args.ChangedDocuments);
                    break;
                case DocumentCollectionFieldModelController.CollectionFieldUpdatedEventArgs.CollectionChangedAction.Replace:
                    DocumentViewModels.Clear();
                    AddDocumentsCollectionIsCaller(args.ChangedDocuments, c);
                    break;
            }
        }

        private void RemoveDocumentsCollectionIsCaller(List<DocumentController> documents)
        {
            var ids = documents.Select(doc => doc.GetId());
            var vms = DocumentViewModels.Where(vm => ids.Contains(vm.DocumentController.GetId())).ToList();
            foreach (var vm in vms)
            {
                DocumentViewModels.Remove(vm);
            }
        }

        private void AddDocumentsCollectionIsCaller(List<DocumentController> documents, Context context)
        {
            foreach (var doc in documents)
            {
                var viewModel = new DocumentViewModel(doc, IsInterfaceBuilder, context);
                viewModel.DoubleTapEnabled = false;
                DocumentViewModels.Add(viewModel);
            }
        }

        public void AddDocuments(List<DocumentController> documents, Context context)
        {
            foreach (var doc in documents)
            {
                AddDocument(doc, context);
            }
        }

        public void AddDocument(DocumentController doc, Context context)
        {
            if (context != null && context.DocContextList.Contains(doc) || doc.DocumentType.Type.Contains("Box"))
            {
                return;
            }

            // just update the collection, the colllection will update our view automatically
            _collectionFieldModelController.AddDocument(doc);
        }

        public void RemoveDocuments(List<DocumentController> documents)
        {
            foreach (var doc in documents)
            {
                RemoveDocument(doc);
            }
        }

        public void RemoveDocument(DocumentController document)
        {
            // just update the collection, the colllection will update our view automatically
            _collectionFieldModelController.RemoveDocument(document);
        }

        #endregion


    }
}
