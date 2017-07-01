using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Security.Cryptography.X509Certificates;
using Windows.UI;
using Windows.UI.Input;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Dash.Models;
using Dash.StaticClasses;
using DashShared;
using Microsoft.Extensions.DependencyInjection;
using Windows.Foundation;
using Dash.ViewModels;
using Visibility = Windows.UI.Xaml.Visibility;
using System.Linq;

namespace Dash
{
    public class CollectionViewModel : ViewModelBase
    {
       

        private CollectionModel _collectionModel;

        public CollectionModel CollectionModel { get { return _collectionModel; } }

        public CollectionView ParentCollection { get; set; }
        public DocumentView ParentDocument { get; set; }

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

        public ObservableCollection<UIElement> SoloDisplayElements
        {
            get { return _soloDisplayElements; }
            set { SetProperty(ref _soloDisplayElements, value); }
        }

        private bool _filtered;

        public enum FilterMode
        {
            FieldContains,
            FieldEquals,
            HasField
        }

        public FilterMode CollectionFilterMode;
        public string SearchFieldBoxText;
        public string FieldBoxText;
        public string SearchBoxText;

        public bool IsEditorMode { get; set; } = true;


        #region Private & Backing variables
        

        private double _cellSize = 300;
        private double _outerGridWidth;
        private double _outerGridHeight;
        private double _containerGridHeight;
        private double _containerGridWidth;

        private Thickness _draggerMargin;
        private Thickness _proportionalDraggerMargin;
        private Thickness _closeButtonMargin;
        private Thickness _bottomBarMargin;
        private Thickness _selectButtonMargin;
        private Thickness _deleteButtonMargin;

        private SolidColorBrush _proportionalDraggerStroke;
        private SolidColorBrush _proportionalDraggerFill;
        private SolidColorBrush _draggerFill;

        private ListViewSelectionMode _itemSelectionMode;

        private Visibility _gridViewVisibility;
        private Visibility _gridViewWhichIsActuallyGridViewAndNotAnItemsControlVisibility;
        private Visibility _listViewVisibility;
        private Visibility _controlsVisibility;
        private Visibility _filterViewVisibility;

        //Not backing variable; used to keep track of which items selected in view
        private ObservableCollection<DocumentViewModel> _selectedItems;

        private ObservableCollection<DocumentViewModel> _dataBindingSource;

        private DocumentViewModel _soloDisplayDocument;
        private Visibility _soloDisplayVisibility;

        private bool _viewIsEnabled;

        private double _soloDocDisplayGridWidth;
        private double _soloDocDisplayGridHeight;
        private double _soloDisplaySize;
        private ObservableCollection<UIElement> _soloDisplayElements;

        #endregion

        #region Size Variables

        /// <summary>
        /// The size of each cell in the GridView.
        /// </summary>
        public double CellSize
        {
            get { return _cellSize; }
            set { SetProperty(ref _cellSize, value); }
        }

        public double OuterGridWidth
        {
            get { return _outerGridWidth; }
            set { SetProperty(ref _outerGridWidth, value); }
        }

        public double OuterGridHeight
        {
            get { return _outerGridHeight; }
            set { SetProperty(ref _outerGridHeight, value); }
        }

        public double ContainerGridWidth
        {
            get { return _containerGridWidth; }
            set { SetProperty(ref _containerGridWidth, value); }
        }

        public double ContainerGridHeight
        {
            get { return _containerGridHeight; }
            set { SetProperty(ref _containerGridHeight, value); }
        }

        #endregion

        #region Appearance & Location properties

        public double SoloDisplaySize
        {
            get { return _soloDisplaySize; }
            set { SetProperty(ref _soloDisplaySize, value); }
        }

        public Visibility FilterViewVisibility
        {
            get { return _filterViewVisibility; }
            set { SetProperty(ref _filterViewVisibility, value); }
        }

        public Visibility SoloDisplayVisibility
        {
            get { return _soloDisplayVisibility; }
            set { SetProperty(ref _soloDisplayVisibility, value); }
        }

        public bool ViewIsEnabled
        {
            get { return _viewIsEnabled; }
            set { SetProperty(ref _viewIsEnabled, value); }
        }

