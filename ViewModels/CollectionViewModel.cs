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

        //private void DocumentView_ManipulationDelta(object sender, ManipulationDeltaRoutedEventArgs e)
        //{
        //    var cvm = DataContext as CollectionViewModel;
        //    var dv  = (sender as DocumentView);
        //    var dvm = dv.DataContext as DocumentViewModel;
        //    var where = dv.RenderTransform.TransformPoint(new Point(e.Delta.Translation.X, e.Delta.Translation.Y));
        //    dvm.Position = where;
        //    e.Handled = true;
        //}

        //private void DocumentViewContainerGrid_OnSizeChanged(object sender, SizeChangedEventArgs e)
        //{
        //    Thickness border = DocumentViewContainerGrid.BorderThickness;
        //    ClipRect.Rect = new Rect(border.Left, border.Top, e.NewSize.Width - border.Left * 2, e.NewSize.Height - border.Top * 2);
        //}



        //public void StartDrag(OperatorView.IOReference ioReference)
        //{
        //    if (!ViewModel.IsEditorMode)
        //    {
        //        return;
        //    }
        //    if (_currentPointers.Contains(ioReference.PointerArgs.Pointer.PointerId))
        //    {
        //        return;
        //    }
        //    _currentPointers.Add(ioReference.PointerArgs.Pointer.PointerId);

        //    _currReference = ioReference;

        //    _connectionLine = new Path
        //    {
        //        StrokeThickness = 5,
        //        Stroke = new SolidColorBrush(Colors.Orange),
        //        IsHitTestVisible = false,
        //        //CompositeMode =
        //        //    ElementCompositeMode.SourceOver //TODO Bug in xaml, shouldn't need this line when the bug is fixed 
        //        //                                    //(https://social.msdn.microsoft.com/Forums/sqlserver/en-US/d24e2dc7-78cf-4eed-abfc-ee4d789ba964/windows-10-creators-update-uielement-clipping-issue?forum=wpdevelop)
        //    };
        //    Canvas.SetZIndex(_connectionLine, -1);
        //    _converter = new BezierConverter(ioReference.FrameworkElement, null, FreeformCanvas);
        //    _converter.Pos2 = ioReference.PointerArgs.GetCurrentPoint(FreeformCanvas).Position;
        //    _lineBinding =
        //        new MultiBinding<PathFigureCollection>(_converter, null);
        //    _lineBinding.AddBinding(ioReference.ContainerView, FrameworkElement.RenderTransformProperty);
        //    _lineBinding.AddBinding(ioReference.ContainerView, FrameworkElement.WidthProperty);
        //    _lineBinding.AddBinding(ioReference.ContainerView, FrameworkElement.HeightProperty);
        //    Binding lineBinding = new Binding
        //    {
        //        Source = _lineBinding,
        //        Path = new PropertyPath("Property")
        //    };
        //    PathGeometry pathGeo = new PathGeometry();
        //    BindingOperations.SetBinding(pathGeo, PathGeometry.FiguresProperty, lineBinding);
        //    _connectionLine.Data = pathGeo;

        //    Binding visibilityBinding = new Binding
        //    {
        //        Source = ViewModel,
        //        Path = new PropertyPath("IsEditorMode"),
        //        Converter = new VisibilityConverter()
        //    };
        //    _connectionLine.SetBinding(UIElement.VisibilityProperty, visibilityBinding);

        //    FreeformCanvas.Children.Add(_connectionLine);

        //    if (!ioReference.IsOutput)
        //    {
        //        CheckLinePresence(ioReference.ReferenceFieldModel);
        //        _lineDict.Add(ioReference.ReferenceFieldModel, _connectionLine);
        //    }
        //}

        //public void CancelDrag(Pointer p)
        //{
        //    _currentPointers.Remove(p.PointerId);
        //    UndoLine();
        //}

        //public void EndDrag(OperatorView.IOReference ioReference)
        //{
        //    if (!ViewModel.IsEditorMode)
        //    {
        //        return;
        //    }
        //    _currentPointers.Remove(ioReference.PointerArgs.Pointer.PointerId);
        //    if (_connectionLine == null) return;

        //    if (_currReference.IsOutput == ioReference.IsOutput)
        //    {
        //        UndoLine();
        //        return;
        //    }
        //    if (_currReference.IsOutput)
        //    {
        //        _graph.AddEdge(ContentController.DereferenceToRootFieldModel(_currReference.ReferenceFieldModel).Id, ContentController.DereferenceToRootFieldModel(ioReference.ReferenceFieldModel).Id);
        //    }
        //    else
        //    {
        //        _graph.AddEdge(ContentController.DereferenceToRootFieldModel(ioReference.ReferenceFieldModel).Id, ContentController.DereferenceToRootFieldModel(_currReference.ReferenceFieldModel).Id);
        //    }
        //    if (_graph.IsCyclic())
        //    {
        //        if (_currReference.IsOutput)
        //        {
        //            _graph.RemoveEdge(ContentController.DereferenceToRootFieldModel(_currReference.ReferenceFieldModel).Id, ContentController.DereferenceToRootFieldModel(ioReference.ReferenceFieldModel).Id);
        //        }
        //        else
        //        {
        //            _graph.RemoveEdge(ContentController.DereferenceToRootFieldModel(ioReference.ReferenceFieldModel).Id, ContentController.DereferenceToRootFieldModel(_currReference.ReferenceFieldModel).Id);
        //        }
        //        CancelDrag(ioReference.PointerArgs.Pointer);
        //        Debug.WriteLine("Cycle detected");
        //        return;
        //    }

        //    if (!ioReference.IsOutput)
        //    {
        //        CheckLinePresence(ioReference.ReferenceFieldModel);
        //        _lineDict.Add(ioReference.ReferenceFieldModel, _connectionLine);
        //    }

        //    _converter.Element2 = ioReference.FrameworkElement;
        //    _lineBinding.AddBinding(ioReference.ContainerView, FrameworkElement.RenderTransformProperty);
        //    _lineBinding.AddBinding(ioReference.ContainerView, FrameworkElement.WidthProperty);
        //    _lineBinding.AddBinding(ioReference.ContainerView, FrameworkElement.HeightProperty);

        //    if (ioReference.IsOutput)
        //    {
        //        ContentController.GetController<DocumentController>(_currReference.ReferenceFieldModel.DocId).AddInputReference(_currReference.ReferenceFieldModel.FieldKey, ioReference.ReferenceFieldModel);
        //        _connectionLine = null;
        //    }
        //    else
        //    {
        //        ContentController.GetController<DocumentController>(ioReference.ReferenceFieldModel.DocId).AddInputReference(ioReference.ReferenceFieldModel.FieldKey, _currReference.ReferenceFieldModel);
        //        _connectionLine = null;
        //    }
        //}

        ///// <summary>
        ///// Helper function that checks if connection line is already present for input ellipse; if so, destroy that line and create a new one  
        ///// </summary>
        ///// <param name="x"></param>
        ///// <param name="y"></param>
        //private void CheckLinePresence(ReferenceFieldModel model)
        //{
        //    if (_lineDict.ContainsKey(model))
        //    {
        //        Path line = _lineDict[model];
        //        FreeformCanvas.Children.Remove(line);
        //        _lineDict.Remove(model);
        //    }
        //}

        //private void UndoLine()
        //{
        //    FreeformCanvas.Children.Remove(_connectionLine);
        //    _connectionLine = null;
        //    _currReference = null;
        //}

        //#endregion

        //private void FreeformGrid_OnPointerMoved(object sender, PointerRoutedEventArgs e)
        //{
        //    if (_connectionLine != null)
        //    {
        //        Point pos = e.GetCurrentPoint(FreeformCanvas).Position;
        //        _converter.Pos2 = pos;
        //        _lineBinding.ForceUpdate();
        //    }
        //}

        //private void FreeformGrid_OnPointerReleased(object sender, PointerRoutedEventArgs e)
        //{
        //    if (_currReference != null)
        //    {
        //        CancelDrag(e.Pointer);

        //        //DocumentView view = new DocumentView();
        //        //DocumentViewModel viewModel = new DocumentViewModel();
        //        //view.DataContext = viewModel;
        //        //FreeformView.MainFreeformView.Canvas.Children.Add(view);

        //    }
        //}

        //private void ConnectionEllipse_OnManipulationStarted(object sender, ManipulationStartedRoutedEventArgs e)
        //{
        //    e.Complete();
        //}

        //private void ConnectionEllipse_OnPointerPressed(object sender, PointerRoutedEventArgs e)
        //{
        //    string docId = (ViewModel.ParentDocument.DataContext as DocumentViewModel).DocumentController.GetId();
        //    Ellipse el = sender as Ellipse;
        //    Key outputKey = DocumentCollectionFieldModelController.CollectionKey;
        //    OperatorView.IOReference ioRef = new OperatorView.IOReference(new ReferenceFieldModel(docId, outputKey), true, e, el, ViewModel.ParentDocument);
        //    CollectionView view = ViewModel.ParentCollection;
        //    view?.StartDrag(ioRef);
        //}

        //private void ConnectionEllipse_OnPointerReleased(object sender, PointerRoutedEventArgs e)
        //{
        //    string docId = (ViewModel.ParentDocument.DataContext as DocumentViewModel).DocumentController.GetId();
        //    Ellipse el = sender as Ellipse;
        //    Key outputKey = DocumentCollectionFieldModelController.CollectionKey;
        //    OperatorView.IOReference ioRef = new OperatorView.IOReference(new ReferenceFieldModel(docId, outputKey), false, e, el, ViewModel.ParentDocument);
        //    CollectionView view = ViewModel.ParentCollection;
        //    view?.EndDrag(ioRef);
        //}

        //private void xGridView_OnDragItemsStarting(object sender, DragItemsStartingEventArgs e)
        //{
        //    MainPage.Instance.MainDocView.DragOver -= MainPage.Instance.XCanvas_DragOver_1;
        //    ItemsCarrier carrier = ItemsCarrier.GetInstance();
        //    carrier.Source = this;
        //    foreach(var item in e.Items)
        //        carrier.Payload.Add(item as DocumentViewModel);
        //    e.Data.RequestedOperation = DataPackageOperation.Move;
        //}

        //private void xGridView_OnDragItemsCompleted(ListViewBase sender, DragItemsCompletedEventArgs args)
        //{
        //    if (args.DropResult == DataPackageOperation.Move && !KeepItemsOnMove)
        //    {
        //        ChangeDocuments(ItemsCarrier.GetInstance().Payload, false);
        //        RefreshItemsBinding();
        //    }
        //    KeepItemsOnMove = true;
        //    var carrier = ItemsCarrier.GetInstance();
        //    carrier.Payload.Clear();
        //    carrier.Source = null;
        //    carrier.Destination = null;
        //    carrier.Translate = new Point();
        //    MainPage.Instance.MainDocView.DragOver += MainPage.Instance.XCanvas_DragOver_1;
        //}

        //private void ChangeDocuments(List<DocumentViewModel> docViewModels, bool add)
        //{
        //    var docControllers = docViewModels.Select(item => item.DocumentController);
        //    var parentDoc = (ViewModel.ParentDocument.ViewModel)?.DocumentController;
        //    var controller = ContentController.GetController<DocumentCollectionFieldModelController>(ViewModel.CollectionFieldModelController.DocumentCollectionFieldModel.Id);
        //    if (controller != null)
        //        foreach (var item in docControllers)
        //            if (add) controller.AddDocument(item);
        //            else controller.RemoveDocument(item);
        //}

        //private void CollectionGrid_DragOver(object sender, DragEventArgs e)
        //{
        //    e.Handled = true;
        //    e.AcceptedOperation = DataPackageOperation.Move;
        //}

        //private void CollectionGrid_Drop(object sender, DragEventArgs e)
        //{
        //    e.Handled = true;
        //    RefreshItemsBinding();
        //    ItemsCarrier.GetInstance().Destination = this;
        //    ItemsCarrier.GetInstance().Source.KeepItemsOnMove = false;
        //    ItemsCarrier.GetInstance().Translate = e.GetPosition(xItemsControl.ItemsPanelRoot);
        //    ChangeDocuments(ItemsCarrier.GetInstance().Payload, true);
        //}

        //private void RefreshItemsBinding()
        //{
        //    if (ViewModel.GridViewVisibility == Visibility.Visible)
        //    {

        //        xGridView.ItemsSource = null;
        //        xGridView.ItemsSource = ViewModel.DataBindingSource;
        //    }
        //    else if (ViewModel.ListViewVisibility == Visibility.Visible)
        //    {
        //        HListView.ItemsSource = null;
        //        HListView.ItemsSource = ViewModel.DataBindingSource;
        //    }
        //}

        ///// <summary>
        ///// Zooms upon mousewheel interaction 
        ///// </summary>
        //private void UserControl_PointerWheelChanged(object sender, PointerRoutedEventArgs e)
        //{
        //    Canvas canvas = xItemsControl.ItemsPanelRoot as Canvas;
        //    Debug.Assert(canvas != null);
        //    e.Handled = true;
        //    //Get mousepoint in canvas space 
        //    PointerPoint point = e.GetCurrentPoint(canvas);
        //    double scaleAmount = Math.Pow(1 + 0.15 * Math.Sign(point.Properties.MouseWheelDelta),
        //        Math.Abs(point.Properties.MouseWheelDelta) / 120.0f);
        //    scaleAmount = Math.Max(Math.Min(scaleAmount, 1.7f), 0.4f);
        //    CanvasScale *= (float)scaleAmount;
        //    Debug.Assert(canvas.RenderTransform != null);
        //    Point p = point.Position;
        //    //Create initial ScaleTransform 
        //    ScaleTransform scale = new ScaleTransform
        //    {
        //        CenterX = p.X,
        //        CenterY = p.Y,
        //        ScaleX = scaleAmount,
        //        ScaleY = scaleAmount
        //    };

        //    //Clamp scale
        //    if (CanvasScale > MaxScale)
        //    {
        //        CanvasScale = MaxScale;
        //        scale.ScaleX = MaxScale / CanvasScale;
        //        scale.ScaleY = MaxScale / CanvasScale;
        //    }
        //    if (CanvasScale < MinScale)
        //    {
        //        CanvasScale = MinScale;
        //        scale.ScaleX = MinScale / CanvasScale;
        //        scale.ScaleY = MinScale / CanvasScale;
        //    }

        //    //Create initial composite transform
        //    TransformGroup composite = new TransformGroup();
        //    composite.Children.Add(scale);
        //    composite.Children.Add(canvas.RenderTransform);

        //    GeneralTransform inverse = composite.Inverse;
        //    Debug.Assert(inverse != null);
        //    GeneralTransform renderInverse = canvas.RenderTransform.Inverse;
        //    Debug.Assert(inverse != null);
        //    Debug.Assert(renderInverse != null);
        //    Point topLeft = inverse.TransformPoint(new Point(0, 0));
        //    Point bottomRight = inverse.TransformPoint(new Point(Grid.ActualWidth, Grid.ActualHeight));
        //    Point preTopLeft = renderInverse.TransformPoint(new Point(0, 0));
        //    Point preBottomRight = renderInverse.TransformPoint(new Point(Grid.ActualWidth, Grid.ActualHeight));

        //    //Check if the zooming puts the view out of bounds of the canvas
        //    //Nullify scale or translate components accordingly 
        //    bool outOfBounds = false;
        //    //Create a canvas space translation to correct the translation if necessary
        //    TranslateTransform fixTranslate = new TranslateTransform();
        //    if (topLeft.X < Bounds.Left && bottomRight.X > Bounds.Right)
        //    {
        //        fixTranslate.X = 0;
        //        scaleAmount = (bottomRight.X - topLeft.X) / Bounds.Width;
        //        scale.ScaleY = scaleAmount;
        //        scale.ScaleX = scaleAmount;
        //        outOfBounds = true;
        //    }
        //    else if (topLeft.X < Bounds.Left)
        //    {
        //        fixTranslate.X = preTopLeft.X;
        //        scale.CenterX = Bounds.Left;
        //        outOfBounds = true;
        //    }
        //    else if (bottomRight.X > Bounds.Right)
        //    {
        //        fixTranslate.X = -(Bounds.Right - preBottomRight.X - 1);
        //        scale.CenterX = Bounds.Right;
        //        outOfBounds = true;
        //    }
        //    if (topLeft.Y < Bounds.Top && bottomRight.Y > Bounds.Bottom)
        //    {
        //        fixTranslate.Y = 0;
        //        scaleAmount = (bottomRight.Y - topLeft.Y) / Bounds.Height;
        //        scale.ScaleX = scaleAmount;
        //        scale.ScaleY = scaleAmount;
        //        outOfBounds = true;
        //    }
        //    else if (topLeft.Y < Bounds.Top)
        //    {
        //        fixTranslate.Y = preTopLeft.Y;
        //        scale.CenterY = Bounds.Top;
        //        outOfBounds = true;
        //    }
        //    else if (bottomRight.Y > Bounds.Bottom)
        //    {
        //        fixTranslate.Y = -(Bounds.Bottom - preBottomRight.Y - 1);
        //        scale.CenterY = Bounds.Bottom;
        //        outOfBounds = true;
        //    }

        //    //If the view was out of bounds recalculate the composite matrix
        //    if (outOfBounds)
        //    {
        //        composite = new TransformGroup();
        //        composite.Children.Add(fixTranslate);
        //        composite.Children.Add(scale);
        //        composite.Children.Add(canvas.RenderTransform);
        //    }
        //    canvas.RenderTransform = new MatrixTransform { Matrix = composite.Value };
        //}

        ///// <summary>
        ///// Make translation inertia slow down faster
        ///// </summary>
        //private void UserControl_ManipulationInertiaStarting(object sender, ManipulationInertiaStartingRoutedEventArgs e)
        //{
        //    e.TranslationBehavior.DesiredDeceleration = 0.01;
        //}

        ///// <summary>
        ///// Make sure the canvas is still in bounds after resize
        ///// </summary>
        //private void UserControl_SizeChanged(object sender, SizeChangedEventArgs e)
        //{
        //    TranslateTransform translate = new TranslateTransform();

        //    //Calculate bottomRight corner of screen in canvas space before and after resize 
        //    Debug.Assert(DocumentViewContainerGrid.RenderTransform != null);
        //    Debug.Assert(DocumentViewContainerGrid.RenderTransform.Inverse != null);
        //    Point oldBottomRight =
        //        DocumentViewContainerGrid.RenderTransform.Inverse.TransformPoint(new Point(e.PreviousSize.Width, e.PreviousSize.Height));
        //    Point bottomRight =
        //        DocumentViewContainerGrid.RenderTransform.Inverse.TransformPoint(new Point(e.NewSize.Width, e.NewSize.Height));

        //    //Check if new bottom right is out of bounds
        //    bool outOfBounds = false;
        //    if (bottomRight.X > Grid.ActualWidth - 1)
        //    {
        //        translate.X = -(oldBottomRight.X - bottomRight.X);
        //        outOfBounds = true;
        //    }
        //    if (bottomRight.Y > Grid.ActualHeight - 1)
        //    {
        //        translate.Y = -(oldBottomRight.Y - bottomRight.Y);
        //        outOfBounds = true;
        //    }
        //    //If it is out of bounds, translate so that is is in bounds
        //    if (outOfBounds)
        //    {
        //        TransformGroup composite = new TransformGroup();
        //        composite.Children.Add(translate);
        //        composite.Children.Add(DocumentViewContainerGrid.RenderTransform);
        //        DocumentViewContainerGrid.RenderTransform = new MatrixTransform { Matrix = composite.Value };
        //    }

        //    Clip = new RectangleGeometry { Rect = new Rect(0, 0, e.NewSize.Width, e.NewSize.Height) };
        //}




    }
}
