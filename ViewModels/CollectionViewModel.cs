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

        public CollectionView ParentCollection { get; set; }
        public DocumentView ParentDocument { get; set; }
    
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


        public CollectionViewModel(DocumentCollectionFieldModelController collection)
        {
            _collectionFieldModelController = collection;
            _selectedItems = new ObservableCollection<DocumentViewModel>();
            DataBindingSource = new ObservableCollection<DocumentViewModel>();
            UpdateViewModels(MakeViewModels(_collectionFieldModelController.DocumentCollectionFieldModel));
            collection.FieldModelUpdatedEvent += Controller_FieldModelUpdatedEvent;
            CellSize = 250;
        }

        private void Controller_FieldModelUpdatedEvent(FieldModelController sender)
        {
            UpdateViewModels(MakeViewModels((sender as DocumentCollectionFieldModelController).DocumentCollectionFieldModel));
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
                DataBindingSource.Remove(vm);
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


        private bool ViewModelContains(ObservableCollection<DocumentViewModel> col, DocumentViewModel vm)
        {
            foreach (var viewModel in col)
                if (viewModel.DocumentController.GetId() == vm.DocumentController.GetId())
                    return true;
            return false;
        }

        public void UpdateViewModels(ObservableCollection<DocumentViewModel> viewModels)
        {
            foreach (var viewModel in viewModels)
            {
                if (ViewModelContains(DataBindingSource, viewModel)) continue;
                viewModel.ManipulationMode = ManipulationModes.System;
                viewModel.DoubleTapEnabled = false;
                DataBindingSource.Add(viewModel);
            }
            for (int i = DataBindingSource.Count - 1; i >= 0; --i)
            {
                if (ViewModelContains(viewModels, DataBindingSource[i])) continue;
                DataBindingSource.RemoveAt(i);
            }
        }

        /// <summary>
        /// Constructs standard DocumentViewModels from the passed in DocumentModels
        /// </summary>
        /// <param name="documents"></param>
        /// <returns></returns>
        public ObservableCollection<DocumentViewModel> MakeViewModels(DocumentCollectionFieldModel documents)
         {
            ObservableCollection<DocumentViewModel> viewModels = new ObservableCollection<DocumentViewModel>();
            var offset = 0;
            for (int i = 0; i<documents.Data.ToList().Count; i++)
            {
                var controller = ContentController.GetController(documents.Data.ToList()[i]) as DocumentController;
                var viewModel = new DocumentViewModel(controller);
                if (ItemsCarrier.GetInstance().Payload.Select(item => item.DocumentController).Contains(controller))
                {
                    var x = ItemsCarrier.GetInstance().Translate.X - 10 + offset;
                    var y = ItemsCarrier.GetInstance().Translate.Y - 10 + offset;
                    viewModel.Position = new Point(x, y);
                    offset += 15;
                }
                viewModels.Add(viewModel);
            }
            return viewModels;
        }


        #endregion

        
    }
}