        public Visibility GridViewVisibility
        {
            get { return _gridViewVisibility; }
            set { SetProperty(ref _gridViewVisibility, value); }
        }

        public Visibility GridViewWhichIsActuallyGridViewAndNotAnItemsControlVisibility
        {
            get { return _gridViewWhichIsActuallyGridViewAndNotAnItemsControlVisibility; }
            set { SetProperty(ref _gridViewWhichIsActuallyGridViewAndNotAnItemsControlVisibility, value); }
        }

        public Visibility ListViewVisibility
        {
            get { return _listViewVisibility; }
            set { SetProperty(ref _listViewVisibility, value); }
        }

        public Visibility ControlsVisibility
        {
            get { return _controlsVisibility; }
            set { SetProperty(ref _controlsVisibility, value); }
        }

        public ListViewSelectionMode ItemSelectionMode
        {
            get { return _itemSelectionMode; }
            set { SetProperty(ref _itemSelectionMode, value); }
        }

        public Thickness SelectButtonMargin
        {
            get { return _selectButtonMargin; }
            set { SetProperty(ref _selectButtonMargin, value); }
        }

        public Thickness DeleteButtonMargin
        {
            get { return _deleteButtonMargin; }
            set { SetProperty(ref _deleteButtonMargin, value); }
        }

        public Thickness DraggerMargin
        {
            get { return _draggerMargin; }
            set { SetProperty(ref _draggerMargin, value); }
        }

        public Thickness ProportionalDraggerMargin
        {
            get { return _proportionalDraggerMargin; }
            set { SetProperty(ref _proportionalDraggerMargin, value); }
        }

        public Thickness CloseButtonMargin
        {
            get { return _closeButtonMargin; }
            set { SetProperty(ref _closeButtonMargin, value); }
        }

        public SolidColorBrush ProportionalDraggerStroke
        {
            get { return _proportionalDraggerStroke; }
            set { SetProperty(ref _proportionalDraggerStroke, value); }
        }

        public SolidColorBrush ProportionalDraggerFill
        {
            get { return _proportionalDraggerFill; }
            set { SetProperty(ref _proportionalDraggerFill, value); }
        }

        public SolidColorBrush DraggerFill
        {
            get { return _draggerFill; }
            set { SetProperty(ref _draggerFill, value); }
        }

        public Thickness BottomBarMargin
        {
            get { return _bottomBarMargin; }
            set { SetProperty(ref _bottomBarMargin, value); }
        }

        public double SoloDocDisplayGridWidth
        {
            get { return _soloDocDisplayGridWidth; }
            set { SetProperty(ref _soloDocDisplayGridWidth, value); }
        }

        public double SoloDocDisplayGridHeight
        {
            get { return _soloDocDisplayGridHeight; }
            set { SetProperty(ref _soloDocDisplayGridHeight, value); }
        }

        #endregion


        public CollectionViewModel(CollectionModel model)
        {
            _collectionModel = model;

            SetInitialValues();
            UpdateViewModels(MakeViewModels(_collectionModel.DocumentCollectionFieldModel));
            //SetDimensions();
           var controller = ContentController.GetController<DocumentCollectionFieldModelController>(_collectionModel.DocumentCollectionFieldModel.Id);
            controller.FieldModelUpdatedEvent += Controller_FieldModelUpdatedEvent;
           // _collectionModel.Documents.CollectionChanged += Documents_CollectionChanged;
        }

        private void Controller_FieldModelUpdatedEvent(FieldModelController sender)
        {
            //AddDocuments(_collectionModel.Documents.Data);
            UpdateViewModels(MakeViewModels((sender as DocumentCollectionFieldModelController).DocumentCollectionFieldModel));
        }

        private void Documents_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
           // AddDocuments(_collectionModel.Documents);
        }

