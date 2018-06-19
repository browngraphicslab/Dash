using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Foundation.Metadata;
using Windows.UI;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using Windows.UI.Xaml.Shapes;
using Dash.Annotations;
using DashShared;

// The User Control item template is documented at https://go.microsoft.com/fwlink/?LinkId=234236

namespace Dash
{
    public sealed partial class GraphNodeView : UserControl, INotifyPropertyChanged
    {
        private double _smallWidth = 0;
        private double _largeWidth = 0;
        public GraphNodeViewModel ViewModel { get; private set; }
        public CollectionGraphView ParentGraph { get; private set; }
        public double ConstantRadiusWidth { get; set; }

        public Point Center => new Point
        {
            X = ViewModel.XPosition + (xGrid.ActualWidth) / 2,
            Y = ViewModel.YPosition + (xEllipse.Height) / 2
        };

        public Action PositionsLoaded;

        /// <summary>
        /// Constructor
        /// </summary>
        public GraphNodeView()
        {
            InitializeComponent();
            DataContextChanged += OnDataContextChanged;
            Loaded += GraphNodeView_Loaded;
            Unloaded += GraphNodeView_Unloaded;
            PositionsLoaded += Positions_Loaded;
        }

        private void Positions_Loaded()
        {
            PositionsLoaded -= Positions_Loaded;
            ConstantRadiusWidth = ParentGraph.ConstantRadiusWidth;
            var dataDoc = ViewModel.DocumentViewModel.DataDocument;
            var toConnections = dataDoc.GetField<ListController<DocumentController>>(KeyStore.LinkToKey)?.Count + 1 ?? 1;
            var fromConnections = dataDoc.GetField<ListController<DocumentController>>(KeyStore.LinkFromKey)?.Count + 1 ?? 1;

            var newDiam = (toConnections + fromConnections) * ConstantRadiusWidth;
            if (newDiam > _smallWidth && newDiam < _largeWidth)
            {
                UpdateTitleBlock();
                xEllipse.Width = newDiam;
                xEllipse.Height = xEllipse.Width;
            }
            else if (newDiam > _smallWidth && newDiam > _largeWidth)
            {
                UpdateTitleBlock();
                AppendToTitle();
                xEllipse.Width = newDiam;
                xEllipse.Height = xEllipse.Width;
            }
            PositionsLoaded?.Invoke();
        }

        private void Test_TitleHandler(object sender, SizeChangedEventArgs e)
        {
            if (_largeWidth != xTitleBlock.ActualWidth && _smallWidth == 0)
            {
                _largeWidth = xTitleBlock.ActualWidth;
                _smallWidth = 12;
                PositionsLoaded?.Invoke();
            }
        }

        private void GraphNodeView_Unloaded(object sender, RoutedEventArgs e)
        {
            ViewModel.PropertyChanged -= ViewModel_PropertyChanged;
        }

        private void ViewModel_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            switch (e.PropertyName)
            {
                case "XPosition":
                    (xGrid.RenderTransform as TranslateTransform).X = ViewModel.XPosition;
                    break;
                case "YPosition":
                    (xGrid.RenderTransform as TranslateTransform).Y = ViewModel.YPosition;
                    break;
            }

            if (ConstantRadiusWidth != ParentGraph.ConstantRadiusWidth)
            {
                ConstantRadiusWidth = ParentGraph.ConstantRadiusWidth;
                var dataDoc = ViewModel.DocumentViewModel.DataDocument;
                var toConnections = dataDoc.GetField<ListController<DocumentController>>(KeyStore.LinkToKey)?.Count + 1 ?? 1;
                var fromConnections = dataDoc.GetField<ListController<DocumentController>>(KeyStore.LinkFromKey)?.Count + 1 ?? 1;

                var newDiam = (toConnections + fromConnections) * ConstantRadiusWidth;
                if (newDiam > _smallWidth && newDiam < _largeWidth)
                {
                    UpdateTitleBlock();
                    xEllipse.Width = newDiam;
                    xEllipse.Height = xEllipse.Width;
                }
                else if (newDiam > _smallWidth && newDiam > _largeWidth)
                {
                    UpdateTitleBlock();
                    AppendToTitle();
                    xEllipse.Width = newDiam;
                    xEllipse.Height = xEllipse.Width;
                }
            }
        }

        #region loading

        private void GraphNodeView_Loaded(object sender, RoutedEventArgs e)
        {
            ViewModel.PropertyChanged += ViewModel_PropertyChanged;
            ParentGraph = this.GetFirstAncestorOfType<CollectionGraphView>();
            ConstantRadiusWidth = ParentGraph.ConstantRadiusWidth;
            ParentGraph.CollectionCanvas.Add(this);

            CreateLinks();
            xTitleBlock.SizeChanged += Test_TitleHandler;

            DocumentController_TitleUpdated(null, null, null);

            ViewModel.DocumentController.AddFieldUpdatedListener(KeyStore.TitleKey, DocumentController_TitleUpdated);
            ViewModel.DocumentController.GetDataDocument().AddFieldUpdatedListener(KeyStore.LinkFromKey, Links_Updated);
        }

