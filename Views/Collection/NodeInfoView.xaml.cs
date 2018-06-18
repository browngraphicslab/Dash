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
    public sealed partial class NodeInfoView : UserControl, INotifyPropertyChanged
    {
        public DocumentViewModel ViewModel { get; set; }
        public CollectionGraphView ParentGraph { get; private set; }
        public double ConstantRadiusWidth { get; set; }
        public ObservableCollection<ListViewItem> Keys { get; set; }
        public ObservableCollection<ListViewItem> Values { get; set; }
        /// <summary>
        /// Constructor
        /// </summary>

        public NodeInfoView(DocumentViewModel viewmodel, CollectionGraphView parent)
        {
            Keys = new ObservableCollection<ListViewItem>();
            Values = new ObservableCollection<ListViewItem>();
            Keys.Add(new ListViewItem { Content = "Keys", FontWeight = FontWeights.Bold });
            Values.Add(new ListViewItem { Content = "Values", FontWeight = FontWeights.Bold });
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
                     
                var keyValuePairs = ViewModel.DocumentController.GetDataDocument().EnumDisplayableFields();
                foreach (var kvp in keyValuePairs)
                {
                    Keys.Add(new ListViewItem { Content = kvp.Key.Name + ":" });
                    Values.Add(new ListViewItem { Content = kvp.Value.GetValue(null) });
                    
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
