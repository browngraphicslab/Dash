using System;
using System.Collections.Generic;
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

// The User Control item template is documented at http://go.microsoft.com/fwlink/?LinkId=234236

namespace Dash
{
    public sealed partial class SearchView : UserControl
    {
        /// <summary>
        /// Maps PivotItems to the ListView they contain
        /// </summary>
        private Dictionary<PivotItem, ListView> _listViews = new Dictionary<PivotItem, ListView>();
        /// <summary>
        /// Maps ListView to the Category they display (to get the action specified for the category)
        /// </summary>
        private Dictionary<ListView, Category> _categories = new Dictionary<ListView,Category>();
        /// <summary>
        /// Maps PivotItems to the Borders around their headers (to change their backgrounds when selecte)
        /// </summary>
        private Dictionary<PivotItem, Border> _borders = new Dictionary<PivotItem, Border>();

        public SearchView(List<Category> categories)
        {
            this.InitializeComponent();
            MakeCategories(categories);
        }

        private void AddListTappedHandler(ListView list)
        {
            list.Tapped += delegate { this.ItemsSelected(list); };
        }

        /// <summary>
        /// Invoke action specified for the category, passing in the selected item in the ListView
        /// </summary>
        /// <param name="listView"></param>
        private void ItemsSelected(ListView listView)
        {
            var item = listView.SelectedItem;
            _categories[listView].SelectedAction.Invoke(item);
        }


        private void MakeCategories(List<Category> categories)
        {
            foreach(var category in categories)
            {
                MakePivotItem(category);
            }
        }

        private void MakePivotItem(Category category)
        {
            var pivotItem = new PivotItem();

            // border around the header of the pivotitem
            var border = new Border();
            border.Width = 65;
            border.Height = 45;
            border.CornerRadius = new CornerRadius(5);
            border.Background = new SolidColorBrush(Colors.Gray);

            // text part of the title
            var pivotTitle = new TextBlock();
            pivotTitle.Text = category.Title;
            pivotTitle.Foreground = new SolidColorBrush(Colors.White);
            pivotTitle.HorizontalAlignment = HorizontalAlignment.Center;

            // icon part of the title (can be empty)
            var pivotIcon = new TextBlock();
            pivotIcon.Text = category.Icon;
            pivotIcon.Foreground = new SolidColorBrush(Colors.White);
            pivotIcon.HorizontalAlignment = HorizontalAlignment.Center;

            // arrange layout of the header accordingly
            if (category.Icon == string.Empty)
            {
                border.Child = pivotTitle;
                pivotTitle.FontSize = 12;
            } else
            {
                var stack = new StackPanel();
                pivotIcon.FontSize = 20;
                pivotTitle.FontSize = 10;
                pivotTitle.VerticalAlignment = VerticalAlignment.Center;
                stack.Children.Add(pivotIcon);
                stack.Children.Add(pivotTitle);
                border.Child = stack;
            }
            pivotItem.Header = border;

            // grid around the listview that displays the members of the category / search results
            var grid = new Grid();
            grid.CornerRadius = new CornerRadius(10);
            grid.Margin = new Thickness(0, 10, 0, 10);

            var background = new SolidColorBrush(Colors.White);
            background.Opacity = 0.5;
            grid.Background = background;

            // set up listview 
            var listView = new ListView();
            // set up binding, what should generic search bind to? (what to search through)
            var listBinding = new Binding
            {
                Source = category.List,
                Path = new PropertyPath(nameof(OperatorFieldModel.Type)),
                Mode = BindingMode.OneWay,
                UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged
            };
            listView.SetBinding(ListView.ItemsSourceProperty, listBinding);
            AddListTappedHandler(listView);

            _listViews.Add(pivotItem, listView);
            _categories.Add(listView, category);
            _borders.Add(pivotItem, border);
            pivotItem.Content = listView;
            xRootPivot.Items.Add(pivotItem);
        }

        /// <summary>
        /// Highlights header of the selected PivotItem
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void xRootPivot_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            foreach(var border in _borders.Values)
            {
                border.Background = new SolidColorBrush(Colors.Gray);
            }
            _borders[xRootPivot.SelectedItem as PivotItem].Background = new SolidColorBrush(Colors.SteelBlue);
        }
    }

    /// <summary>
    /// Holds information for a category in the SearchView
    /// </summary>
    public class Category{
        public string Icon { get; set; }
        public string Title { get; set; }
        public List<object> List { get; set; }
        public Action<object> SelectedAction;
        public Category(string icon, string title, List<object> list, Action<object> action)
        {
            Icon = icon;
            Title = title;
            List = list;
            SelectedAction = action;
        }
    }

}
