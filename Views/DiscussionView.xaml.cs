using System;
using System.Collections.Generic;
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
using Microsoft.Toolkit.Uwp.UI.Controls;
using System.Collections.ObjectModel;

// The User Control item template is documented at https://go.microsoft.com/fwlink/?LinkId=234236

namespace Dash.Views
{
    public sealed partial class DiscussionView : UserControl
    {
        public KeyController DataFieldKey { get; set; }
        public DocumentController DataDocument => ViewModel?.DataDocument;
        public DocumentController LayoutDocument => ViewModel?.LayoutDocument;
        public DocumentViewModel ViewModel => getDocView()?.ViewModel;  // DataContext as DocumentViewModel;  would prefer to use DataContext, but it can be null when getDocView() is not
        private DocumentView getDocView() { return this.GetFirstAncestorOfType<DocumentView>(); }
        
        public DiscussionView()
        {
            InitializeComponent();
            Loaded += DiscussionView_Loaded;
        }


        private Item buildItem(DocumentController doc)
        {
            var initialNode = new Item() { IsExpanded = true, DVM = new DocumentViewModel(doc) { IsDimensionless = true, ResizersVisible = false, Undecorated = true } };
            var fields = doc.GetDataDocument().GetDereferencedField<ListController<DocumentController>>(KeyController.Get("Replies"), null);
            if (fields != null)
            {
                foreach (var f in fields)
                {
                    initialNode.Children.Add(buildItem(f));
                }
            }
            return initialNode;
        }
        private void DiscussionView_Loaded(object sender, RoutedEventArgs e)
        {
            var initdoc = DataDocument.GetDereferencedField<DocumentController>(KeyStore.DataKey, null);
            var tl = xTreeView.GetFirstDescendantOfType<TreeViewList>();
            tl.ItemTemplate = xTemplate.ItemTemplate;
            xTreeView.RootNodes.Add(buildItem(initdoc));
            xTreeView.SelectionMode = TreeViewSelectionMode.None;
            xTreeView.LayoutUpdated += XTreeView_LayoutUpdated;
        }

        private void XTreeView_LayoutUpdated(object sender, object e)
        {
            foreach (var cb in xTreeView.GetDescendantsOfType<Grid>().Where((g)=>g.Name.Contains("Expand")))
                cb.Padding = new Thickness(0);
            foreach (var cb in xTreeView.GetDescendantsOfType<CheckBox>().Select((c) => c.Parent))
                if (cb is FrameworkElement fe)
                    fe.Visibility = Visibility.Collapsed;
        }

        private void checkedChanged(object sender, RoutedEventArgs e)
        {
            var item = (sender as Button).DataContext as Item;
            var rtn = new RichTextNote("Next...").Document;
            var stuff = item.DVM.DataDocument.GetFieldOrCreateDefault<ListController<DocumentController>>(KeyController.Get("Replies"));
            stuff.Add(rtn);
            item.Children.Add(new Item() { IsExpanded = true, DVM = new DocumentViewModel(rtn) { IsDimensionless = true, ResizersVisible = false, Undecorated = true } });
        }

        private class Item : TreeViewNode
        {
            public DocumentViewModel  DVM { get; set; }
        }
    }
}
