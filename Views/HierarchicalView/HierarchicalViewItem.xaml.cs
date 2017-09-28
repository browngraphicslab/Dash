using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI;
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
    public sealed partial class HierarchicalViewItem : UserControl
    {
        private List<DocumentController> ListItemSource { get; set; } =
            new List<DocumentController>();
        public DocumentController DocController;
        public HierarchicalViewItemList List;

        public bool IsChild
        {
            get { return IsChild; }
            set
            {
                xCollapseExpandButton.Visibility = value ? Visibility.Collapsed : Visibility.Visible;
                this.SetList(value);
                this.SetIcon(value);
            }
        }

        private void SetIcon(bool isChild)
        {
            if (isChild)
            {
                xIcon.Text = "□";
            }
            else
            {
                xIcon.Text = "▣";
            }
        }

        public void SetDocViewIcon()
        {
            xIcon.Text = "■";
        }

        private void SetList(bool isChild)
        {
            if (!isChild)
            {
                if (List == null)
                {
                    List = new HierarchicalViewItemList(ListItemSource);
                    xListViewGrid.Children.Add(List);
                }
            }
            else
            {
                if (List != null)
                {
                    xListViewGrid.Children.Remove(List);
                    List = null;
                }
            }
        }

        public HierarchicalViewItem(DocumentController controller)
        {
            this.InitializeComponent();
            DocController = controller;
        }

        public void SetController(DocumentController controller)
        {
            DocController = controller;
        }

        private void Grid_OnTapped(object sender, TappedRoutedEventArgs e)
        {
            HierarchicalMenu.Instance.SelectItem(this);
        }

        public void SetGridColor(SolidColorBrush color)
        {
            xGrid.Background = color;
        }

        public void AddChild(HierarchicalViewItem item)
        {
            ListItemSource.Add(item.DocController);
            List.AddItem(item.DocController);
        }

        public void RemoveChild(HierarchicalViewItem item)
        {
            if (ListItemSource.Contains(item.DocController))
            {
                ListItemSource.Remove(item.DocController);
            }
            DocController.GetPositionField();
            // if the list of children is empty, make this not a parent
            if (ListItemSource.Count == 0)
            {
                IsChild = true;
            }
        }

        private void XCollapseExpandButton_OnTapped(object sender, TappedRoutedEventArgs e)
        {
            if (List != null)
            {
                if (xCollapseExpandButton.Content == "˃")
                {
                    xCollapseExpandButton.Content = "˅";
                    List.IsVisible = true;
                }
                else
                {
                    xCollapseExpandButton.Content = "˃";
                    List.IsVisible = false;
                }
            }
        }

    }
}
