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
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Shapes;
using Windows.Foundation.Collections;
using DashShared;
using DocumentMenu;
using static Dash.CourtesyDocuments;

// The User Control item template is documented at http://go.microsoft.com/fwlink/?LinkId=234236

namespace Dash
{
    public sealed partial class CollectionView : UserControl
    {
        public double CanvasScale { get; set; } = 1;
        public int MaxZ { get; set; } = 0;
        public const float MaxScale = 10;
        public const float MinScale = 0.001f;
        public Rect Bounds = new Rect(double.NegativeInfinity, double.NegativeInfinity, double.PositiveInfinity, double.PositiveInfinity);

        // whether the user can draw links currently or not
        public bool CanLink
        {
            get
            {
                if (CurrentView is CollectionFreeformView)
                    return (CurrentView as CollectionFreeformView).CanLink;
                else
                    return false;
            }
            set
            {
                if (CurrentView is CollectionFreeformView)
                    (CurrentView as CollectionFreeformView).CanLink = value;
            }
        }

        public PointerRoutedEventArgs PointerArgs
        {
            get
            {
                if (CurrentView is CollectionFreeformView)
                    return (CurrentView as CollectionFreeformView).PointerArgs;
                else
                    return null;
            }
            set
            {
                if (CurrentView is CollectionFreeformView)
                    (CurrentView as CollectionFreeformView).PointerArgs = value;
            }

        }

        //i think this belong elsewhere
        public static Graph<string> Graph = new Graph<string>();

        public UserControl CurrentView { get; set; }
        private OverlayMenu _colMenu = null;

        public CollectionViewModel ViewModel;

        private CollectionView _activeCollection;

        public CollectionView ParentCollection { get; set; }
        public DocumentView ParentDocument { get; set; }


        public CollectionView(CollectionViewModel vm)
        {
            this.InitializeComponent();
            DataContext = ViewModel = vm;
            vm.CollectionFieldModelController.FieldModelUpdated += DocFieldCtrler_FieldModelUpdatedEvent;
            CurrentView = new CollectionFreeformView { DataContext = ViewModel };
            xContentControl.Content = CurrentView;
            SetEventHandlers();
            CanLink = false;
        }
        private void DocFieldCtrler_FieldModelUpdatedEvent(FieldModelController sender, Context c)
        {
            DataContext = ViewModel;
        }
        private void SetEventHandlers()
        {
            Loaded += CollectionView_Loaded;
            ViewModel.DataBindingSource.CollectionChanged += DataBindingSource_CollectionChanged;
            DocumentViewContainerGrid.DragOver += CollectionGrid_DragOver;
            DocumentViewContainerGrid.Drop += CollectionGrid_Drop;
            ConnectionEllipse.ManipulationStarted += ConnectionEllipse_OnManipulationStarted;
            Tapped += CollectionView_Tapped;
        }

        private void CollectionView_Loaded(object sender, RoutedEventArgs e)
        {
            ParentDocument = this.GetFirstAncestorOfType<DocumentView>();
            ParentCollection = this.GetFirstAncestorOfType<CollectionView>();
            ParentDocument.HasCollection = true;
            //Temporary graphical hax. to be removed when collectionview menu moved to its document.
            ParentDocument.XGrid.Background = new SolidColorBrush(Colors.Transparent);
            ParentDocument.xBorder.Margin = new Thickness(ParentDocument.xBorder.Margin.Left + 5,
                                                ParentDocument.xBorder.Margin.Top + 5,
                                                ParentDocument.xBorder.Margin.Right,
                                                ParentDocument.xBorder.Margin.Bottom);

            if (ParentDocument != MainPage.Instance.MainDocView)
            {
                ParentDocument.SizeChanged += (ss, ee) =>
                {
                    Height = ee.NewSize.Height;
                    Width = ee.NewSize.Width;
                };
                SetEnabled(false);
            }
            else
            {
                OpenMenu();
                SetEnabled(true);
                xOuterGrid.BorderThickness = new Thickness(0);
            }
        }

