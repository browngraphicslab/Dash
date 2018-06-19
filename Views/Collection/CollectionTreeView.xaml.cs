using Dash.Models.DragModels;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Reflection;
using Windows.ApplicationModel.DataTransfer;
using Windows.Storage;
using Windows.Storage.Pickers;
using Windows.UI;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using DashShared;
using Color = Windows.UI.Color;
using Point = Windows.Foundation.Point;
using Visibility = Windows.UI.Xaml.Visibility;

// The User Control item template is documented at https://go.microsoft.com/fwlink/?LinkId=234236

namespace Dash
{
   public sealed partial class CollectionTreeView : ICollectionView
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
            UndoManager.startBatch();
            Debug.Assert(ViewModel != null, "ViewModel != null");
            var documentController = new CollectionNote(new Point(0, 0), CollectionView.CollectionViewType.Freeform, double.NaN, double.NaN).Document;//, "New Workspace " + cvm.CollectionController.Count);
            ViewModel.ContainerDocument.GetField<ListController<DocumentController>>(KeyStore.DataKey)?.Add(documentController);
            UndoManager.endBatch();
        }

        public void Highlight(DocumentController document, bool? flag)
        {
            xTreeRoot.Highlight(document, flag);
        }

        private async void MakePdf_OnTapped(object sender, TappedRoutedEventArgs e)
        {
            //List of Document Controller - one document controller for each collection
            //so a data file is made for each element in this list
            var collectionDataDocs = ViewModel.CollectionController.TypedData.Select(dc => dc.GetDataDocument());

            ExportToTxt newExport = new ExportToTxt();

            //Now call function in ExportToTxt that converts all collections to files
           newExport.DashToTxt(collectionDataDocs);
        }
        
        private void TogglePresentationMode(object sender, TappedRoutedEventArgs e)
        {
            MainPage.Instance.TogglePresentationMode();
        }

        public void TogglePresentationMode(bool on)
        {
            presentationModeButton.Background = on ? (SolidColorBrush) Application.Current.Resources["AccentGreenLight"] : (SolidColorBrush) Application.Current.Resources["AccentGreen"];
        }

        // This does not change the title of the underlying collection.
        public void ChangeTreeViewTitle(string title)
        {
            Textblock.Text = title;
            Textbox.Text = title;
        }

        private void Textblock_OnDoubleTapped(object sender, DoubleTappedRoutedEventArgs e)
        {
            TriggerTextVisibility(true);
        }

        private void Textbox_OnLostFocus(object sender, RoutedEventArgs e)
        {
            ChangeTreeViewTitle(Textbox.Text);
            TriggerTextVisibility(false);
        }

        private void TriggerTextVisibility(bool turnEditingOn)
        {
            Textblock.Visibility = turnEditingOn ? Visibility.Collapsed : Visibility.Visible;
            Textbox.Visibility = turnEditingOn ? Visibility.Visible : Visibility.Collapsed;
        }

        public void ToggleDarkMode(bool dark)
        {
            xTreeGrid.Background = dark ? 
                (SolidColorBrush) Application.Current.Resources["WindowsBlue"] : (SolidColorBrush) Application.Current.Resources["DocumentBackgroundColor"];
            Textblock.Foreground = Textbox.Foreground = XFilterBox.Foreground = xTreeRoot.Foreground = dark
                    ? (SolidColorBrush) Application.Current.Resources["InverseTextColor"]
                    : (SolidColorBrush) Application.Current.Resources["MainText"];
        }
    }
}
