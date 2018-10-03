using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using Windows.UI;
using Windows.UI.Text;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Dash.Annotations;

// The User Control item template is documented at https://go.microsoft.com/fwlink/?LinkId=234236

namespace Dash
{
    public sealed partial class NodeConnectionsView : UserControl, INotifyPropertyChanged
    {
        //the viewmodel of the particular node
        public DocumentViewModel ViewModel { get; set; }
        //the graph the node is in
        public CollectionGraphView ParentGraph { get; private set; }
        public double ConstantRadiusWidth { get; set; }
        //list of toconnections of a node
        public ObservableCollection<ListViewItem> ToConnections { get; set; }
        //list of fromconnections of a node
        public ObservableCollection<ListViewItem> FromConnections { get; set; }
        /// <summary>
        /// Constructor
        /// </summary>

        public NodeConnectionsView(DocumentViewModel viewmodel, CollectionGraphView parent)
        {
            ToConnections = new ObservableCollection<ListViewItem>();
            ToConnections.Add(new ListViewItem { Content = "Linked To:", FontWeight = FontWeights.Bold });
            FromConnections = new ObservableCollection<ListViewItem>();
            FromConnections.Add(new ListViewItem { Content = "Linked From:", FontWeight = FontWeights.Bold });
            ViewModel = viewmodel;
            ParentGraph = parent;
            InitializeComponent();
            
            Loaded += GraphInfoView_Loaded;
            Unloaded += GraphInfoView_Unloaded;
        }

        //removes properties tracking handler when info panel is exited
        private void GraphInfoView_Unloaded(object sender, RoutedEventArgs e)
        {
            ViewModel.PropertyChanged -= ViewModel_PropertyChanged;
        }

        private void ViewModel_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
           
        }
        
        #region loading

        //adds properties tracking handler and instantiates information when info panel loads
        private void GraphInfoView_Loaded(object sender, RoutedEventArgs e)
        {
            ViewModel.PropertyChanged += ViewModel_PropertyChanged;
            CreateInfo();
        }

   
        private void UpdateData()
        {

            
        }

        private void CreateInfo()
        {
            //handles toconnections
            var toLinks = ParentGraph.AdjacencyLists[ViewModel];
            foreach (var doc in toLinks)
            {
                foreach (var connection in ParentGraph.Links)
                {   //if links to and from the same two documents exist, make their strokes a mutual connection
                    if (connection.FromDoc.ViewModel.DocumentViewModel.Equals(ViewModel) &&
                        connection.ToDoc.ViewModel.DocumentViewModel.Equals(doc))
                    {
                        var reverseLink = ParentGraph.Links.FirstOrDefault(i =>
                            (i.FromDoc.Equals(connection.ToDoc) && i.ToDoc.Equals(connection.FromDoc)));
                        
                        if (reverseLink != null)
                        {
                            reverseLink.Stroke = new SolidColorBrush(Color.FromArgb(255, 181, 149, 214));
                            connection.Stroke = new SolidColorBrush(Color.FromArgb(255, 181, 149, 214));
                        }
                        //otherwise make the connections have a from stroke
                        else
                        {
                            connection.Stroke = new SolidColorBrush(Color.FromArgb(255, 149, 174, 214));
                        }
                        connection.Thickness = 4;
                    }
                }

                //add this toconnection to the list
                var lvi = new ListViewItem
                {
                    Content =
                        doc.DocumentController.GetDereferencedField<TextController>(KeyStore.TitleKey, null).Data,
                    DataContext = doc
                };
                lvi.Tapped += ListItem_Tapped;
                ToConnections.Add(lvi);
            }

            //handles fromconnections
            var allLinks = ParentGraph.Connections;
            foreach (var link in allLinks)
            {
                if (link.Value.Equals(ViewModel))
                {
                    foreach (var connection in ParentGraph.Links)
                    {
                        //if links to and from the same two documents exist, make their strokes a mutual connection
                        if (connection.FromDoc.ViewModel.DocumentViewModel.Equals(link.Key) &&
                            connection.ToDoc.ViewModel.DocumentViewModel.Equals(ViewModel))
                        {
                            var reverseLink = ParentGraph.Links.FirstOrDefault(i =>
                                (i.FromDoc.Equals(connection.ToDoc) && i.ToDoc.Equals(connection.FromDoc)));
                            if (reverseLink != null)
                            {
                                reverseLink.Stroke = new SolidColorBrush(Color.FromArgb(255, 181, 149, 214));
                                connection.Stroke = new SolidColorBrush(Color.FromArgb(255, 181, 149, 214));
                            }
                            else
                            {
                                //otherwise make the connections have a to stroke
                                connection.Stroke = new SolidColorBrush(Color.FromArgb(255, 214, 153, 149));
                            }

                            connection.Thickness = 4;
                        }
                    }

                    //add this fromconnection to the list
                    var lvi = new ListViewItem
                    {
                        Content =
                            link.Key.DocumentController.GetDereferencedField<TextController>(KeyStore.TitleKey, null).Data,
                        DataContext = link.Key
                    };
                    lvi.Tapped += ListItem_Tapped;
                    FromConnections.Add(lvi);
                }
            }

            //if there are no current toconnections, hide the panel
            if (ToConnections.Count <= 1)
            {
                xLinkToDocs.Visibility = Visibility.Collapsed;
            }

            //if there are no current fromconnections, hide the panel
            if (FromConnections.Count <= 1)
            {
                xLinkFromDocs.Visibility = Visibility.Collapsed;
            }
        }

        //navigates to endpoint of connection when clicked
        private void ListItem_Tapped(object sender, TappedRoutedEventArgs tappedRoutedEventArgs)
        {
            //so long as the enpoint is found/correct
            if ((sender as ListViewItem)?.DataContext is DocumentViewModel dvm)
            {
                var gnv = ParentGraph.CollectionCanvas.First(i => i.ViewModel.DocumentViewModel.Equals(dvm));
                gnv.NavigateTo();
            }
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
