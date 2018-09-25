using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using Windows.UI.Text;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;
using Dash.Annotations;

// The User Control item template is documented at https://go.microsoft.com/fwlink/?LinkId=234236

namespace Dash
{
    public sealed partial class HiddenNodesView : UserControl, INotifyPropertyChanged
    {
        public CollectionGraphView ParentGraph { get; private set; }
        
        public ObservableCollection<ListViewItem> HiddenNodesList { get; set; }
        private ObservableCollection<GraphNodeView> HiddenNodes { get; set; }
      
        /// <summary>
        /// Constructor
        /// </summary>

        public HiddenNodesView(CollectionGraphView parent)
        {
            HiddenNodesList = new ObservableCollection<ListViewItem>();
            HiddenNodes = new ObservableCollection<GraphNodeView>();
            HiddenNodesList.Add(new ListViewItem { Content = "Hidden Nodes:", FontWeight = FontWeights.Bold });
            ParentGraph = parent;
            InitializeComponent();
        }
        

        public void AddNode(GraphNodeView gnv)
        {
            // we don't have any hidden nodes before this, add the title
            if (HiddenNodesList.Count == 0)
            {
                HiddenNodesList.Add(new ListViewItem { Content = "Hidden Nodes:", FontWeight = FontWeights.Bold });
            }
            HiddenNodes.Add(gnv);

            // create a new list view item with the title as the content and the graph node view as data context
            var lvi = new ListViewItem
            {
                Content =
                    gnv.ViewModel.DocumentViewModel.DocumentController.GetDereferencedField<TextController>(
                        KeyStore.TitleKey, null),
                DataContext = gnv
            };
            lvi.DoubleTapped += Node_DoubleTapped;
            HiddenNodesList.Add(lvi);

            // hide the graph node view
            gnv.Visibility = Visibility.Collapsed;
            // hide any links connected to the graph node view
            foreach (var link in ParentGraph.Links)
            {
                if (link.FromDoc.Equals(gnv) || link.ToDoc.Equals(gnv))
                {
                    link.Connection.Visibility = Visibility.Collapsed;
                }
            }

            xButton.Visibility = Visibility.Visible;
        }

        private void Node_DoubleTapped(object sender, DoubleTappedRoutedEventArgs doubleTappedRoutedEventArgs)
        {
            if ((sender as ListViewItem)?.DataContext is GraphNodeView gnv)
            {
                // unhide the graph node view and remove it from the list of hidden nodes
                gnv.Visibility = Visibility.Visible;
                HiddenNodesList.Remove((ListViewItem) sender);
                HiddenNodes.Remove(gnv);

                foreach (var link in ParentGraph.Links)
                {
                    // unhide any links if both the from and to documents are visible
                    if ((link.FromDoc.Equals(gnv) && link.ToDoc.Visibility == Visibility.Visible) ||
                        (link.ToDoc.Equals(gnv) && link.FromDoc.Visibility == Visibility.Visible))
                    {
                        link.Connection.Visibility = Visibility.Visible;
                    }
                }

                // if there's nothing left in the hidden nodes list, we can remove it
                if (HiddenNodesList.Count == 1)
                    ParentGraph.xInfoPanel.Children.Remove(this);
            }
        }
        
        #region property changed

        public event PropertyChangedEventHandler PropertyChanged;

        [NotifyPropertyChangedInvocator]
        private void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        #endregion

        private void ButtonBase_OnClick(object sender, RoutedEventArgs e)
        {
            foreach (var node in HiddenNodes)
            {
                node.Visibility = Visibility.Visible;
            }
            foreach (var link in ParentGraph.Links)
            {
                link.Connection.Visibility = Visibility.Visible;
                
            }

            ParentGraph.xInfoPanel.Children.Remove(this);
        }
    }
}
