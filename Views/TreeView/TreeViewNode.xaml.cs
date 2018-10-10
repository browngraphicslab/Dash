using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.System;
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

namespace Dash.Views.TreeView
{
    public class ExpandedToArrowConverter : SafeDataToXamlConverter<bool, string>
    {
        public string ExpandedGlyph { get; set; } = "\uF107";
        public string CollapsedGlyph { get; set; } = "\uF105";

        public override string ConvertDataToXaml(bool data, object parameter = null)
        {
            return data ? ExpandedGlyph : CollapsedGlyph;
        }

        public override bool ConvertXamlToData(string xaml, object parameter = null)
        {
            throw new NotImplementedException();
        }
    }

    public sealed partial class TreeViewNode : UserControl, INotifyPropertyChanged
    {
        public DocumentViewModel ViewModel
        {
            get
            {
                //TODO DBUpdate: This shouldn't be necessary
                try
                {
                    return DataContext as DocumentViewModel;
                }
                catch (Exception)
                {
                    return null;
                }
            }
        }

        public TreeView TreeView => this.GetFirstAncestorOfType<TreeView>();

        private bool _isCollection;
        private bool _isExpanded;

        public static readonly DependencyProperty FilterFuncProperty = DependencyProperty.Register(
            "FilterFunc", typeof(Func<DocumentController, bool>), typeof(TreeViewNode), new PropertyMetadata(default(Func<DocumentController, bool>)));

        public Func<DocumentController, bool> FilterFunc
        {
            get => (Func<DocumentController, bool>)GetValue(FilterFuncProperty);
            set => SetValue(FilterFuncProperty, value);
        }

        public bool IsCollection
        {
            get => _isCollection;
            set
            {
                if (value == _isCollection)
                {
                    return;
                }

                _isCollection = value;
                OnPropertyChanged();
            }
        }

        public bool IsExpanded
        {
            get => _isExpanded;
            set
            {
                if (value == _isExpanded)
                {
                    return;
                }

                _isExpanded = value;
                OnPropertyChanged();
            }
        }

        public bool IsEditing
        {
            get => _isEditing;
            set
            {
                if (value == _isEditing) return;
                _isEditing = value;
                OnPropertyChanged();
                XRenameBox.Focus(FocusState.Programmatic);
            }
        }

        private static readonly SolidColorBrush SelectedBrush = new SolidColorBrush(Colors.DeepSkyBlue);
        public bool IsSelected { get; private set; }

        /// <summary>
        /// This should only be called from TreeView.SelectedItem
        /// </summary>
        public void Select()
        {
            IsSelected = true;
            XTitleBorder.Background = SelectedBrush;
        }

        /// <summary>
        /// This should only be called from TreeView.SelectedItem
        /// </summary>
        public void Deselect()
        {
            IsSelected = false;
            XTitleBorder.Background = null;
        }

        public TreeViewNode()
        {
            InitializeComponent();

        }

        private void XArrowBlock_OnTapped(object sender, TappedRoutedEventArgs e)
        {
            IsExpanded = !IsExpanded;
        }

        public event PropertyChangedEventHandler PropertyChanged;

        [NotifyPropertyChangedInvocator]
        private void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        private DocumentViewModel _oldViewModel;
        private bool _isEditing;

        private void TreeViewNode_OnDataContextChanged(FrameworkElement sender, DataContextChangedEventArgs args)
        {
            if (Equals(ViewModel, _oldViewModel))
            {
                return;
            }

            _oldViewModel = ViewModel;

            if (ViewModel != null)
            {
                XTitleBlock.AddFieldBinding(TextBlock.TextProperty, new FieldBinding<TextController> { Document = ViewModel.DocumentController, Key = KeyStore.TitleKey, Mode = BindingMode.OneWay });

                SplitDocumentOnActiveDocumentChanged(SplitFrame.ActiveFrame);

                var collectionField = ViewModel.DocumentController.GetDereferencedField<ListController<DocumentController>>(KeyStore.DataKey, null);
                if (collectionField != null)
                {
                    IsCollection = true;
                    XTreeViewList.DataContext = new CollectionViewModel(ViewModel.DocumentController, KeyStore.DataKey);
                }
            }
        }