        /// <summary>
        /// Sets initial values of instance variables required for the CollectionView to display nicely.
        /// </summary>
        private void SetInitialValues()
        {
            CellSize = 300;
            GridViewVisibility = Visibility.Visible;
            ListViewVisibility = Visibility.Collapsed;
            FilterViewVisibility = Visibility.Collapsed;
            SoloDisplayVisibility = Visibility.Collapsed;
            GridViewWhichIsActuallyGridViewAndNotAnItemsControlVisibility = Visibility.Collapsed;
            _selectedItems = new ObservableCollection<DocumentViewModel>();
            DataBindingSource = new ObservableCollection<DocumentViewModel>();
            ViewIsEnabled = true;
        }

        #region Size and Location methods

        /// <summary>
        /// Returns a bool indicating whether any of the grids used to diplay items are Visible.
        /// </summary>
        /// <returns></returns>
        private bool DisplayingItems()
        {
            return (GridViewVisibility == Visibility.Visible || ListViewVisibility == Visibility.Visible);
        }


        #endregion

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
        /// Changes the view to the GridView by making that Grid visible in the CollectionView.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void GridViewButton_Tapped(object sender, TappedRoutedEventArgs e)
        {
            if (_filtered)
            {
                ObservableCollection<DocumentViewModel> filteredDocumentViewModels = DataBindingSource;
                ListViewVisibility = Visibility.Collapsed;
                GridViewWhichIsActuallyGridViewAndNotAnItemsControlVisibility = Visibility.Collapsed;
                DataBindingSource = filteredDocumentViewModels;
                GridViewVisibility = Visibility.Visible;
            }
            else
            {
                ListViewVisibility = Visibility.Collapsed;
                GridViewWhichIsActuallyGridViewAndNotAnItemsControlVisibility = Visibility.Collapsed;
                GridViewVisibility = Visibility.Visible;
            }
        }

        /// <summary>
        /// Changes the view to the LIstView by making that Grid visible in the CollectionView.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void ListViewButton_Tapped(object sender, TappedRoutedEventArgs e)
        {
            if (_filtered)
            {
                ObservableCollection<DocumentViewModel> filteredDocumentViewModels = DataBindingSource;
                GridViewVisibility = Visibility.Collapsed;
                GridViewWhichIsActuallyGridViewAndNotAnItemsControlVisibility = Visibility.Collapsed;
                DataBindingSource = filteredDocumentViewModels;
                ListViewVisibility = Visibility.Visible;
            }
            else
            {
                GridViewVisibility = Visibility.Collapsed;
                GridViewWhichIsActuallyGridViewAndNotAnItemsControlVisibility = Visibility.Collapsed;
                ListViewVisibility = Visibility.Visible;
            }                    
            OuterGridHeight = CellSize + 44;
            //SetDimensions();
        }

