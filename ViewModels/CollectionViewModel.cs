using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using Windows.UI;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Dash.Models;

namespace Dash
{
    public class CollectionViewModel : ViewModelBase
    {
        private ObservableCollection<Border> _documentContainers;

        public CollectionView View;
        //private CollectionGridView _gridView;
        //private CollectionListView _listView;
        private FrameworkElement _currentView;
        private CollectionModel _collectionModel;



        public ObservableCollection<Border> DocumentContainers
        { get { return _documentContainers; } set { SetProperty(ref _documentContainers, value); } }


        #region Backing variables

        private double _cellSize = 150;
        private double _outerGridWidth;
        private double _outerGridHeight;
        private double _containerGridHeight;
        private Thickness _draggerMargin;
        private Thickness _proportionalDraggerMargin;
        private Thickness _closeButtonMargin;
        private SolidColorBrush _proportionalDraggerStroke;
        private SolidColorBrush _proportionalDraggerFill;
        private SolidColorBrush _draggerStroke;
        private SolidColorBrush _draggerFill;
        private double _containerGridWidth;
        private Thickness _bottomBarMargin;

        private Visibility _gridViewVisibility;
        private Visibility _listViewVisibility;

        public Visibility GridViewVisibility
        { get { return _gridViewVisibility; } set { SetProperty(ref _gridViewVisibility, value); } }


        public Visibility ListViewVisibility
        { get { return _listViewVisibility; } set { SetProperty(ref _listViewVisibility, value); } }


        #endregion

        #region Size Variables

        private double _minWidth = 241;
        private double _minHeight = 100;
        /// <summary>
        /// The size of each cell in the GridView.
        /// </summary>
        public double CellSize
        { get { return _cellSize; } set { SetProperty(ref _cellSize, value); } }


        public double OuterGridWidth
        { get { return _outerGridWidth; } set { SetProperty(ref _outerGridWidth, value); } }


        public double OuterGridHeight
        { get { return _outerGridHeight; } set { SetProperty(ref _outerGridHeight, value); } }


        public double ContainerGridWidth
        { get { return _containerGridWidth; } set { SetProperty(ref _containerGridWidth, value); } }

        public double ContainerGridHeight
        { get { return _containerGridHeight; } set { SetProperty(ref _containerGridHeight, value); } }


        #endregion

        #region Dragger, Close Button, Lower Bar properties

        public bool ProportionalScaling;
        private bool _manipulation;
        private ListViewSelectionMode _itemSelectionMode;
        private Thickness _selectButtonMargin;
        private Visibility _controlsVisibility;

        public Visibility ControlsVisibility
        { get { return _controlsVisibility; } set { SetProperty(ref _controlsVisibility, value); } }

        public ListViewSelectionMode ItemSelectionMode
        { get { return _itemSelectionMode; } set { SetProperty(ref _itemSelectionMode, value); } }


        public Thickness SelectButtonMargin
        {
            get { return _selectButtonMargin; }
            set { SetProperty(ref _selectButtonMargin, value); }
        }

        public Thickness DraggerMargin
        { get { return _draggerMargin; } set { SetProperty(ref _draggerMargin, value); } }



        public Thickness ProportionalDraggerMargin
        { get { return _proportionalDraggerMargin; } set { SetProperty(ref _proportionalDraggerMargin, value); } }


        public Thickness CloseButtonMargin
        { get { return _closeButtonMargin; } set { SetProperty(ref _closeButtonMargin, value); } }


        public SolidColorBrush ProportionalDraggerStroke
        { get { return _proportionalDraggerStroke; } set { SetProperty(ref _proportionalDraggerStroke, value); } }


        public SolidColorBrush ProportionalDraggerFill
        { get { return _proportionalDraggerFill; } set { SetProperty(ref _proportionalDraggerFill, value); } }


        public SolidColorBrush DraggerStroke
        { get { return _draggerStroke; } set { SetProperty(ref _draggerStroke, value); } }


