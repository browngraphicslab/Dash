using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using Windows.Foundation;
using Windows.UI;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Dash.Annotations;
using DashShared;

// The User Control item template is documented at https://go.microsoft.com/fwlink/?LinkId=234236

namespace Dash
{
    public sealed partial class GraphNodeView : UserControl, INotifyPropertyChanged
    {
        private double _largeWidth;
        private double _smallWidth;

        public Action PositionsLoaded;

        /// <summary>
        ///     Constructor
        /// </summary>
        public GraphNodeView()
        {
            InitializeComponent();
            DataContextChanged += OnDataContextChanged;
            Loaded += GraphNodeView_Loaded;
            Unloaded += GraphNodeView_Unloaded;
            PositionsLoaded += Positions_Loaded;
        }

        public GraphNodeViewModel ViewModel { get; private set; }
        public CollectionGraphView ParentGraph { get; private set; }
        public double VariableConstantRadiusWidth { get; set; }

        public Point Center => new Point
        {
            // max of ellipse or title width, since either can arbitrarily be larger than the other, affecting the position of the center of the ellipse
            X = (xGrid.RenderTransform as TranslateTransform).X + Math.Max(xEllipse.Width, xTitleBlock.ActualWidth) / 2,
            Y = (xGrid.RenderTransform as TranslateTransform).Y + xEllipse.Height / 2
        };
        
        private void Positions_Loaded()
        {
            // positions_loaded should only run once, but action is used multiple times
            PositionsLoaded -= Positions_Loaded;
            VariableConstantRadiusWidth = ParentGraph.ConstantRadiusWidth;
            var dataDoc = ViewModel.DocumentViewModel.DataDocument;
            // default to 1 to avoid nodes with radius = 0
            var toConnections = dataDoc.GetField<ListController<DocumentController>>(KeyStore.LinkToKey)?.Count + 1 ??
                                1;
            var fromConnections =
                dataDoc.GetField<ListController<DocumentController>>(KeyStore.LinkFromKey)?.Count + 1 ?? 1;

            var newDiam = (toConnections + fromConnections) * VariableConstantRadiusWidth;
            // keep the diameter above a lower threshold, but if it's below a higher threshold, only display the type icon
            if (newDiam > _smallWidth && newDiam < _largeWidth)
            {
                UpdateTitleBlock();
            }
            // if diameter is above both thresholds, append the document title to the icon
            else if (newDiam > _smallWidth && newDiam > _largeWidth)
            {
                UpdateTitleBlock();
                AppendToTitle();
            }
            xEllipse.Width = newDiam;
            xEllipse.Height = xEllipse.Width;

            PositionsLoaded?.Invoke();
        }

        private void Title_OnSizeChanged(object sender, SizeChangedEventArgs e)
        {
            // initializes the lower and upper thresholds for the title block
            if (_largeWidth != xTitleBlock.ActualWidth && _smallWidth == 0)
            {
                _largeWidth = xTitleBlock.ActualWidth;
                _smallWidth = 12;
                PositionsLoaded?.Invoke();
            }

            // keeps the circle in place when title is forcibly displayed
            if (xTitleBlock.ActualWidth > xEllipse.Width)
            {
                var difference = xTitleBlock.ActualWidth - xEllipse.Width;
                (xGrid.RenderTransform as TranslateTransform).X -= difference / 2;
            }

            // keeps the circle in place when forcibly displayed title is undisplayed
            if (e.PreviousSize.Width > xEllipse.Width && e.NewSize.Width < e.PreviousSize.Width)
            {
                var difference = e.PreviousSize.Width - e.NewSize.Width;
                (xGrid.RenderTransform as TranslateTransform).X += difference / 2;
                PositionsLoaded?.Invoke();
            }
        }

        private void GraphNodeView_Unloaded(object sender, RoutedEventArgs e)
        {
            // when unloaded, stop listening for events to prevent events from triggering multiple times when reloaded
            ViewModel.PropertyChanged -= ViewModel_PropertyChanged;
            ViewModel.DocumentController.RemoveFieldUpdatedListener(KeyStore.TitleKey, DocumentController_TitleUpdated);
            ViewModel.DocumentController.GetDataDocument()
                .RemoveFieldUpdatedListener(KeyStore.LinkToKey, LinkFieldUpdated);
        }