        private void TreeViewNode_OnLoaded(object sender, RoutedEventArgs e)
        {
            SplitFrame.ActiveDocumentChanged += SplitDocumentOnActiveDocumentChanged;
        }

        private void TreeViewNode_OnUnloaded(object sender, RoutedEventArgs e)
        {
            SplitFrame.ActiveDocumentChanged -= SplitDocumentOnActiveDocumentChanged;
        }

        private void SplitDocumentOnActiveDocumentChanged(SplitFrame splitFrame)
        {
            XTitleBlock.FontWeight = ViewModel?.DataDocument.Equals(splitFrame.DocumentController.GetDataDocument()) == true ? FontWeights.Bold : FontWeights.Normal;
        }

        #region Renaming

        private void CommitEdit()
        {
            using (UndoManager.GetBatchHandle())
            {
                if (ViewModel.DocumentController.GetField<TextController>(KeyStore.TitleKey) != null)
                    ViewModel.DocumentController.SetTitle(XRenameBox.Text);
                else ViewModel.DataDocument.SetTitle(XRenameBox.Text);
                IsEditing = false;
            }
        }

        private void CancelEdit()
        {
            IsEditing = false;
        }

        private Visibility Not(bool b)
        {
            return b ? Visibility.Collapsed : Visibility.Visible;
        }

        private void RenameFlyoutItem_OnClick(object sender, RoutedEventArgs e)
        {
            void FocusResizeBox(object s, object o)
            {
                MenuFlyout.Closed -= FocusResizeBox;
                IsEditing = true;
                XRenameBox.Focus(FocusState.Keyboard);
            }

            MenuFlyout.Closed += FocusResizeBox; //Psuedo-hack to get focusing the text box to work
        }

        private void XRenameBox_OnGotFocus(object sender, RoutedEventArgs e)
        {
            XRenameBox.Text = ViewModel.DocumentController.Title ?? ViewModel.DataDocument.Title;
            XRenameBox.SelectAll();
        }

        private void XRenameBox_OnLostFocus(object sender, RoutedEventArgs e)
        {
            CommitEdit();
        }

        private void XRenameBox_OnKeyDown(object sender, KeyRoutedEventArgs e)
        {
            if (e.Key == VirtualKey.Enter)
            {
                CommitEdit();
            }
            else if (e.Key == VirtualKey.Escape)
            {
                CancelEdit();
            }
        }

        #endregion

        private bool _tapped;
        private async void XTitleBlock_OnTapped(object sender, TappedRoutedEventArgs e)
        {
            e.Handled = true;
            var treeView = TreeView;
            if (treeView == null)
            {
                return;
            }

            if (!IsSelected)
            {
                treeView.SelectedItem = this;
                Focus(FocusState.Programmatic);
            }
            else
            {
                _tapped = true;
                await Task.Delay(100);//Delay to allow for double tapped
                if (_tapped)
                {
                    IsEditing = true;
                }
            }
        }

        private void XTitleBlock_OnDoubleTapped(object sender, DoubleTappedRoutedEventArgs e)
        {
            var treeView = TreeView;
            if (treeView == null)
            {
                return;
            }

            _tapped = false;

            //if (treeView.UseActiveFrame)
            //{
            SplitFrame.OpenInActiveFrame(ViewModel.DocumentController);
            //}

            treeView.ViewModel.ContainerDocument.SetField(KeyStore.CollectionOutputKey, ViewModel.DocumentController, true);
        }

        private void OpenFlyoutItem_OnClick(object sender, RoutedEventArgs e)
        {
            var treeView = TreeView;
            if (treeView == null)
            {
                return;
            }

            //if (treeView.UseActiveFrame)
            //{
            SplitFrame.OpenInActiveFrame(ViewModel.DocumentController);
            //}

            treeView.ViewModel.ContainerDocument.SetField(KeyStore.CollectionOutputKey, ViewModel.DocumentController, true);
        }

