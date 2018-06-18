using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;
using Windows.Foundation;
using Windows.UI;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Shapes;
using Microsoft.Toolkit.Uwp.UI;

// The User Control item template is documented at https://go.microsoft.com/fwlink/?LinkId=234236

namespace Dash
{
   
    
    public class GraphNodeViewModel : ViewModelBase
    {
        #region variables

        private DocumentViewModel _documentViewModel;
        private DocumentController _documentController;
        private double _xPosition;
        private double _yPosition;

        public DocumentViewModel DocumentViewModel
        {
            get => _documentViewModel;
            set => SetProperty(ref _documentViewModel, value);
        }

        public DocumentController DocumentController
        {
            get => _documentController;
            set => SetProperty(ref _documentController, value);
        }

        public double XPosition
        {
            get => _xPosition;
            set => SetProperty(ref _xPosition, value);
        }

        public double YPosition
        {
            get => _yPosition;
            set => SetProperty(ref _yPosition, value);
        }

        public ObservableCollection<Polyline> Links;

        #endregion

        #region constructors
        public GraphNodeViewModel()
        {
        }

        public GraphNodeViewModel(DocumentViewModel dvm, double x, double y)
        {
            Links = new ObservableCollection<Polyline>();
            DocumentViewModel = dvm;
            DocumentController = dvm.DocumentController;
            XPosition = x;
            YPosition = y;
        }

        #endregion
    }

    public sealed partial class CollectionGraphView : UserControl, ICollectionView
    {
        private DocumentController _parentDocument;
        private Random _randInt;
        private ObservableCollection<DocumentController> CollectionDocuments { get; set; }

        public GraphNodeView SelectedNode
        {
            get => _selectedNode;
            set
            {
                if (_selectedNode != null) _selectedNode.xEllipse.Stroke = null;
                _selectedNode = value;
                _selectedNode.xEllipse.Stroke = new SolidColorBrush(Color.FromArgb(180, 128, 185, 238));
                _selectedNode.xEllipse.StrokeThickness = 6;
            }
        }

        public ObservableCollection<GraphConnection> Links { get; set; }

        public ObservableCollection<GraphNodeViewModel> Nodes { get; set; }

        public CollectionViewModel ViewModel { get; set; }

        public DocumentController ParentDocument
        {
            get => _parentDocument;
            set
            {
                _parentDocument = value;
                if (value != null)
                {
                    if (ParentDocument.GetField(CollectionDBView.FilterFieldKey) == null)
                        ParentDocument.SetField(CollectionDBView.FilterFieldKey, new KeyController(), true);
                }
            }
        }

        public ObservableDictionary<DocumentViewModel, ObservableCollection<DocumentViewModel>> AdjacencyLists { get; set; }

        public ObservableCollection<KeyValuePair<DocumentViewModel, DocumentViewModel>> Connections { get; private set; }
        public double ConstantRadiusWidth
        {
            get => ActualWidth / 20;
        }

        private double _minGap = 50;
        private double _maxGap = 100;
        private GraphNodeView _selectedNode;

        public CollectionGraphView()
        {
            InitializeComponent();
            AdjacencyLists = new ObservableDictionary<DocumentViewModel, ObservableCollection<DocumentViewModel>>();
            Connections = new ObservableCollection<KeyValuePair<DocumentViewModel, DocumentViewModel>>();
            CollectionDocuments = new ObservableCollection<DocumentController>();
            Links = new ObservableCollection<GraphConnection>();
            Nodes = new ObservableCollection<GraphNodeViewModel>();
            _randInt = new Random();

            Loaded += CollectionGraphView_Loaded;
            Unloaded += CollectionGraphView_Unloaded;
        }

        private void CollectionGraphView_Loaded(object sender, RoutedEventArgs e)
        {
            xScrollViewCanvas.Width = xScrollViewer.ActualWidth;
            xScrollViewCanvas.Height = xScrollViewer.ActualHeight;
            xExpandingBoy.Height = xScrollViewer.ActualHeight;
            xInfoPanel.Height = xScrollViewer.ActualHeight;

            DataContextChanged += CollectionGraphView_DataContextChanged;
            CollectionGraphView_DataContextChanged(null, null);
            xInfoPanel.Children.Add(new GraphInfoView(ViewModel, this));
        }

        private void CollectionGraphView_Unloaded(object sender, RoutedEventArgs e)
        {
            DataContextChanged -= CollectionGraphView_DataContextChanged;
        }