        public SolidColorBrush DraggerFill
        { get { return _draggerFill; } set { SetProperty(ref _draggerFill, value); } }


        public Thickness BottomBarMargin
        { get { return _bottomBarMargin; } set { SetProperty(ref _bottomBarMargin, value); } }


        #endregion

        public CollectionViewModel(CollectionModel model)
        {
            _collectionModel = model;

            View = new CollectionView(this);
            
            UpdateContainers();
            SetInitialValues();
            SetDimensions();
            ResizeElements();

            GridViewButton_Tapped(null, null);
        }

        #region Setup Methods

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
            DraggerStroke = new SolidColorBrush(Colors.Transparent);
            ProportionalDraggerFill = new SolidColorBrush(Color.FromArgb(255, 139, 139, 139));
            ProportionalDraggerStroke = new SolidColorBrush(Colors.Transparent);

            CellSize = 150;
            ListViewVisibility = Visibility.Collapsed;
            GridViewVisibility = Visibility.Collapsed;
        }

        private void UpdateContainers()
        {
            DocumentContainers = new ObservableCollection<Border>();
            foreach (var view in _collectionModel.DocumentViews)
            {
                Border border = new Border();
                border.Width = CellSize;
                border.Height = CellSize;
                border.Child = view;
                border.Visibility = Visibility.Visible;
                DocumentContainers.Add(border);
                
            }
            Debug.WriteLine(DocumentContainers.Count);
        }
        
        #endregion

        #region Size and Location methods

        /// <summary>
        /// Scales height and width properties of collection of Elements to which the XAML items are bound
        /// </summary>
        public void ScaleElements(double scaleFactor)
        {
            ScaleTransform scaleTrans = new ScaleTransform();
            scaleTrans.ScaleY = scaleFactor;
            scaleTrans.ScaleX = scaleFactor;
            foreach (DocumentView elem in _collectionModel.DocumentViews)
            {
                elem.RenderTransform = scaleTrans;
            }
        }


        /// <summary>
        /// Resizes the elements after they are added (see constructor).
        /// </summary>
        private void ResizeElements()
        {
            foreach (DocumentView view in _collectionModel.DocumentViews)
            {
                //var containerWidth = (View.GridView.ContainerFromItem(view) as GridViewItem).Width;
                var aspectRatio = view.Width / view.Height;
                // If at least one dimension is greater than CellSize execute the rest of the loop contents
                if (Math.Max(view.Height, view.Width) <= CellSize) continue;
                var prevWidth = view.Width;
                // And the width is greater than the height
                if (view.Width > view.Height)
                {
                    //Adjust width to be cellsize and height proportionally
                    
                    view.Height = CellSize / aspectRatio;
                    view.Width = CellSize;
                    
                }
                else
                {
                    //Otherwise make height cellsize and width proportional
                    view.Width = CellSize * aspectRatio;
                    view.Height = CellSize;
                }
                var elementScale = view.Width/prevWidth;
                foreach (UIElement elem in ((DocumentViewModel)view.DataContext).GetUiElements())
                {
                    ScaleTransform scale = new ScaleTransform();
                    scale.ScaleX = elementScale;
                    scale.ScaleY = elementScale;
                    elem.RenderTransform = scale;
                }
            }
        }

        private bool DisplayingItems()
        {
            return (GridViewVisibility == Visibility.Visible || ListViewVisibility == Visibility.Visible);
        }

