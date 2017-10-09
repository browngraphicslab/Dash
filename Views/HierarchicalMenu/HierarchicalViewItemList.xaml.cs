using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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

// The User Control item template is documented at https://go.microsoft.com/fwlink/?LinkId=234236

namespace Dash
{
    public sealed partial class HierarchicalViewItemList : UserControl
    {
        private ObservableCollection<HierarchicalViewItem> ListItemSource = new ObservableCollection<HierarchicalViewItem>();
        public Dictionary<DocumentController, HierarchicalViewItem> Items = new Dictionary<DocumentController, HierarchicalViewItem>();

        public bool IsVisible
        {
            get { return IsVisible; }
            set { xList.Visibility = value ? Visibility.Visible : Visibility.Collapsed; }
        }

        public HierarchicalViewItemList(List<DocumentController> controllers)
        {
            this.InitializeComponent();
            foreach (var controller in controllers)
            {
                var item = new HierarchicalViewItem(controller);
                if (!Items.ContainsKey(controller))
                    Items.Add(controller, item);
                ListItemSource.Add(item);
            }
            xList.ItemsSource = ListItemSource;
        }

        public void AddItem(DocumentController controller)
        {
            var item = new HierarchicalViewItem(controller);
            if (!Items.ContainsKey(controller))
                Items.Add(controller, item);
            ListItemSource.Add(item);
        }

        public void RemoveItem(DocumentController controller)
        {
            //// issues with delete, what should happen to delegates when documentview of the prototype is removed from the workspace?
            //if (Items[controller].List?.ListItemSource.Count != 0)
            //{
            //    foreach (var child in Items[controller].List?.ListItemSource)
            //    {
            //        ListItemSource.Add(child);
            //        Items.Add(child.DocController, child);
            //    }
            //}
            //ListItemSource.Remove(Items[controller]);
            //Items.Remove(controller);
            Items[controller].Deactivate();
        }
    }
}
