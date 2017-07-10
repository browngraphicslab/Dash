using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Diagnostics;
using System.Linq;
using Windows.ApplicationModel.DataTransfer;
using Windows.Foundation;
using Windows.UI;
using Windows.UI.Input;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Shapes;
using Windows.Foundation.Collections;
using Windows.UI.Core;
using DashShared;
using Visibility = Windows.UI.Xaml.Visibility;
using Windows.Storage;
using Windows.UI.Xaml.Media.Imaging;
using System.Collections.ObjectModel;

// The User Control item template is documented at http://go.microsoft.com/fwlink/?LinkId=234236

namespace Dash
{
    public sealed partial class CollectionView : UserControl
    {

        public double CanvasScale { get; set; } = 1;
        public const float MaxScale = 10;
        public const float MinScale = 0.5f;
        public Rect Bounds = new Rect(0, 0, 5000, 5000);

        private Canvas FreeformCanvas => xItemsControl.ItemsPanelRoot as Canvas;

        public CollectionViewModel ViewModel;
        private bool _isHasFieldPreviouslySelected;
        public Grid OuterGrid
        {
            get { return Grid; }
            set { Grid = value; }
        }

        public bool KeepItemsOnMove { get; set; } = true;

        public CollectionView(CollectionViewModel vm)
        {
            this.InitializeComponent();
            DataContext = ViewModel = vm;
            var docFieldCtrler = ContentController.GetController<FieldModelController>(vm.CollectionModel.DocumentCollectionFieldModel.Id);
            docFieldCtrler.FieldModelUpdatedEvent += DocFieldCtrler_FieldModelUpdatedEvent;
            SetEventHandlers();

            InkSource.Presenters.Add(xInkCanvas.InkPresenter);
        }

        private void DocFieldCtrler_FieldModelUpdatedEvent(FieldModelController sender)
        {
            DataContext = ViewModel;
        }

        private void SetEventHandlers()
        {
            Loaded += CollectionView_Loaded;
            
            xItemsControl.Items.VectorChanged += ItemsControl_ItemsChanged;
            //ViewModel.DataBindingSource.CollectionChanged += DataBindingSource_CollectionChanged;
            FreeformOption.Tapped += ViewModel.FreeformButton_Tapped;
            GridViewOption.Tapped +=
                ViewModel.GridViewButton_Tapped;
            ListOption.Tapped += ViewModel.ListViewButton_Tapped;
            CloseButton.Tapped += CloseButton_Tapped;
            SelectButton.Tapped += ViewModel.SelectButton_Tapped;
            DeleteSelected.Tapped += ViewModel.DeleteSelected_Tapped;
            Filter.Tapped += ViewModel.FilterSelection_Tapped;
            ClearFilter.Tapped += ViewModel.ClearFilter_Tapped;
            //CancelSoloDisplayButton.Tapped += ViewModel.CancelSoloDisplayButton_Tapped;

            HListView.SelectionChanged += ViewModel.SelectionChanged;
            xGridView.SelectionChanged += ViewModel.SelectionChanged;
            // xItemsControl.SelectionChanged += ViewModel.SelectionChanged;
            
            Grid.DoubleTapped += ViewModel.OuterGrid_DoubleTapped;

            SingleDocDisplayGrid.Tapped += ViewModel.SingleDocDisplayGrid_Tapped;

            xFilterExit.Tapped += ViewModel.FilterExit_Tapped;
            xFilterButton.Tapped += ViewModel.FilterButton_Tapped;
            
            xSearchBox.TextCompositionEnded += ViewModel.SearchBox_TextEntered;
            xSearchBox.TextChanged += ViewModel.xSearchBox_TextChanged;

            xFieldBox.TextChanged += ViewModel.FilterFieldBox_OnTextChanged;
            xFieldBox.SuggestionChosen += ViewModel.FilterFieldBox_SuggestionChosen;
            xFieldBox.QuerySubmitted += ViewModel.FilterFieldBox_QuerySubmitted;
            
            xSearchFieldBox.TextChanged += ViewModel.xSearchFieldBox_TextChanged;

        }

        private void CollectionView_Loaded(object sender, RoutedEventArgs e)
        {
            ViewModel.ParentDocument = this.GetFirstAncestorOfType<DocumentView>();
            ViewModel.ParentCollection = this.GetFirstAncestorOfType<CollectionView>();

            if (ViewModel.ParentDocument != MainPage.Instance.MainDocView)
            {
                ViewModel.ParentDocument.SizeChanged += (ss, ee) =>
                {
                    var height = (ViewModel.ParentDocument.DataContext as DocumentViewModel)?.Height;
                    if (height != null)
                        Height = (double) height;
                    var width = (ViewModel.ParentDocument.DataContext as DocumentViewModel)?.Width;
                    if (width != null)
                        Width = (double) width;
                };
            }
        }