        private void CollectionGraphView_DataContextChanged(FrameworkElement sender, DataContextChangedEventArgs args)
        {
            if (DataContext is CollectionViewModel cvm)
            {
                // if datacontext hasn't actually changed just return
                if (ViewModel != null && ViewModel.CollectionController.Equals(cvm.CollectionController)) return;

                // remove events from previous datacontext
                if (ViewModel != null)
                    ViewModel.CollectionController.FieldModelUpdated -= CollectionController_FieldModelUpdated;

                // add events to new datacontext and set it
                // cvm.CollectionController.FieldModelUpdated += CollectionController_FieldModelUpdated;
                ViewModel = cvm;
                ViewModel.DocumentViewModels.CollectionChanged += DocumentViewModels_CollectionChanged;

                // set the parentDocument which is the document holding this collection
                ParentDocument = this.GetFirstAncestorOfType<DocumentView>()?.ViewModel?.DocumentController;
                if (ParentDocument != null)
                {
                    CreateCollection();
                    GenerateGraph();
                }
            }
        }

        private void DocumentViewModels_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            switch (e.Action)
            {
                case NotifyCollectionChangedAction.Add:
                    AddNodes(new ObservableCollection<DocumentViewModel>(e.NewItems.Cast<DocumentViewModel>()));
                    break;
                case NotifyCollectionChangedAction.Move:
                    break;
                case NotifyCollectionChangedAction.Remove:
                    RemoveNodes(new ObservableCollection<DocumentViewModel>(e.OldItems.Cast<DocumentViewModel>()));
                    break;
                case NotifyCollectionChangedAction.Replace:
                    break;
                case NotifyCollectionChangedAction.Reset:
                    CollectionDocuments.Clear();
                    Nodes.Clear();
                    break;
            }
        }

        private void RemoveNodes(ObservableCollection<DocumentViewModel> oldDocs)
        {
            foreach (var doc in oldDocs)
            {
                CollectionDocuments.Remove(doc.DocumentController);
                var nodeToRemove = Nodes.First(gvm => gvm.DocumentViewModel.Equals(doc));
                if (nodeToRemove != null)
                    Nodes.Remove(nodeToRemove);
            }
        }

        private void CreateCollection()
        {
            foreach (var dvm in ViewModel.DocumentViewModels)
            {
                if (dvm != null)
                {
                    CollectionDocuments.Add(dvm.DocumentController);
                    
                }
            }
        }

        private void GenerateGraph()
        {
            AdjacencyLists.Clear();
            var sortX = new ObservableCollection<DocumentViewModel>(ViewModel.DocumentViewModels);
            var sortedX = sortX.OrderBy(i => i.DocumentController.GetField<PointController>(KeyStore.PositionFieldKey).Data.X);
            var sortY = new ObservableCollection<DocumentViewModel>(ViewModel.DocumentViewModels);
            var sortedY = sortY.OrderBy(i =>
                i.DocumentController.GetField<PointController>(KeyStore.PositionFieldKey).Data.Y);
            var maxNodeDiam = ((ViewModel.DocumentViewModels.Max(dvm =>
                                dvm.DataDocument
                                    .GetDereferencedField<ListController<DocumentController>>(KeyStore.LinkFromKey,
                                        null)?.TypedData.Count ?? 0) + ViewModel.DocumentViewModels.Max(dvm =>
                                dvm.DataDocument
                                    .GetDereferencedField<ListController<DocumentController>>(KeyStore.LinkToKey, null)
                                    ?.TypedData.Count ?? 0)) * ConstantRadiusWidth) + 50;
            var gridX = xScrollViewCanvas.Width / sortedX.Count();
            if (xScrollViewCanvas.Width > maxNodeDiam)
            {
                gridX = (xScrollViewCanvas.Width - maxNodeDiam) / sortedX.Count();
            }
            var gridY = xScrollViewCanvas.Height / sortedY.Count();
            if (xScrollViewCanvas.Height > maxNodeDiam)
            {
                gridY = (xScrollViewCanvas.Height - maxNodeDiam) / sortedY.Count();
            }

            var xPositions = new ObservableDictionary<DocumentViewModel, double>();
            double x = 0;
            foreach (var dvm in sortedX)
            {
                xPositions.Add(dvm, (_randInt.Next((int) x, (int) (x + gridX))));
                x += gridX;
            }

            var yPositions = new ObservableDictionary<DocumentViewModel, double>();
            double y = 0;
            foreach (var dvm in sortedY)
            {
                yPositions.Add(dvm, (_randInt.Next((int)y, (int)(y + gridY))));
                y += gridY;
            }

            foreach (var dvm in ViewModel.DocumentViewModels)
            {
                if (dvm != null)
                {
                    AdjacencyLists[dvm] = new ObservableCollection<DocumentViewModel>();
                    Nodes.Add(new GraphNodeViewModel(dvm, xPositions.First(i => i.Key.Equals(dvm)).Value,
                        yPositions.First(i => i.Key.Equals(dvm)).Value));
                }
            }
        }