        private void ViewModel_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            // update graphicaly position when logical position is changed
            switch (e.PropertyName)
            {
                case "XPosition":
                    (xGrid.RenderTransform as TranslateTransform).X = ViewModel.XPosition;
                    break;
                case "YPosition":
                    (xGrid.RenderTransform as TranslateTransform).Y = ViewModel.YPosition;
                    break;
            }

            // if the parent graph changes its constant radius value, updates ours
            if (VariableConstantRadiusWidth != ParentGraph.ConstantRadiusWidth)
            {
                VariableConstantRadiusWidth = ParentGraph.ConstantRadiusWidth;
                var dataDoc = ViewModel.DocumentViewModel.DataDocument;
                var toConnections =
                    dataDoc.GetField<ListController<DocumentController>>(KeyStore.LinkToKey)?.Count + 1 ?? 1;
                var fromConnections =
                    dataDoc.GetField<ListController<DocumentController>>(KeyStore.LinkFromKey)?.Count + 1 ?? 1;

                // update the nodes with the new radii
                var newDiam = (toConnections + fromConnections) * VariableConstantRadiusWidth;
                if (newDiam > _smallWidth && newDiam < _largeWidth)
                {
                    UpdateTitleBlock();
                }
                else if (newDiam > _largeWidth)
                {
                    UpdateTitleBlock();
                    AppendToTitle();
                }
                xEllipse.Width = newDiam;
                xEllipse.Height = xEllipse.Width;
            }
        }

        public void NavigateTo()
        {
            // selects this node, called when an item in the connections view panel is selected
            Node_OnTapped(null, null);
        }

        private void Node_OnTapped(object sender, TappedRoutedEventArgs e)
        {
            // notify parent graph that this node has been selected
            ParentGraph.SelectedNode = this;
            // remove any existing node info and node connection views
            var infoviews = ParentGraph.xInfoPanel.Children.FirstOrDefault(i => i is NodeInfoView);
            if (infoviews != null) ParentGraph.xInfoPanel.Children.Remove(infoviews);
            var connectionviews = ParentGraph.xInfoPanel.Children.FirstOrDefault(i => i is NodeConnectionsView);
            if (connectionviews != null) ParentGraph.xInfoPanel.Children.Remove(connectionviews);
            // add new ones with this node as the information source
            ParentGraph.xInfoPanel.Children.Add(new NodeInfoView(ViewModel.DocumentViewModel, ParentGraph));
            ParentGraph.xInfoPanel.Children.Add(new NodeConnectionsView(ViewModel.DocumentViewModel, ParentGraph));
            if (e != null) e.Handled = true;
        }

        private void XGrid_OnDoubleTapped(object sender, DoubleTappedRoutedEventArgs e)
        {
            // tell the parent graph that this node has been hidden
            ParentGraph.SelectedNode = null;
            
            // create a new hidden nodes view if none currently exists and update it
            var existingPanel =
                (HiddenNodesView) ParentGraph.xInfoPanel.Children.FirstOrDefault(i => i is HiddenNodesView);
            if (existingPanel == null)
            {
                var hnv = new HiddenNodesView(ParentGraph);
                hnv.AddNode(this);
                ParentGraph.xInfoPanel.Children.Add(hnv);
            }
            else
            {
                existingPanel.AddNode(this);
            }
        }

        private void XEllipse_OnPointerEntered(object sender, PointerRoutedEventArgs e)
        {
            // visually show that the ellipse is clickable
            Window.Current.CoreWindow.PointerCursor = new CoreCursor(CoreCursorType.Hand, 0);
            (xGrid.RenderTransform as TranslateTransform).X -= xEllipse.Width * 0.1 / 2;
            (xGrid.RenderTransform as TranslateTransform).Y -= xEllipse.Width * 0.1 / 2;
            xEllipse.Width *= 1.1;
            xEllipse.Height *= 1.1;
            xEllipse.Fill = new SolidColorBrush(Color.FromArgb(255, 139, 165, 159));
        }

