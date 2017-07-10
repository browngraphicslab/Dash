﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

// The User Control item template is documented at http://go.microsoft.com/fwlink/?LinkId=234236

namespace Dash
{
    public sealed partial class CollectionListView : UserControl
    {
        public CollectionListView(CollectionView view)
        {
            this.InitializeComponent();
            HListView.DragItemsStarting += view.xGridView_OnDragItemsStarting;
            HListView.DragItemsCompleted += view.xGridView_OnDragItemsCompleted;
            DataContextChanged += OnDataContextChanged;
        }

        private void OnDataContextChanged(FrameworkElement sender, DataContextChangedEventArgs args)
        {
            var vm = DataContext as CollectionViewModel;
            HListView.SelectionChanged += vm.SelectionChanged;
        }
    }
}
