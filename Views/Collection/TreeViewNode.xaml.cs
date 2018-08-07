using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using Windows.ApplicationModel.DataTransfer;
using Windows.System;
using Windows.UI;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Imaging;
using Dash.Models.DragModels;
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
        private bool snapStarted;

        public TreeViewNode()
        {
            this.InitializeComponent();
            MainPage.Instance.xMainTreeView.TreeViewNodes.Add(this);
          focusOnSelected();
        }

        public async void NewSnapshot()
        {
            var snapshots =
                (ViewModel.DocumentController.GetDataDocument().GetField(KeyStore.SnapshotsKey) as
                    ListController<DocumentController>);
            var index = _items.Count;
            var doc = snapshots?[index];

            string image = null;
            string time = null;
            if (snapshots != null)
            {
                image = doc.GetField<TextController>(KeyStore.SnapshotImage, true)?.Data;
                time = doc.GetField<TextController>(KeyStore.DateModifiedKey, true)?.Data;
            }

            if (image == null)
            {
                image = await Util.ExportAsImage(MainPage.Instance.MainDocView, "snapshot.png", true);
                doc?.SetField<TextController>(KeyStore.SnapshotImage, image, true);
                time = DateTime.Now.ToString(new CultureInfo("en-US"));
                doc?.SetField<TextController>(KeyStore.DateModifiedKey, time, true);
            }

            var newSnapshot = new SnapshotView(time, Windows.Storage.ApplicationData.Current.LocalFolder.Path + "\\" + image, index);
            _items.Add(newSnapshot);
            snapStarted = false;

        }

        public void UpdateSnapshots()
        {
            var dvm = ViewModel;
            var snapshots = dvm?.DocumentController.GetDataDocument().GetField(KeyStore.SnapshotsKey) as ListController<DocumentController>;
            if (snapshots != null && snapshots.Count > _items.Count && !snapStarted)
            {
                snapStarted = true;
                XSnapshotArrowBlock.Visibility = Visibility.Visible;

                NewSnapshot();
            }

            if (snapshots == null || snapshots.Count == 0)
            {
                XSnapshotArrowBlock.Visibility = Visibility.Collapsed;
            }
        }

        private void snapshotsFieldUpdated(DocumentController sender, DocumentController.DocumentFieldUpdatedEventArgs args, Context context)
        {
            UpdateSnapshots();
        }

        private DocumentViewModel oldViewModel = null;
        private void TreeViewNode_OnDataContextChanged(FrameworkElement sender, DataContextChangedEventArgs args)
        {
            oldViewModel?.DocumentController.GetDataDocument().RemoveFieldUpdatedListener(KeyStore.SnapshotsKey, snapshotsFieldUpdated);
            if (Equals(args.NewValue, oldViewModel))
            {
                var dvm = (DocumentViewModel)args.NewValue;
                if (dvm != null)
                {
                    dvm.DocumentController.GetDataDocument().AddFieldUpdatedListener(KeyStore.SnapshotsKey, snapshotsFieldUpdated);
                    snapshotsFieldUpdated(null, null, null);
                }
                return;
            }
            if (args.NewValue != null)
            {
                var dvm = (DocumentViewModel) args.NewValue;
                oldViewModel = dvm;

                var textBlockBinding = new FieldBinding<TextController>
                {
                    Document = dvm.DataDocument,
                    Key = KeyStore.TitleKey,
                    FallbackValue = "Untitled",
                    Mode = BindingMode.OneWay,
                    Context = new Context(dvm.DocumentController.GetDataDocument()),
                    Tag = "TreeViewNode text block binding"
                };

                var textBoxBinding = new FieldBinding<TextController>
                {
                    Document = dvm.DataDocument,
                    Key = KeyStore.TitleKey,
                    FallbackValue = "Untitled",
                    Mode = BindingMode.TwoWay,
                    Context = new Context(dvm.DocumentController.GetDataDocument()),
                    FieldAssignmentDereferenceLevel = XamlDereferenceLevel.DontDereference,
                    Tag = "TreeViewNode text box binding"
                };

                var headerBinding = new FieldBinding<NumberController>
                {
                    Document = dvm.DocumentController,
                    Key = KeyStore.SelectedKey,
                    FallbackValue = new SolidColorBrush(Colors.Transparent),
                    Mode = BindingMode.OneWay,
                    Converter = new SelectedToColorConverter()
                };
                
                var collection = dvm.DocumentController.GetDataDocument().GetField(KeyStore.DataKey) as ListController<DocumentController>;
                var snapshots = dvm.DocumentController.GetDataDocument().GetField(KeyStore.SnapshotsKey) as ListController<DocumentController>;

                if (collection != null)
                {
                    var collectionViewModel = new CollectionViewModel(dvm.DocumentController.GetDataDocument(), KeyStore.DataKey);
                    CollectionTreeView.DataContext = collectionViewModel;
                    CollectionTreeView.ContainingDocument = dvm.DocumentController.GetDataDocument();
                    //xArrowBlock.Text = (string)Application.Current.Resources["ExpandArrowIcon"];
                    xArrowBlock.Visibility = Visibility.Visible;
                    textBlockBinding.Tag = "TreeViewNodeCol";
                }
                else
                {
                    //xArrowBlock.Text = "";
                    xArrowBlock.Visibility = Visibility.Collapsed;
                    CollectionTreeView.DataContext = null;
                    CollectionTreeView.Visibility = Visibility.Collapsed;
                }
                if (snapshots != null && snapshots.Count != 0)
                {
                    XSnapshotArrowBlock.Visibility = Visibility.Visible;
                    textBlockBinding.Tag = "TreeViewNodeSnapCol";
                }
                else
                {
                    XSnapshotArrowBlock.Visibility = Visibility.Collapsed;
                    XSnapshotsPopup.Visibility = Visibility.Collapsed;
                }
                XTextBlock.AddFieldBinding(TextBlock.TextProperty, textBlockBinding);
                XTextBox.AddFieldBinding(TextBox.TextProperty, textBoxBinding);
                XHeader.AddFieldBinding(Panel.BackgroundProperty, headerBinding);

                UpdateSnapshots();
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
        public void Highlight(bool ? flag)
        {
            if (flag == null)
                ViewModel.DecorationState = (ViewModel.Undecorated == false) && !ViewModel.DecorationState;
            else if (flag == true)
                ViewModel.DecorationState = (ViewModel.Undecorated == false);
            else if (flag == false)
                ViewModel.DecorationState = false;
        }
        private void XArrowBlock_OnTapped(object sender, TappedRoutedEventArgs e)
        { 
            e.Handled = true;
            //Toggle visibility
            if (CollectionTreeView.Visibility == Visibility.Collapsed)
            {
                CollectionTreeView.Visibility = Visibility.Visible;

                xCollectionIn.Begin();

                var centX = (float)xArrowBlock.ActualWidth / 2 + 1;
                var centY = (float)xArrowBlock.ActualHeight / 2 + 1;
                //open search bar
                xArrowBlock.Rotate(value: 90.0f, centerX: centX, centerY: centY, duration: 300, delay: 0,
                    easingType: EasingType.Default).Start();

                ClosePopups();
            }
            else
            {

                xCollectionOut.Begin();
                CollectionTreeView.Visibility = Visibility.Collapsed;

                var centX = (float)xArrowBlock.ActualWidth / 2;
                var centY = (float)xArrowBlock.ActualHeight / 2;
                //open search bar

                xArrowBlock.Rotate(value: 0.0f, centerX: centX, centerY: centY, duration: 300, delay: 0,
                    easingType: EasingType.Default).Start();
            }
        }

        private void XSnapshotBlock_OnTapped(object sender, TappedRoutedEventArgs e)
        {
            e.Handled = true;
            //Toggle visibility
            if (XSnapshotsPopup.Visibility == Visibility.Collapsed)
            {
                ClosePopups();
                XSnapshotsPopup.Visibility = Visibility.Visible;
            }
            else
            {
                XSnapshotsPopup.Visibility = Visibility.Collapsed;
            }
        }

        private void XTextBlock_PointerEntered(object sender, PointerRoutedEventArgs e)
        {
            var docTapped = (DataContext as DocumentViewModel).DocumentController;
            Highlight(true);
            MainPage.Instance.HighlightDoc(docTapped, true);

        }

        private void XTextBlock_PointerExited(object sender, PointerRoutedEventArgs e)
        {
            Highlight(false);
            var docTapped = (DataContext as DocumentViewModel).DocumentController;
            MainPage.Instance.HighlightDoc(docTapped, false);
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
            if (! MainPage.Instance.NavigateToDocumentInWorkspaceAnimated(docToFocus, false))
                MainPage.Instance.SetCurrentWorkspace((DataContext as DocumentViewModel).DocumentController);

            UnfocusText();
            ClosePopups();

	        XBlockBorder.Background = Application.Current.Resources["DashLightBlueBrush"] as Brush;
            XTextBlock.Foreground = new SolidColorBrush(Windows.UI.Colors.White);
        }

        private void XTextBlock_OnDragStarting(UIElement sender, DragStartingEventArgs args)
        {
            args.Data.Properties[nameof(DragDocumentModel)] = new DragDocumentModel((DataContext as DocumentViewModel).DocumentController, true);
            args.AllowedOperations = DataPackageOperation.Link | DataPackageOperation.Copy;
        }

        public void DeleteDocument()
        {
            ClosePopups();
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
            ClosePopups();
            UndoManager.StartBatch();
            xBorder.Visibility = Visibility.Visible;
            XTextBlock.Visibility = Visibility.Collapsed;
            XTextBox.Focus(FocusState.Keyboard);
            XTextBox.SelectAll();
        }
        

        private void Open_OnClick(object sender, RoutedEventArgs e)
        {
            MainPage.Instance.SetCurrentWorkspace((DataContext as DocumentViewModel).DocumentController);
        }

        private void XTextBox_OnLostFocus(object sender, RoutedEventArgs e)
        {
            xBorder.Visibility = Visibility.Collapsed;
            XTextBlock.Visibility = Visibility.Visible;
            UndoManager.EndBatch();
        }


        private void XTextBox_OnKeyUp(object sender, KeyRoutedEventArgs e)
        {
            //finish rename on enter
            if (e.Key == VirtualKey.Enter)
            {
                xBorder.Visibility = Visibility.Collapsed;
                XTextBlock.Visibility = Visibility.Visible;
            }
        }

        private void XTextBox_LosingFocus(UIElement sender, LosingFocusEventArgs args)
        {
            if (args.NewFocusedElement == this.GetFirstAncestorOfType<ListViewItem>())
                args.Cancel = true;
        }

        private void DeleteSnap_OnClick(object sender, TappedRoutedEventArgs e)
        {
            var data = (e.OriginalSource as TextBlock).DataContext as SnapshotView;
            var index = data.Index;
            if (!_items.Count.Equals(0))
            {
                _items.RemoveAt(index);
            }
            
         
            (ViewModel.DocumentController.GetDataDocument().GetField(KeyStore.SnapshotsKey) as
                ListController<DocumentController>)?.RemoveAt(index);
            foreach (var snap in _items)
            {
                if (snap.Index > index)
                {
                    snap.Index -= 1;
                }
            }

            if (_items.Count == 0)
            {
                XSnapshotArrowBlock.Visibility = Visibility.Collapsed;
            }

        }

        private void ClosePopups()
        {
            foreach (var node in MainPage.Instance.xMainTreeView.TreeViewNodes)
            {
                node.XSnapshotsPopup.Visibility = Visibility.Collapsed;
            }
        }

        private void UnfocusText()
        {
            foreach (var node in MainPage.Instance.xMainTreeView.TreeViewNodes)
            {
                node.XBlockBorder.Background = new SolidColorBrush(Windows.UI.Colors.Transparent);
                node.XTextBlock.Foreground = new SolidColorBrush(Windows.UI.Colors.White);

                node.XSnapshotSelected.Visibility = Visibility.Collapsed;
            }
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
                if (!MainPage.Instance.NavigateToDocumentInWorkspaceAnimated(docToFocus, false))
                    MainPage.Instance.SetCurrentWorkspace(docToFocus);
            }

            ClosePopups();
            UnfocusText();

            SelectedTitle.Text = item.Title;
            SelectedImage.Source = new BitmapImage(new Uri(item.Image));
            XSnapshotSelected.Visibility = Visibility.Visible;
        }

        private void focusOnSelected()
        {
            var workspace = MainPage.Instance.MainDocument.GetField(KeyStore.LastWorkspaceKey, true) as DocumentController;
            foreach (var node in MainPage.Instance.xMainTreeView.TreeViewNodes)
            {
                if (node.ViewModel?.DocumentController == workspace)
                {
                    node.XBlockBorder.Background = Application.Current.Resources["DashLightBlueBrush"] as Brush;
                    node.XTextBlock.Foreground = new SolidColorBrush(Windows.UI.Colors.White);
                }
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

            SelectedTitle.Text = newTitle;

            var item = (sender as TextBox)?.DataContext as SnapshotView;
            item.Title = newTitle;

            (ViewModel.DocumentController.GetDataDocument().GetField(KeyStore.SnapshotsKey) as
                ListController<DocumentController>)?[item.Index]?.SetField<TextController>(KeyStore.DateModifiedKey, newTitle, true);
        }

        private void xControlIcon_DragStarting(UIElement uiElement, DragStartingEventArgs args)
        {
            var snapshots = ViewModel.DataDocument.GetField<ListController<DocumentController>>(KeyStore.SnapshotsKey);
            foreach (var d in snapshots)
            {
                d.SetWidth(200);
                d.SetHeight(200);
            }
            var dvm = ViewModel;
            args.Data.Properties[nameof(DragDocumentModel)] = new DragDocumentModel(dvm.DataDocument, KeyStore.SnapshotsKey);
            args.Data.RequestedOperation = DataPackageOperation.Move | DataPackageOperation.Copy | DataPackageOperation.Link;
        }

        private void ListViewBase_OnDragItemsStarting(object sender, DragItemsStartingEventArgs e)
        {
            if (e.Items.Count.Equals(0)) return;
            var first = e.Items.First() as SnapshotView;
            var snapshots = ViewModel.DataDocument.GetField<ListController<DocumentController>>(KeyStore.SnapshotsKey);
            e.Data.Properties[nameof(DragDocumentModel)] = new DragDocumentModel(snapshots[first.Index], true);
            e.Data.RequestedOperation = DataPackageOperation.Move | DataPackageOperation.Copy | DataPackageOperation.Link;
        }

        private void ListViewBase_OnDragItemsCompleted(ListViewBase sender, DragItemsCompletedEventArgs args)
        {
            ClosePopups();
        }
    }
}
