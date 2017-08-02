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
    public class CollectionViewModel : ViewModelBase
    {



        #region Properties
        public DocumentCollectionFieldModelController CollectionFieldModelController { get; }

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


        public CollectionViewModel(FieldModelController collection, Context context = null)
        {
            _selectedItems = new ObservableCollection<DocumentViewModel>();
            DataBindingSource = new ObservableCollection<DocumentViewModel>();
            CollectionFieldModelController =
                collection.DereferenceToRoot<DocumentCollectionFieldModelController>(context);
            UpdateViewModels(CollectionFieldModelController, context);
            var copiedContext = new Context(context);
          
            collection.FieldModelUpdated += delegate (FieldModelController sender, Context context1)
            {
                UpdateViewModels(sender.DereferenceToRoot<DocumentCollectionFieldModelController>(context1),
                    copiedContext);
            };
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

        public void UpdateViewModels(DocumentCollectionFieldModelController documents, Context context)
        {
            //// bcz: shouldn't need this conditional once the collection updates properly
            //if (documents == null)
            //    documents = DocController.GetDereferencedField(Key, context) as DocumentCollectionFieldModelController;

            var offset = 0;
            var carriedControllers = ItemsCarrier.GetInstance().Payload.Select(item => item.DocumentController).ToList();
            foreach (var docController in documents.GetDocuments())
            {
                if (!context.DocContextList.Contains(docController) && !docController.DocumentType.Type.Contains("Box"))
                {
                    if (ViewModelContains(DataBindingSource, docController))
                        continue;
                    var viewModel = new DocumentViewModel(docController, false, context);

                    if (carriedControllers.Contains(docController))
                    {
                        var x = ItemsCarrier.GetInstance().Translate.X - 10 + offset;
                        var y = ItemsCarrier.GetInstance().Translate.Y - 10 + offset;
                        viewModel.GroupTransform = new TransformGroupData(new Point(x, y),
                            viewModel.GroupTransform.ScaleCenter, viewModel.GroupTransform.ScaleAmount);
                        offset += 15;
                    }
                    //viewModel.ManipulationMode = ManipulationModes.All;
                    viewModel.DoubleTapEnabled = false;
                    DataBindingSource.Add(viewModel);
                }
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
