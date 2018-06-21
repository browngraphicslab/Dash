using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using Windows.UI.Text;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Dash.Annotations;

// The User Control item template is documented at https://go.microsoft.com/fwlink/?LinkId=234236

namespace Dash
{
    public sealed partial class GraphInfoView : UserControl, INotifyPropertyChanged
    {
        /// <summary>
        ///     Constructor
        /// </summary>
        public GraphInfoView(CollectionViewModel viewmodel, CollectionGraphView parent)
        {
            Labels = new ObservableCollection<ListViewItem>();
            LabelData = new ObservableCollection<ListViewItem>();
            Labels.Add(new ListViewItem {Content = "Graph Properties", FontWeight = FontWeights.Bold});
            LabelData.Add(new ListViewItem {Content = "Values", FontWeight = FontWeights.Bold});
            ViewModel = viewmodel;
            ParentGraph = parent;
            InitializeComponent();

            Loaded += GraphInfoView_Loaded;
        }

        public CollectionViewModel ViewModel { get; set; }
        public CollectionGraphView ParentGraph { get; }
        public double ConstantRadiusWidth { get; set; }
        public ObservableCollection<ListViewItem> Labels { get; set; }
        public ObservableCollection<ListViewItem> LabelData { get; set; }

        private void GraphInfoView_Loaded(object sender, RoutedEventArgs e)
        {
            CreateInfo();
        }

        private void CreateInfo()
        {
            var nodes = ViewModel.DocumentViewModels;
            var connections = ParentGraph.Connections;
            nodes.CollectionChanged += UpdateGraphInfo;
            connections.CollectionChanged += UpdateGraphInfo;

            // find the min and max amount of links
            int min = 0;
            int max = 0;
            if (nodes.Count != 0)
            {
                min = nodes.Min(i =>
                    (i.DataDocument.GetField<ListController<DocumentController>>(KeyStore.LinkToKey)?.Count ?? 0 +
                     i.DataDocument.GetField<ListController<DocumentController>>(KeyStore.LinkFromKey)?.Count) ?? 0);
                max = nodes.Max(i =>
                    (i.DataDocument.GetField<ListController<DocumentController>>(KeyStore.LinkToKey)?.Count ?? 0 +
                     i.DataDocument.GetField<ListController<DocumentController>>(KeyStore.LinkFromKey)?.Count) ?? 0);
            }

            // add all of the labels
            Labels.Add(new ListViewItem {Content = "Nodes: "});
            LabelData.Add(new ListViewItem
            {
                Content = nodes.Count.ToString(),
                Name = nameof(ViewModel.DocumentViewModels)
            });
            Labels.Add(new ListViewItem {Content = "Connections: "});
            LabelData.Add(new ListViewItem
            {
                Content = connections.Count.ToString(),
                Name = nameof(ParentGraph.Connections)
            });
            Labels.Add(new ListViewItem {Content = "Average: "});
            LabelData.Add(new ListViewItem
            {
                Content = (connections.Count / (double) nodes.Count).ToString(),
                Name = "Average"
            });
            Labels.Add(new ListViewItem {Content = "Min: "});
            LabelData.Add(new ListViewItem {Content = min.ToString(), Name = "Min"});
            Labels.Add(new ListViewItem {Content = "Max: "});
            LabelData.Add(new ListViewItem {Content = max.ToString(), Name = "Max"});
            Labels.Add(new ListViewItem {Content = "Range: "});
            LabelData.Add(new ListViewItem {Content = (max - min).ToString(), Name = "Range"});
        }

        private void UpdateGraphInfo(object sender, NotifyCollectionChangedEventArgs e)
        {
            // recalculates the labels when any nodes or collections are changed
            var nodes = ViewModel.DocumentViewModels;

            double min;
            double max;
            if (nodes.Count != 0)
            {
                min = nodes.Min(i =>
                    (i.DataDocument.GetField<ListController<DocumentController>>(KeyStore.LinkToKey)?.Count ?? 0 +
                     i.DataDocument.GetField<ListController<DocumentController>>(KeyStore.LinkFromKey)?.Count) ?? 0);
                max = nodes.Max(i =>
                    (i.DataDocument.GetField<ListController<DocumentController>>(KeyStore.LinkToKey)?.Count ?? 0 +
                     i.DataDocument.GetField<ListController<DocumentController>>(KeyStore.LinkFromKey)?.Count) ?? 0);
            }
            else
            {
                min = max = 0;
            }

            LabelData.First(i => i.Name.Equals(nameof(ViewModel.DocumentViewModels))).Content =
                nodes.Count.ToString();
            LabelData.First(i => i.Name.Equals("Min")).Content = min.ToString();
            LabelData.First(i => i.Name.Equals("Max")).Content = max.ToString();
            LabelData.First(i => i.Name.Equals("Range")).Content = (max - min).ToString();
            LabelData.First(i => i.Name.Equals("Average")).Content =
                (ParentGraph.Connections.Count / (double) ViewModel.DocumentViewModels.Count).ToString();
            LabelData.First(i => i.Name.Equals(nameof(ParentGraph.Connections))).Content =
                ParentGraph.Connections.Count;
        }

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