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
        private SearchCategoryItem _searchList;
        public SearchView(SearchCategoryItem categories)
        {
            this.InitializeComponent();
            this.MakeCategories(categories);
            //this.SetManipulation();
            xSearch.TextChanged += XSearch_TextChanged;
            xSearch.QuerySubmitted += XSearch_QuerySubmitted;
            xSearch.Loaded += (sender, args) => SetTextBoxFocus();

        }

        public void ConfigureForTouch()
        {
            _searchList.List.ItemContainerStyle = this.Resources["TouchStyle"] as Style;
        }

        public void ConfigureForMouse()
        {
            _searchList.List.ItemContainerStyle = this.Resources["MouseStyle"] as Style;
        }

        public void MoveSelectedDown()
        {
            if (_searchList.List.SelectedIndex < 0)
            {
                _searchList.List.SelectedIndex = 0;

            } 
            else if (_searchList.List.SelectedIndex != _searchList.List.Items.Count - 1)
            {
                _searchList.List.SelectedIndex = _searchList.List.SelectedIndex + 1;
                _searchList.List.ScrollIntoView(_searchList.List.SelectedItem);

            }
            _searchList.SelectedItem = _searchList.List.Items[_searchList.List.SelectedIndex];
            _searchList.List.ScrollIntoView(_searchList.SelectedItem);

        }

        public void ActivateItem()
        {
            _searchList.ActivateItem(_searchList.SelectedItem);
        }

        public void MoveSelectedUp()
        {
            if (_searchList.List.SelectedIndex <= 0)
            {
                _searchList.List.SelectedIndex = 0;
                
            }
            else
            {
                _searchList.List.SelectedIndex = _searchList.List.SelectedIndex - 1;
            }
            _searchList.SelectedItem = _searchList.List.Items[_searchList.List.SelectedIndex];
            _searchList.List.ScrollIntoView(_searchList.SelectedItem);
        }

        private void MakeCategories(SearchCategoryItem categories)
        {
            _searchList = categories;
            ListGrid.Children.Add(categories);
            categories.Margin=new Thickness(0);
            OuterGrid.Width = categories.List.Width;
        }

        public void SetNoSelection()
        {
            if (_searchList != null)
            {
                _searchList.List.SelectedIndex = -1;
                _searchList.SelectedItem = null;
            }
            xSearch.Text = string.Empty;
            UpdateList(string.Empty);
        }

        public void SetTextBoxFocus()
        {
            xSearch.Focus(FocusState.Programmatic);
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
            //if (xTitleList.SelectedItem != null)
            _searchList.List.ItemsSource = results;
        }

        /// <summary>
        /// Returns results that match the query
        /// </summary>
        /// <param name="searchInput"></param>
        /// <returns></returns>
        private ObservableCollection<object> GetMatches(string searchInput)
        {
            var suggestions = new ObservableCollection<object>();
            //if (xTitleList.SelectedItem == null) return suggestions; 
            var docNames = _searchList.ListContent;
            if (docNames != null)
            {
                foreach (var name in docNames)
                {
                    if (name.ToLower().Contains(searchInput.ToLower()) || searchInput == string.Empty)
                    {
                        suggestions.Add(name);
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
                UpdateList(sender.Text);

                //if (sender.Text.Length > 0)
                //{
                //    //sender.ItemsSource = GetMatches(sender.Text);
                //    UpdateList(sender.Text);
                //}
                //else
                //{
                //    sender.ItemsSource = null;
                //    if (xTitleList.SelectedItem != null)
                //        _items[xTitleList.SelectedItem as PivotItem].List.ItemsSource = _items[xTitleList.SelectedItem as PivotItem].ListContent;
                //}
            }
        }

        /// <summary>
        /// Highlights the header of the selected pivot item
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void xRootPivot_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            //foreach(var item in xRootPivot.Items)
            //{
            //    var pivotItem = item as PivotItem;
            //    var headerBorder = pivotItem.Header as Border;
            //    if (xRootPivot.SelectedItem as PivotItem == pivotItem)
            //    {
            //        headerBorder.Background = new SolidColorBrush(Colors.SteelBlue);
            //    }
            //    else
            //    {
            //        headerBorder.Background = new SolidColorBrush(Colors.Gray);
            //    }
            //}
            xSearch.ItemsSource = null;
            // doesn't update list
//            UpdateList(xSearch.Text);
        }

     
    }
}
