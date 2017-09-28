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
    public sealed partial class SearchMenuFlyout : UserControl
    {
        public Dictionary<MenuFlyoutItem, OperatorBuilder> ItemsToOperatorBuilders = new Dictionary<MenuFlyoutItem, OperatorBuilder>();
        public Dictionary<MenuFlyoutSubItem, SearchCategoryItem> SubMenusToCategories = new Dictionary<MenuFlyoutSubItem, SearchCategoryItem>();
        private List<SearchCategoryItem> _categoryItems;

        public SearchMenuFlyout(List<SearchCategoryItem> categories)
        {
            this.InitializeComponent();
            _categoryItems = categories;
            foreach (var category in categories)
            {
                var subMenu = new MenuFlyoutSubItem();
                foreach (var operatorBuilder in category.ListContent)
                {
                    var menuFlyoutItem = new MenuFlyoutItem {Text = operatorBuilder.Name};
                    ItemsToOperatorBuilders[menuFlyoutItem] = operatorBuilder;
                    subMenu.Items?.Add(menuFlyoutItem);
                }
                SubMenusToCategories[subMenu] = category;
            }
        }

        private void XMenuFlyout_OnClosed(object sender, object e)
        {
            //throw new NotImplementedException();
        }

        private void XSearch_OnTextChanged(AutoSuggestBox sender, AutoSuggestBoxTextChangedEventArgs args)
        {
            UpdateList(xSearch.Text);
        }

        /// <summary>
        /// Updates items source of the current listview to reflect search results within the current category
        /// </summary>
        /// <param name="query"></param>
        private void UpdateList(string query)
        {
            xMenuFlyout.Items?.Clear();
            foreach (var item in ItemsToOperatorBuilders.Keys)
            {
                // don't know what to filter yet (how to filter document, collection, fields... etc.)
                var type = item.Text.ToLower();
                var input = query.ToLower();
                if (type.Contains(input))
                {
                    xMenuFlyout.Items?.Add(item);
                }
            }
        }
        
    }
}
