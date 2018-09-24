using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Windows.ApplicationModel.DataTransfer;
using Windows.Foundation;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using static Dash.DataTransferTypeInfo;

// The User Control item template is documented at https://go.microsoft.com/fwlink/?LinkId=234236

namespace Dash
{
   public sealed partial class CollectionTreeView : ICollectionView
    {
        public UserControl UserControl => this;
        public CollectionViewModel ViewModel => DataContext as CollectionViewModel;

        public CollectionTreeView()
        {
            InitializeComponent();

            var snapshot = new ToolTip()
            {
                Content = "Snapshot Workspace",
                Placement = PlacementMode.Bottom,
                VerticalOffset = 5,
            };
            ToolTipService.SetToolTip(xSnapshot, snapshot);

            var newWorkspace = new ToolTip()
            {
                Content = "Add New Workspace",
                Placement = PlacementMode.Bottom,
                VerticalOffset = 5,
            };
            ToolTipService.SetToolTip(xAddWorkspace, newWorkspace);
        }

        private void XOnPointerEntered(object sender, PointerRoutedEventArgs e)
        {
            Window.Current.CoreWindow.PointerCursor = new Windows.UI.Core.CoreCursor(Windows.UI.Core.CoreCursorType.Hand, 1);
            if (sender is TextBlock button && ToolTipService.GetToolTip(button) is ToolTip tip) tip.IsOpen = true;
        }

        private void XOnPointerExited(object sender, PointerRoutedEventArgs e)
        {
            Window.Current.CoreWindow.PointerCursor = new Windows.UI.Core.CoreCursor(Windows.UI.Core.CoreCursorType.Arrow, 1);
            if (sender is TextBlock button && ToolTipService.GetToolTip(button) is ToolTip tip) tip.IsOpen = false;
        }

        private void AddWorkspace_OnTapped(object sender, TappedRoutedEventArgs e)
        {
            using (UndoManager.GetBatchHandle())
            {
                Debug.Assert(ViewModel != null, "ViewModel != null");
                var documentController = new CollectionNote(new Point(0, 0), CollectionView.CollectionViewType.Freeform, double.NaN, double.NaN).Document;
                ViewModel.AddDocument(documentController);
            }
        }

        public void Highlight(DocumentController document, bool? flag)
        {
            //TODO TreeView: Get highlighting working in some form
        }

        public void MakePdf_OnTapped(object sender, TappedRoutedEventArgs e)
        {
            //List of Document Controller - one document controller for each collection
            //so a data file is made for each element in this list
            var collectionDataDocs = ViewModel.CollectionController.TypedData.Select(dc => dc.GetDataDocument());

            ExportToTxt newExport = new ExportToTxt();

            //Now call function in ExportToTxt that converts all collections to files
           newExport.DashToTxt(collectionDataDocs);
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
            Textblock.Foreground = Textbox.Foreground = XFilterBox.Foreground = XTreeView.Foreground = dark
                    ? (SolidColorBrush) Application.Current.Resources["InverseTextColor"]
                    : (SolidColorBrush) Application.Current.Resources["MainText"];
        }

        public void Snapshot_OnTapped(object sender, TappedRoutedEventArgs e)
        {
            if (!(SplitFrame.ActiveFrame.ViewModel.Content is CollectionView cview)) return;

            MainPage.Instance.SnapshotOverlay.Visibility = Visibility.Visible;
            MainPage.Instance.FadeIn.Begin();
            MainPage.Instance.FadeOut.Begin();

            using (UndoManager.GetBatchHandle())
            {
                cview.ViewModel.ContainerDocument.CreateSnapshot();
            }
        }

        //From ICollectionView, TreeView doesn't explicitly need to do anything for this
        public void SetDropIndicationFill(Brush fill) { }

        private void XFilterBox_OnTextChanged(object sender, TextChangedEventArgs e)
        {
            XTreeView.FilterFunc = controller => controller.Title.IndexOf(XFilterBox.Text, StringComparison.OrdinalIgnoreCase) >= 0;
        }
    }
}