        private void CreateLinks()
        {
            var dataDoc = ViewModel.DocumentViewModel.DataDocument;
            // gets all the connections that are emanating outwards from the datadoc
            var toConnections = dataDoc.GetField<ListController<DocumentController>>(KeyStore.LinkToKey)?.Count + 1 ?? 1;
            // incoming connections to the datadoc, + 1 to avoid any ellipses with a radius of 0
            var fromConnections = dataDoc.GetField<ListController<DocumentController>>(KeyStore.LinkFromKey)?.Count + 1 ?? 1;

            var newDiam = (toConnections + fromConnections) * ConstantRadiusWidth;
            if (newDiam > _smallWidth)
            {
                xEllipse.Width = newDiam;
                xEllipse.Height = xEllipse.Width;
                UpdateTitleBlock();
                AppendToTitle();
            }
          
            TranslateTransform transformation = new TranslateTransform
            {
                X = ViewModel.XPosition,
                Y = ViewModel.YPosition
            };
            xGrid.RenderTransform = transformation;

            if (toConnections > 1)
            {
                CreateLink(dataDoc, KeyStore.LinkToKey);
            }

            if (fromConnections > 1)
            {
                CreateLink(dataDoc, KeyStore.LinkFromKey);
            }
        }

        private void Links_Updated(FieldControllerBase sender, FieldUpdatedEventArgs args, Context context)
        {
            var dataDoc = sender as DocumentController;
            // gets all the connections that are emanating outwards from the datadoc
            var fromConnections = dataDoc.GetField<ListController<DocumentController>>(KeyStore.LinkFromKey)?.Count + 1 ?? 1;

            if (fromConnections > 1)
            {
                var oldLinks = ParentGraph.AdjacencyLists[ViewModel.DocumentViewModel];
                var newLinks = dataDoc.GetField<ListController<DocumentController>>(KeyStore.LinkFromKey).TypedData;
                for (int i = oldLinks.Count; i < newLinks.Count; i++)
                {
                    var endDoc = newLinks[i].GetDataDocument()
                        .GetField<ListController<DocumentController>>(KeyStore.LinkFromKey).TypedData[0];

                    if (endDoc != null)
                    {
                        var newConnection = new GraphConnection
                        {
                            FromDoc = this,
                            ToDoc = ParentGraph.CollectionCanvas.FirstOrDefault(gnv =>
                                gnv.ViewModel.DocumentController.GetDataDocument().Equals(endDoc.GetDataDocument()))
                        };

                        var toDataDoc = newConnection.ToDoc.ViewModel.DocumentController.GetDataDocument();
                        // gets all the connections that are emanating outwards from the datadoc
                        var toDocToConnections = toDataDoc.GetField<ListController<DocumentController>>(KeyStore.LinkToKey)?.Count + 1 ?? 1;
                        // incoming connections to the datadoc, + 1 to avoid any ellipses with a radius of 0
                        var toDocFromConnections = toDataDoc.GetField<ListController<DocumentController>>(KeyStore.LinkFromKey)?.Count + 1 ?? 1;

                        var toNewDiam = (toDocToConnections + toDocFromConnections) * ConstantRadiusWidth;
                        if (toNewDiam > _smallWidth)
                        {
                            newConnection.ToDoc.xEllipse.Width = toNewDiam;
                            newConnection.ToDoc.xEllipse.Height = newConnection.ToDoc.xEllipse.Width;
                            newConnection.ToDoc.UpdateTitleBlock();
                            newConnection.ToDoc.AppendToTitle();
                            newConnection.ToDoc.PositionsLoaded?.Invoke();
                        }

                        fromConnections = dataDoc.GetField<ListController<DocumentController>>(KeyStore.LinkFromKey)?.Count + 1 ?? 1;
                        // incoming connections to the datadoc, + 1 to avoid any ellipses with a radius of 0
                        var toConnections = dataDoc.GetField<ListController<DocumentController>>(KeyStore.LinkToKey)?.Count + 1 ?? 1;

                        var newDiam = (fromConnections + toConnections) * ConstantRadiusWidth;
                        if (newDiam > _smallWidth)
                        {
                            xEllipse.Width = newDiam;
                            xEllipse.Height = xEllipse.Width;
                            UpdateTitleBlock();
                            AppendToTitle();
                            PositionsLoaded?.Invoke();
                        }

                        ParentGraph.Links.Add(newConnection);
                        ParentGraph.AdjacencyLists[newConnection.FromDoc.ViewModel.DocumentViewModel]
                            .Add(newConnection.ToDoc.ViewModel.DocumentViewModel);
                        ParentGraph.Connections.Add(new KeyValuePair<DocumentViewModel, DocumentViewModel>(
                            newConnection.FromDoc.ViewModel.DocumentViewModel,
                            newConnection.ToDoc.ViewModel.DocumentViewModel));
                        ParentGraph.xScrollViewCanvas.Children.Add(newConnection.Connection);
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
                    .GetField<ListController<DocumentController>>(startKey).TypedData ?? new List<DocumentController>();
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
                        {
                            existingLink = ParentGraph.Links.FirstOrDefault(gc =>
                                (gc.FromDoc?.ViewModel.DocumentViewModel.Equals(endViewModel) ?? false) && (gc.ToDoc == null));
                        }
                        else
                        {
                            existingLink = ParentGraph.Links.FirstOrDefault(gc =>
                                (gc.ToDoc?.ViewModel.DocumentViewModel.Equals(endViewModel) ?? false) && (gc.FromDoc == null));
                        }
                        
                        if (existingLink != null)
                        {
                            // if such a link exists, set the missing endpoint to this graph node view
                            if (startKey == KeyStore.LinkFromKey)
                            {
                                existingLink.ToDoc = this;
                            }
                            else
                            {
                                existingLink.FromDoc = this;
                            }

                            // after that, we are sure that both endpoints exist, so we can logically and graphically add it to the collection
                            ParentGraph.AdjacencyLists[existingLink.FromDoc.ViewModel.DocumentViewModel].Add(existingLink.ToDoc.ViewModel.DocumentViewModel);
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
                            {
                                newConnection.ToDoc = this;
                            }
                            else
                            {
                                newConnection.FromDoc = this;
                            }
                            // add the semicomplete link to ParentGraph.Links
                            ParentGraph.Links.Add(newConnection);
                        }
                    }
                }
            }
        }

