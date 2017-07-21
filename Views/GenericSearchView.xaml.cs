﻿using System;
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
    public sealed partial class GenericSearchView : UserControl
    {
        private Dictionary<PivotItem, SearchCategoryItem> _items = new Dictionary<PivotItem, SearchCategoryItem>();
        public GenericSearchView(List<SearchCategoryItem> categories)
        {
            this.InitializeComponent();
            this.MakeCategories(categories);
            this.SetManipulation();
        }

        private void MakeCategories(List<SearchCategoryItem> categories)
        {
            foreach (var category in categories)
            {
                _items.Add(category.Item, category);
                xRootPivot.Items.Add(category.Item);
            }
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

        /// <summary>
        /// Returns operator types that match the query
        /// </summary>
        /// <param name="searchInput"></param>
        /// <returns></returns>
        private ObservableCollection<object> GetMatches(string searchInput)
        {
            ObservableCollection<object> items = _items[xRootPivot.SelectedItem as PivotItem].OriginalContent;
            ObservableCollection<object> suggestions = new ObservableCollection<object>();
            if (items != null)
            {
                foreach (var item in items)
                {
                    // don't know what to filter yet (how to filter document, collection, fields... etc.)
                    var type = (item as string).ToLower();
                    var input = searchInput.ToLower();
                    if (type.Contains(input))
                    {
                        suggestions.Add(item);
                    }
                }
            }
            return suggestions;
        }

        private void xRootPivot_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            foreach(var item in xRootPivot.Items)
            {
                var pivotItem = item as PivotItem;
                var category = _items[pivotItem];
                category.HeaderBackground = new SolidColorBrush(Colors.Gray);
            }
            _items[xRootPivot.SelectedItem as PivotItem].HeaderBackground = new SolidColorBrush(Colors.SteelBlue);
            xSearch.ItemsSource = null;
        }
    }
}