        public void GridViewWhichIsActuallyGridViewAndNotAnItemsControlButton_Tapped(object sender, TappedRoutedEventArgs e)
        {
            if (_filtered)
            {
                ObservableCollection<DocumentViewModel> filteredDocumentViewModels = DataBindingSource;
                ListViewVisibility = Visibility.Collapsed;
                GridViewVisibility = Visibility.Collapsed;
                DataBindingSource = filteredDocumentViewModels;
                GridViewWhichIsActuallyGridViewAndNotAnItemsControlVisibility = Visibility.Visible;
            }
            else
            {
                ListViewVisibility = Visibility.Collapsed;               
                GridViewVisibility = Visibility.Collapsed;
                GridViewWhichIsActuallyGridViewAndNotAnItemsControlVisibility = Visibility.Visible;
            }
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

        /// <summary>
        /// Called when the user double taps on the document being displayed in the 
        /// enlarged view; closes that view and returns to the normal display.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void SingleDocDisplayGrid_Tapped(object sender, TappedRoutedEventArgs e)
        {
            ViewIsEnabled = true;
            SoloDisplayVisibility = Visibility.Collapsed;
            //Resize();
            e.Handled = true;
        }
        
        #endregion

        #region DocumentModel and DocumentViewModel Data Changes

        
        /// <summary>
        /// Adds a collection of new documents to the CollectionModel, and adds new 
        /// DocumentViewModels for each new DocumentModel to the CollectionViewModel
        /// </summary>
        /// <param name="documents"></param>
        public void AddDocuments(List<DocumentController> documents)
        {
            var docList = new List<string>(_collectionModel.DocumentCollectionFieldModel.Data);
            foreach (var document in documents)
            {
                if (!docList.Contains(document.GetId()))
                    docList.Add(document.GetId());
            }
            UpdateViewModels(MakeViewModels(_collectionModel.DocumentCollectionFieldModel));
        }

        /// <summary>
        /// Removes DocumentModels from the Collection and removes all DocumentViewModels 
        /// that no longer reference DocumentModels in the Collection.
        /// </summary>
        /// <param name="documents"></param>
        public void RemoveDocuments(ObservableCollection<DocumentController> documents)
        { 
            foreach (var document in documents)
            {
                if (new List<string>(_collectionModel.DocumentCollectionFieldModel.Data).Contains(document.GetId()))
                    ;//_collectionModel.DocumentCollectionFieldModel.Remove(document);
            }
            RemoveDefunctViewModels();
        }

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
                    viewModel.X = ItemsCarrier.GetInstance().Translate.X - 10 + offset;
                    viewModel.Y = ItemsCarrier.GetInstance().Translate.Y - 10 + offset;
                    offset += 15;
                }
                viewModels.Add(viewModel);
            }
            return viewModels;
        }

        /// <summary>
        /// Adds a collection of DocumentViewModels to the Collection, and thus displays their corresponding views.
        /// </summary>
        /// <param name="viewModels"></param>
        private void AddViewModels(ObservableCollection<DocumentViewModel> viewModels)
        {
            foreach (var viewModel in viewModels)
            {
                bool found = false;
                foreach (var vm in DataBindingSource)
                    if (vm.DocumentController.GetId() == viewModel.DocumentController.GetId())
                        found = true;
                if (!found)
                {
                    //viewModel.DefaultViewVisibility = Visibility.Collapsed;
                    //viewModel.ListViewVisibility = Visibility.Visible;
                    Debug.WriteLine($"{viewModel.ManipulationMode}, {ManipulationModes.None}");
                    viewModel.ManipulationMode = ManipulationModes.System;
                    viewModel.DoubleTapEnabled = false;
                    //viewModel.CanMoveControl = false;
                    DataBindingSource.Add(viewModel);
                }
            }
            //ScaleDocumentsToFitCell();
        }

        /// <summary>
        /// Removes the selected DocumentViewModels (but not their DocumentModels) from the collection
        /// </summary>
        /// <param name="viewModels"></param>
        public void RemoveViewModels(ObservableCollection<DocumentViewModel> viewModels)
        {
            foreach (DocumentViewModel viewModel in viewModels)
            {
                DataBindingSource.Remove(viewModel);
            }
        }

        /// <summary>
        /// The collection creates delegates for each document it displays so that it can associate display-specific
        /// information on the documents.  This allows different collection views to save different views of the same
        /// document collection.
        /// </summary>
        Dictionary<string, DocumentModel> DocumentToDelegateMap = new Dictionary<string, DocumentModel>();

        /// <summary>
        /// Removes all DocumentViewModels whose DocumentModels are no longer contained in the CollectionModel.
        /// </summary>
        public void RemoveDefunctViewModels()
        {
            throw new NotImplementedException();
            //ObservableCollection<DocumentViewModel> toRemove = new ObservableCollection<DocumentViewModel>();
            //foreach (DocumentViewModel vm in DocumentViewModels)
            //{
            //    if (!_collectionModel.Documents.Contains(vm.DocumentModel))
            //    {
            //        toRemove.Add(vm);
            //    }
            //}
            //RemoveViewModels(toRemove);
        }

        #endregion

        public void OuterGrid_DoubleTapped(object sender, DoubleTappedRoutedEventArgs e)
        {
            //Debug.WriteLine("hi");
        }

        public void SelectAll_Tapped(object sender, TappedRoutedEventArgs e)
        {
            throw new NotImplementedException();
        }

        #region Filtering Methods

        public void FilterSelection_Tapped(object sender, TappedRoutedEventArgs e)
        {
            FilterViewVisibility = Visibility.Visible;

            if (OuterGridHeight < 350) OuterGridHeight = 350;
            if (OuterGridWidth < 605)
            {
                OuterGridWidth = 650;
            }
            //SetDimensions();
        }

        public void FilterExit_Tapped(object sender, TappedRoutedEventArgs e)
        {
            FilterViewVisibility = Visibility.Collapsed;
            //Resize();
        }

        public void FilterButton_Tapped(object sender, TappedRoutedEventArgs e)
        {
            throw new NotImplementedException();
            //FilterModel filterModel = null;

            //// generate FilterModels accordingly
            //if (CollectionFilterMode == FilterMode.HasField)
            //{
            //    filterModel = new FilterModel(FilterModel.FilterType.containsKey, SearchFieldBoxText, string.Empty);
            //}
            //else if (CollectionFilterMode == FilterMode.FieldContains)
            //{
            //    filterModel = new FilterModel(FilterModel.FilterType.valueContains, FieldBoxText, SearchBoxText);
            //}
            //else if (CollectionFilterMode == FilterMode.FieldEquals)
            //{
            //    filterModel = new FilterModel(FilterModel.FilterType.valueEquals, FieldBoxText, SearchBoxText);
            //}

            //var list = FilterUtils.Filter(new List<DocumentModel>(_collectionModel.Documents), filterModel);

            
            //ObservableCollection<DocumentViewModel> ViewModels = new ObservableCollection<DocumentViewModel>();
            //foreach (var dvm in DocumentViewModels)
            //{
            //    if (list.Contains(dvm.DocumentModel))
            //    {
            //        ViewModels.Add(dvm);
            //    }
            //}
            //DataBindingSource = ViewModels;
            //_filtered = true;
        }

        public void FilterFieldBox_OnTextChanged(AutoSuggestBox sender, AutoSuggestBoxTextChangedEventArgs args)
        {
            if (args.Reason == AutoSuggestionBoxTextChangeReason.UserInput)
            {
                if (sender.Text.Length > 0)
                {
                    FieldBoxText = sender.Text;
                    throw new Exception();
                    //sender.ItemsSource = FilterUtils.GetKeySuggestions(new List<DocumentController>(
                    //    _collectionModel.DocumentCollectionFieldModel.Data), sender.Text.ToLower());
                }
                else
                {
                    sender.ItemsSource = new string[] { "No suggestions..." };
                }
            }
        }

        public void xSearchBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            TextBox textBox = sender as TextBox;
            if (textBox != null)
            {
                SearchBoxText = textBox.Text;
            }
        }

        public void xSearchFieldBox_TextChanged(AutoSuggestBox sender, AutoSuggestBoxTextChangedEventArgs args)
        {
            SearchFieldBoxText = sender.Text;
        }

        public void xFieldBox_TextChanged(AutoSuggestBox sender, AutoSuggestBoxTextChangedEventArgs args)
        {
            FieldBoxText = sender.Text;
        }

        #endregion

        public void SearchBox_TextEntered(TextBox sender, TextCompositionEndedEventArgs args)
        {
            TextBox textBox = sender as TextBox;
            if (textBox != null)
            {
                SearchBoxText = textBox.Text;
            }
        }

        public void FilterFieldBox_SuggestionChosen(AutoSuggestBox sender, AutoSuggestBoxSuggestionChosenEventArgs args)
        {
            //
        }

        public void FilterFieldBox_QuerySubmitted(AutoSuggestBox sender, AutoSuggestBoxQuerySubmittedEventArgs args)
        {
            FieldBoxText = args.QueryText;
        }

        public void ClearFilter_Tapped(object sender, TappedRoutedEventArgs e)
        {
            FilterViewVisibility = Visibility.Collapsed;
            _filtered = false;
        }
        public void MoveDocument(DocumentViewModel docViewModel, Point where)
        {

            docViewModel.DocumentController.SetField(DashConstants.KeyStore.XPositionFieldKey, new NumberFieldModelController(new NumberFieldModel(where.X)), true);
            docViewModel.DocumentController.SetField(DashConstants.KeyStore.XPositionFieldKey, new NumberFieldModelController(new NumberFieldModel(where.Y)), true);
        }
    }
}
