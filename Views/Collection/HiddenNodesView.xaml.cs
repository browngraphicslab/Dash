﻿using System;
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
            if (HiddenNodesList.Count == 0)
            {
                HiddenNodesList.Add(new ListViewItem { Content = "Hidden Nodes:", FontWeight = FontWeights.Bold });
            }
            HiddenNodes.Add(gnv);
            var lvi = new ListViewItem
            {
                Content =
                    gnv.ViewModel.DocumentViewModel.DocumentController.GetDereferencedField<TextController>(
                        KeyStore.TitleKey, null),
                DataContext = gnv
            };
            lvi.DoubleTapped += Node_DoubleTapped;
            HiddenNodesList.Add(lvi);
            gnv.Visibility = Visibility.Collapsed;

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
                gnv.Visibility = Visibility.Visible;
                HiddenNodesList.Remove((ListViewItem) sender);
                HiddenNodes.Remove(gnv);

                foreach (var link in ParentGraph.Links)
                {
                    if ((link.FromDoc.Equals(gnv) && link.ToDoc.Visibility == Visibility.Visible) ||
                        (link.ToDoc.Equals(gnv) && link.FromDoc.Visibility == Visibility.Visible))
                    {
                        link.Connection.Visibility = Visibility.Visible;
                    }
                }

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
