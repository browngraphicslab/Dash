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

        
        private void DiscussionView_Loaded(object sender, RoutedEventArgs e)
        {
            var tl = xTreeView.GetFirstDescendantOfType<TreeViewList>();
            tl.ItemTemplate = xTemplate.ItemTemplate;
            xTreeView.SelectionMode = TreeViewSelectionMode.None;
            xTreeView.LayoutUpdated += XTreeView_LayoutUpdated;

            var binding = new FieldBinding<ListController<DocumentController>>()
            {
                Converter = new DocsToItemsConverter(),
                Mode = BindingMode.OneWay,
                Document = DataDocument,
                Key = KeyController.Get("DiscussionItems"),
                Tag = "bind ItemSource in DiscussionView"
            };
            tl.AddFieldBinding(TreeViewList.ItemsSourceProperty, binding);
        }
        public class DocsToItemsConverter : SafeDataToXamlConverter<List<DocumentController>, List<Item>>
        {
            public override List<Item> ConvertDataToXaml(List<DocumentController> wrapping, object parameter = null)
            {
                    return wrapping.Select((d) => new Item(d)).ToList();
            }

            public override List<DocumentController> ConvertXamlToData(List<Item> xaml, object parameter = null)
            {
                throw new NotImplementedException();
            }
        }

        private void XTreeView_LayoutUpdated(object sender, object e)
        {
            foreach (var cb in xTreeView.GetDescendantsOfType<Grid>().Where((g) => g.Name.Contains("Expand")))
                cb.Visibility = Visibility.Collapsed;
            foreach (var cb in xTreeView.GetDescendantsOfType<CheckBox>().Select((c) => c.Parent))
                if (cb is FrameworkElement fe)
                    fe.Visibility = Visibility.Collapsed;
        }

        public class Item : TreeViewNode
        {
            public Item(DocumentController d)
            {
                DVM = new DocumentViewModel(d) { ResizersVisible = false, IsDimensionless = true };
                IsExpanded = true;
                var binding = new FieldBinding<NumberController>()
                {
                    Converter = new DoubleToIntConverter(),
                    Mode = BindingMode.TwoWay,
                    Document = d.GetDataDocument(),
                    Key = KeyController.Get("DiscussionDepth"),
                    Tag = "bind ItemSource in Item TreeViewNode"
                };
                this.AddFieldBinding(TreeViewNode.DepthProperty, binding);
            }
            public DocumentViewModel  DVM { get; set; }
        }
    }
}
