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
        }


        #region loading

        private void GraphNodeView_Loaded(object sender, RoutedEventArgs e)
        {

            ViewModel.PropertyChanged += ViewModel_PropertyChanged;
            ParentGraph = this.GetFirstAncestorOfType<CollectionGraphView>();
            ConstantRadiusWidth = ParentGraph.ActualWidth / 20;

            var dataDoc = ViewModel.DocumentViewModel.DataDocument;
            var toConnections = dataDoc.GetField<ListController<DocumentController>>(KeyStore.LinkToKey)?.Count + 1 ?? 1;
            var fromConnections = dataDoc.GetField<ListController<DocumentController>>(KeyStore.LinkFromKey)?.Count + 1 ?? 1;

            xEllipse.Width = toConnections + fromConnections * ConstantRadiusWidth;
            xEllipse.Height = xEllipse.Width;
            if (xEllipse.Width > ParentGraph.MaxNodeWidth)
            {
                ParentGraph.MaxNodeWidth = xEllipse.Width;
            }

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
            var startLinks =
                dataDoc.GetDereferencedField<ListController<DocumentController>>(startKey, null)
                    ?.TypedData ??
                new List<DocumentController>();

            foreach (var link in startLinks)
            {
                var startDocs = link.GetDataDocument()
                    .GetField<ListController<DocumentController>>(startKey).TypedData;
                foreach (var startDoc in startDocs)
                {
                    var startViewModel = ParentGraph.ViewModel.DocumentViewModels.First(vm =>
                        vm.DocumentController.GetDataDocument().Equals(startDoc));

                    if (startViewModel != null)
                    {
                        ParentGraph.AdjacencyLists[ViewModel.DocumentViewModel].Add(startViewModel);
                        ParentGraph.Connections.Add(
                            new KeyValuePair<DocumentViewModel, DocumentViewModel>(startViewModel,
                                ViewModel.DocumentViewModel));

                        GraphConnection existingLink = null;
                        if (startKey == KeyStore.LinkFromKey)
                        {
                            existingLink = ParentGraph.Links.FirstOrDefault(gc =>
                                gc.FromDoc?.ViewModel.DocumentViewModel.Equals(startViewModel) ?? false);
                        }
                        else
                        {
                            existingLink = ParentGraph.Links.FirstOrDefault(gc =>
                                gc.ToDoc?.ViewModel.DocumentViewModel.Equals(startViewModel) ?? false);
                        }
                        if (existingLink != null && ((startKey == KeyStore.LinkFromKey && existingLink.ToDoc == null) ||
                                                     (startKey == KeyStore.LinkToKey && existingLink.FromDoc == null)))
                        {
                            if (startKey == KeyStore.LinkFromKey)
                            {
                                existingLink.ToDoc = this;
                            }
                            else
                            {
                                existingLink.FromDoc = this;
                            }
                            ParentGraph.Connections.Add(new KeyValuePair<DocumentViewModel, DocumentViewModel>(
                                existingLink.FromDoc.ViewModel.DocumentViewModel,
                                existingLink.ToDoc.ViewModel.DocumentViewModel));
                            ParentGraph.xScrollViewCanvas.Children.Add(existingLink.Connection);
                            
                        }
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
            e.Handled = true;
        }
    }
}