        private void DocumentController_TitleUpdated(FieldControllerBase sender, FieldUpdatedEventArgs args, Context context)
        {
            UpdateTitleBlock();
            AppendToTitle();
        }

        public void AppendToTitle(bool forceAppend = false)
        {
            if (xEllipse.Width > _largeWidth || forceAppend)
            {
                var title = ViewModel.DocumentController
                                .GetDereferencedField<TextController>(KeyStore.TitleKey, null)?
                                .Data ?? "Untitled " + ViewModel.DocumentController.DocumentType.Type;
                xTitleBlock.Text += "   " + title;
            }
        }

        public void UpdateTitleBlock()
        {
            var type = ViewModel.DocumentController.GetDereferencedField(KeyStore.DataKey, null).TypeInfo;

            switch (type)
            {
                case TypeInfo.Image:
                    xTitleBlock.Text = Application.Current.Resources["ImageIcon"] as string;
                    break;
                case TypeInfo.Audio:
                    xTitleBlock.Text = Application.Current.Resources["AudioIcon"] as string;
                    break;
                case TypeInfo.Video:
                    xTitleBlock.Text = Application.Current.Resources["VideoIcon"] as string;
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

        public void NavigateTo()
        {
            Node_OnTapped(null, null);
        }

        private void Node_OnTapped(object sender, TappedRoutedEventArgs e)
        {
            ParentGraph.SelectedNode = this;
            var infoviews = ParentGraph.xInfoPanel.Children.FirstOrDefault(i => i is NodeInfoView);
            if (infoviews != null) ParentGraph.xInfoPanel.Children.Remove(infoviews);
            var connectionviews = ParentGraph.xInfoPanel.Children.FirstOrDefault(i => i is NodeConnectionsView);
            if (connectionviews != null) ParentGraph.xInfoPanel.Children.Remove(connectionviews);
            //ParentGraph.xInfoPanel.Children.RemoveAt(2);
            ParentGraph.xInfoPanel.Children.Add(new NodeInfoView(ViewModel.DocumentViewModel, ParentGraph));
            ParentGraph.xInfoPanel.Children.Add(new NodeConnectionsView( ViewModel.DocumentViewModel, ParentGraph));
            if (e != null) e.Handled = true;
        }

        private void XGrid_OnDoubleTapped(object sender, DoubleTappedRoutedEventArgs e)
        {
            ParentGraph.SelectedNode = null;
            var existingPanel = (HiddenNodesView) ParentGraph.xInfoPanel.Children.FirstOrDefault(i => i is HiddenNodesView);
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
            Window.Current.CoreWindow.PointerCursor = new Windows.UI.Core.CoreCursor(Windows.UI.Core.CoreCursorType.Hand, 0);
            (xGrid.RenderTransform as TranslateTransform).X -= xEllipse.Width * 0.1 / 2;
            (xGrid.RenderTransform as TranslateTransform).Y -= xEllipse.Width * 0.1 / 2;
            xEllipse.Width *= 1.1;
            xEllipse.Height *= 1.1;
            xEllipse.Fill = new SolidColorBrush(Color.FromArgb(255, 139, 165, 159));
        }

        private void XEllipse_OnPointerExited(object sender, PointerRoutedEventArgs e)
        {
            Window.Current.CoreWindow.PointerCursor = new Windows.UI.Core.CoreCursor(Windows.UI.Core.CoreCursorType.Arrow, 0);
            xEllipse.Width = xEllipse.Width * (10f / 11f);
            xEllipse.Height = xEllipse.Width;
            (xGrid.RenderTransform as TranslateTransform).X += xEllipse.Width * 0.1 / 2;
            (xGrid.RenderTransform as TranslateTransform).Y += xEllipse.Width * 0.1 / 2;
            xEllipse.Fill = new SolidColorBrush(Color.FromArgb(255, 120, 145, 139));
        }
    }
}
