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

        public bool IsEditorMode { get; set; } = true;
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

        /// <summary>
        /// References the ItemsControl used to 
        /// </summary>
        public UIElement DocumentDisplayView
        {
            get { return _documentDisplayView; }
            set { SetProperty(ref _documentDisplayView, value); }
        }
        private UIElement _documentDisplayView;

        /// <summary>
        /// The size of each cell in the GridView.
        /// </summary>
        public double CellSize
        {
            get { return _cellSize; }
            set { SetProperty(ref _cellSize, value); }
        }
        private double _cellSize;

        /// <summary>
        /// Clips the grid containing the documents to the correct size
        /// </summary>
        public Rect ClipRect
        {
            get { return _clipRect; }
            set { SetProperty(ref _clipRect, value); }
        }
        private Rect _clipRect;

        /// <summary>
        /// Determines the selection mode of the control currently displaying the documents
        /// </summary>
        public ListViewSelectionMode ItemSelectionMode
        {
            get { return _itemSelectionMode; }
            set { SetProperty(ref _itemSelectionMode, value); }
        }
        private ListViewSelectionMode _itemSelectionMode;

        public Visibility MenuVisibility
        {
            get { return _menuVisibility; }
            set { SetProperty(ref _menuVisibility, value); }
        }
        private Visibility _menuVisibility;

        public GridLength MenuColumnWidth
        {
            get { return _menuColumnWidth; }
            set { SetProperty(ref _menuColumnWidth, value); }
        }
        private GridLength _menuColumnWidth;


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


        public CollectionViewModel(DocumentCollectionFieldModelController collection)
        {
            _collectionFieldModelController = collection;

            SetInitialValues();
            UpdateViewModels(MakeViewModels(_collectionFieldModelController.DocumentCollectionFieldModel));
            collection.FieldModelUpdatedEvent += Controller_FieldModelUpdatedEvent;
        }

        private void Controller_FieldModelUpdatedEvent(FieldModelController sender)
        {
            //AddDocuments(_collectionFieldModelController.Documents.Data);
            UpdateViewModels(MakeViewModels((sender as DocumentCollectionFieldModelController).DocumentCollectionFieldModel));
        }

        /// <summary>
        /// Sets initial values of instance variables required for the CollectionView to display nicely.
        /// </summary>
        private void SetInitialValues()
        {
            CellSize = 250;
            DocumentDisplayView = new CollectionFreeformView {DataContext = this};
            _selectedItems = new ObservableCollection<DocumentViewModel>();
            DataBindingSource = new ObservableCollection<DocumentViewModel>();
            MenuColumnWidth = new GridLength(80);
        }

        #region Event Handlers

        private void DocumentViewContainerGrid_OnSizeChanged(object sender, SizeChangedEventArgs e)
        {
            Thickness border = new Thickness(1);
            ClipRect = new Rect(border.Left, border.Top, e.NewSize.Width - border.Left * 2, e.NewSize.Height - border.Top * 2);
        }

        

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
        /// Changes the view to the Freeform by making that Freeform visible in the CollectionView.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void FreeformButton_Tapped(object sender, TappedRoutedEventArgs e)
        {
            DocumentDisplayView = new CollectionFreeformView { DataContext = this };
        }

        /// <summary>
        /// Changes the view to the LIstView by making that Grid visible in the CollectionView.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void ListViewButton_Tapped(object sender, TappedRoutedEventArgs e)
        {
            DocumentDisplayView = new CollectionListView { DataContext = this };
            //SetDimensions();
        }

        public void GridViewButton_Tapped(object sender, TappedRoutedEventArgs e)
        {
            DocumentDisplayView = new CollectionGridView {DataContext = this};
        }

        /// <summary>
        /// Changes the selection mode to reflect the tapped Select Button.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void SelectButton_Tapped(object sender, TappedRoutedEventArgs e)
        {
            if (ItemSelectionMode == ListViewSelectionMode.None)
            {
                ItemSelectionMode = ListViewSelectionMode.Multiple;
                
            }
            else
            {
                ItemSelectionMode= ListViewSelectionMode.None;
                
            }
            e.Handled = true;
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

        public void Grid_DoubleTapped(object sender, DoubleTappedRoutedEventArgs e)
        {
            if (MenuVisibility == Visibility.Visible)
            {
                MenuVisibility = Visibility.Collapsed;
                MenuColumnWidth = new GridLength(0);
            }
            else
            {
                MenuVisibility = Visibility.Visible;
                MenuColumnWidth = new GridLength(50);
            }
            e.Handled = true;
        }

    }
}
