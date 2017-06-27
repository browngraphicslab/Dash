using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
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

namespace Dash
{
    public class CollectionViewModel : ViewModelBase
    {
       

        private CollectionModel _collectionModel;


        public ObservableCollection<DocumentViewModel> DocumentViewModels
        {
            get { return _documentViewModels; }
            set { SetProperty(ref _documentViewModels, value); }
        }

        /// <summary>
        /// The DocumentViewModels that the CollectionView actually binds to.
        /// </summary>
        public ObservableCollection<DocumentViewModel> DataBindingSource
        {
            get { return _dataBindingSource; }
            set { SetProperty(ref _dataBindingSource, value); }
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
        

        private double _cellSize = 400;
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
        private Visibility _listViewVisibility;
        private Visibility _controlsVisibility;
        private Visibility _filterViewVisibility;

        //Not backing variable; used to keep track of which items selected in view
        private ObservableCollection<DocumentViewModel> _selectedItems;

        private ObservableCollection<DocumentViewModel> _documentViewModels;
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
            AddViewModels(MakeViewModels(_collectionModel.Documents));
            //SetDimensions();
            _collectionModel.Documents.CollectionChanged += Documents_CollectionChanged;
            
        }

        private void Documents_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            AddDocuments(_collectionModel.Documents);
        }