        private void Delete()
        {
            using (UndoManager.GetBatchHandle())
            {
                var list = this.GetFirstAncestorOfType<TreeViewList>();

                list?.ViewModel.RemoveDocument(ViewModel.LayoutDocument);
            }
        }

        private void DeleteFlyoutItem_OnClick(object sender, RoutedEventArgs e)
        {
            Delete();
        }

        private void GotoFlyoutItem_OnClick(object sender, RoutedEventArgs e)
        {
            var workspace = this.GetFirstAncestorOfType<TreeViewNode>();
            if (workspace == null)
            {
                return;
            }

            var workspaceDoc = workspace.ViewModel.DocumentController;
            var doc = ViewModel.DocumentController;
            SplitFrame.OpenDocumentInWorkspace(doc, workspaceDoc);
        }

        #region Collection Flyout

        private void AddCollectionItem_OnClick(object sender, RoutedEventArgs routedEventArgs)
        {
            if (!IsCollection)
            {
                return;
            }

            XTreeViewList.ViewModel.AddDocument(new CollectionNote(new Point(), CollectionView.CollectionViewType.Grid).Document);
        }

        private void ShowInMapItemOnClick(object sender, RoutedEventArgs routedEventArgs)
        {
            if (!IsCollection)
            {
                return;
            }

            MainPage.Instance.SetupMapView(ViewModel.DocumentController);
        }

        private void SnapshotItemOnClick(object sender, RoutedEventArgs routedEventArgs)
        {
            if (!IsCollection)
            {
                return;
            }

            ViewModel.DocumentController.CreateSnapshot();
        }

        private void FocusItemOnClick(object sender, RoutedEventArgs routedEventArgs)
        {
            if (!IsCollection)
            {
                return;
            }

            var treeView = TreeView;
            if (treeView == null)
            {
                return;
            }
            CollectionViewModel cvm = treeView.ViewModel;

            if (treeView.UseActiveFrame)
            {
                SplitFrame.OpenInActiveFrame(ViewModel.DocumentController);
            }

            cvm?.ContainerDocument.SetField(KeyStore.DocumentContextKey, ViewModel.DataDocument, true);
        }

        private void MenuFlyout_OnClosed(object sender, object e)
        {
            var itemsToRemove = MenuFlyout.Items?.Where(mfi => mfi.Tag is bool b && b).ToList();
            if (itemsToRemove == null) return;
            foreach (var menuFlyoutItemBase in itemsToRemove)
            {
                MenuFlyout.Items?.Remove(menuFlyoutItemBase);
            }
        }

        private void MenuFlyout_OnOpening(object sender, object e)
        {
            if (IsCollection)
            {
                foreach (var collectionItem in GetCollectionItems())
                {
                    collectionItem.Tag = true;
                    MenuFlyout.Items?.Add(collectionItem);
                }
            }
        }

        private List<MenuFlyoutItemBase> GetCollectionItems()
        {
            var addCollectionItem = new MenuFlyoutItem()
            {
                Text = "Add Collection",
            };
            addCollectionItem.Click += AddCollectionItem_OnClick;

            var showInMapItem = new MenuFlyoutItem()
            {
                Text = "Show in Mini-map",
            };
            showInMapItem.Click += ShowInMapItemOnClick;

            var focusItem = new MenuFlyoutItem()
            {
                Text = "Focus Collection",
            };
            focusItem.Click += FocusItemOnClick;

            var snapshotItem = new MenuFlyoutItem()
            {
                Text = "Take Snapshop",
            };
            snapshotItem.Click += SnapshotItemOnClick;

            return new List<MenuFlyoutItemBase>
            {
                new MenuFlyoutSeparator(),
                addCollectionItem,
                snapshotItem,
                focusItem,
                showInMapItem
            };
        }

        #endregion

        private void TreeViewNode_OnKeyDown(object sender, KeyRoutedEventArgs e)
        {
            e.Handled = true;
            switch (e.Key)
            {
            case VirtualKey.F2:
                IsEditing = true;
                break;
            case VirtualKey.Delete:
                Delete();
                break;
            default:
                e.Handled = false;
                break;
            }
        }
    }
}
