using System;
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
using Dash.Models.DragModels;
using Windows.ApplicationModel.DataTransfer;

// The User Control item template is documented at https://go.microsoft.com/fwlink/?LinkId=234236

namespace Dash
{
    public sealed partial class CollectionTreeView : UserControl
    {

        public CollectionViewModel ViewModel => DataContext as CollectionViewModel;

        public CollectionTreeView()
        {
            this.InitializeComponent();
            this.AllowDrop = true;
            this.Drop += CollectionTreeView_Drop;
            this.DragOver += CollectionTreeView_DragOver;
        }

        private void CollectionTreeView_DragOver(object sender, DragEventArgs e)
        {
            if (e.DataView.Properties.ContainsKey(nameof(DragDocumentModel)) || e.DataView.Properties.ContainsKey(nameof(List<DragDocumentModel>)))
            {
                e.AcceptedOperation = e.DataView.RequestedOperation == DataPackageOperation.None ? DataPackageOperation.Copy : e.DataView.RequestedOperation;
            }
            else
                e.AcceptedOperation = DataPackageOperation.None;
            e.Handled = true;
        }

        private void CollectionTreeView_Drop(object sender, DragEventArgs e)
        {
            Debug.Assert(ViewModel != null, "ViewModel != null");
            var dvm = e.DataView.Properties.ContainsKey(nameof(DragDocumentModel)) ? e.DataView.Properties[nameof(DragDocumentModel)] as DragDocumentModel : null;
            if (dvm != null)
                ViewModel.ContainerDocument.GetField<ListController<DocumentController>>(KeyStore.DataKey)?.Add(dvm.DraggedDocument);
            e.Handled = true;
        }

        private void UIElement_OnTapped(object sender, TappedRoutedEventArgs e)
        {
            Debug.Assert(ViewModel != null, "ViewModel != null");
            var documentController = new NoteDocuments.CollectionNote(new Point(0, 0), CollectionView.CollectionViewType.Freeform, double.NaN, double.NaN);//, "New Workspace " + cvm.CollectionController.Count);
            ViewModel.ContainerDocument.GetField<ListController<DocumentController>>(KeyStore.DataKey)?.Add(documentController.Document);
            
        }
    }
}
