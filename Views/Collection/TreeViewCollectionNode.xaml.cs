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
            if(FilterString == null)
            {
                return true;
            }
            doc = doc.GetDataDocument();
            var matchesFilter = doc.Title.Contains(FilterString) ||
                                (doc.GetField<ListController<DocumentController>>(KeyStore.CollectionKey)?.TypedData
                                     .Any(MatchesFilter) ?? false);
            return matchesFilter;
        }

    }
}