        private void DataBindingSource_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            if (e.Action == NotifyCollectionChangedAction.Add)
            {
                foreach (var eNewItem in e.NewItems)
                {
                    var docVM = eNewItem as DocumentViewModel;
                    Debug.Assert(docVM != null);
                    var ofm =
                        docVM.DocumentController.GetDereferencedField(OperatorDocumentModel.OperatorKey, DocContextList) as
                            OperatorFieldModelController;
                    if (ofm != null)
                    {
                        foreach (var inputKey in ofm.Inputs)
                        {
                            foreach (var outputKey in ofm.Outputs)
                            {
                                ReferenceFieldModelController irfm =
                                    new ReferenceFieldModelController(docVM.DocumentController.GetId(), inputKey.Key);
                                ReferenceFieldModelController orfm =
                                    new ReferenceFieldModelController(docVM.DocumentController.GetId(), outputKey.Key);
                                //_graph.AddEdge(ContentController.DereferenceToRootFieldModel(irfm).GetId(),
                                    //ContentController.DereferenceToRootFieldModel(orfm).GetId());
                            }
                        }
                    }
                }
            }
            else if (e.Action == NotifyCollectionChangedAction.Remove)
            {
                foreach (var eOldItem in e.OldItems)
                {
                    var docVM = eOldItem as DocumentViewModel;
                    Debug.Assert(docVM != null);
                    OperatorFieldModelController ofm =
                        docVM.DocumentController.GetDereferencedField(OperatorDocumentModel.OperatorKey, DocContextList) as
                            OperatorFieldModelController;
                    if (ofm != null)
                    {
                        foreach (var inputKey in ofm.Inputs)
                        {
                            foreach (var outputKey in ofm.Outputs)
                            {
                                ReferenceFieldModelController irfm =
                                    new ReferenceFieldModelController(docVM.DocumentController.GetId(), inputKey.Key);
                                ReferenceFieldModelController orfm =
                                    new ReferenceFieldModelController(docVM.DocumentController.GetId(), outputKey.Key);
                                //_graph.RemoveEdge(ContentController.DereferenceToRootFieldModel(irfm).GetId(),
                                    //ContentController.DereferenceToRootFieldModel(orfm).GetId());
                            }
                        }
                    }
                }
            }
        }
        List<DocumentController> DocContextList {  get { return (DataContext as CollectionViewModel).DocContextList;  } }
        private void ItemsControl_ItemsChanged(IObservableVector<object> sender, IVectorChangedEventArgs e)
        {
            RefreshItemsBinding();
            if (e.CollectionChange == CollectionChange.ItemInserted)
            {
                var docVM = sender[(int)e.Index] as DocumentViewModel;
                Debug.Assert(docVM != null);
                OperatorFieldModelController ofm = docVM.DocumentController.GetDereferencedField(OperatorDocumentModel.OperatorKey, DocContextList) as OperatorFieldModelController;
                if (ofm != null)
                {
                    foreach (var inputKey in ofm.Inputs)
                    {
                        foreach (var outputKey in ofm.Outputs)
                        {
                            ReferenceFieldModelController irfm = new ReferenceFieldModelController(docVM.DocumentController.GetId(), inputKey.Key);
                            ReferenceFieldModelController orfm = new ReferenceFieldModelController(docVM.DocumentController.GetId(), outputKey.Key);
                            //_graph.AddEdge(ContentController.DereferenceToRootFieldModel(irfm).GetId(), ContentController.DereferenceToRootFieldModel(orfm).GetId());
                        }
                    }
                }
            }
            //else if (e.CollectionChange == CollectionChange.ItemRemoved)
            //{
            //    var docVM = sender[(int)e.Index] as DocumentViewModel;
            //    Debug.Assert(docVM != null);
            //    OperatorFieldModelController ofm = docVM.DocumentController.GetField(OperatorDocumentModel.OperatorKey) as OperatorFieldModelController;
            //    if (ofm != null)
            //    {
            //        foreach (var inputKey in ofm.InputKeys)
            //        {
            //            foreach (var outputKey in ofm.OutputKeys)
            //            {
            //                ReferenceFieldModel irfm = new ReferenceFieldModel(docVM.DocumentController.GetId(), inputKey);
            //                ReferenceFieldModel orfm = new ReferenceFieldModel(docVM.DocumentController.GetId(), outputKey);
            //                _graph.RemoveEdge(irfm, orfm);
            //            }
            //        }
            //    }
            //}
        }

        private void CloseButton_Tapped(object sender, TappedRoutedEventArgs e)
        {
            var contentPresentor = this.GetFirstAncestorOfType<ContentPresenter>();
            (VisualTreeHelper.GetParent(contentPresentor) as Canvas)?.Children.Remove(this
                .GetFirstAncestorOfType<ContentPresenter>());
        }

        private void SoloDocument_OnTapped(object sender, TappedRoutedEventArgs e)
        {
            e.Handled = true;
        }

        private void Grid_OnManipulationStarted(object sender, ManipulationStartedRoutedEventArgs e)
        {
            if (e.Container is ScrollBar || e.Container is ScrollViewer)
            {
                e.Complete();
                e.Handled = true;
            }
        }

