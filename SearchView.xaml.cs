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

        public SearchView(List<Category> categories)
        {
            this.InitializeComponent();
            MakeCategories(categories);
        }

        private void AddListTappedHandler(ListView list)
        {
            list.Tapped += delegate { this.ItemsSelected(list); };
        }

        private void ItemsSelected(ListView listView)
        {
            var item = listView.SelectedItem;
            
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

            var border = new Border();
            border.Width = 65;
            border.Height = 45;
            border.CornerRadius = new CornerRadius(5);

            var pivotTitle = new TextBlock();
            pivotTitle.Text = category.Title;
            pivotTitle.Foreground = new SolidColorBrush(Colors.White);
            pivotTitle.HorizontalAlignment = HorizontalAlignment.Center;

            var pivotIcon = new TextBlock();
            pivotIcon.Text = category.Icon;
            pivotIcon.Foreground = new SolidColorBrush(Colors.White);
            pivotIcon.HorizontalAlignment = HorizontalAlignment.Center;

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

            var grid = new Grid();
            grid.CornerRadius = new CornerRadius(10);
            grid.Margin = new Thickness(0, 10, 0, 10);

            var background = new SolidColorBrush(Colors.White);
            background.Opacity = 0.5;
            grid.Background = background;

            var listView = new ListView();
            // set up binding, what should generic search bind to? (what to search through)
            var listBinding = new Binding
            {
                Source = category.List,
                Path = new PropertyPath(nameof(OperatorFieldModel.Type)),
                Mode = BindingMode.OneWay,
                UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged
            };
            _listViews.Add(pivotItem, listView);
            pivotItem.Content = listView;
            xRootPivot.Items.Add(pivotItem);
        }
    }

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
