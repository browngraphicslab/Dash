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
using Dash.Views;

namespace Dash
{
    public class CollectionViewModel : ViewModelBase
    { 


    
        #region Properties
        public DocumentCollectionFieldModelController CollectionFieldModelController { get { return _collectionFieldModelController; } }
        
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
        #endregion

        /// <summary>
        /// The collection creates delegates for each document it displays so that it can associate display-specific
        /// information on the documents.  This allows different collection views to save different views of the same
        /// document collection.
        /// </summary>
        Dictionary<string, DocumentModel> DocumentToDelegateMap = new Dictionary<string, DocumentModel>();


        private DocumentCollectionFieldModelController _collectionFieldModelController;
        //Not backing variable; used to keep track of which items selected in view
        private ObservableCollection<DocumentViewModel> _selectedItems;

        /// <summary>
        /// The size of each cell in the GridView.
        /// </summary>
        public double CellSize { get; set; }

        public Context DocumentContext { get; }


        public CollectionViewModel(DocumentCollectionFieldModelController collection, Context context)
        {
            DocumentContext = context;
            _collectionFieldModelController = collection;
            _selectedItems = new ObservableCollection<DocumentViewModel>();
            DataBindingSource = new ObservableCollection<DocumentViewModel>();
            UpdateViewModels(_collectionFieldModelController, context);
            collection.FieldModelUpdated += Controller_FieldModelUpdatedEvent;
            CellSize = 250;
        }

        private void Controller_FieldModelUpdatedEvent(FieldModelController sender)
        {
            UpdateViewModels(sender as DocumentCollectionFieldModelController);
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

        private bool ViewModelContains(ObservableCollection<DocumentViewModel> col, DocumentController docController)
        {
            foreach (var viewModel in col)
                if (viewModel.DocumentController.GetId() == docController.GetId())
                    return true;
            return false;
        }

        private bool ViewModelContains(List<DocumentController> documentControllerList, DocumentViewModel vm)
        {
            foreach (var docController in documentControllerList)
                if (vm.DocumentController.GetId() == docController.GetId())
                    return true;
            return false;
        }

        public void UpdateViewModels(DocumentCollectionFieldModelController documents, Context context=null)
        {
            var offset = 0;
            var carriedControllers = ItemsCarrier.GetInstance().Payload.Select(item => item.DocumentController);
            foreach (var docController in documents.GetDocuments())
            {
                if (ViewModelContains(DataBindingSource, docController)) continue;
                var viewModel = new DocumentViewModel(docController, DocumentContext);  // TODO LSM: why are we passing the DocContextList Here to the documents
                if (carriedControllers.Contains(docController))
                {
                    var x = ItemsCarrier.GetInstance().Translate.X - 10 + offset;
                    var y = ItemsCarrier.GetInstance().Translate.Y - 10 + offset;
                    viewModel.GroupTransform = new TransformGroupData(new Point(x, y), viewModel.GroupTransform.ScaleCenter, viewModel.GroupTransform.ScaleAmount);
                    offset += 15;
                }
                viewModel.ManipulationMode = ManipulationModes.System;
                viewModel.DoubleTapEnabled = false;
                DataBindingSource.Add(viewModel);
            }
            for (int i = DataBindingSource.Count - 1; i >= 0; --i)
            {
                if (ViewModelContains(documents.GetDocuments(), DataBindingSource[i])) continue;
                DataBindingSource.RemoveAt(i);
            }
        }

        #endregion


    }
}