        private void AddLink(DocumentViewModel fromViewModel, DocumentViewModel toViewmodel)
        {
            //var link = new GraphConnection();

            //var toGvm = _nodes.First(gvm => gvm.DocumentViewModel.DataDocument.Equals(toViewmodel.DataDocument));
            //Point toPoint = new Point
            //{
            //    X = toGvm.XPosition,
            //    Y = toGvm.YPosition
            //};

            //var fromGvm = _nodes.First(gvm => gvm.DocumentViewModel.DataDocument.Equals(fromViewModel.DataDocument));
            //Point fromPoint = new Point
            //{
            //    X = fromGvm.XPosition,
            //    Y = fromGvm.YPosition
            //};

            //link.Points.Add(fromPoint);
            //link.Points.Add(toPoint);
            //link.Fill = Application.Current.Resources["BorderHighlight"] as SolidColorBrush;

            //xScrollViewCanvas.Children.Add(link);
        }

        private void CollectionController_FieldModelUpdated(FieldControllerBase sender, FieldUpdatedEventArgs args, Context context)
        {
            //var properArgs = (ListController<DocumentController>.ListFieldUpdatedEventArgs) args;

            //switch (properArgs.ListAction)
            //{
            //    case ListController<DocumentController>.ListFieldUpdatedEventArgs.ListChangedAction.Add:
            //        AddNodes(properArgs);
            //        break;
            //    case ListController<DocumentController>.ListFieldUpdatedEventArgs.ListChangedAction.Remove:
            //        break;
            //    case ListController<DocumentController>.ListFieldUpdatedEventArgs.ListChangedAction.Replace:
            //        break;
            //    case ListController<DocumentController>.ListFieldUpdatedEventArgs.ListChangedAction.Clear:
            //        break;
            //}
        }

        private void AddNodes(ObservableCollection<DocumentViewModel> newDocs)
        {
            foreach (var newDoc in newDocs)
            {
                if (!CollectionDocuments.Contains(newDoc.DocumentController.GetDataDocument()))
                {
                    CollectionDocuments.Add(newDoc.DocumentController.GetDataDocument());
                    Nodes.Add(new GraphNodeViewModel(newDoc, _randInt.Next(0, (int)xScrollViewCanvas.Width), _randInt.Next(0, (int)xScrollViewCanvas.Height)));
                }
            }
        }

        private void CollectionGraphView_OnSizeChanged(object sender, SizeChangedEventArgs e)
        {
            var offsetX = e.NewSize.Width - e.PreviousSize.Width;
            var offsetY = e.NewSize.Height - e.PreviousSize.Height;
            xScrollViewCanvas.Width = xScrollViewer.ActualWidth;
            xScrollViewCanvas.Height = xScrollViewer.ActualHeight;
            xExpandingBoy.Height = xScrollViewer.ActualHeight;
            xInfoPanel.Height = xScrollViewer.ActualHeight;

            foreach (var node in Nodes)
            {
                var x = node.XPosition;
                var y = node.YPosition;
                node.XPosition = x + (offsetX * (x / e.PreviousSize.Width));
                node.YPosition = y + (offsetY * (y / e.PreviousSize.Height));
            }
        }

        private void CollectionGraphView_OnTapped(object sender, Windows.UI.Xaml.Input.TappedRoutedEventArgs e)
        {
            e.Handled = true;
        }

        private void Expander_OnExpanded(object sender, EventArgs e)
        {
            xExpandingBoy.Width = 300;
            xExpandingBoy.Header = "Close Info Panel";
            xScrollViewCanvas.Width -= 260;
            xExpandingBoy.Foreground = new SolidColorBrush(Color.FromArgb(255, 0, 0, 0));
           

            foreach (var node in Nodes)
            {
                var x = node.XPosition;
                node.XPosition = x + (-260 * (x / ActualWidth));
            }
        }

        private void Expander_OnCollapsed(object sender, EventArgs e)
        {
            xExpandingBoy.Width = 40;
            xExpandingBoy.Header = "Open Info Panel";
            xScrollViewCanvas.Width += 260;
            xExpandingBoy.Foreground = new SolidColorBrush(Color.FromArgb(255, 255, 255, 255));

            foreach (var node in Nodes)
            {
                var x = node.XPosition;
                node.XPosition = x + (260 * (x / ActualWidth));
            }
        }
    }
}
