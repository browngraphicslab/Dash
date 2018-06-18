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
    public sealed partial class NodeConnectionsView : UserControl, INotifyPropertyChanged
    {
        public DocumentViewModel ViewModel { get; set; }
        public CollectionGraphView ParentGraph { get; private set; }
        public double ConstantRadiusWidth { get; set; }
        public ObservableCollection<ListViewItem> ToConnections { get; set; }
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
            var toLinks = ParentGraph.AdjacencyLists[ViewModel];
            foreach (var doc in toLinks)
            {
                foreach (var connection in ParentGraph.Links)
                {
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
                        else
                        {
                            connection.Stroke = new SolidColorBrush(Color.FromArgb(255, 149, 174, 214));
                        }
                        connection.Thickness = 4;
                    }
                }

                var lvi = new ListViewItem
                {
                    Content =
                        doc.DocumentController.GetDereferencedField<TextController>(KeyStore.TitleKey, null).Data,
                    DataContext = doc
                };
                lvi.Tapped += ListItem_Tapped;
                ToConnections.Add(lvi);
            }

            var allLinks = ParentGraph.Connections;
            foreach (var link in allLinks)
            {
                if (link.Value.Equals(ViewModel))
                {
                    foreach (var connection in ParentGraph.Links)
                    {
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
                                connection.Stroke = new SolidColorBrush(Color.FromArgb(255, 214, 153, 149));
                            }

                            connection.Thickness = 4;
                        }
                    }

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

            if (ToConnections.Count <= 1)
            {
                xLinkToDocs.Visibility = Visibility.Collapsed;
            }

            if (FromConnections.Count <= 1)
            {
                xLinkFromDocs.Visibility = Visibility.Collapsed;
            }
        }

        private void ListItem_Tapped(object sender, TappedRoutedEventArgs tappedRoutedEventArgs)
        {
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
