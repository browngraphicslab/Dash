using System;
using System.Linq;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Microsoft.Toolkit.Uwp.UI;
using Windows.ApplicationModel.DataTransfer;
using static Dash.DataTransferTypeInfo;

// The User Control item template is documented at https://go.microsoft.com/fwlink/?LinkId=234236

namespace Dash
{
    public sealed partial class TreeViewCollectionNode : UserControl
    {
        public static readonly DependencyProperty FilterStringProperty = DependencyProperty.Register(
            "FilterString", typeof(string), typeof(TreeViewCollectionNode), new PropertyMetadata(default(string)));

        public string FilterString
        {
            get { return (string)GetValue(FilterStringProperty); }
            set { SetValue(FilterStringProperty, value); }
        }

        public static readonly DependencyProperty ContainingDocumentProperty = DependencyProperty.Register(
            "ContainingDocument", typeof(DocumentController), typeof(TreeViewCollectionNode), new PropertyMetadata(default(DocumentController)));

        public DocumentController ContainingDocument
        {
            get { return (DocumentController)GetValue(ContainingDocumentProperty); }
            set { SetValue(ContainingDocumentProperty, value); }
        }

        public static readonly DependencyProperty SortCriterionProperty = DependencyProperty.Register(
            "SortCriterion", typeof(string), typeof(TreeViewCollectionNode), new PropertyMetadata("YPos"));

        public string SortCriterion
        {
            get { return (string)GetValue(SortCriterionProperty); }
            set { SetValue(SortCriterionProperty, value); }
        }

        private CollectionViewModel _oldViewModel;
        public CollectionViewModel ViewModel => DataContext as CollectionViewModel;
        
        public TreeViewCollectionNode()
        {
            this.InitializeComponent();
            this.RegisterPropertyChangedCallback(FilterStringProperty,
                (sender, dp) => ViewModel?.BindableDocumentViewModels.RefreshFilter());


            Loaded += (sender, e) =>
            {
                //if (ViewModel != null)
                //    ViewModel.Loaded(true);
            };
            Unloaded += (sender, e) =>
            {
                ViewModel?.Loaded(false);
            };
        }
        
        public void Highlight(DocumentController document, bool? flag)
        {
            if (xListView.ItemsPanelRoot != null)
            {
                foreach (var noda in xListView.ItemsPanelRoot.Children.OfType<ListViewItem>())
                {
                    var cp = noda.GetFirstDescendantOfType<ContentPresenter>();
                    var d = (cp.DataContext as DocumentViewModel)?.DataDocument;
                    if (d != null)
                    {
                        var tv = noda.GetFirstDescendantOfType<TreeViewNode>();
                        var tc = noda.GetFirstDescendantOfType<TreeViewCollectionNode>();
                        if (d.DocumentType.Equals(CollectionNote.DocumentType))
                        {
                            tc.Highlight(document, flag);
                        }
                        if (tv.ViewModel.DocumentController.Equals(document))
                        {
                            tv.Highlight(flag);
                        }
                    }
                }
            }
        }

        private void TreeViewCollectionNode_OnDataContextChanged(FrameworkElement sender, DataContextChangedEventArgs args)
        {
            if (ViewModel == _oldViewModel || ViewModel == null)
            {
                return;
            }

            _oldViewModel = ViewModel;
            
            if (!string.IsNullOrEmpty(SortCriterion))
                ViewModel.BindableDocumentViewModels.SortDescriptions.Add(new SortDescription(SortCriterion, SortDirection.Ascending));
            ViewModel.BindableDocumentViewModels.Filter = Filter;
        }
        private bool Filter(object o)
        {
            var dvm = (DocumentViewModel)o;
            return MatchesFilter(dvm.DocumentController);
        }

        public bool MatchesFilter(DocumentController doc)
        {
            if (FilterString == null)//TODO Why is this null?
            {
                return false;
            }
            doc = doc.GetDataDocument();
            var text = doc.GetDereferencedField<TextController>(KeyStore.DocumentTextKey, null)?.Data ?? string.Empty;
            var matchesFilter = doc.Title.ToLower().Contains(FilterString.ToLower()) || text.ToLower().Contains(FilterString.ToLower()) ||
                                (doc.GetField<ListController<DocumentController>>(KeyStore.DataKey)?.TypedData
                                     .Any(MatchesFilter) ?? false);
            return matchesFilter;
            throw new NotImplementedException();
        }

        private async void TreeViewNode_Drop(object sender, DragEventArgs e)
        {
            if (e.DataView.HasDataOfType(Any))
            {
                var droppableDocs = await e.DataView.GetDroppableDocumentsForDataOfType(Any, sender as FrameworkElement);

                var viewModel = (sender as TreeViewNode)?.DataContext as DocumentViewModel;
                var coll = viewModel?.DataDocument.GetField<ListController<DocumentController>>(KeyStore.DataKey);

                if (coll != null && !droppableDocs.Any(d => d.Equals(viewModel.DocumentController)))
                    coll.AddRange(droppableDocs);
            }

            e.Handled = true;
        }

        private void TreeViewNode_DragOver(object sender, DragEventArgs e)
        {
            if (e.DataView.HasDataOfType(Internal))
            {
                e.AcceptedOperation = e.DataView.RequestedOperation == DataPackageOperation.None ? DataPackageOperation.Copy : e.DataView.RequestedOperation;
            }
            else e.AcceptedOperation = DataPackageOperation.None;
            e.Handled = true;
        }
    }
}