        /// <summary>
        /// Resizes the CollectionView according to the increments in width and height. 
        /// The CollectionListView vertically resizes corresponding to the change in the size of its cells, so if ProportionalScaling is true and the ListView is being displayed, the Grid must change size to accomodate the height of the ListView.
        /// </summary>
        /// <param name="dx"></param>
        /// <param name="dy"></param>
        public void Resize(double dx, double dy)
        {

            //Changes width if permissible within size constraints.
            if (OuterGridWidth + dx > _minWidth || dx > 0)
            {
                OuterGridWidth += dx;
                if (ProportionalScaling && DisplayingItems())
                {
                    var scaleFactor = OuterGridWidth / (OuterGridWidth-dx);
                    CellSize = CellSize * scaleFactor;
                    foreach (var container in DocumentContainers)
                    {
                        container.Width = CellSize;
                        container.Height = CellSize;
                    }
                    ScaleElements(scaleFactor);

                    foreach (var docView in _collectionModel.DocumentViews)
                    {
                        docView.Width *= scaleFactor;
                        docView.Height *= scaleFactor;
                    }

                    //Takes care of proportional height resizing if proportional dragger is used
                    if (ListViewVisibility == Visibility.Visible)
                    {
                        OuterGridHeight = CellSize + 44;

                    }
                    else if (GridViewVisibility == Visibility.Visible)
                    {
                        var aspectRatio = OuterGridHeight / OuterGridWidth;
                        OuterGridHeight += dx * aspectRatio;
                    }
                }
            }

            //Changes height if permissible within size constraints; makes the height of the Grid track the height of the ListView if the ListView is showing and proportional scaling is allowed.
            if ((OuterGridHeight + dy > _minHeight || dy > 0) && (!ProportionalScaling || !DisplayingItems()))
            {
                if (DisplayingItems() && ListViewVisibility == Visibility.Visible)
                {
                    OuterGridHeight = CellSize + 44;
                }
                else
                {
                    OuterGridHeight += dy;
                }
            }

            SetDimensions();
        }

        /// <summary>
        /// Sets the sizes and/or locations of all of the components of the CollectionView correspoding to the size of the Grid.
        /// </summary>
        public void SetDimensions()
        {
            ContainerGridHeight = OuterGridHeight - 20;
            ContainerGridWidth = OuterGridWidth-2;

            DraggerMargin = new Thickness(OuterGridWidth - 62, OuterGridHeight - 20, 0, 0);
            ProportionalDraggerMargin = new Thickness(OuterGridWidth - 22, OuterGridHeight - 20, 0, 0);
            CloseButtonMargin = new Thickness(OuterGridWidth - 34, 0, 0, 0);

            SelectButtonMargin = new Thickness(0, OuterGridHeight - 23, 0, 0);

            BottomBarMargin = new Thickness(0, OuterGridHeight - 21, 0, 0);

            View.DeleteButton.Margin = new Thickness(38, OuterGridHeight - 23, 0, 0);
        }

        #endregion

        #region Event Handlers


        public void GridDragItemsCompleted(ListViewBase sender, DragItemsCompletedEventArgs args)
        {
            
            //List<DocumentView> selected = new List<DocumentView>();
            //if (GridViewVisibility == Visibility.Visible)
            //{

            //    foreach (object container in View.GridView.SelectedItems)
            //    {
            //        selected.Add(((Border)container).Child as DocumentView);
            //    }
            //}

            //if (ListViewVisibility == Visibility.Visible)
            //{
            //    foreach (object container in View.ListView.SelectedItems)
            //    {
            //        selected.Add(((Border)container).Child as DocumentView);
            //    }
            //}

            ////maybe hacky, maybe have real reference to Canvas?
            ////Also will eventually want to 
            //foreach (var view in selected)
            //{
            //    (View.Parent as Canvas)?.Children.Add(_collectionModel.GetCopyOf(view));
            //}
            
        }

        public void Deselect_Tapped(object sender, TappedRoutedEventArgs e)
        {
            View.GridView.SelectedItems.Clear();
            View.ListView.SelectedItems.Clear();
        }

