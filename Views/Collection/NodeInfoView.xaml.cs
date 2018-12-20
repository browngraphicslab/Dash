using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using Windows.UI.Text;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Dash.Annotations;

// The User Control item template is documented at https://go.microsoft.com/fwlink/?LinkId=234236

namespace Dash
{
    public sealed partial class NodeInfoView : UserControl, INotifyPropertyChanged
    {
        /// <summary>
        ///     Constructor
        /// </summary>
        public NodeInfoView(DocumentViewModel viewmodel, CollectionGraphView parent)
        {
            Keys = new ObservableCollection<ListViewItem>();
            Values = new ObservableCollection<ListViewItem>();
            Keys.Add(new ListViewItem {Content = "Keys", FontWeight = FontWeights.Bold});
            Values.Add(new ListViewItem {Content = "Values", FontWeight = FontWeights.Bold});
            ViewModel = viewmodel;
            ParentGraph = parent;
            InitializeComponent();

            Loaded += GraphInfoView_Loaded;
        }

        public DocumentViewModel ViewModel { get; set; }
        public CollectionGraphView ParentGraph { get; }
        public double ConstantRadiusWidth { get; set; }
        public ObservableCollection<ListViewItem> Keys { get; set; }
        public ObservableCollection<ListViewItem> Values { get; set; }


        #region loading

        private void GraphInfoView_Loaded(object sender, RoutedEventArgs e)
        {
            CreateInfo();
        }
        
        private void CreateInfo()
        {
            // add something akin to key value pane as a panel
            var keyValuePairs = ViewModel.DocumentController.GetDataDocument().EnumDisplayableFields();
            foreach (var kvp in keyValuePairs)
            {
                Keys.Add(new ListViewItem {Content = kvp.Key.Name + ":"});
                Values.Add(new ListViewItem {Content = kvp.Value.GetValue()});
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
