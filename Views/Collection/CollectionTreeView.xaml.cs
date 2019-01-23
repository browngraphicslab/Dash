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
using Windows.UI.Xaml.Data;
using static Dash.DataTransferTypeInfo;

// The User Control item template is documented at https://go.microsoft.com/fwlink/?LinkId=234236

namespace Dash
{
    public sealed partial class CollectionTreeView : ICollectionView
    {
        public UserControl UserControl => this;
        public CollectionViewType ViewType => CollectionViewType.TreeView;
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

            var upLevel = new ToolTip
            {
                Content = "Up a level",
                Placement = PlacementMode.Bottom,
                VerticalOffset = 5,
            };
            ToolTipService.SetToolTip(xUpOneLevel, upLevel);
        }
        public void SetupContextMenu(MenuFlyout contextMenu)
        {

        }
        public void OnDocumentSelected(bool selected)
        {
        }

        public void SetUseActiveFrame(bool useActiveFrame)
        {
            XTreeView.UseActiveFrame = useActiveFrame;
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
                var documentController = new CollectionNote(new Point(), CollectionViewType.Freeform, double.NaN, double.NaN).Document;
                ViewModel.AddDocument(documentController);
            }
        }

        private void XUpOneLevel_OnTapped(object sender, TappedRoutedEventArgs e)
        {
            var paths = DocumentTree.GetPathsToDocuments(ViewModel.ContainerDocument);
            var flyout = FlyoutBase.GetAttachedFlyout(xUpOneLevel) as MenuFlyout;
            Debug.Assert(flyout != null, nameof(flyout) + " != null");
            if (flyout.Items == null || paths.Count == 0)
            {
                return;
            }
            flyout.Items.Clear();

            void GoUp(DocumentController doc)
            {
                ViewModel.ContainerDocument.SetField(KeyStore.DocumentContextKey, doc.GetDataDocument(), true);
            }

            foreach (var path in paths)
            {
                if(path.Count < 2) continue;
                var item = new MenuFlyoutItem()
                {
                    Text = "/" + string.Join('/', path.SkipLast(1).Select(d => d.Title)),
                };
                item.Click += (o, args) => GoUp(path[path.Count - 2]);
                flyout.Items.Add(item);
            }

            if (flyout.Items.Count == 0)
            {
                return;
            }

            flyout.ShowAt(xUpOneLevel);
        }

        public void Highlight(DocumentController document, bool? flag)
        {
            //TODO TreeView: Get highlighting working in some form
        }

        public void MakePdf_OnTapped(object sender, TappedRoutedEventArgs e)
        {
            //List of Document Controller - one document controller for each collection
            //so a data file is made for each element in this list
            var collectionDataDocs = ViewModel.CollectionController.Select(dc => dc.GetDataDocument());

            ExportToTxt newExport = new ExportToTxt();

            //Now call function in ExportToTxt that converts all collections to files
            newExport.DashToTxt(collectionDataDocs);
        }

        public void ToggleDarkMode(bool dark)
        {
            xTreeGrid.Background = dark ?
                (SolidColorBrush)Application.Current.Resources["WindowsBlue"] : (SolidColorBrush)Application.Current.Resources["DocumentBackgroundColor"];
        }

        public void Snapshot_OnTapped(object sender, TappedRoutedEventArgs e)
        {
            if (!(SplitFrame.ActiveFrame.ViewModel.Content is CollectionView cview)) return;

            MainPage.Instance.SnapshotOverlay.Visibility = Visibility.Visible;
            MainPage.Instance.FadeIn.Begin();
            MainPage.Instance.FadeOut.Begin();

            using (UndoManager.GetBatchHandle())
            {
                var snapshot = cview.ViewModel.ContainerDocument.CreateSnapshot();
                MainPage.Instance.xMainTreeView.ViewModel.AddDocument(snapshot);
            }
        }

        //From ICollectionView, TreeView doesn't explicitly need to do anything for this
        public void SetDropIndicationFill(Brush fill) { }

        private void XFilterBox_OnTextChanged(object sender, TextChangedEventArgs e)
        {
            XTreeView.FilterFunc = controller => controller.Title.IndexOf(XFilterBox.Text, StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private CollectionViewModel _oldViewModel;
        private void CollectionTreeView_OnDataContextChanged(FrameworkElement sender, DataContextChangedEventArgs args)
        {
            if (ViewModel == _oldViewModel)
            {
                return;
            }

            _oldViewModel = ViewModel;

            if (ViewModel == null)
            {
                return;
            }

            XTitleBlock.AddFieldBinding(TextBlock.TextProperty, new FieldBinding<TextController>
            {
                Document = ViewModel.ContainerDocument,
                Key = KeyStore.TitleKey,
                Mode = BindingMode.OneWay,
                FallbackValue = "Untitled"
            });
        }

        private DocumentView _xLibraryView = null;
        private void xExportButton_Click(object sender, RoutedEventArgs e)
        {
            if (XTreeView.Visibility == Visibility.Visible)
            {
                xTitleBarGrid.Visibility = Visibility.Collapsed;
                XTreeView.Visibility = Visibility.Collapsed;
                xFilterBarGrid.Visibility = Visibility.Collapsed;
                xLibraryGrid.Visibility = Visibility.Visible;

                if (_xLibraryView == null)
                {
                    var docs = SearchFunctions.Library();
                    var col  = new CollectionBox(docs, viewType: CollectionViewType.Schema).Document;
                    FieldControllerBase.MakeRoot(col);
                    col.SetField<TextController>(KeyStore.ScriptSourceKey, "library()", true);
                    _xLibraryView = new DocumentView() { DataContext = new DocumentViewModel(col) { IsDimensionless = true, ResizersVisible = false } };
                    Grid.SetRow(_xLibraryView, 2);
                }
                else
                {
                    _xLibraryView.Visibility = Visibility.Visible;
                }
                xTreeGrid.Children.Add(_xLibraryView);
            }
            else
            {

                xLibraryGrid.Visibility = Visibility.Collapsed;
                xTitleBarGrid.Visibility = Visibility.Visible;
                xFilterBarGrid.Visibility = Visibility.Visible;
                XTreeView.Visibility = Visibility.Visible;
                xTreeGrid.Children.Remove(_xLibraryView);
            }
           // MainPage.Instance.Publish();
        }
    }
}
