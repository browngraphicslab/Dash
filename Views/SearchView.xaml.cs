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

// The User Control item template is documented at http://go.microsoft.com/fwlink/?LinkId=234236

namespace Dash
{
    public sealed partial class SearchView : UserControl
    {
        private Dictionary<PivotItem, SearchCategoryItem> _items = new Dictionary<PivotItem, SearchCategoryItem>();
        public SearchView(List<SearchCategoryItem> categories)
        {
            this.InitializeComponent();
            this.MakeCategories(categories);
            //this.SetManipulation();
            xSearch.TextChanged += XSearch_TextChanged;
            xSearch.QuerySubmitted += XSearch_QuerySubmitted;
        }

        private void MakeCategories(List<SearchCategoryItem> categories)
        {
            foreach (var category in categories)
            {
                var pivotItem = new PivotItem();
                pivotItem.Content = category;
                pivotItem.Header = MakePivotItemHeader(category);
                _items.Add(pivotItem, category);
                xRootPivot.Items?.Add(pivotItem);
            }
        }

        /// <summary>
        /// Creates header for the pivot item
        /// </summary>
        /// <param name="category"></param>
        /// <returns></returns>
        private Border MakePivotItemHeader(SearchCategoryItem category)
        {
            var iconTextBlock = new TextBlock()
            {
                Text = category.Icon,
                Foreground = new SolidColorBrush(Colors.White),
                HorizontalAlignment = HorizontalAlignment.Center,
                FontSize = 20
            };

            var titleTextBlock = new TextBlock()
            {
                Text = category.Title,
                FontSize= 10,
                Foreground = new SolidColorBrush(Colors.White),
                HorizontalAlignment = HorizontalAlignment.Center
            };

            var headerBorder = new Border()
            {
                Width = 55,
                Height = 50,
                Background = new SolidColorBrush(Colors.Transparent),
                CornerRadius = new CornerRadius(0)
            };
            if (category.Icon != string.Empty && category.Icon != null)
            {
                var stack = new StackPanel();
                stack.Orientation = Orientation.Vertical;
                stack.Children.Add(iconTextBlock);
                stack.Children.Add(titleTextBlock);
                headerBorder.Child = stack;
            }
            else
            {
                titleTextBlock.FontSize = 10;
                titleTextBlock.VerticalAlignment = VerticalAlignment.Center;
                headerBorder.Child = titleTextBlock;
            }
            return headerBorder;
        }

        /// <summary>
        /// Adds handler to move the control
        /// </summary>
        private void SetManipulation()
        {
            ManipulationMode = ManipulationModes.All;
            RenderTransform = new CompositeTransform();
            ManipulationDelta += delegate (object sender, ManipulationDeltaRoutedEventArgs e)
            {
                var transform = RenderTransform as CompositeTransform;
                if (transform != null)
                {
                    transform.TranslateX += e.Delta.Translation.X;
                    transform.TranslateY += e.Delta.Translation.Y;
                }
            };
        }

        private void XSearch_QuerySubmitted(AutoSuggestBox sender, AutoSuggestBoxQuerySubmittedEventArgs args)
        {
            this.UpdateList(args.QueryText);
        }

        /// <summary>
        /// Updates items source of the current listview to reflect search results within the current category
        /// </summary>
        /// <param name="query"></param>
        private void UpdateList(string query)
        {
            var results = GetMatches(query);
            if (xRootPivot.SelectedItem != null)
                _items[xRootPivot.SelectedItem as PivotItem].List.ItemsSource = results;
        }

        /// <summary>
        /// Returns results that match the query
        /// </summary>
        /// <param name="searchInput"></param>
        /// <returns></returns>
        private ObservableCollection<object> GetMatches(string searchInput)
        {
            var suggestions = new ObservableCollection<object>();
            if (xRootPivot.SelectedItem == null) return suggestions; 
            var items = _items[xRootPivot.SelectedItem as PivotItem].ListContent;
            if (items != null)
            {
                foreach (var item in items)
                {
                    // don't know what to filter yet (how to filter document, collection, fields... etc.)
                    var type = item.Name.ToLower();
                    var input = searchInput.ToLower();
                    if (type.Contains(input))
                    {
                        suggestions.Add(item);
                    }
                }
            }
            return suggestions;
        }

        /// <summary>
        /// Generates suggestions for searchbox
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="args"></param>
        private void XSearch_TextChanged(AutoSuggestBox sender, AutoSuggestBoxTextChangedEventArgs args)
        {
            if (args.Reason == AutoSuggestionBoxTextChangeReason.UserInput)
            {
                if (sender.Text.Length > 0)
                {
                    sender.ItemsSource = GetMatches(sender.Text);
                }
                else
                {
                    sender.ItemsSource = null;
                    if (xRootPivot.SelectedItem != null)
                        _items[xRootPivot.SelectedItem as PivotItem].List.ItemsSource = _items[xRootPivot.SelectedItem as PivotItem].ListContent;
                }
            }
        }

        /// <summary>
        /// Highlights the header of the selected pivot item
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void xRootPivot_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            foreach(var item in xRootPivot.Items)
            {
                var pivotItem = item as PivotItem;
                var headerBorder = pivotItem.Header as Border;
                if (xRootPivot.SelectedItem as PivotItem == pivotItem)
                {
                    headerBorder.Background = new SolidColorBrush(Colors.SteelBlue);
                }
                else
                {
                    headerBorder.Background = new SolidColorBrush(Colors.Gray);
                }
            }
            xSearch.ItemsSource = null;
            // doesn't update list
//            UpdateList(xSearch.Text);
        }
        
    }
}
