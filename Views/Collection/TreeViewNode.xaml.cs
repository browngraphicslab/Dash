using System;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using Windows.ApplicationModel.DataTransfer;
using Windows.System;
using Windows.UI;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Imaging;
using Microsoft.Toolkit.Uwp.UI.Animations;

// The User Control item template is documented at https://go.microsoft.com/fwlink/?LinkId=234236

namespace Dash
{
    public sealed partial class TreeViewNode : UserControl
    {

        public static readonly DependencyProperty FilterStringProperty = DependencyProperty.Register(
            "FilterString", typeof(string), typeof(TreeViewNode), new PropertyMetadata(default(string)));

        public string FilterString
        {
            get => (string) GetValue(FilterStringProperty);
            set => SetValue(FilterStringProperty, value);
        }

        public static readonly DependencyProperty ContainingDocumentProperty = DependencyProperty.Register(
            "ContainingDocument", typeof(DocumentController), typeof(TreeViewNode), new PropertyMetadata(default(DocumentController)));

        public DocumentController ContainingDocument
        {
            get => (DocumentController) GetValue(ContainingDocumentProperty);
            set => SetValue(ContainingDocumentProperty, value);
        }

        public static readonly DependencyProperty SortCriterionProperty = DependencyProperty.Register(
            "SortCriterion", typeof(string), typeof(TreeViewNode), new PropertyMetadata("YPos"));

        public string SortCriterion
        {
            get => (string)GetValue(SortCriterionProperty);
            set => SetValue(SortCriterionProperty, value);
        }
        public DocumentViewModel ViewModel => DataContext as DocumentViewModel;

        private readonly ObservableCollection<SnapshotView> _items = new ObservableCollection<SnapshotView>();

        public TreeViewNode()
        {
            InitializeComponent();
        }

        private void XOnPointerEntered(object sender, PointerRoutedEventArgs e)
        {
            Windows.UI.Xaml.Window.Current.CoreWindow.PointerCursor = new Windows.UI.Core.CoreCursor(Windows.UI.Core.CoreCursorType.Hand, 1);
            if (sender is Grid button && ToolTipService.GetToolTip(button) is ToolTip tip) tip.IsOpen = true;
            if (sender is FontIcon icon && ToolTipService.GetToolTip(icon) is ToolTip tipIcon) tipIcon.IsOpen = true;
        }

        private void XOnPointerExited(object sender, PointerRoutedEventArgs e)
        {
            Windows.UI.Xaml.Window.Current.CoreWindow.PointerCursor = new Windows.UI.Core.CoreCursor(Windows.UI.Core.CoreCursorType.Arrow, 1);
            if (sender is Grid button && ToolTipService.GetToolTip(button) is ToolTip tip) tip.IsOpen = false;
            if (sender is FontIcon icon && ToolTipService.GetToolTip(icon) is ToolTip tipIcon) tipIcon.IsOpen = false;
        }
        
        private DocumentViewModel oldViewModel = null;
        private void TreeViewNode_OnDataContextChanged(FrameworkElement sender, DataContextChangedEventArgs args)
        {
            if (Equals(args.NewValue, oldViewModel))
            {
                return;
            }
            if (args.NewValue != null)
            {
                var dvm = (DocumentViewModel) args.NewValue;
                oldViewModel = dvm;
                
                var collection = dvm.DocumentController.GetDataDocument().GetField(KeyStore.DataKey) as ListController<DocumentController>;

                if (collection != null)
                {
                    var collectionViewModel = new CollectionViewModel(dvm.DocumentController.GetDataDocument(), KeyStore.DataKey);
                    CollectionTreeView.DataContext = collectionViewModel;
                    CollectionTreeView.ContainingDocument = dvm.DocumentController.GetDataDocument();
                    //xArrowBlock.Text = (string)Application.Current.Resources["ExpandArrowIcon"];
                }
                else
                {
                    //xArrowBlock.Text = "";
                    CollectionTreeView.DataContext = null;
                    CollectionTreeView.Visibility = Visibility.Collapsed;
                }
            }
        }

        private class SelectedToColorConverter : SafeDataToXamlConverter<double, Brush>
        {
            private readonly SolidColorBrush _unselectedBrush = new SolidColorBrush(Colors.Transparent);
            private readonly SolidColorBrush _selectedBrush = new SolidColorBrush(Color.FromArgb(0x35, 0xFF, 0xFF, 0xFF));
            public override Brush ConvertDataToXaml(double data, object parameter = null)
            {
                return data == 0 ? _unselectedBrush : _selectedBrush;
            }

            public override double ConvertXamlToData(Brush xaml, object parameter = null)
            {
                throw new NotImplementedException();
            }
        }
        private void XArrowBlock_OnTapped(object sender, TappedRoutedEventArgs e)
        { 
            e.Handled = true;
            //Toggle visibility
            if (CollectionTreeView.Visibility == Visibility.Collapsed)
            {
                CollectionTreeView.Visibility = Visibility.Visible;
            }
            else
            {
                CollectionTreeView.Visibility = Visibility.Collapsed;
            }
        }

        private void XTextBlock_PointerEntered(object sender, PointerRoutedEventArgs e)
        {
            var docTapped = (DataContext as DocumentViewModel).DocumentController;
            SplitFrame.HighlightDoc(docTapped, SplitFrame.HighlightMode.Highlight);
        }

