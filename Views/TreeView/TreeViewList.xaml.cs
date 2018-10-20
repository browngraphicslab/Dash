using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text.RegularExpressions;
using Windows.ApplicationModel.DataTransfer;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

// The User Control item template is documented at https://go.microsoft.com/fwlink/?LinkId=234236

namespace Dash.Views.TreeView
{
    public sealed partial class TreeViewList : UserControl
    {
        public CollectionViewModel ViewModel => DataContext as CollectionViewModel;

        public static readonly DependencyProperty FilterFuncProperty = DependencyProperty.Register(
            "FilterFunc", typeof(Func<DocumentController, bool>), typeof(TreeViewList), new PropertyMetadata(default(Func<DocumentController, bool>), FilterFuncChanged));

        private static void FilterFuncChanged(DependencyObject dependencyObject, DependencyPropertyChangedEventArgs dependencyPropertyChangedEventArgs)
        {
            ((TreeViewList)dependencyObject).OnFilterChanged();
        }

        public Func<DocumentController, bool> FilterFunc
        {
            get => (Func<DocumentController, bool>)GetValue(FilterFuncProperty);
            set => SetValue(FilterFuncProperty, value);
        }

        public TreeViewList()
        {
            InitializeComponent();
        }

        private CollectionViewModel _oldViewModel;
        private void TreeView_OnDataContextChanged(FrameworkElement sender, DataContextChangedEventArgs args)
        {
            if (ViewModel == _oldViewModel)
            {
                return;
            }

            Bindings.Update();
            bool wasLoaded = _oldViewModel?.IsLoaded ?? true;
            if (wasLoaded)
            {
                _oldViewModel?.Loaded(false);
            }

            _oldViewModel = ViewModel;

            if (wasLoaded)
            {
                _oldViewModel?.Loaded(true);
            }
        }


        private void TreeView_OnLoaded(object sender, RoutedEventArgs e)
        {
            ViewModel?.Loaded(true);
        }

        private void TreeView_OnUnloaded(object sender, RoutedEventArgs e)
        {
            _oldViewModel?.Loaded(false);
        }

        private void OnFilterChanged()
        {
            if (ViewModel != null)
            {
                ViewModel.BindableDocumentViewModels.Filter =
                    o => MatchesFilter(((DocumentViewModel)o).DocumentController);
                ViewModel.BindableDocumentViewModels.RefreshFilter();
            }
        }

        private bool MatchesFilter(DocumentController controller)
        {
            bool matches = FilterFunc?.Invoke(controller) ?? true;

            var col = controller.GetDereferencedField<ListController<DocumentController>>(KeyStore.DataKey, null);
            if (col != null)
            {
                matches |= col.Any(MatchesFilter);
            }

            return matches;
        }

        private void TreeViewNode_OnDragStarting(UIElement sender, DragStartingEventArgs args)
        {
            args.AllowedOperations = DataPackageOperation.Copy | DataPackageOperation.Move | DataPackageOperation.Link;

            var node = (TreeViewNode) sender;
            _dragIndex = ViewModel.DocumentViewModels.IndexOf(node.ViewModel);
            args.Data.SetDragModel(new DragDocumentModel(node.ViewModel.DocumentController) { DraggedDocCollectionViews = new List<CollectionViewModel> { ViewModel } });
        }

        private int _dragIndex = -1;
        private int _dropIndex = -1;
        private void TreeViewList_OnDragOver(object sender, DragEventArgs e)
        {
            e.Handled = true;
            var dragModel = e.DataView.GetDragModel();

            if (XListControl.Items == null || dragModel == null)
            {
                return;
            }

            //TODO Prevent dragging a collection into itself 
            if (!dragModel.CanDrop(this))
            {
                e.AcceptedOperation = DataPackageOperation.None;
            }
            e.AcceptedOperation = this.IsShiftPressed() ? DataPackageOperation.Copy : DataPackageOperation.Move;

            var pos = e.GetPosition(XListControl);

            double threshold = 15;
            double previewY = 0;
            int i;
            Debug.Assert(XListControl.Items != null, "XListControl.Items != null");
            bool found = false;
            for (i = 0; i < XListControl.Items.Count; i++)
            {
                var ele = (FrameworkElement)XListControl.ContainerFromIndex(i);
                Debug.Assert(ele != null, nameof(ele) + " != null");
                var bounds = new Rect(0, 0, ele.ActualWidth, ele.ActualHeight);
                bounds = ele.TransformToVisual(XListControl).TransformBounds(bounds);
                if (pos.Y < bounds.Top + threshold)
                {
                    found = true;
                    break;
                }

                previewY += bounds.Height;

                if (pos.Y > bounds.Bottom - threshold && pos.Y < bounds.Bottom)
                {
                    if (i < _dragIndex)
                    {
                        i++;
                    }
                    found = true;
                    break;
                }
            }

            if (!found)
            {
                XPreviewLine.Visibility = Visibility.Collapsed;
                _dropIndex = -1;
                return;
            }
            _dropIndex = i;

            XPreviewLine.X1 = 0;
            XPreviewLine.X2 = ActualWidth;
            XPreviewLine.Y1 = XPreviewLine.Y2 = previewY + 2;
            XPreviewLine.Visibility = Visibility.Visible;
        }

        private void TreeViewList_OnDragLeave(object sender, DragEventArgs e)
        {
            XPreviewLine.Visibility = Visibility.Collapsed;
            _dropIndex = _dragIndex = -1;
        }

        private async void TreeViewList_OnDrop(object sender, DragEventArgs e)
        {
            using (UndoManager.GetBatchHandle())
            {
                _dragIndex = -1;
                if (_dropIndex == -1)
                {
                    return;
                }

                XPreviewLine.Visibility = Visibility.Collapsed;

                e.Handled = true;

                e.AcceptedOperation = this.IsShiftPressed() ? DataPackageOperation.Copy : DataPackageOperation.Move;
                var docs = await e.DataView.GetDroppableDocumentsForDataOfType(DataTransferTypeInfo.Internal, this);
                if (!this.IsShiftPressed())
                {
                    if (e.DataView.GetDragModel() is DragDocumentModel ddm)
                    {
                        for (var i = 0; i < ddm.DraggedDocCollectionViews?.Count; i++)
                        {
                            ddm.DraggedDocCollectionViews[i].RemoveDocument(ddm.DraggedDocuments[i]);
                        }
                    }
                }

                foreach (var doc in docs)
                {
                    ViewModel.InsertDocument(doc, _dropIndex++);
                }

                _dropIndex = -1;
            }
        }
    }
}