        /// <summary>
        /// Update document view model representatino of items on internal collection change.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void DataBindingSource_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            if (e.Action == NotifyCollectionChangedAction.Add)
            {
                foreach (var eNewItem in e.NewItems)
                {
                    var docVM = eNewItem as DocumentViewModel;
                    Debug.Assert(docVM != null);
                    var ofm =
                        docVM.DocumentController.GetDereferencedField(OperatorDocumentModel.OperatorKey, null) as
                            OperatorFieldModelController;
                    if (ofm != null)
                    {
                        foreach (KeyValuePair<Key, TypeInfo> inputKey in ofm.Inputs)
                        {
                            foreach (KeyValuePair<Key, TypeInfo> outputKey in ofm.Outputs)
                            {
                                ReferenceFieldModelController irfm =
                                    new DocumentReferenceController(docVM.DocumentController.GetId(), inputKey.Key);
                                ReferenceFieldModelController orfm =
                                    new DocumentReferenceController(docVM.DocumentController.GetId(), outputKey.Key);
                                //Graph.AddEdge(irfm.DereferenceToRoot().GetId(),
                                //    orfm.DereferenceToRoot().GetId());
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
                        docVM.DocumentController.GetDereferencedField(OperatorDocumentModel.OperatorKey, null) as
                            OperatorFieldModelController;
                    if (ofm != null)
                    {
                        foreach (KeyValuePair<Key, TypeInfo> inputKey in ofm.Inputs)
                        {
                            foreach (KeyValuePair<Key, TypeInfo> outputKey in ofm.Outputs)
                            {
                                ReferenceFieldModelController irfm =
                                    new DocumentReferenceController(docVM.DocumentController.GetId(), inputKey.Key);
                                ReferenceFieldModelController orfm =
                                    new DocumentReferenceController(docVM.DocumentController.GetId(), outputKey.Key);
                                Graph.RemoveEdge(irfm.DereferenceToRoot(null).GetId(),
                                    orfm.DereferenceToRoot(null).GetId());
                            }
                        }
                    }
                }
            }
        }

        private void ItemsControl_ItemsChanged(IObservableVector<object> sender, IVectorChangedEventArgs e)
        {
            //RefreshItemsBinding();
            if (e.CollectionChange == CollectionChange.ItemInserted)
            {
                var docVM = sender[(int)e.Index] as DocumentViewModel;
                Debug.Assert(docVM != null);
                OperatorFieldModelController ofm = docVM.DocumentController.GetDereferencedField(OperatorDocumentModel.OperatorKey, null) as OperatorFieldModelController;
                if (ofm != null)
                {
                    foreach (KeyValuePair<Key, TypeInfo> inputKey in ofm.Inputs)
                    {
                        foreach (KeyValuePair<Key, TypeInfo> outputKey in ofm.Outputs)
                        {
                            ReferenceFieldModelController irfm = new DocumentReferenceController(docVM.DocumentController.GetId(), inputKey.Key);
                            ReferenceFieldModelController orfm = new DocumentReferenceController(docVM.DocumentController.GetId(), outputKey.Key);
                            Graph.AddEdge(irfm.DereferenceToRoot(null).GetId(), orfm.DereferenceToRoot(null).GetId());
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

        //private void Grid_OnManipulationStarted(object sender, ManipulationStartedRoutedEventArgs e)
        //{
        //    if (e.Container is ScrollBar || e.Container is ScrollViewer)
        //    {
        //        e.Complete();
        //        e.Handled = true;
        //    }
        //}

        private void DocumentViewContainerGrid_OnSizeChanged(object sender, SizeChangedEventArgs e)
        {
            Thickness border = DocumentViewContainerGrid.BorderThickness;
            ClipRect.Rect = new Rect(border.Left, border.Top, e.NewSize.Width - border.Left * 2, e.NewSize.Height - border.Top * 2);
        }

        private void ClampScale(ScaleTransform scale)
        {
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
        }

        /// <summary>
        /// Pans and zooms upon touch manipulation 
        /// </summary>
        private void UserControl_ManipulationDelta(object sender, ManipulationDeltaRoutedEventArgs e)
        {
            if (!(CurrentView is CollectionFreeformView) || !CurrentView.IsHitTestVisible) return;
            Canvas canvas = (CurrentView as CollectionFreeformView).xItemsControl.ItemsPanelRoot as Canvas;
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
            ClampScale(scale);


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
            if (!(CurrentView is CollectionFreeformView) || !CurrentView.IsHitTestVisible) return;
            Canvas canvas = (CurrentView as CollectionFreeformView).xItemsControl.ItemsPanelRoot as Canvas;
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
            ClampScale(scale);

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

        /// <summary>
        /// IOReference (containing reference to fields) being referred to when creating the visual connection between fields 
        /// </summary>
        private OperatorView.IOReference _currReference;

        private Dictionary<ReferenceFieldModelController, Path> _lineDict = new Dictionary<ReferenceFieldModelController, Path>();

        /// <summary>
        /// HashSet of current pointers in use so that the OperatorView does not respond to multiple inputs 
        /// </summary>
        private HashSet<uint> _currentPointers = new HashSet<uint>();

        #endregion

        private void ConnectionEllipse_OnManipulationStarted(object sender, ManipulationStartedRoutedEventArgs e)
        {
            e.Complete();
        }

        private void ConnectionEllipse_OnPointerPressed(object sender, PointerRoutedEventArgs e)
        {
            string docId = (ParentDocument.DataContext as DocumentViewModel).DocumentController.GetId();
            Ellipse el = sender as Ellipse;
            Key outputKey = DocumentCollectionFieldModelController.CollectionKey;
            OperatorView.IOReference ioRef = new OperatorView.IOReference(new DocumentReferenceController(docId, outputKey), true, e, el, ParentDocument);
            CollectionView view = ParentCollection;
            (view.CurrentView as CollectionFreeformView)?.StartDrag(ioRef);
        }

        private void ConnectionEllipse_OnPointerReleased(object sender, PointerRoutedEventArgs e)
        {
            string docId = (ParentDocument.DataContext as DocumentViewModel).DocumentController.GetId();
            Ellipse el = sender as Ellipse;
            Key outputKey = DocumentCollectionFieldModelController.CollectionKey;
            OperatorView.IOReference ioRef = new OperatorView.IOReference(new DocumentReferenceController(docId, outputKey), false, e, el, ParentDocument);
            CollectionView view = ParentCollection;
            (view.CurrentView as CollectionFreeformView)?.EndDrag(ioRef);
        }

        public void xGridView_OnDragItemsStarting(object sender, DragItemsStartingEventArgs e)
        {
            MainPage.Instance.MainDocView.DragOver -= MainPage.Instance.xCanvas_DragOver;
            ItemsCarrier carrier = ItemsCarrier.GetInstance();
            carrier.Source = ViewModel;
            foreach (var item in e.Items)
                carrier.Payload.Add(item as DocumentViewModel);
            e.Data.RequestedOperation = DataPackageOperation.Move;
        }

        public void xGridView_OnDragItemsCompleted(ListViewBase sender, DragItemsCompletedEventArgs args)
        {
            if (args.DropResult == DataPackageOperation.Move && !ViewModel.KeepItemsOnMove)
                ChangeDocuments(ItemsCarrier.GetInstance().Payload, false);
            //RefreshItemsBinding();
            ViewModel.KeepItemsOnMove = true;
            var carrier = ItemsCarrier.GetInstance();
            carrier.Payload.Clear();
            carrier.Source = null;
            carrier.Destination = null;
            carrier.Translate = new Point();
            MainPage.Instance.MainDocView.DragOver += MainPage.Instance.xCanvas_DragOver;
        }

        private void ChangeDocuments(List<DocumentViewModel> docViewModels, bool add)
        {
            var docControllers = docViewModels.Select(item => item.DocumentController);
            var controller = ViewModel.CollectionFieldModelController;
            if (controller != null)
                foreach (var item in docControllers)
                    if (add) controller.AddDocument(item);
                    else controller.RemoveDocument(item);
        }

        private void CollectionGrid_DragOver(object sender, DragEventArgs e)
        {
            e.Handled = true;
            if(ItemsCarrier.GetInstance().Source != ViewModel)
                e.AcceptedOperation = DataPackageOperation.Move;
        }

        private async void CollectionGrid_Drop(object sender, DragEventArgs e)
        {
            e.Handled = true;
            RefreshItemsBinding();
            foreach (var s in e.DataView.AvailableFormats)
                Debug.Write("" + s);
            if (e.DataView.Contains(StandardDataFormats.StorageItems))
            {
                var items = await e.DataView.GetStorageItemsAsync();
                if (items.Count > 0)
                {
                    foreach (var i in items)
                        if (i is Windows.Storage.StorageFile)
                        {
                            var storageFile = i as Windows.Storage.StorageFile;
                            if (storageFile.ContentType.Contains("image"))
                            {
                                var bitmapImage = new Windows.UI.Xaml.Media.Imaging.BitmapImage();
                                bitmapImage.SetSource(await storageFile.OpenAsync(Windows.Storage.FileAccessMode.Read));
                                var doc = new AnnotatedImage(new Uri(i.Path), i.Name);
                                (DataContext as CollectionViewModel).CollectionFieldModelController.AddDocument(doc.Document);
                            }
                        }
                }
            }
            else if (ItemsCarrier.GetInstance().Source != null)
            {
                //var text = await e.DataView.GetTextAsync(StandardDataFormats.Html).AsTask();
                ItemsCarrier.GetInstance().Destination = ViewModel;
                ItemsCarrier.GetInstance().Source.KeepItemsOnMove = false;
                ItemsCarrier.GetInstance().Translate = e.GetPosition(DocumentViewContainerGrid);
                ChangeDocuments(ItemsCarrier.GetInstance().Payload, true);
            }
        }

        private void RefreshItemsBinding()
        {
            var gridView = CurrentView as CollectionGridView;
            var listView = CurrentView as CollectionListView;
            if (gridView != null)
            {
                gridView.xGridView.ItemsSource = null;
                gridView.xGridView.ItemsSource = ViewModel.DataBindingSource;
            }
            else if (listView != null)
            {
                listView.HListView.ItemsSource = null;
                listView.HListView.ItemsSource = ViewModel.DataBindingSource;
            }
        }



        #region Menu
        /// <summary>
        /// Changes the view to the Freeform by making that Freeform visible in the CollectionView.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void SetFreeformView()
        {
            if (CurrentView is CollectionFreeformView) return;
            CurrentView = new CollectionFreeformView { DataContext = ViewModel };
            (CurrentView as CollectionFreeformView).xItemsControl.Items.VectorChanged += ItemsControl_ItemsChanged;
            xContentControl.Content = CurrentView;
        }
        /// <summary>
        /// Changes the view to the ListView by making that Grid visible in the CollectionView.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void SetListView()
        {
            if (CurrentView is CollectionListView) return;
            CurrentView = new CollectionListView(this) { DataContext = ViewModel };
            (CurrentView as CollectionListView).HListView.SelectionChanged += ViewModel.SelectionChanged;
            xContentControl.Content = CurrentView;
        }
        /// <summary>
        /// Changes the view to the GridView by making that Grid visible in the CollectionView.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void SetGridView()
        {
            if (CurrentView is CollectionGridView) return;
            CurrentView = new CollectionGridView(this) { DataContext = ViewModel };
            (CurrentView as CollectionGridView).xGridView.SelectionChanged += ViewModel.SelectionChanged;
            xContentControl.Content = CurrentView;
        }

        private void MakeSelectionModeMultiple()
        {
            ViewModel.ItemSelectionMode = ListViewSelectionMode.Multiple;
            ViewModel.CanDragItems = true;
            _colMenu.GoToDocumentMenu();
        }

        private void CloseMenu()
        {
            var panel = _colMenu.Parent as Panel;
            if (panel != null) panel.Children.Remove(_colMenu);
            _colMenu = null;
            xMenuColumn.Width = new GridLength(0);
            ParentDocument.ViewModel.Width -= 50;
            //Temporary graphical hax. to be removed when collectionview menu moved to its document.
            ParentDocument.xBorder.Margin = new Thickness(ParentDocument.xBorder.Margin.Left - 50,
                                                            ParentDocument.xBorder.Margin.Top,
                                                            ParentDocument.xBorder.Margin.Right,
                                                            ParentDocument.xBorder.Margin.Bottom);
        }

        private void SelectAllItems()
        {
            if (CurrentView is CollectionGridView)
            {
                var gridView = (CurrentView as CollectionGridView).xGridView;
                if (gridView.SelectedItems.Count != ViewModel.DataBindingSource.Count)
                    gridView.SelectAll();
                else gridView.SelectedItems.Clear();
            }
            if (CurrentView is CollectionListView)
            {
                var listView = (CurrentView as CollectionListView).HListView;
                if (listView.SelectedItems.Count != ViewModel.DataBindingSource.Count)
                    listView.SelectAll();
                else
                    listView.SelectedItems.Clear();
            }
        }

        private void MakeSelectionModeSingle()
        {
            ViewModel.ItemSelectionMode = ListViewSelectionMode.Single;
            _colMenu.BackToCollectionMenu();
        }

        private void DeleteSelection()
        {
            ViewModel.DeleteSelected_Tapped(null, null);
        }

        private void DeleteCollection()
        {
            ParentDocument.DeleteDocument();
        }

        private void OpenMenu()
        {
            var multipleSelection = new Action(MakeSelectionModeMultiple);
            var deleteSelection = new Action(DeleteSelection);
            var singleSelection = new Action(MakeSelectionModeSingle);
            var selectAll = new Action(SelectAllItems);
            var setGrid = new Action(SetGridView);
            var setList = new Action(SetListView);
            var setFreeform = new Action(SetFreeformView);
            var deleteCollection = new Action(DeleteCollection);

            var collectionButtons = new List<MenuButton>()
            {
                new MenuButton(Symbol.TouchPointer, "Select", Colors.SteelBlue, multipleSelection)
                {
                    RotateOnTap = true
                },
                new MenuButton(Symbol.ViewAll, "Grid", Colors.SteelBlue, setGrid),
                new MenuButton(Symbol.List, "List", Colors.SteelBlue, setList),
                new MenuButton(Symbol.View, "Freeform", Colors.SteelBlue, setFreeform),
                new MenuButton(Symbol.Camera, "ScrCap", Colors.SteelBlue, new Action(ScreenCap)),
                new MenuButton(Symbol.Page, "Json", Colors.SteelBlue, new Action(GetJson))
            };

            if (ParentDocument != MainPage.Instance.MainDocView)
                collectionButtons.Add(new MenuButton(Symbol.Delete, "Delete", Colors.SteelBlue, deleteCollection));

            var documentButtons = new List<MenuButton>()
            {
                new MenuButton(Symbol.Back, "Back", Colors.SteelBlue, singleSelection)
                {
                    RotateOnTap = true
                },
                new MenuButton(Symbol.Edit, "Interface", Colors.SteelBlue, null),
                new MenuButton(Symbol.SelectAll, "All", Colors.SteelBlue, selectAll),
                new MenuButton(Symbol.Delete, "Delete", Colors.SteelBlue, deleteSelection),
            };
            _colMenu = new OverlayMenu(collectionButtons, documentButtons);
            xMenuCanvas.Children.Add(_colMenu);
            xMenuColumn.Width = new GridLength(50);
            ParentDocument.ViewModel.Width += 50;
            //Temporary graphical hax. to be removed when collectionview menu moved to its document.
            ParentDocument.xBorder.Margin = new Thickness(ParentDocument.xBorder.Margin.Left + 50,
                                                            ParentDocument.xBorder.Margin.Top,
                                                            ParentDocument.xBorder.Margin.Right,
                                                            ParentDocument.xBorder.Margin.Bottom);
        }


        #endregion

        public void GetJson()
        {
            throw new NotImplementedException("The document view model does not have a context any more");
            //Util.ExportAsJson(ViewModel.DocumentContext.DocContextList); 
        }
        public void ScreenCap()
        {
            Util.ExportAsImage(xOuterGrid);
        }

        #region Collection Activation

        public void CollectionView_Tapped(object sender, TappedRoutedEventArgs e)
        {
            if (ParentDocument != MainPage.Instance.MainDocView)
            {
                if (_colMenu != null)
                {
                    CloseMenu();
                    SetEnabled(false);
                    ViewModel.ItemSelectionMode = ListViewSelectionMode.None;
                    ViewModel.CanDragItems = false;
                }
                else
                {
                    OpenMenu();
                    ParentCollection.SetActiveCollection(this);
                }
            }
            SetActiveCollection(null);
            e.Handled = true;
        }

        public void SetEnabled(bool enabled)
        {
            if (enabled)
            {
                CurrentView.IsHitTestVisible = true;
            }
            else
            {
                CurrentView.IsHitTestVisible = false;
                xOuterGrid.BorderBrush = new SolidColorBrush(Colors.Transparent);
                ViewModel.ItemSelectionMode = ListViewSelectionMode.None;
                ViewModel.CanDragItems = false;
                if (_colMenu != null)
                    CloseMenu();
                foreach (var dvm in ViewModel.DataBindingSource)
                {
                    dvm.CloseMenu();
                }
            }
        }

        //private void CollectionView_OnTapped(object sender, TappedRoutedEventArgs e)
        //{
        //    if (ParentCollection?.GetActiveCollection() != this)
        //        ParentCollection?.SetActiveCollection(this);
        //    SetActiveCollection(null);
        //    e.Handled = true;
        //}

        public void SetActiveCollection(CollectionView collection)
        {
            if (_activeCollection != null && _activeCollection != collection)
            {
                _activeCollection.SetEnabled(false);
                if (_activeCollection.GetActiveCollection() != null)
                {
                    _activeCollection.SetActiveCollection(null);
                }
            }
            _activeCollection = collection;
            if (collection != null)
            {
                _activeCollection.SetEnabled(true);
            }
        }

        public CollectionView GetActiveCollection()
        {
            return _activeCollection;
        }

        #endregion

        /// <summary>
        /// Retiles the BG
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Grid_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            xBackgroundTileContainer.Children.Clear();
            new ManipulationControls(xBackgroundTileContainer);
            var width = 100;
            var height = 100;
            for (double x = 0; x < Grid.ActualWidth; x += width)
            {
                for (double y = 0; y < Grid.ActualHeight; y += height)
                {
                    var image = new Image { Source = xTileSource.Source };
                    image.Height = height;
                    image.Width = width;
                    image.Opacity = .2;
                    image.Stretch = Stretch.Fill;
                    Canvas.SetLeft(image, x);
                    Canvas.SetTop(image, y);
                    xBackgroundTileContainer.Children.Add(image);
                }
            }
        }
    }
}