        private void XTextBlock_PointerExited(object sender, PointerRoutedEventArgs e)
        {
            var docTapped = (DataContext as DocumentViewModel).DocumentController;
            SplitFrame.HighlightDoc(docTapped, SplitFrame.HighlightMode.Unhighlight);
        }

        private void XTextBlock_Tapped(object sender, TappedRoutedEventArgs e)
        {
            //var docTapped = (DataContext as DocumentViewModel).DocumentController;
            //MainPage.Instance.HighlightDoc(docTapped);
        }

        private void XTextBlock_OnDoubleTapped(object sender, DoubleTappedRoutedEventArgs e)
        {
            MainPage.Instance.ToggleSettingsVisibility(false);
            e.Handled = true;
            var docToFocus = (DataContext as DocumentViewModel).DocumentController;
            //if (! MainPage.Instance.NavigateToDocumentInWorkspaceAnimated(docToFocus, false))
            //    MainPage.Instance.SetCurrentWorkspace(docToFocus);
            //TODO TreeView
        }

        private void XTextBlock_OnDragStarting(UIElement sender, DragStartingEventArgs args)
        {
            args.Data.SetDragModel(new DragDocumentModel((DataContext as DocumentViewModel)?.DocumentController));
            args.AllowedOperations = DataPackageOperation.Link | DataPackageOperation.Copy;
        }

        public void DeleteDocument()
        {
            var collTreeView = this.GetFirstAncestorOfType<TreeViewCollectionNode>();
            var cvm = collTreeView.ViewModel;
            var doc = ViewModel.DocumentController;
            cvm.RemoveDocument(doc);
            cvm.ContainerDocument.GetDereferencedField<ListController<DocumentController>>(KeyStore.DataKey, null)
                ?.Remove(doc);//TODO Kind of a hack
        }

        private void Delete_OnClick(object sender, RoutedEventArgs e)
        {
            DeleteDocument();
        }
        
        private void Rename_OnClick(object sender, RoutedEventArgs e)
        {
            UndoManager.StartBatch();
        }
        

        private void Open_OnClick(object sender, RoutedEventArgs e)
        {
            SplitFrame.OpenInActiveFrame((DataContext as DocumentViewModel).DocumentController);
        }

        private void XTextBox_OnLostFocus(object sender, RoutedEventArgs e)
        {
            UndoManager.EndBatch();
        }


        private void XTextBox_OnKeyUp(object sender, KeyRoutedEventArgs e)
        {
            //finish rename on enter
            if (e.Key == VirtualKey.Enter)
            {
            }
        }

        private void XTextBox_LosingFocus(UIElement sender, LosingFocusEventArgs args)
        {
            if (args.NewFocusedElement == this.GetFirstAncestorOfType<ListViewItem>())
                args.Cancel = true;
        }

        private void UIElement_OnDoubleTapped(object sender, DoubleTappedRoutedEventArgs e)
        {
            var item = (sender as StackPanel)?.DataContext as SnapshotView;
            var itemNum = item.Index;
            MainPage.Instance.ToggleSettingsVisibility(false);
            var snaps = (ViewModel.DocumentController.GetDataDocument().GetField(KeyStore.SnapshotsKey) as
                ListController<DocumentController>);
            if (snaps != null && snaps.Count > itemNum)
            {
                var docToFocus = snaps[itemNum];
                //if (!MainPage.Instance.NavigateToDocumentInWorkspaceAnimated(docToFocus, false))
                //    MainPage.Instance.SetCurrentWorkspace(docToFocus);
                //TODO TreeView
            }
        }

        private void TextBox_OnKeyDown(object sender, KeyRoutedEventArgs e)
        {
            //snapshot title changed
            var newKey = e.Key.ToString().ToLower();
            newKey = newKey == "enter" ? "" : newKey;
            var text = (sender as TextBox).Text;
            var newTitle = text + newKey;
            newTitle = newKey == "back" ? text.Substring(0, text.Length ) : newTitle;

            var item = (sender as TextBox)?.DataContext as SnapshotView;
            item.Title = newTitle;

            (ViewModel.DocumentController.GetDataDocument().GetField(KeyStore.SnapshotsKey) as
                ListController<DocumentController>)?[item.Index]?.SetField<TextController>(KeyStore.DateModifiedKey, newTitle, true);
        }

        private void xControlIcon_DragStarting(UIElement uiElement, DragStartingEventArgs args)
        {
            var snapshots = ViewModel.DataDocument.GetField<ListController<DocumentController>>(KeyStore.SnapshotsKey);
            foreach (DocumentController d in snapshots)
            {
                d.SetWidth(200);
                d.SetHeight(200);
            }
            DocumentViewModel dvm = ViewModel;
            args.Data.SetDragModel(new DragFieldModel(new DocumentFieldReference(dvm.DataDocument, KeyStore.SnapshotsKey)));
            args.Data.RequestedOperation = DataPackageOperation.Move | DataPackageOperation.Copy | DataPackageOperation.Link;
        }

        private void ListViewBase_OnDragItemsStarting(object sender, DragItemsStartingEventArgs e)
        {
            if (e.Items.Count.Equals(0)) return;
            var first = (SnapshotView) e.Items.First();
            var snapshots = ViewModel.DataDocument.GetField<ListController<DocumentController>>(KeyStore.SnapshotsKey);
            e.Data.SetDragModel(new DragDocumentModel(snapshots[first.Index]));
            e.Data.RequestedOperation = DataPackageOperation.Move | DataPackageOperation.Copy | DataPackageOperation.Link;
        }
    }
}