        /// <summary>
        /// Animate fadeout of the xFieldBox and the collapsing of the xMainGrid
        /// when the "Has field" option is selected in the combobox
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void hasField_Tapped(object sender, TappedRoutedEventArgs e)
        {
            _isHasFieldPreviouslySelected = true;

            ViewModel.CollectionFilterMode = CollectionViewModel.FilterMode.HasField;

            // collapse only if the grid that the xFieldBox is located in is expanded
            if (xFieldBoxColumn.Width > 0)
            {
                xHideFieldBox.Begin();
                xCollapseMainGrid.Begin();
            }

            xSearchBox.Visibility = Visibility.Collapsed;
            xSearchFieldBox.Visibility = Visibility.Visible;

            // case where xSearchBox is filled in before user clicks on xHasField
            if (xSearchFieldBox.Text != "")
            {
                xFilterButton.Visibility = Visibility.Visible;
            }

            if (xFieldBox.Text != "")
            {
                xSearchFieldBox.Text = xFieldBox.Text;
                xFieldBox.Text = "";
            }
        }
        /// <summary>
        /// Animate expansion of xMainGrid when the "Field contains" or "Field equals" option is
        /// selected in the combobox (and the previously selected option is "Has field")
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void fieldContainsOrEquals_Tapped(object sender, TappedRoutedEventArgs e)
        {
            // expand only if the grid that the xFieldBox is located in is collapsed
            if (xFieldBoxColumn.Width == 0)
            {
                // resize actual grid column
                xFieldBoxColumn.Width = 165;
                xExpandMainGrid.Begin();
            }

            xSearchBox.Visibility = Visibility.Visible;
            xSearchFieldBox.Visibility = Visibility.Collapsed;

            // xFieldBox is cleared when xFieldContains or xFieldEquals is selected, button must be disabled
            if (xFieldBox.Text == "")
            {
                xFilterButton.Visibility = Visibility.Collapsed;
                // case where field option is selected after the text boxes are filled in
            }
            else if (xFieldBox.Text != "" && xSearchBox.Text != "")
            {
                xFilterButton.Visibility = Visibility.Visible;
            }

            if (xSearchFieldBox.Text != "" && _isHasFieldPreviouslySelected)
            {
                xFieldBox.Text = xSearchFieldBox.Text;
                xSearchFieldBox.Text = "";
            }

            _isHasFieldPreviouslySelected = false;
        }

        /// <summary>
        /// Animate fadein of the xFieldBox when the animation that expands the xMainGrid finishes playing
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void xExpandMainGrid_Completed(object sender, object e)
        {
            xShowFieldBox.Begin();
        }

