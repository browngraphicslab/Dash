using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Windows.ApplicationModel.DataTransfer;
using Windows.Foundation;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Dash.Models.DragModels;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;

// The User Control item template is documented at https://go.microsoft.com/fwlink/?LinkId=234236

namespace Dash
{
   public sealed partial class CollectionTreeView : ICollectionView
    {
        public UserControl UserControl => this;
        public CollectionViewModel ViewModel => DataContext as CollectionViewModel;

        public List<TreeViewNode> TreeViewNodes = new List<TreeViewNode>();
        private ToolTip _snapshot;
        private ToolTip _newWorkspace;

        public CollectionTreeView()
        {
            InitializeComponent();
            AllowDrop = true;
            Drop += CollectionTreeView_Drop;
            DragOver += CollectionTreeView_DragOver;
            SetupTooltips();
        }

        private void SetupTooltips()
        {
            _snapshot = new ToolTip()
            {
                Content = "Snapshot Workspace",
                Placement = PlacementMode.Bottom,
                VerticalOffset = 5,
            };
            ToolTipService.SetToolTip(xSnapshot, _snapshot);

            _newWorkspace = new ToolTip()
            {
                Content = "Add New Workspace",
                Placement = PlacementMode.Bottom,
                VerticalOffset = 5,
            };
            ToolTipService.SetToolTip(xAddWorkspace, _newWorkspace);
        }

        private void XOnPointerEntered(object sender, PointerRoutedEventArgs e)
        {
            Windows.UI.Xaml.Window.Current.CoreWindow.PointerCursor = new Windows.UI.Core.CoreCursor(Windows.UI.Core.CoreCursorType.Hand, 1);
            if (sender is TextBlock button && ToolTipService.GetToolTip(button) is ToolTip tip) tip.IsOpen = true;
        }

        private void XOnPointerExited(object sender, PointerRoutedEventArgs e)
        {
            Windows.UI.Xaml.Window.Current.CoreWindow.PointerCursor = new Windows.UI.Core.CoreCursor(Windows.UI.Core.CoreCursorType.Arrow, 1);
            if (sender is TextBlock button && ToolTipService.GetToolTip(button) is ToolTip tip) tip.IsOpen = false;
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

        private void AddWorkspace_OnTapped(object sender, TappedRoutedEventArgs e)
        {
            using (UndoManager.GetBatchHandle())
            {
                Debug.Assert(ViewModel != null, "ViewModel != null");
                var documentController = new CollectionNote(new Point(0, 0), CollectionView.CollectionViewType.Freeform, double.NaN, double.NaN).Document;
                ViewModel.ContainerDocument.GetField<ListController<DocumentController>>(KeyStore.DataKey)?.Add(documentController);
            }
        }

        public void Highlight(DocumentController document, bool? flag) => xTreeRoot.Highlight(document, flag);

        public async void MakePdf_OnTapped(object sender, TappedRoutedEventArgs e)
        {
            //List of Document Controller - one document controller for each collection
            //so a data file is made for each element in this list
            var collectionDataDocs = ViewModel.CollectionController.TypedData.Select(dc => dc.GetDataDocument());

            ExportToTxt newExport = new ExportToTxt();

            //Now call function in ExportToTxt that converts all collections to files
           newExport.DashToTxt(collectionDataDocs);
        }
        
        public void TogglePresentationMode(object sender, TappedRoutedEventArgs e)
        {
            MainPage.Instance.SetPresentationState(MainPage.Instance.CurrPresViewState == MainPage.PresentationViewState.Collapsed);
        }

        //public void TogglePresentationMode(bool on)
        //{
        //    presentationModeButton.Background = on ? (SolidColorBrush) Application.Current.Resources["AccentGreenLight"] : (SolidColorBrush) Application.Current.Resources["AccentGreen"];
        //}

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

        public void Snapshot_OnTapped(object sender, TappedRoutedEventArgs e)
        {
            MainPage.Instance.SnapshotOverlay.Visibility = Visibility.Visible;
            MainPage.Instance.FadeIn.Begin();
            MainPage.Instance.FadeOut.Begin();

            if (!(SplitFrame.ActiveFrame.GetFirstDescendantOfType<CollectionFreeformView>() is CollectionFreeformView freeFormView)) return;

            DocumentController snapshot = freeFormView.Snapshot();
            DocumentController freeFormDoc = freeFormView.ViewModel.ContainerDocument.GetDataDocument();
            var snapshots = freeFormDoc.GetDereferencedField<ListController<DocumentController>>(KeyStore.SnapshotsKey, null);

            if (snapshots == null)
            {
                var nsnapshots = new List<DocumentController> {snapshot};
                freeFormDoc.SetField(KeyStore.SnapshotsKey, new ListController<DocumentController>(nsnapshots), true);
            }
            else snapshots.Add(snapshot);
                
            foreach (TreeViewNode node in TreeViewNodes)
            {
                node.UpdateSnapshots();
            }
        }

        public void SetDropIndicationFill(Brush fill) { }
    }
}
