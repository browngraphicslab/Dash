using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;
using Windows.UI;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Shapes;

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


        private GraphNodeView _selectedNode;
        public UserControl UserControl => this;

        public CollectionGraphView()
        {
            InitializeComponent();
            OriginalXPositions = new ObservableDictionary<GraphNodeViewModel, double>();
            CollectionCanvas = new ObservableCollection<GraphNodeView>();
            AdjacencyLists = new ObservableDictionary<DocumentViewModel, ObservableCollection<DocumentViewModel>>();
            Connections = new ObservableCollection<KeyValuePair<DocumentViewModel, DocumentViewModel>>();
            CollectionDocuments = new ObservableCollection<DocumentController>();
            Links = new ObservableCollection<GraphConnection>();
            Nodes = new ObservableCollection<GraphNodeViewModel>();

            Loaded += CollectionGraphView_Loaded;
            Unloaded += CollectionGraphView_Unloaded;
        }

        public DocumentController ParentDocument
        {
            get => _parentDocument;
            set
            {
                _parentDocument = value;
                if (value != null)
                    if (ParentDocument.GetField(CollectionDBView.FilterFieldKey) == null)
                        ParentDocument.SetField(CollectionDBView.FilterFieldKey, new KeyController(), true);
            }
        }

        public ObservableCollection<GraphConnection> Links { get; set; }
        public ObservableCollection<GraphNodeViewModel> Nodes { get; set; }
        public ObservableCollection<DocumentController> CollectionDocuments { get; set; }
        public ObservableCollection<GraphNodeView> CollectionCanvas { get; set; }

        public ObservableDictionary<DocumentViewModel, ObservableCollection<DocumentViewModel>> AdjacencyLists
        {
            get;
            set;
        }

        public ObservableDictionary<GraphNodeViewModel, double> OriginalXPositions { get; set; }

        public ObservableCollection<KeyValuePair<DocumentViewModel, DocumentViewModel>> Connections { get; set; }

        //keeps track of which nodes is currently selected
        public GraphNodeView SelectedNode
        {
            get => _selectedNode;
            set
            {
                //reset the previously selected node (graphically)
                if (_selectedNode != null)
                {
                    foreach (var connection in Links)
                        if (connection.FromDoc.Equals(_selectedNode) || connection.ToDoc.Equals(_selectedNode))
                        {
                            connection.Stroke = new SolidColorBrush(Color.FromArgb(255, 0, 0, 0));
                            connection.Thickness = 2;
                        }

                    _selectedNode.xEllipse.Stroke = null;
                    _selectedNode.UpdateTitleBlock();
                    _selectedNode.AppendToTitle();
                }

                //if no new node is selected, remove corresponding information from the info panel
                if (value == null)
                {
                    var infoviews = xInfoPanel.Children.FirstOrDefault(i => i is NodeInfoView);
                    if (infoviews != null) xInfoPanel.Children.Remove(infoviews);
                    var connectionviews = xInfoPanel.Children.FirstOrDefault(i => i is NodeConnectionsView);
                    if (connectionviews != null) xInfoPanel.Children.Remove(connectionviews);
                    _selectedNode = value;
                }

                //set the new selected node values/designations upon a change
                else
                {
                    _selectedNode = value;
                    _selectedNode.xEllipse.Stroke = new SolidColorBrush(Color.FromArgb(180, 128, 185, 238));
                    _selectedNode.xEllipse.StrokeThickness = 6;
                    _selectedNode.UpdateTitleBlock();
                    _selectedNode.AppendToTitle(true);
                }
            }
        }

        //sets the changing radius used to scale/weigh the node sizes
        public double ConstantRadiusWidth
        {
            get
            {
                if (CollectionDocuments.Count <= 3) return ActualWidth / 30;
                return ActualWidth / (10 * CollectionDocuments.Count);
            }
        }

        public CollectionViewModel ViewModel => DataContext as CollectionViewModel;

        // formats and populates the information panel on the right
        private void CollectionGraphView_Loaded(object sender, RoutedEventArgs e)
        {
            xScrollViewCanvas.Width = xScrollViewer.ActualWidth;
            xScrollViewCanvas.Height = xScrollViewer.ActualHeight;
            xExpandingBoy.Height = xScrollViewer.ActualHeight;
            xInfoScroller.Height = xScrollViewer.ActualHeight;
            xContainerGrid.Height = xInfoPanel.ActualHeight;
            xInfoPanel.Height = xScrollViewer.ActualHeight;

            DataContextChanged += CollectionGraphView_DataContextChanged;
            CollectionGraphView_DataContextChanged(null, null);
            xInfoPanel.Children.Add(new GraphInfoView(ViewModel, this));
        }

        private void CollectionGraphView_Unloaded(object sender, RoutedEventArgs e)
        {
            DataContextChanged -= CollectionGraphView_DataContextChanged;
            if (_oldViewmodel != null)
            {
                _oldViewmodel.DocumentViewModels.CollectionChanged -= DocumentViewModels_CollectionChanged;
            }
        }

        private CollectionViewModel _oldViewmodel;
        private void CollectionGraphView_DataContextChanged(FrameworkElement sender, DataContextChangedEventArgs args)
        {
            if (DataContext != _oldViewmodel && DataContext is CollectionViewModel cvm)
            {
                _oldViewmodel = ViewModel;

                // add events to new datacontext and set it
                if (_oldViewmodel != null)
                {
                    _oldViewmodel.DocumentViewModels.CollectionChanged -= DocumentViewModels_CollectionChanged;
                }
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

        private void DocumentViewModels_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            switch (e.Action)
            {
                case NotifyCollectionChangedAction.Add:
                    AddNodes(new ObservableCollection<DocumentViewModel>(e.NewItems.Cast<DocumentViewModel>()));
                    break;
                case NotifyCollectionChangedAction.Remove:
                    RemoveNodes(new ObservableCollection<DocumentViewModel>(e.OldItems.Cast<DocumentViewModel>()));
                    break;
                case NotifyCollectionChangedAction.Reset:
                    CollectionDocuments.Clear();
                    Nodes.Clear();
                    break;
            }
        }

        private void RemoveNodes(ObservableCollection<DocumentViewModel> oldDocs)
        {
            //loops through each removed document
            foreach (var doc in oldDocs)
            {
                CollectionDocuments.Remove(doc.DocumentController);
                //finds node to remove
                var nvmToRemove = Nodes.First(gvm => gvm.DocumentViewModel.Equals(doc));
                var connectionsToRemove =
                    new ObservableCollection<KeyValuePair<DocumentViewModel, DocumentViewModel>>();
                //removes all the links connecting to the node
                foreach (var connection in Links)
                    if (connection.ToDoc.ViewModel == nvmToRemove ||
                        connection.FromDoc.ViewModel == nvmToRemove)
                        xScrollViewCanvas.Children.Remove(connection.Connection);
                //removes all the connections logically
                foreach (var connection in Connections)
                    if (connection.Key.Equals(doc) || connection.Value.Equals(doc))
                        connectionsToRemove.Add(connection);

                foreach (var connection in connectionsToRemove) Connections.Remove(connection);
                if (nvmToRemove != null)
                    Nodes.Remove(nvmToRemove);
            }
        }

        private void CreateCollection()
        {
            foreach (var dvm in ViewModel.DocumentViewModels)
                if (dvm != null)
                    CollectionDocuments.Add(dvm.DocumentController);
        }

        private void GenerateGraph()
        {
            AdjacencyLists.Clear();
            // initialize and sort each node by their freeform positions
            var sortX = new ObservableCollection<DocumentViewModel>(ViewModel.DocumentViewModels);
            var sortedX = sortX.OrderBy(i =>
                i.DocumentController?.GetField<PointController>(KeyStore.PositionFieldKey).Data.X);
            var sortY = new ObservableCollection<DocumentViewModel>(ViewModel.DocumentViewModels);
            var sortedY = sortY.OrderBy(i =>
                i.DocumentController.GetField<PointController>(KeyStore.PositionFieldKey).Data.Y);

            // find the largest possible node to prevent any from initializing out of bounds
            double maxNodeDiam = 0;
            if (ViewModel.DocumentViewModels.Count != 0)
                maxNodeDiam = (ViewModel.DocumentViewModels.Max(dvm =>
                                   dvm.DataDocument
                                       .GetDereferencedField<ListController<DocumentController>>(KeyStore.LinkFromKey,
                                           null)?.TypedData.Count + 1 ?? 1) + ViewModel.DocumentViewModels.Max(dvm =>
                                   dvm.DataDocument
                                       .GetDereferencedField<ListController<DocumentController>>(KeyStore.LinkToKey,
                                           null)
                                       ?.TypedData.Count + 1 ?? 1)) * ConstantRadiusWidth +
                              50; // account for title height

            // logically divides the space into n by n grid where n is the amount of nodes
            var gridX = xScrollViewCanvas.Width / sortedX.Count();
            if (xScrollViewCanvas.Width > maxNodeDiam)
                gridX = (xScrollViewCanvas.Width - maxNodeDiam) / sortedX.Count();
            var gridY = xScrollViewCanvas.Height / sortedY.Count();
            if (xScrollViewCanvas.Height > maxNodeDiam)
                gridY = (xScrollViewCanvas.Height - maxNodeDiam) / sortedY.Count();

            // place nodes in relation to their freeform spatial positions
            var xPositions = new ObservableDictionary<DocumentViewModel, double>();
            double x = 0;
            foreach (var dvm in sortedX)
            {
                xPositions.Add(dvm, x + gridX / 2);
                x += gridX;
            }

            var yPositions = new ObservableDictionary<DocumentViewModel, double>();
            double y = 0;
            foreach (var dvm in sortedY)
            {
                yPositions.Add(dvm, y + gridY / 2);
                y += gridY;
            }

            // creates the nodes
            foreach (var dvm in ViewModel.DocumentViewModels)
                if (dvm != null)
                {
                    AdjacencyLists[dvm] = new ObservableCollection<DocumentViewModel>();
                    Nodes.Add(new GraphNodeViewModel(dvm, xPositions.First(i => i.Key.Equals(dvm)).Value,
                        yPositions.First(i => i.Key.Equals(dvm)).Value));
                }
        }

        /// <summary>
        ///     when adding a node, we have to recreate the entire graph because of how
        ///     positioning functions. thus, we must clear both graphically and logically
        ///     nearly all of the information associated with the graph
        /// </summary>
        /// <param name="newDocs"></param>
        private void AddNodes(ObservableCollection<DocumentViewModel> newDocs)
        {
            // add each new document to the list of documents
            foreach (var newDoc in newDocs)
                if (!CollectionDocuments.Contains(newDoc.DocumentController))
                    CollectionDocuments.Add(newDoc.DocumentController);

            // removes connections graphically
            foreach (var connection in Links) xScrollViewCanvas.Children.Remove(connection.Connection);

            // remove what we need to remove logically
            Links.Clear();
            Nodes.Clear();
            Connections.Clear();
            CollectionCanvas.Clear();

            // RECREATE!
            GenerateGraph();
        }

        private void CollectionGraphView_OnSizeChanged(object sender, SizeChangedEventArgs e)
        {
            var newWidth = e?.NewSize.Width ?? 400;
            // ensure that the graph doesn't shrink to below 300
            if (newWidth >= 300)
            {
                // positions elements when being resized
                var offsetX = e?.NewSize.Width - e?.PreviousSize.Width ?? 0;
                var offsetY = e?.NewSize.Height - e?.PreviousSize.Height ?? 0;
                xScrollViewCanvas.Width = xScrollViewer.ActualWidth;
                xScrollViewCanvas.Height = xScrollViewer.ActualHeight;
                xExpandingBoy.Height = xScrollViewer.ActualHeight;
                xInfoScroller.Height = xScrollViewCanvas.ActualHeight;
                xContainerGrid.Height = xInfoPanel.ActualHeight;
                xInfoPanel.Height = xScrollViewer.ActualHeight;

                // udpates the position of the nodes
                foreach (var node in Nodes)
                {
                    var x = node.XPosition;
                    var y = node.YPosition;
                    node.XPosition = x + offsetX * (x / e?.PreviousSize.Width ?? xScrollViewCanvas.Width);
                    node.YPosition = y + offsetY * (y / e?.PreviousSize.Height ?? xScrollViewCanvas.Height);
                }
            }
        }

        private void CollectionGraphView_OnTapped(object sender, TappedRoutedEventArgs e)
        {
            // necessary
            e.Handled = true;
        }

        private void Expander_OnExpanded(object sender, EventArgs e)
        {
            xExpandingBoy.Width = 300;
            xExpandingBoy.Header = "Close Info Panel";
            xScrollViewCanvas.Width -= 260;
            xExpandingBoy.Foreground = new SolidColorBrush(Color.FromArgb(255, 0, 0, 0));

            // store the original x positions of each node so we know where to put them back
            foreach (var node in Nodes)
            {
                var x = node.XPosition;
                OriginalXPositions[node] = x;
                node.XPosition = x + -260 * (x / ActualWidth);
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
                // this will only occur when a node is added while the expander is opened
                if (!OriginalXPositions.ContainsKey(node))
                    OriginalXPositions.Add(node, ActualWidth * node.XPosition / (ActualWidth - 260));

                node.XPosition = OriginalXPositions[node];
            }
        }

        public void SetDropIndicationFill(Brush fill) { }
    }
}