        /// <summary>
        /// Sets initial values of instance variables required for the CollectionView to display nicely.
        /// </summary>
        private void SetInitialValues()
        {
            OuterGridHeight = 420;
            OuterGridWidth = 400;

            DraggerMargin = new Thickness(360, 400, 0, 0);
            ProportionalDraggerMargin = new Thickness(380, 400, 0, 0);
            CloseButtonMargin = new Thickness(366, 0, 0, 0);
            BottomBarMargin = new Thickness(0, 400, 0, 0);
            SelectButtonMargin = new Thickness(0, OuterGridHeight-20, 0,0);

            DraggerFill = new SolidColorBrush(Color.FromArgb(255, 95, 95, 95));
            ProportionalDraggerFill = new SolidColorBrush(Color.FromArgb(255, 139, 139, 139));
            ProportionalDraggerStroke = new SolidColorBrush(Colors.Transparent);

            CellSize = 400;
            ListViewVisibility = Visibility.Collapsed;
            GridViewVisibility = Visibility.Visible;
            FilterViewVisibility = Visibility.Collapsed;

            _selectedItems = new ObservableCollection<DocumentViewModel>();
            DocumentViewModels = new ObservableCollection<DocumentViewModel>();

            ViewIsEnabled = true;
            SoloDisplayVisibility = Visibility.Collapsed;
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
                DocumentViewModels.Remove(vm);
            }
            DataBindingSource = DocumentViewModels;
        }

        /// <summary>
        /// Changes the view to the GridView by making that Grid visible in the CollectionView.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void GridViewButton_Tapped(object sender, TappedRoutedEventArgs e)
        {
            DataBindingSource = null;
            if (_filtered)
            {
                ObservableCollection<DocumentViewModel> filteredDocumentViewModels = DataBindingSource;
                ListViewVisibility = Visibility.Collapsed;
                DataBindingSource = filteredDocumentViewModels;
                GridViewVisibility = Visibility.Visible;
            }
            else
            {
                ListViewVisibility = Visibility.Collapsed;
                DataBindingSource = DocumentViewModels;
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
            DataBindingSource = null;
            if (_filtered)
            {
                ObservableCollection<DocumentViewModel> filteredDocumentViewModels = DataBindingSource;
                GridViewVisibility = Visibility.Collapsed;
                DataBindingSource = filteredDocumentViewModels;
                ListViewVisibility = Visibility.Visible;
            }
            else
            {
                GridViewVisibility = Visibility.Collapsed;
                DataBindingSource = DocumentViewModels;
                ListViewVisibility = Visibility.Visible;
            }

                       
            OuterGridHeight = CellSize + 44;
            //SetDimensions();
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
        
        

        /// <summary>
        /// Called when the user double taps on a documentView displayed in the collection; 
        /// displays that document in an enlarged format in front of the others and disables 
        /// interactions with the other documents while the solo document is displayed.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void DocumentView_OnDoubleTapped(object sender, DoubleTappedRoutedEventArgs e)
        {
            var dvm = (sender as DocumentView)?.DataContext as DocumentViewModel;
            if (dvm != null)
            {
                if (dvm.DocumentModel.DocumentType.Id == "itunes")
                    dvm.DocumentModel.DocumentType = new DocumentType("itunesLite", "itunesLite");
                else if (dvm.DocumentModel.DocumentType.Id == "itunesLite")
                    dvm.DocumentModel.DocumentType = new DocumentType("itunes", "itunes");
                (sender as DocumentView).DataContext = dvm;
                var testPrototypedoc = dvm.DocumentModel.MakeDelegate();
                // testPrototypedoc.DocumentType = new DocumentType("generic", "generic");
                var annotatedImageModel = new DocumentModel(new Dictionary<Key,FieldModel>(), new DocumentType("annotatedImage", "annotatedImage"));
                annotatedImageModel.SetField(DocumentModel.GetFieldKeyByName("Annotation1"), new TextFieldModel("Header Text"), false);
                annotatedImageModel.SetField(DocumentModel.GetFieldKeyByName("Image"), new ReferenceFieldModel(dvm.DocumentModel.Id, DocumentModel.GetFieldKeyByName("itunes.apple.comartworkUrl100")), false);
                annotatedImageModel.SetField(DocumentModel.GetFieldKeyByName("Annotation2"), new TextFieldModel("Trailing Text"), false);
                testPrototypedoc.SetField(DocumentModel.GetFieldKeyByName("itunes.apple.comartworkUrl100"), new DocumentModelFieldModel(annotatedImageModel), true);
                // var DocView2 = new DocumentView(new DocumentViewModel());
                // var center = e.GetPosition(FreeformView.MainFreeformView);
                MainPage.Instance.DisplayDocument(testPrototypedoc);
                //FreeformView.MainFreeformView.ViewModel.AddElement(DocView2, (float)(center.X - (sender as DocumentView).ActualWidth / 2), (float)(center.Y - (sender as DocumentView).ActualHeight / 2));

                if (GridViewVisibility == Visibility.Visible)
                {
                    SoloDisplaySize = CellSize + 50;
                    if (OuterGridHeight < CellSize + 125) OuterGridHeight = CellSize + 125;
                    if (OuterGridWidth < CellSize + 125) OuterGridWidth = CellSize + 125;
                    //Resize();
                    //SetDimensions();
                }
                else if (ListViewVisibility == Visibility.Visible)
                {
                    SoloDisplaySize = CellSize;
                }

                SoloDisplayElements = new ObservableCollection<UIElement>(dvm.GetUiElements(new Windows.Foundation.Rect()));
                foreach (var s in SoloDisplayElements)
                    s.RenderTransform = new TranslateTransform();
                ViewIsEnabled = false;
                SoloDisplayVisibility = Visibility.Visible;
            }
            e.Handled = true;
        }

       
       

        #endregion

        #region DocumentModel and DocumentViewModel Data Changes

        
        /// <summary>
        /// Adds a collection of new documents to the CollectionModel, and adds new 
        /// DocumentViewModels for each new DocumentModel to the CollectionViewModel
        /// </summary>
        /// <param name="documents"></param>
        public void AddDocuments(ObservableCollection<DocumentModel> documents)
        {
            foreach (DocumentModel document in documents)
            {
                if (!_collectionModel.Documents.Contains(document))
                    _collectionModel.Documents.Add(document);
            }
            AddViewModels(MakeViewModels(documents));
        }

        /// <summary>
        /// Removes DocumentModels from the Collection and removes all DocumentViewModels 
        /// that no longer reference DocumentModels in the Collection.
        /// </summary>
        /// <param name="documents"></param>
        public void RemoveDocuments(ObservableCollection<DocumentModel> documents)
        { 
            foreach (DocumentModel document in documents)
            {
                if(_collectionModel.Documents.Contains(document)) _collectionModel.Documents.Remove(document);
            }
            RemoveDefunctViewModels();
        }

        /// <summary>
        /// Adds a collection of DocumentViewModels to the Collection, and thus displays their corresponding views.
        /// </summary>
        /// <param name="viewModels"></param>
        private void AddViewModels(ObservableCollection<DocumentViewModel> viewModels)
        {
            foreach (DocumentViewModel viewModel in viewModels)
            {
                bool found = false;
                foreach (var vm in DocumentViewModels)
                    if (vm.DocumentModel.Id == viewModel.DocumentModel.Id)
                        found = true;
                if (!found)
                {
                    //viewModel.DefaultViewVisibility = Visibility.Collapsed;
                    //viewModel.ListViewVisibility = Visibility.Visible;
                    viewModel.ManipulationMode = ManipulationModes.System;
                    viewModel.DoubleTapEnabled = false;
                    //viewModel.CanMoveControl = false;
                    DocumentViewModels.Add(viewModel);
                }
            }
            //ScaleDocumentsToFitCell();
            DataBindingSource = DocumentViewModels;
        }

        /// <summary>
        /// Removes the selected DocumentViewModels (but not their DocumentModels) from the collection
        /// </summary>
        /// <param name="viewModels"></param>
        public void RemoveViewModels(ObservableCollection<DocumentViewModel> viewModels)
        {
            foreach (DocumentViewModel viewModel in viewModels)
            {
                if (DocumentViewModels.Contains(viewModel)) DocumentViewModels.Remove(viewModel);
            }
            DataBindingSource = DocumentViewModels;
        }

        /// <summary>
        /// The collection creates delegates for each document it displays so that it can associate display-specific
        /// information on the documents.  This allows different collection views to save different views of the same
        /// document collection.
        /// </summary>
        Dictionary<string, DocumentModel> DocumentToDelegateMap = new Dictionary<string, DocumentModel>();
        /// <summary>
        /// Constructs standard DocumentViewModels from the passed in DocumentModels
        /// </summary>
        /// <param name="documents"></param>
        /// <returns></returns>
        public ObservableCollection<DocumentViewModel> MakeViewModels(ObservableCollection<DocumentModel> documents)
        {
            var docController = App.Instance.Container.GetRequiredService<DocumentEndpoint>();

            ObservableCollection<DocumentViewModel> viewModels = new ObservableCollection<DocumentViewModel>();
            foreach (DocumentModel document in documents)
            {
                var documentDisplayDelegate = DocumentToDelegateMap.ContainsKey(document.Id) ? DocumentToDelegateMap[document.Id] : null;
                if (documentDisplayDelegate == null) {
                    foreach (var deleg in docController.GetDelegates(document.Id))
                    {
                        var field = deleg.Field(DocumentModel.GetFieldKeyByName("CollectionDelegate")) as TextFieldModel;
                        if (field != null && field.Data == _collectionModel.Context.Id)
                            documentDisplayDelegate = deleg;
                    }
                    if (documentDisplayDelegate == null)
                    {
                        documentDisplayDelegate = document.MakeDelegate();
                        documentDisplayDelegate.SetField(DocumentModel.GetFieldKeyByName("CollectionDelegate"), new TextFieldModel(_collectionModel.Context.Id), true);
                     }
                    DocumentToDelegateMap.Add(document.Id, documentDisplayDelegate);
                }
                viewModels.Add(new DocumentViewModel(documentDisplayDelegate));
            }
            return viewModels;
        }


        /// <summary>
        /// Removes all DocumentViewModels whose DocumentModels are no longer contained in the CollectionModel.
        /// </summary>
        public void RemoveDefunctViewModels()
        {
            ObservableCollection<DocumentViewModel> toRemove = new ObservableCollection<DocumentViewModel>();
            foreach (DocumentViewModel vm in DocumentViewModels)
            {
                if (!_collectionModel.Documents.Contains(vm.DocumentModel))
                {
                    toRemove.Add(vm);
                }
            }
            RemoveViewModels(toRemove);
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
            FilterModel filterModel = null;

            // generate FilterModels accordingly
            if (CollectionFilterMode == FilterMode.HasField)
            {
                filterModel = new FilterModel(FilterModel.FilterType.containsKey, SearchFieldBoxText, string.Empty);
            }
            else if (CollectionFilterMode == FilterMode.FieldContains)
            {
                filterModel = new FilterModel(FilterModel.FilterType.valueContains, FieldBoxText, SearchBoxText);
            }
            else if (CollectionFilterMode == FilterMode.FieldEquals)
            {
                filterModel = new FilterModel(FilterModel.FilterType.valueEquals, FieldBoxText, SearchBoxText);
            }

            var list = FilterUtils.Filter(new List<DocumentModel>(_collectionModel.Documents), filterModel);

            
            ObservableCollection<DocumentViewModel> ViewModels = new ObservableCollection<DocumentViewModel>();
            foreach (var dvm in DocumentViewModels)
            {
                if (list.Contains(dvm.DocumentModel))
                {
                    ViewModels.Add(dvm);
                }
            }
            DataBindingSource = ViewModels;
            _filtered = true;
        }

        public void FilterFieldBox_OnTextChanged(AutoSuggestBox sender, AutoSuggestBoxTextChangedEventArgs args)
        {
            if (args.Reason == AutoSuggestionBoxTextChangeReason.UserInput)
            {
                if (sender.Text.Length > 0)
                {
                    FieldBoxText = sender.Text;
                    sender.ItemsSource = FilterUtils.GetKeySuggestions(new List<DocumentModel>(_collectionModel.Documents), sender.Text.ToLower());
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
            DataBindingSource = DocumentViewModels;
            _filtered = false;
        }
        public void MoveDocument(DocumentViewModel docViewModel, Point where)
        {
            docViewModel.DocumentModel.SetField(DocumentModel.GetFieldKeyByName("X"), new NumberFieldModel(where.X), true);
            docViewModel.DocumentModel.SetField(DocumentModel.GetFieldKeyByName("Y"), new NumberFieldModel(where.Y), true);
        }
    }
}
