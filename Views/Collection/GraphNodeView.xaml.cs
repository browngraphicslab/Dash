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
using Windows.UI;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using Dash.Annotations;

// The User Control item template is documented at https://go.microsoft.com/fwlink/?LinkId=234236

namespace Dash
{
    public sealed partial class GraphNodeView : UserControl, INotifyPropertyChanged
    {
        public GraphNodeViewModel ViewModel { get; private set; }
        public CollectionGraphView ParentGraph { get; private set; }
        public double ConstantRadiusWidth { get; set; }
    
        /// <summary>
        /// Constructor
        /// </summary>
        public GraphNodeView()
        {
            InitializeComponent();
            DataContextChanged += OnDataContextChanged;
            Loaded += GraphNodeView_Loaded;
            Unloaded += GraphNodeView_Unloaded;
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

                xEllipse.Width = (toConnections + fromConnections) * ConstantRadiusWidth;
                xEllipse.Height = xEllipse.Width;
            }
        }


        #region loading

        private void GraphNodeView_Loaded(object sender, RoutedEventArgs e)
        {
            ViewModel.PropertyChanged += ViewModel_PropertyChanged;
            ParentGraph = this.GetFirstAncestorOfType<CollectionGraphView>();
            ConstantRadiusWidth = ParentGraph.ConstantRadiusWidth;
            ParentGraph.CollectionCanvas.Add(this);

            var dataDoc = ViewModel.DocumentViewModel.DataDocument;
            // gets all the connections that are emanating outwards from the datadoc
            var toConnections = dataDoc.GetField<ListController<DocumentController>>(KeyStore.LinkToKey)?.Count + 1 ?? 1;
            // incoming connections to the datadoc, + 1 to avoid any ellipses with a radius of 0
            var fromConnections = dataDoc.GetField<ListController<DocumentController>>(KeyStore.LinkFromKey)?.Count + 1 ?? 1;

            xEllipse.Width = (toConnections + fromConnections) * ConstantRadiusWidth;
            xEllipse.Height = xEllipse.Width;
            xEllipse.MinWidth = xTitleBlock.ActualWidth;
            xEllipse.MinHeight = xTitleBlock.ActualWidth;

            if (toConnections > 1)
            {
                CreateLink(dataDoc, KeyStore.LinkToKey);
            }

            if (fromConnections > 1)
            {
                 CreateLink(dataDoc, KeyStore.LinkFromKey);
            }
            
            DocumentController_TitleUpdated(null, null, null);

            ViewModel.DocumentController.AddFieldUpdatedListener(KeyStore.TitleKey, DocumentController_TitleUpdated);

            TranslateTransform transformation = new TranslateTransform
            {
                X = ViewModel.XPosition,
                Y = ViewModel.YPosition
            };
            xGrid.RenderTransform = transformation;
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
                    .GetField<ListController<DocumentController>>(startKey).TypedData;
                foreach (var endDoc in endDocs)
                {
                    // gets the viewmodel for the documents in endDocs
                    var endViewModel = ParentGraph.ViewModel.DocumentViewModels.First(vm =>
                        vm.DocumentController.GetDataDocument().Equals(endDoc));

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
            xTitleBlock.Text += "   " + 
                ViewModel.DocumentController.GetDereferencedField<TextController>(KeyStore.TitleKey, context)
                    .Data ?? "Untitled " + ViewModel.DocumentController.DocumentType.Type;
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
    }
}