        private void XEllipse_OnPointerExited(object sender, PointerRoutedEventArgs e)
        {
            // undoes the pointer_entered method
            Window.Current.CoreWindow.PointerCursor = new CoreCursor(CoreCursorType.Arrow, 0);
            xEllipse.Width = xEllipse.Width * ((double) 10 / 11);
            xEllipse.Height = xEllipse.Width;
            // this undo is only necessary if the titleblock's width is the node's defining width
            if (xTitleBlock.ActualWidth > xEllipse.Width)
                (xGrid.RenderTransform as TranslateTransform).X += xEllipse.Width * 0.1 / 2;
            (xGrid.RenderTransform as TranslateTransform).Y += xEllipse.Width * 0.1 / 2;
            xEllipse.Fill = new SolidColorBrush(Color.FromArgb(255, 120, 145, 139));
            PositionsLoaded?.Invoke();
        }

        #region loading

        private void GraphNodeView_Loaded(object sender, RoutedEventArgs e)
        {
            ViewModel.PropertyChanged += ViewModel_PropertyChanged;
            ParentGraph = this.GetFirstAncestorOfType<CollectionGraphView>();
            VariableConstantRadiusWidth = ParentGraph.ConstantRadiusWidth;
            ParentGraph.CollectionCanvas.Add(this);

            // create all links with this node
            CreateLinks();
            xTitleBlock.SizeChanged += Title_OnSizeChanged;

            DocumentController_TitleUpdated(null, null, null);

            // listen for title updates and link updates
            ViewModel.DocumentController.AddFieldUpdatedListener(KeyStore.TitleKey, DocumentController_TitleUpdated);
            ViewModel.DocumentController.GetDataDocument()
                .AddFieldUpdatedListener(KeyStore.LinkToKey, LinkFieldUpdated);
        }

        private void CreateLinks()
        {
            var dataDoc = ViewModel.DocumentViewModel.DataDocument;
            // gets all the connections that are emanating outwards from the datadoc
            var toConnections = dataDoc.GetField<ListController<DocumentController>>(KeyStore.LinkToKey)?.Count + 1 ??
                                1;
            // incoming connections to the datadoc, + 1 to avoid any ellipses with a radius of 0
            var fromConnections =
                dataDoc.GetField<ListController<DocumentController>>(KeyStore.LinkFromKey)?.Count + 1 ?? 1;

            var newDiam = (toConnections + fromConnections) * VariableConstantRadiusWidth;
            if (newDiam > _smallWidth)
            {
                xEllipse.Width = newDiam;
                xEllipse.Height = xEllipse.Width;
                UpdateTitleBlock();
                AppendToTitle();
            }

            var transformation = new TranslateTransform
            {
                X = ViewModel.XPosition,
                Y = ViewModel.YPosition
            };
            xGrid.RenderTransform = transformation;

            if (toConnections > 1) CreateLink(dataDoc, KeyStore.LinkToKey);

            if (fromConnections > 1) CreateLink(dataDoc, KeyStore.LinkFromKey);
        }

        private void LinkFieldUpdated(FieldControllerBase sender, FieldUpdatedEventArgs args, Context context)
        {
            // get the list of changed documents from args
            var dargs = args as DocumentController.DocumentFieldUpdatedEventArgs;
            if (dargs.FieldArgs is ListController<DocumentController>.ListFieldUpdatedEventArgs largs)
                switch (largs.ListAction)
                {
                    case ListController<DocumentController>.ListFieldUpdatedEventArgs.ListChangedAction.Add:
                        AddLinks(largs.NewItems);
                        // update side panel (by removing and adding it)
                        var panel = ParentGraph.xInfoPanel.Children.FirstOrDefault(i => i is NodeConnectionsView);
                        if (panel != null)
                        {
                            ParentGraph.xInfoPanel.Children.Remove(panel);
                            ParentGraph.xInfoPanel.Children.Add(
                                new NodeConnectionsView(ParentGraph.SelectedNode.ViewModel.DocumentViewModel,
                                    ParentGraph));
                        }
                        break;
                    case ListController<DocumentController>.ListFieldUpdatedEventArgs.ListChangedAction.Remove:
                        break;
                    case ListController<DocumentController>.ListFieldUpdatedEventArgs.ListChangedAction.Clear:
                        break;
                }
        }

