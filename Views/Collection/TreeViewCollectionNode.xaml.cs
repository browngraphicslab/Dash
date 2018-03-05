using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using Microsoft.Toolkit.Uwp.UI;
using Dash.Models.DragModels;

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
                                (doc.GetField<ListController<DocumentController>>(KeyStore.CollectionKey)?.TypedData
                                     .Any(MatchesFilter) ?? false);
            return matchesFilter;
            throw new NotImplementedException();
        }

        private void TreeViewNode_Drop(object sender, DragEventArgs e)
        {
            if (e.DataView.Properties.ContainsKey(nameof(DragDocumentModel)))
            {
                var data = e.DataView.Properties[nameof(DragDocumentModel)] as DragDocumentModel;
                var doc = (sender as TreeViewNode).DataContext as DocumentViewModel;
                var coll = doc.DataDocument.GetField<ListController<DocumentController>>(KeyStore.CollectionKey);
                if (coll != null && !doc.Equals(data.GetDraggedDocument()))
                {
                    coll.Add(data.GetDropDocument(new Point(), true));
                }
            }
            if (e.DataView.Properties.ContainsKey(nameof(List<DragDocumentModel>)))
            {
                var data = e.DataView.Properties[nameof(List<DragDocumentModel>)] as List<DragDocumentModel>;
                var doc = (sender as TreeViewNode).DataContext as DocumentViewModel;
                var coll = doc.DataDocument.GetField<ListController<DocumentController>>(KeyStore.CollectionKey);
                if (coll != null && data.Count > 0)
                {
                    var start = data.First().GetDraggedDocument().GetPositionField().Data;
                    coll.AddRange(data.Where((dm) => !doc.DocumentController.Equals(dm.GetDraggedDocument())).
                                       Select((dm) => dm.GetDropDocument(new Point(dm.GetDraggedDocument().GetPositionField().Data.X-start.X,
                                                                                   dm.GetDraggedDocument().GetPositionField().Data.Y-start.Y), true)).ToList());
                }
            }
        }
    }
}
