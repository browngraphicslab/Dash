﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
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
using DashShared;

// The User Control item template is documented at https://go.microsoft.com/fwlink/?LinkId=234236

namespace Dash.Views.Collection
{
    public sealed partial class CollectionTreeView : UserControl
    {
        public CollectionTreeView()
        {
            this.InitializeComponent();
        }

        private void UIElement_OnTapped(object sender, TappedRoutedEventArgs e)
        {
            var cvm = DataContext as CollectionViewModel;
            Debug.Assert(cvm != null, "cvm != null");
            var documentController = new NoteDocuments.CollectionNote(null, new Point(0, 0), CollectionView.CollectionViewType.Freeform, "New Workspace " + cvm.CollectionController.Count);
            documentController.DataDocument.SetField(KeyStore.CollectionKey, new ListController<DocumentController>(), true);
            cvm.CollectionController.Add(documentController.Document);
        }
    }
}