        /// <summary>
        /// Ensure that the filter button is only responsive when all available combo and text boxes are filled in
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void xSearchBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            EnableOrDisableFilterButton();
        }

        /// <summary>
        /// Generate autosuggestions according to available fields when user types into the autosuggestionbox to prevent mispelling
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="args"></param>


        /// <summary>
        /// Specify conditions for the FILTER button to enable or disable
        /// </summary>
        private void EnableOrDisableFilterButton()
        {
            if (xComboBox.SelectedItem == xHasField && xSearchFieldBox.Text != "" || xComboBox.SelectedItem != xHasField && xComboBox.SelectedItem != null && xSearchBox.Text != "" && xFieldBox.Text != "")
            {
                xFilterButton.Visibility = Visibility.Visible;
            }
            else
            {
                xFilterButton.Visibility = Visibility.Collapsed;
            }
        }

        /// <summary>
        /// Remove entire filter view from its parent when the animation finishes playing
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void FadeOutThemeAnimation_Completed(object sender, object e)
        {
            ((Grid)this.Parent).Children.Remove(this);
        }

        /// <summary>
        /// Resize the grid column that the xFieldBox is located in when the animation that collapses
        /// the xMainGrid and fades out the xFieldBox finishes playing
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void xCollapseMainGrid_Completed(object sender, object e)
        {
            xFieldBoxColumn.Width = 0;
        }

        private void XFieldBox_OnTextChanged(AutoSuggestBox sender, AutoSuggestBoxTextChangedEventArgs args)
        {
            
            // enable and disable button accordingly
            EnableOrDisableFilterButton();
        }

        private void fieldContains_Tapped(object sender, TappedRoutedEventArgs e)
        {
            ViewModel.CollectionFilterMode = CollectionViewModel.FilterMode.FieldContains;
            fieldContainsOrEquals_Tapped(sender, e);
        }

        private void fieldEquals_Tapped(object sender, TappedRoutedEventArgs e)
        {
            ViewModel.CollectionFilterMode = CollectionViewModel.FilterMode.FieldEquals;
            fieldContainsOrEquals_Tapped(sender, e);
        }
      
        private void DocumentView_ManipulationStarted(object sender, ManipulationStartedRoutedEventArgs e)
        {
            var cvm = DataContext as CollectionViewModel;
            //(sender as DocumentView).Manipulator.TurnOff();

        }

        private void DocumentView_ManipulationDelta(object sender, ManipulationDeltaRoutedEventArgs e)
        {
            var cvm = DataContext as CollectionViewModel;
            var dv  = (sender as DocumentView);
            var dvm = dv.DataContext as DocumentViewModel;
            var where = dv.RenderTransform.TransformPoint(new Point(e.Delta.Translation.X, e.Delta.Translation.Y));
            dvm.Position = where;
            e.Handled = true;
        }

        private void DocumentViewContainerGrid_OnSizeChanged(object sender, SizeChangedEventArgs e)
        {
            Thickness border = DocumentViewContainerGrid.BorderThickness;
            ClipRect.Rect = new Rect(border.Left, border.Top, e.NewSize.Width - border.Left * 2, e.NewSize.Height - border.Top * 2);
        }

        /// <summary>
        /// Pans and zooms upon touch manipulation 
        /// </summary>
        private void UserControl_ManipulationDelta(object sender, ManipulationDeltaRoutedEventArgs e)
        {
            Canvas canvas = xItemsControl.ItemsPanelRoot as Canvas;
            Debug.Assert(canvas != null);
            e.Handled = true;
            ManipulationDelta delta = e.Delta;

            //Create initial translate and scale transforms
            //Translate is in screen space, scale is in canvas space
            TranslateTransform translate = new TranslateTransform
            {
                X = delta.Translation.X,
                Y = delta.Translation.Y
            };

            Point p = Util.PointTransformFromVisual(e.Position, canvas);
            ScaleTransform scale = new ScaleTransform
            {
                CenterX = p.X,
                CenterY = p.Y,
                ScaleX = delta.Scale,
                ScaleY = delta.Scale
            };

            //Clamp the zoom
            CanvasScale *= delta.Scale;
            if (CanvasScale > MaxScale)
            {
                CanvasScale = MaxScale;
                scale.ScaleX = 1;
                scale.ScaleY = 1;
            }
            if (CanvasScale < MinScale)
            {
                CanvasScale = MinScale;
                scale.ScaleX = 1;
                scale.ScaleY = 1;
            }

            //Create initial composite transform
            TransformGroup composite = new TransformGroup();
            composite.Children.Add(scale);
            composite.Children.Add(canvas.RenderTransform);
            composite.Children.Add(translate);

            //Get top left and bottom right screen space points in canvas space
            GeneralTransform inverse = composite.Inverse;
            Debug.Assert(inverse != null);
            Debug.Assert(canvas.RenderTransform != null);
            GeneralTransform renderInverse = canvas.RenderTransform.Inverse;
            Debug.Assert(renderInverse != null);
            Point topLeft = inverse.TransformPoint(new Point(0, 0));
            Point bottomRight = inverse.TransformPoint(new Point(Grid.ActualWidth, Grid.ActualHeight));
            Point preTopLeft = renderInverse.TransformPoint(new Point(0, 0));
            Point preBottomRight = renderInverse.TransformPoint(new Point(Grid.ActualWidth, Grid.ActualHeight));

            //Check if the panning or zooming puts the view out of bounds of the canvas
            //Nullify scale or translate components accordingly
            bool outOfBounds = false;
            //Create a canvas space translation to correct the translation if necessary
            TranslateTransform fixTranslate = new TranslateTransform();
            if (topLeft.X < Bounds.Left && bottomRight.X > Bounds.Right)
            {
                translate.X = 0;
                fixTranslate.X = 0;
                double scaleAmount = (bottomRight.X - topLeft.X) / Bounds.Width;
                scale.ScaleY = scaleAmount;
                scale.ScaleX = scaleAmount;
                outOfBounds = true;
            }
            else if (topLeft.X < Bounds.Left)
            {
                translate.X = 0;
                fixTranslate.X = preTopLeft.X;
                scale.CenterX = Bounds.Left;
                outOfBounds = true;
            }
            else if (bottomRight.X > Bounds.Right)
            {
                translate.X = 0;
                fixTranslate.X = -(Bounds.Right - preBottomRight.X - 1);
                scale.CenterX = Bounds.Right;
                outOfBounds = true;
            }
            if (topLeft.Y < Bounds.Top && bottomRight.Y > Bounds.Bottom)
            {
                translate.Y = 0;
                fixTranslate.Y = 0;
                double scaleAmount = (bottomRight.Y - topLeft.Y) / Bounds.Height;
                scale.ScaleX = scaleAmount;
                scale.ScaleY = scaleAmount;
                outOfBounds = true;
            }
            else if (topLeft.Y < Bounds.Top)
            {
                translate.Y = 0;
                fixTranslate.Y = preTopLeft.Y;
                scale.CenterY = Bounds.Top;
                outOfBounds = true;
            }
            else if (bottomRight.Y > Bounds.Bottom)
            {
                translate.Y = 0;
                fixTranslate.Y = -(Bounds.Bottom - preBottomRight.Y - 1);
                scale.CenterY = Bounds.Bottom;
                outOfBounds = true;
            }

            //If the view was out of bounds recalculate the composite matrix
            if (outOfBounds)
            {
                composite = new TransformGroup();
                composite.Children.Add(fixTranslate);
                composite.Children.Add(scale);
                composite.Children.Add(canvas.RenderTransform);
                composite.Children.Add(translate);
            }

            canvas.RenderTransform = new MatrixTransform { Matrix = composite.Value };
        }

        /// <summary>
        /// Zooms upon mousewheel interaction 
        /// </summary>
        private void UserControl_PointerWheelChanged(object sender, PointerRoutedEventArgs e)
        {
            Canvas canvas = xItemsControl.ItemsPanelRoot as Canvas;
            Debug.Assert(canvas != null);
            e.Handled = true;
            //Get mousepoint in canvas space 
            PointerPoint point = e.GetCurrentPoint(canvas);
            double scaleAmount = Math.Pow(1 + 0.15 * Math.Sign(point.Properties.MouseWheelDelta),
                Math.Abs(point.Properties.MouseWheelDelta) / 120.0f);
            scaleAmount = Math.Max(Math.Min(scaleAmount, 1.7f), 0.4f);
            CanvasScale *= (float)scaleAmount;
            Debug.Assert(canvas.RenderTransform != null);
            Point p = point.Position;
            //Create initial ScaleTransform 
            ScaleTransform scale = new ScaleTransform
            {
                CenterX = p.X,
                CenterY = p.Y,
                ScaleX = scaleAmount,
                ScaleY = scaleAmount
            };

            //Clamp scale
            if (CanvasScale > MaxScale)
            {
                CanvasScale = MaxScale;
                scale.ScaleX = MaxScale / CanvasScale;
                scale.ScaleY = MaxScale / CanvasScale;
            }
            if (CanvasScale < MinScale)
            {
                CanvasScale = MinScale;
                scale.ScaleX = MinScale / CanvasScale;
                scale.ScaleY = MinScale / CanvasScale;
            }

            //Create initial composite transform
            TransformGroup composite = new TransformGroup();
            composite.Children.Add(scale);
            composite.Children.Add(canvas.RenderTransform);

            GeneralTransform inverse = composite.Inverse;
            Debug.Assert(inverse != null);
            GeneralTransform renderInverse = canvas.RenderTransform.Inverse;
            Debug.Assert(inverse != null);
            Debug.Assert(renderInverse != null);
            Point topLeft = inverse.TransformPoint(new Point(0, 0));
            Point bottomRight = inverse.TransformPoint(new Point(Grid.ActualWidth, Grid.ActualHeight));
            Point preTopLeft = renderInverse.TransformPoint(new Point(0, 0));
            Point preBottomRight = renderInverse.TransformPoint(new Point(Grid.ActualWidth, Grid.ActualHeight));

            //Check if the zooming puts the view out of bounds of the canvas
            //Nullify scale or translate components accordingly 
            bool outOfBounds = false;
            //Create a canvas space translation to correct the translation if necessary
            TranslateTransform fixTranslate = new TranslateTransform();
            if (topLeft.X < Bounds.Left && bottomRight.X > Bounds.Right)
            {
                fixTranslate.X = 0;
                scaleAmount = (bottomRight.X - topLeft.X) / Bounds.Width;
                scale.ScaleY = scaleAmount;
                scale.ScaleX = scaleAmount;
                outOfBounds = true;
            }
            else if (topLeft.X < Bounds.Left)
            {
                fixTranslate.X = preTopLeft.X;
                scale.CenterX = Bounds.Left;
                outOfBounds = true;
            }
            else if (bottomRight.X > Bounds.Right)
            {
                fixTranslate.X = -(Bounds.Right - preBottomRight.X - 1);
                scale.CenterX = Bounds.Right;
                outOfBounds = true;
            }
            if (topLeft.Y < Bounds.Top && bottomRight.Y > Bounds.Bottom)
            {
                fixTranslate.Y = 0;
                scaleAmount = (bottomRight.Y - topLeft.Y) / Bounds.Height;
                scale.ScaleX = scaleAmount;
                scale.ScaleY = scaleAmount;
                outOfBounds = true;
            }
            else if (topLeft.Y < Bounds.Top)
            {
                fixTranslate.Y = preTopLeft.Y;
                scale.CenterY = Bounds.Top;
                outOfBounds = true;
            }
            else if (bottomRight.Y > Bounds.Bottom)
            {
                fixTranslate.Y = -(Bounds.Bottom - preBottomRight.Y - 1);
                scale.CenterY = Bounds.Bottom;
                outOfBounds = true;
            }

            //If the view was out of bounds recalculate the composite matrix
            if (outOfBounds)
            {
                composite = new TransformGroup();
                composite.Children.Add(fixTranslate);
                composite.Children.Add(scale);
                composite.Children.Add(canvas.RenderTransform);
            }
            canvas.RenderTransform = new MatrixTransform { Matrix = composite.Value };
        }

        /// <summary>
        /// Make translation inertia slow down faster
        /// </summary>
        private void UserControl_ManipulationInertiaStarting(object sender, ManipulationInertiaStartingRoutedEventArgs e)
        {
            e.TranslationBehavior.DesiredDeceleration = 0.01;
        }

        /// <summary>
        /// Make sure the canvas is still in bounds after resize
        /// </summary>
        private void UserControl_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            TranslateTransform translate = new TranslateTransform();

            //Calculate bottomRight corner of screen in canvas space before and after resize 
            Debug.Assert(DocumentViewContainerGrid.RenderTransform != null);
            Debug.Assert(DocumentViewContainerGrid.RenderTransform.Inverse != null);
            Point oldBottomRight =
                DocumentViewContainerGrid.RenderTransform.Inverse.TransformPoint(new Point(e.PreviousSize.Width, e.PreviousSize.Height));
            Point bottomRight =
                DocumentViewContainerGrid.RenderTransform.Inverse.TransformPoint(new Point(e.NewSize.Width, e.NewSize.Height));

            //Check if new bottom right is out of bounds
            bool outOfBounds = false;
            if (bottomRight.X > Grid.ActualWidth - 1)
            {
                translate.X = -(oldBottomRight.X - bottomRight.X);
                outOfBounds = true;
            }
            if (bottomRight.Y > Grid.ActualHeight - 1)
            {
                translate.Y = -(oldBottomRight.Y - bottomRight.Y);
                outOfBounds = true;
            }
            //If it is out of bounds, translate so that is is in bounds
            if (outOfBounds)
            {
                TransformGroup composite = new TransformGroup();
                composite.Children.Add(translate);
                composite.Children.Add(DocumentViewContainerGrid.RenderTransform);
                DocumentViewContainerGrid.RenderTransform = new MatrixTransform { Matrix = composite.Value };
            }

            Clip = new RectangleGeometry { Rect = new Rect(0, 0, e.NewSize.Width, e.NewSize.Height) };
        }

        #region Operator connection stuff
        /// <summary>
        /// Helper class to detect cycles 
        /// </summary>
        private Graph<string> _graph = new Graph<string>();
        /// <summary>
        /// Line to create and display connection lines between OperationView fields and Document fields 
        /// </summary>
        private Path _connectionLine;

        private MultiBinding<PathFigureCollection> _lineBinding;
        private BezierConverter _converter;

        /// <summary>
        /// IOReference (containing reference to fields) being referred to when creating the visual connection between fields 
        /// </summary>
        private OperatorView.IOReference _currReference;

        private Dictionary<ReferenceFieldModelController, Path> _lineDict = new Dictionary<ReferenceFieldModelController, Path>();

        /// <summary>
        /// HashSet of current pointers in use so that the OperatorView does not respond to multiple inputs 
        /// </summary>
        private HashSet<uint> _currentPointers = new HashSet<uint>();

        /// <summary>
        /// Dictionary that maps DocumentViews on maincanvas to its DocumentID 
        /// </summary>
        //private Dictionary<string, DocumentView> _documentViews = new Dictionary<string, DocumentView>();

        private class BezierConverter : IValueConverter
        {
            public BezierConverter(FrameworkElement element1, FrameworkElement element2, FrameworkElement toElement)
            {
                Element1 = element1;
                Element2 = element2;
                ToElement = toElement;
                _figure = new PathFigure();
                _bezier = new BezierSegment();
                _figure.Segments.Add(_bezier);
                _col.Add(_figure);
            }

            public FrameworkElement Element1 { get; set; }
            public FrameworkElement Element2 { get; set; }

            public FrameworkElement ToElement { get; set; }

            public Point Pos2 { get; set; }

            private PathFigureCollection _col = new PathFigureCollection();
            private PathFigure _figure;
            private BezierSegment _bezier;

            public object Convert(object value, Type targetType, object parameter, string language)
            {
                var pos1 = Element1.TransformToVisual(ToElement)
                    .TransformPoint(new Point(Element1.ActualWidth / 2, Element1.ActualHeight / 2));
                var pos2 = Element2?.TransformToVisual(ToElement)
                               .TransformPoint(new Point(Element2.ActualWidth / 2, Element2.ActualHeight / 2)) ?? Pos2;

                double offset = Math.Abs((pos1.X - pos2.X) / 3);
                if (pos1.X < pos2.X)
                {
                    _figure.StartPoint = new Point(pos1.X + Element1.ActualWidth / 2, pos1.Y);
                    _bezier.Point1 = new Point(pos1.X + offset, pos1.Y);
                    _bezier.Point2 = new Point(pos2.X - offset, pos2.Y);
                    _bezier.Point3 = new Point(pos2.X - (Element2?.ActualWidth / 2 ?? 0), pos2.Y);
                }
                else
                {
                    _figure.StartPoint = new Point(pos1.X - Element1.ActualWidth / 2, pos1.Y);
                    _bezier.Point1 = new Point(pos1.X - offset, pos1.Y);
                    _bezier.Point2 = new Point(pos2.X + offset, pos2.Y);
                    _bezier.Point3 = new Point(pos2.X + (Element2?.ActualWidth / 2 ?? 0), pos2.Y);
                }

                return _col;
            }
            

            public object ConvertBack(object value, Type targetType, object parameter, string language)
            {
                throw new NotImplementedException();
            }
        }

        private class VisibilityConverter : IValueConverter
        {
            public object Convert(object value, Type targetType, object parameter, string language)
            {
                bool isEditorMode = (bool)value;
                return isEditorMode ? Visibility.Visible : Visibility.Collapsed;
            }

            public object ConvertBack(object value, Type targetType, object parameter, string language)
            {
                throw new NotImplementedException();
            }
        }
        public void StartDrag(OperatorView.IOReference ioReference)
        {
            if (!ViewModel.IsEditorMode)
            {
                return;
            }
            if (_currentPointers.Contains(ioReference.PointerArgs.Pointer.PointerId))
            {
                return;
            }
            _currentPointers.Add(ioReference.PointerArgs.Pointer.PointerId);

            _currReference = ioReference;
            
            _connectionLine = new Path
            {
                StrokeThickness = 5,
                Stroke = new SolidColorBrush(Colors.Orange),
                IsHitTestVisible = false,
                //CompositeMode =
                //    ElementCompositeMode.SourceOver //TODO Bug in xaml, shouldn't need this line when the bug is fixed 
                //                                    //(https://social.msdn.microsoft.com/Forums/sqlserver/en-US/d24e2dc7-78cf-4eed-abfc-ee4d789ba964/windows-10-creators-update-uielement-clipping-issue?forum=wpdevelop)
            };
            Canvas.SetZIndex(_connectionLine, -1);
            _converter = new BezierConverter(ioReference.FrameworkElement, null, FreeformCanvas);
            _converter.Pos2 = ioReference.PointerArgs.GetCurrentPoint(FreeformCanvas).Position;
            _lineBinding =
                new MultiBinding<PathFigureCollection>(_converter, null);
            _lineBinding.AddBinding(ioReference.ContainerView, FrameworkElement.RenderTransformProperty);
            _lineBinding.AddBinding(ioReference.ContainerView, FrameworkElement.WidthProperty);
            _lineBinding.AddBinding(ioReference.ContainerView, FrameworkElement.HeightProperty);
            Binding lineBinding = new Binding
            {
                Source = _lineBinding,
                Path = new PropertyPath("Property")
            };
            PathGeometry pathGeo = new PathGeometry();
            BindingOperations.SetBinding(pathGeo, PathGeometry.FiguresProperty, lineBinding);
            _connectionLine.Data = pathGeo;

            Binding visibilityBinding = new Binding
            {
                Source = ViewModel,
                Path = new PropertyPath("IsEditorMode"),
                Converter = new VisibilityConverter()
            };
            _connectionLine.SetBinding(UIElement.VisibilityProperty, visibilityBinding);
            
            FreeformCanvas.Children.Add(_connectionLine);

            if (!ioReference.IsOutput)
            {
                CheckLinePresence(ioReference.ReferenceFieldModel);
                _lineDict.Add(ioReference.ReferenceFieldModel, _connectionLine);
            }
        }

        public void CancelDrag(Pointer p)
        {
            _currentPointers.Remove(p.PointerId);
            UndoLine();
        }

        public void EndDrag(OperatorView.IOReference ioReference)
        {
            if (!ViewModel.IsEditorMode)
            {
                return;
            }
            _currentPointers.Remove(ioReference.PointerArgs.Pointer.PointerId);
            if (_connectionLine == null) return;

            if (_currReference.IsOutput == ioReference.IsOutput)
            {
                UndoLine();
                return;
            }
            if (_currReference.IsOutput)
            {
                //_graph.AddEdge(ContentController.DereferenceToRootFieldModel(_currReference.ReferenceFieldModel, (DataContext as CollectionViewModel).DocContextList).GetId(), 
                    //ContentController.DereferenceToRootFieldModel(ioReference.ReferenceFieldModel, (DataContext as CollectionViewModel).DocContextList).GetId());
            }
            else
            {
                //_graph.AddEdge(ContentController.DereferenceToRootFieldModel(ioReference.ReferenceFieldModel).GetId(), ContentController.DereferenceToRootFieldModel(_currReference.ReferenceFieldModel).GetId());
            }
            if (_graph.IsCyclic())
            {
                if (_currReference.IsOutput)
                {
                    //_graph.RemoveEdge(ContentController.DereferenceToRootFieldModel(_currReference.ReferenceFieldModel).GetId(), ContentController.DereferenceToRootFieldModel(ioReference.ReferenceFieldModel).GetId());
                }
                else
                {
                    //_graph.RemoveEdge(ContentController.DereferenceToRootFieldModel(ioReference.ReferenceFieldModel).GetId(), ContentController.DereferenceToRootFieldModel(_currReference.ReferenceFieldModel).GetId());
                }
                CancelDrag(ioReference.PointerArgs.Pointer);
                Debug.WriteLine("Cycle detected");
                return;
            }


            _converter.Element2 = ioReference.FrameworkElement;
            _lineBinding.AddBinding(ioReference.ContainerView, FrameworkElement.RenderTransformProperty);
            _lineBinding.AddBinding(ioReference.ContainerView, FrameworkElement.WidthProperty);
            _lineBinding.AddBinding(ioReference.ContainerView, FrameworkElement.HeightProperty);

            if (ioReference.IsOutput)
            {
                    ContentController.GetController<DocumentController>(_currReference.ReferenceFieldModel.DocId)
                        .AddInputReference(_currReference.ReferenceFieldModel.FieldKey,
                            ioReference.ReferenceFieldModel);
            }
            else
            {
                var contextList = (DataContext as CollectionViewModel).DocContextList;
                var refDocId = ContentController.MapDocumentInstanceReference(ioReference.ReferenceFieldModel.DocId, contextList);
                try
                {
                    ContentController.GetController<DocumentController>(refDocId)
                        .AddInputReference(ioReference.ReferenceFieldModel.FieldKey, _currReference.ReferenceFieldModel,
                            contextList);
                }
                catch (ArgumentException)
                {
                    CancelDrag(ioReference.PointerArgs.Pointer);
                }
            }

            if (!ioReference.IsOutput && _connectionLine != null)
            {
                CheckLinePresence(ioReference.ReferenceFieldModel);
                _lineDict.Add(ioReference.ReferenceFieldModel, _connectionLine);
                _connectionLine = null;
            }
        }

        /// <summary>
        /// Helper function that checks if connection line is already present for input ellipse; if so, destroy that line and create a new one  
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        private void CheckLinePresence(ReferenceFieldModelController model)
        {
            if (_lineDict.ContainsKey(model))
            {
                Path line = _lineDict[model];
                FreeformCanvas.Children.Remove(line);
                _lineDict.Remove(model);
            }
        }

        private void UndoLine()
        {
            FreeformCanvas.Children.Remove(_connectionLine);
            _connectionLine = null;
            _currReference = null;
        }

        #endregion

        private void FreeformGrid_OnPointerMoved(object sender, PointerRoutedEventArgs e)
        {
            if (_connectionLine != null)
            {
                Point pos = e.GetCurrentPoint(FreeformCanvas).Position;
                _converter.Pos2 = pos;
                _lineBinding.ForceUpdate();
            }
        }

        private void FreeformGrid_OnPointerReleased(object sender, PointerRoutedEventArgs e)
        {
            if (_currReference != null)
            {
                CancelDrag(e.Pointer);

                //DocumentView view = new DocumentView();
                //DocumentViewModel viewModel = new DocumentViewModel();
                //view.DataContext = viewModel;
                //FreeformView.MainFreeformView.Canvas.Children.Add(view);

            }
        }

        private void ConnectionEllipse_OnManipulationStarted(object sender, ManipulationStartedRoutedEventArgs e)
        {
            e.Complete();
        }

        private void ConnectionEllipse_OnPointerPressed(object sender, PointerRoutedEventArgs e)
        {
            string docId = (ViewModel.ParentDocument.DataContext as DocumentViewModel).DocumentController.GetId();
            Ellipse el = sender as Ellipse;
            Key outputKey = DocumentCollectionFieldModelController.CollectionKey;
            OperatorView.IOReference ioRef = new OperatorView.IOReference(new ReferenceFieldModelController(docId, outputKey), true, e, el, ViewModel.ParentDocument);
            CollectionView view = ViewModel.ParentCollection;
            view?.StartDrag(ioRef);
        }

        private void ConnectionEllipse_OnPointerReleased(object sender, PointerRoutedEventArgs e)
        {
            string docId = (ViewModel.ParentDocument.DataContext as DocumentViewModel).DocumentController.GetId();
            Ellipse el = sender as Ellipse;
            Key outputKey = DocumentCollectionFieldModelController.CollectionKey;
            OperatorView.IOReference ioRef = new OperatorView.IOReference(new ReferenceFieldModelController(docId, outputKey), false, e, el, ViewModel.ParentDocument);
            CollectionView view = ViewModel.ParentCollection;
            view?.EndDrag(ioRef);
        }

        private void xGridView_OnDragItemsStarting(object sender, DragItemsStartingEventArgs e)
        {
            MainPage.Instance.MainDocView.DragOver -= MainPage.Instance.XCanvas_DragOver_1;
            ItemsCarrier carrier = ItemsCarrier.GetInstance();
            carrier.Source = this;
            foreach(var item in e.Items)
                carrier.Payload.Add(item as DocumentViewModel);
            e.Data.RequestedOperation = DataPackageOperation.Move;
        }

        private void xGridView_OnDragItemsCompleted(ListViewBase sender, DragItemsCompletedEventArgs args)
        {
            if (args.DropResult == DataPackageOperation.Move && !KeepItemsOnMove)
            {
                ChangeDocuments(ItemsCarrier.GetInstance().Payload, false);
                RefreshItemsBinding();
            }
            KeepItemsOnMove = true;
            var carrier = ItemsCarrier.GetInstance();
            carrier.Payload.Clear();
            carrier.Source = null;
            carrier.Destination = null;
            carrier.Translate = new Point();
            MainPage.Instance.MainDocView.DragOver += MainPage.Instance.XCanvas_DragOver_1;
        }

        private void ChangeDocuments(List<DocumentViewModel> docViewModels, bool add)
        {
            var docControllers = docViewModels.Select(item => item.DocumentController);
            var parentDoc = (ViewModel.ParentDocument.ViewModel)?.DocumentController;
            var controller = ContentController.GetController<DocumentCollectionFieldModelController>(ViewModel.CollectionModel.DocumentCollectionFieldModel.Id);
            if (controller != null)
                foreach (var item in docControllers)
                    if (add) controller.AddDocument(item);
                    else controller.RemoveDocument(item);
        }

        private void CollectionGrid_DragOver(object sender, DragEventArgs e)
        {
            e.Handled = true;
            e.AcceptedOperation = DataPackageOperation.Move;
        }

        private void CollectionGrid_Drop(object sender, DragEventArgs e)
        {
            e.Handled = true;
            RefreshItemsBinding();
            ItemsCarrier.GetInstance().Destination = this;
            ItemsCarrier.GetInstance().Source.KeepItemsOnMove = false;
            ItemsCarrier.GetInstance().Translate = e.GetPosition(xItemsControl.ItemsPanelRoot);
            ChangeDocuments(ItemsCarrier.GetInstance().Payload, true);
        }

        private void RefreshItemsBinding()
        {
            if (ViewModel.GridViewVisibility == Visibility.Visible)
            {
                xGridView.ItemsSource = null;
                xGridView.ItemsSource = ViewModel.DataBindingSource;
            }
            else if (ViewModel.ListViewVisibility == Visibility.Visible)
            {
                HListView.ItemsSource = null;
                HListView.ItemsSource = ViewModel.DataBindingSource;
            }
        }

        private void UIElement_OnPointerPressed(object sender, PointerRoutedEventArgs e)
        {
        }
    }
}
