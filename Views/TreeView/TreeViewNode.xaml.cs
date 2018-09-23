using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.System;
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
        public DocumentViewModel ViewModel => DataContext as DocumentViewModel;

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
            }
        }

        public TreeViewNode()
        {
            InitializeComponent();

            _addCollectionItem = new MenuFlyoutItem()
            {
                Text = "Add Collection",
            };
            _addCollectionItem.Click += AddCollectionItem_OnClick;

            _showInMapItem = new MenuFlyoutItem()
            {
                Text = "Show in Mini-map",
            };
            _showInMapItem.Click += ShowInMapItemOnClick;
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
                XTitleBlock.AddFieldBinding(TextBlock.TextProperty, new FieldBinding<TextController> { Document = ViewModel.DataDocument, Key = KeyStore.TitleKey, Mode = BindingMode.OneWay });

                SplitDocumentOnActiveDocumentChanged(SplitFrame.ActiveFrame);

                var collectionField = ViewModel.DocumentController.GetDereferencedField<ListController<DocumentController>>(KeyStore.DataKey, null);
                if (collectionField != null)
                {
                    IsCollection = true;
                    IsExpanded = true;
                    XTreeViewList.DataContext = new CollectionViewModel(ViewModel.DocumentController, KeyStore.DataKey);
                }
            }
        }

        private void XTitleBlock_OnTapped(object sender, TappedRoutedEventArgs e)
        {

        }

        private void XTitleBlock_OnDoubleTapped(object sender, DoubleTappedRoutedEventArgs e)
        {
            SplitFrame.OpenInActiveFrame(ViewModel.DocumentController);
        }

        private void OpenFlyoutItem_OnClick(object sender, RoutedEventArgs e)
        {
            SplitFrame.OpenInActiveFrame(ViewModel.DocumentController);
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
            XTitleBlock.FontWeight = ViewModel.DataDocument.Equals(splitFrame.DocumentController.GetDataDocument()) ? FontWeights.Bold : FontWeights.Normal;
        }

        #region Renaming

        private void CommitEdit()
        {
            using (UndoManager.GetBatchHandle())
            {
                ViewModel.DataDocument.SetTitle(XRenameBox.Text);
                IsEditing = false;
            }
        }

        private void CancelEdit()
        {
            IsEditing = false;
        }

        [UsedImplicitly]
        private Visibility Not(bool b)
        {
            return b ? Visibility.Collapsed : Visibility.Visible;
        }

        private void RenameFlyoutItem_OnClick(object sender, RoutedEventArgs e)
        {
            IsEditing = true;

            void FocusResizer(object s, object o)
            {
                MenuFlyout.Closed -= FocusResizer;
                XRenameBox.Focus(FocusState.Keyboard);
            }

            MenuFlyout.Closed += FocusResizer; //Psuedo-hack to get focusing the text box to work
        }

        private void XRenameBox_OnGotFocus(object sender, RoutedEventArgs e)
        {
            XRenameBox.Text = ViewModel.DataDocument.Title;
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

        private void DeleteFlyoutItem_OnClick(object sender, RoutedEventArgs e)
        {
            using (UndoManager.GetBatchHandle())
            {
                var list = this.GetFirstAncestorOfType<TreeViewList>();

                list?.ViewModel.RemoveDocument(ViewModel.LayoutDocument);
            }
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

        private readonly MenuFlyoutItem _addCollectionItem;
        private readonly MenuFlyoutItem _showInMapItem;
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

        private void MenuFlyout_OnClosed(object sender, object e)
        {
            MenuFlyout.Items?.Remove(_showInMapItem);
            MenuFlyout.Items?.Remove(_addCollectionItem);
        }

        private void MenuFlyout_OnOpening(object sender, object e)
        {
            if (IsCollection)
            {
                MenuFlyout.Items?.Add(_addCollectionItem);
                MenuFlyout.Items?.Add(_showInMapItem);
            }
        }

        #endregion
    }
}