        private void AddLinks(List<DocumentController> newLinks)
        {
            foreach (var link in newLinks)
            {
                // get the from and to document stored in the link
                var fromDoc = link.GetDataDocument().GetField<ListController<DocumentController>>(KeyStore.LinkFromKey)
                    .TypedData[0];
                var toDoc = link.GetDataDocument().GetField<ListController<DocumentController>>(KeyStore.LinkToKey)
                    .TypedData[0];
                // get the matching from and to documents from the parent graph's collection documents
                var matchingFromDoc =
                    ParentGraph.CollectionDocuments.FirstOrDefault(cdc =>
                        cdc.GetDataDocument().Equals(fromDoc.GetDataDocument()));
                var matchingToDoc =
                    ParentGraph.CollectionDocuments.FirstOrDefault(cdc =>
                        cdc.GetDataDocument().Equals(toDoc.GetDataDocument()));
                // check that both the to and from documents are in the collection
                if (matchingFromDoc != null && matchingToDoc != null)
                {
                    // find the from and to graph node views
                    var fromGnv = ParentGraph.CollectionCanvas.FirstOrDefault(gnv =>
                        gnv.ViewModel.DocumentController.GetDataDocument().Equals(fromDoc.GetDataDocument()));
                    var toGnv = ParentGraph.CollectionCanvas.FirstOrDefault(gnv =>
                        gnv.ViewModel.DocumentController.GetDataDocument().Equals(toDoc.GetDataDocument()));
                    if (fromGnv != null && toGnv != null)
                    {
                        // create a new connection with proper from and to
                        var newConnection = new GraphConnection
                        {
                            FromDoc = fromGnv,
                            ToDoc = toGnv
                        };

                        // logically adds the connection
                        ParentGraph.AdjacencyLists[newConnection.FromDoc.ViewModel.DocumentViewModel]
                            .Add(newConnection.ToDoc.ViewModel.DocumentViewModel);
                        ParentGraph.Connections.Add(new KeyValuePair<DocumentViewModel, DocumentViewModel>(
                            newConnection.FromDoc.ViewModel.DocumentViewModel,
                            newConnection.ToDoc.ViewModel.DocumentViewModel));
                        ParentGraph.Links.Add(newConnection);
                        // graphically adds the connection
                        ParentGraph.xScrollViewCanvas.Children.Add(newConnection.Connection);
                    }
                    else
                    {
                        // if we made it to this point, there should already exist graph node views for both from and to
                        throw new Exception("CollectionDocuments was not updated");
                    }
                }
            }
        }