        public void DeleteSelected_Tapped(object sender, TappedRoutedEventArgs e)
        {
            List<DocumentView> selected = new List<DocumentView>();
            List<Border> borders = new List<Border>();
            if (GridViewVisibility == Visibility.Visible)
            {

                foreach (object container in View.GridView.SelectedItems)
                {
                    borders.Add((Border)container);
                    selected.Add(((Border) container).Child as DocumentView);
                }
                foreach (DocumentView view in selected)
                    _collectionModel.RemoveDocument(view);
                foreach (Border border in borders)
                    DocumentContainers.Remove(border);
            }

            if (ListViewVisibility == Visibility.Visible)
            {
                foreach (object container in View.ListView.SelectedItems)
                {
                    borders.Add((Border)container);
                    selected.Add(((Border)container).Child as DocumentView);
                }
                foreach (DocumentView view in selected)
                    _collectionModel.RemoveDocument(view);
                foreach (Border border in borders)
                    DocumentContainers.Remove(border);
            }

        }
        public void GridViewButton_Tapped(object sender, TappedRoutedEventArgs e)
        {
            View.ListView.ItemsSource = null;
            GridViewVisibility = Visibility.Visible;
            View.GridView.ItemsSource = DocumentContainers;
            ListViewVisibility = Visibility.Collapsed;
        }

        public void ListViewButton_Tapped(object sender, TappedRoutedEventArgs e)
        {
            View.GridView.ItemsSource = null;
            GridViewVisibility = Visibility.Collapsed;
            View.ListView.ItemsSource = DocumentContainers;
            ListViewVisibility = Visibility.Visible;

            OuterGridHeight = CellSize + 44;
            SetDimensions();
        }

        public void ProportionalDragger_OnManipulationDelta(object sender, ManipulationDeltaRoutedEventArgs e)
        {
            ProportionalScaling = true;
            Resize(e.Delta.Translation.X, e.Delta.Translation.Y);
            e.Handled = true;
        }

        public void Dragger_OnManipulationDelta(object sender, ManipulationDeltaRoutedEventArgs e)
        {
            ProportionalScaling = false;
            Resize(e.Delta.Translation.X, e.Delta.Translation.Y);
            e.Handled = true;
        }

        public void CloseButton_Tapped(object sender, TappedRoutedEventArgs e)
        {
            //_listView = null;
            //_gridView = null;
            var canvas = View.Parent as Canvas;
            canvas?.Children.Remove(View);
            View = null;
            e.Handled = true;
        }

        public void SpreadsheetViewButton_Tapped(object sender, TappedRoutedEventArgs e)
        {

        }

        public void Dragger_ManipulationStarted(object sender, ManipulationStartedRoutedEventArgs e)
        {
            if (!_manipulation)
            {
                _manipulation = true;
                DraggerFill = new SolidColorBrush(Colors.AliceBlue);
                DraggerStroke = new SolidColorBrush(Colors.DarkBlue);
            }
            else
            {
                e.Complete();
            }

        }

        public void Dragger_ManipulationCompleted(object sender, ManipulationCompletedRoutedEventArgs e)
        {
            if (_manipulation) _manipulation = false;
            DraggerFill = new SolidColorBrush(Color.FromArgb(255, 95, 95, 95));
            DraggerStroke = new SolidColorBrush(Colors.Transparent);
        }

        public void ProportionalDragger_ManipulationStarted(object sender, ManipulationStartedRoutedEventArgs e)
        {
            if (!_manipulation)
            {
                _manipulation = true;
                ProportionalDraggerFill = new SolidColorBrush(Colors.AliceBlue);
                ProportionalDraggerStroke = new SolidColorBrush(Colors.DarkBlue);
            }
            else
            {
                e.Complete();
            }
        }

        public void ProportionalDragger_ManipulationCompleted(object sender, ManipulationCompletedRoutedEventArgs e)
        {
            if (_manipulation) _manipulation = false;
            ProportionalDraggerFill = new SolidColorBrush(Color.FromArgb(255, 139, 139, 139));
            ProportionalDraggerStroke = new SolidColorBrush(Colors.Transparent);
        }

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
        #endregion

        public void OuterGrid_DoubleTapped(object sender, DoubleTappedRoutedEventArgs e)
        {
            Debug.WriteLine("hi");
        }
    }
}
