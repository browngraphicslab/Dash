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
using Windows.UI.Text;
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
    public sealed partial class GraphInfoView : UserControl, INotifyPropertyChanged
    {
        public CollectionViewModel ViewModel { get; set; }
        public CollectionGraphView ParentGraph { get; private set; }
        public double ConstantRadiusWidth { get; set; }
        public ObservableCollection<ListViewItem> Labels { get; set; }
        public ObservableCollection<ListViewItem> LabelData { get; set; }
        
        /// <summary>
        /// Constructor
        /// </summary>

        public GraphInfoView(CollectionViewModel viewmodel, CollectionGraphView parent)
        {
            Labels = new ObservableCollection<ListViewItem>();
            LabelData = new ObservableCollection<ListViewItem>();
            Labels.Add(new ListViewItem { Content = "Graph Properties", FontWeight = FontWeights.Bold });
            LabelData.Add(new ListViewItem { Content = "Values", FontWeight = FontWeights.Bold });
            ViewModel = viewmodel;
            ParentGraph = parent;
            InitializeComponent();
            
            Loaded += GraphInfoView_Loaded;
            Unloaded += GraphInfoView_Unloaded;
            ConstantRadiusWidth = 50;
        }

        private void GraphInfoView_Unloaded(object sender, RoutedEventArgs e)
        {
            ViewModel.PropertyChanged -= ViewModel_PropertyChanged;
        }

        private void ViewModel_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
           
        }


        #region loading

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
            var nodes = ViewModel.DocumentViewModels;
            var connections = ParentGraph.Connections;
            var min = nodes.Min(i =>
                (i.DataDocument.GetField<ListController<DocumentController>>(KeyStore.LinkToKey)?.Count ?? 0 +
                 i.DataDocument.GetField<ListController<DocumentController>>(KeyStore.LinkFromKey)?.Count) ?? 0);
            var max = nodes.Max(i =>
                (i.DataDocument.GetField<ListController<DocumentController>>(KeyStore.LinkToKey)?.Count ?? 0 +
                 i.DataDocument.GetField<ListController<DocumentController>>(KeyStore.LinkFromKey)?.Count) ?? 0);

            Labels.Add(new ListViewItem{ Content = "Nodes: "});
            LabelData.Add(new ListViewItem { Content = nodes.Count.ToString() });
            Labels.Add(new ListViewItem { Content = "Connections: "});
            LabelData.Add(new ListViewItem { Content = connections.Count.ToString() });
            Labels.Add(new ListViewItem {Content = "Average: "});
            LabelData.Add(new ListViewItem { Content = ((double) ((double) connections.Count / (double) nodes.Count)).ToString() });
            Labels.Add(new ListViewItem { Content = "Min: " });
            LabelData.Add(new ListViewItem { Content = min.ToString() });
            Labels.Add(new ListViewItem { Content = "Max: " });
            LabelData.Add(new ListViewItem { Content = max.ToString() });
            Labels.Add(new ListViewItem { Content = "Range: " });
            LabelData.Add(new ListViewItem { Content = (max - min).ToString() });
        }
    

        private void DocumentViewModels_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
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