        private void CreateLink(DocumentController dataDoc, KeyController startKey)
        {
            // gets all the links that come either from or out of the data doc, respective to startKey
            var incidentLinks =
                dataDoc.GetDereferencedField<ListController<DocumentController>>(startKey, null)
                    ?.TypedData ??
                new List<DocumentController>(); // or an empty list if neither

            foreach (var link in incidentLinks)
            {
                // gets all the docs that are at the other endpoint of each incident link
                var endDocs = link.GetDataDocument()
                                  .GetField<ListController<DocumentController>>(startKey).TypedData ??
                              new List<DocumentController>();
                foreach (var endDoc in endDocs)
                {
                    // gets the viewmodel for the documents in endDocs
                    var endViewModel = ParentGraph.ViewModel.DocumentViewModels.FirstOrDefault(vm =>
                        vm.DocumentController.GetDataDocument().Equals(endDoc.GetDataDocument()));

                    if (endViewModel != null)
                    {
                        GraphConnection existingLink = null;
                        /*
                         * go into ParentGraph.Links (observable collection of all logical links) and try to find a link
                         * whose endpoint as defined by the startKey is the same as the endViewModel variable, and whose
                         * converse endpoint is not yet set
                         */
                        if (startKey == KeyStore.LinkFromKey)
                            existingLink = ParentGraph.Links.FirstOrDefault(gc =>
                                (gc.FromDoc?.ViewModel.DocumentViewModel.Equals(endViewModel) ?? false) &&
                                gc.ToDoc == null);
                        else
                            existingLink = ParentGraph.Links.FirstOrDefault(gc =>
                                (gc.ToDoc?.ViewModel.DocumentViewModel.Equals(endViewModel) ?? false) &&
                                gc.FromDoc == null);

                        if (existingLink != null)
                        {
                            // if such a link exists, set the missing endpoint to this graph node view
                            if (startKey == KeyStore.LinkFromKey)
                                existingLink.ToDoc = this;
                            else
                                existingLink.FromDoc = this;

                            // after that, we are sure that both endpoints exist, so we can logically and graphically add it to the collection
                            ParentGraph.AdjacencyLists[existingLink.FromDoc.ViewModel.DocumentViewModel]
                                .Add(existingLink.ToDoc.ViewModel.DocumentViewModel);
                            ParentGraph.Connections.Add(new KeyValuePair<DocumentViewModel, DocumentViewModel>(
                                existingLink.FromDoc.ViewModel.DocumentViewModel,
                                existingLink.ToDoc.ViewModel.DocumentViewModel));
                            ParentGraph.xScrollViewCanvas.Children.Add(existingLink.Connection);
                        }
                        // if no such link exists, create a new connection with one endpoint instantiated
                        else
                        {
                            var newConnection = new GraphConnection();

                            if (startKey == KeyStore.LinkFromKey)
                                newConnection.ToDoc = this;
                            else
                                newConnection.FromDoc = this;
                            // add the semicomplete link to ParentGraph.Links
                            ParentGraph.Links.Add(newConnection);
                        }
                    }
                }
            }
        }

        private void DocumentController_TitleUpdated(FieldControllerBase sender, FieldUpdatedEventArgs args,
            Context context)
        {
            UpdateTitleBlock();
            AppendToTitle();
        }

        public void AppendToTitle(bool forceAppend = false)
        {
            // forceappend forces the title to show, even if the ellipse width is below the upper threshold
            if (xEllipse.Width > _largeWidth || forceAppend)
            {
                // default to "Untitled Document Type"
                var title = ViewModel.DocumentController
                                .GetDereferencedField<TextController>(KeyStore.TitleKey, null)?
                                .Data ?? "Untitled " + ViewModel.DocumentController.DocumentType.Type;
                xTitleBlock.Text += "   " + title;
            }
        }

        public void UpdateTitleBlock()
        {
            // determines the icon preceding the document's title
            var type = ViewModel.DocumentController.GetDereferencedField(KeyStore.DataKey, null).TypeInfo;

            switch (type)
            {
                case TypeInfo.Image:
                    xTitleBlock.Text = Application.Current.Resources["ImageTemplateIcon"] as string;
                    break;
                case TypeInfo.Audio:
                    xTitleBlock.Text = Application.Current.Resources["AudioDocumentIcon"] as string;
                    break;
                case TypeInfo.Video:
                    xTitleBlock.Text = Application.Current.Resources["VideoDocumentIcon"] as string;
                    break;
                case TypeInfo.RichText:
                case TypeInfo.Text:
                    xTitleBlock.Text = Application.Current.Resources["TextIcon"] as string;
                    break;
                case TypeInfo.Document:
                    xTitleBlock.Text = Application.Current.Resources["DocumentPlainIcon"] as string;
                    break;
                default:
                    xTitleBlock.Text = Application.Current.Resources["DefaultIcon"] as string;
                    break;
            }
        }

        private void OnDataContextChanged(FrameworkElement sender, DataContextChangedEventArgs args)
        {
            var vm = DataContext as GraphNodeViewModel;
            Debug.Assert(vm != null);
            ViewModel = vm;
        }

        #endregion


        #region property changed

        public event PropertyChangedEventHandler PropertyChanged;

        [NotifyPropertyChangedInvocator]
        private void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        #endregion
    }
}