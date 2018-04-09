using System;
using System.Collections.Generic;
using System.Linq;
using Windows.Foundation;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Microsoft.Toolkit.Uwp.UI;
using Dash.Models.DragModels;
using Windows.UI.Xaml.Input; 
using Windows.ApplicationModel.DataTransfer;

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

        public CollectionViewModel ViewModel => DataContext as CollectionViewModel;

        public TreeViewCollectionNode()
        {
            this.InitializeComponent();
            this.RegisterPropertyChangedCallback(FilterStringProperty,
                (sender, dp) => ViewModel?.BindableDocumentViewModels.RefreshFilter());
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
                        if (d.DocumentType.Equals(NoteDocuments.CollectionNote.DocumentType))
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
            var cvm = args.NewValue as CollectionViewModel;
            if (cvm == null)
            {
                return;
            }
            cvm.BindableDocumentViewModels.SortDescriptions.Add(new SortDescription("YPos", SortDirection.Ascending));
            cvm.BindableDocumentViewModels.Filter = Filter;
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

        /// <summary>
        /// Determines which document is dropped when treeviewmenu is dragged 
        /// </summary>
        private DocumentViewModel _draggedDocument;
        private void TreeViewNode_PointerEntered(object sender, PointerRoutedEventArgs e)
        {
            _draggedDocument = (sender as TreeViewNode).ViewModel;
        }

        private void xListView_DragItemsStarting(object sender, DragItemsStartingEventArgs e)
        {
            e.Data.Properties[nameof(DragDocumentModel)] = new DragDocumentModel(_draggedDocument?.DocumentController, true);
        }
    }
}
