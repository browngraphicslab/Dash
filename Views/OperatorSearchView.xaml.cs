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

// The User Control item template is documented at http://go.microsoft.com/fwlink/?LinkId=234236

namespace Dash
{
    public sealed partial class OperatorSearchView : UserControl
    {
        private static OperatorSearchView _instance;
        public static OperatorSearchView Instance => _instance ?? (_instance = new OperatorSearchView());


        public SearchView SearchView { get; private set; }
        public static CollectionView AddsToThisCollection = MainPage.Instance.GetMainCollectionView();

        private OperatorSearchView()
        {
            this.InitializeComponent();
            this.MakeView();
        }

        private void MakeView()
        {
            var arithmetics = new ObservableCollection<object>() { "Divide" };
            var sets = new ObservableCollection<object>() { "Union", "Intersection", "Filter" };
            var maps = new ObservableCollection<object>() { "ImageToUri" };
            var all = new ObservableCollection<object>(){ "Divide", "Union", "Intersection", "ImageToUri", "Filter", "Api" };

            var categories = new List<SearchCategoryItem>
            {
                new SearchCategoryItem("∀", "ALL", all, Actions.AddOperator),
                new SearchCategoryItem("÷", "ARITHMETIC", arithmetics, Actions.AddOperator),
                new SearchCategoryItem("→", "MAP", maps, Actions.AddOperator),
                new SearchCategoryItem("∈", "SET", sets, Actions.AddOperator),
                new SearchCategoryItem(string.Empty, "CUSTOM", null, Actions.AddOperator)
            };

            xMainGrid.Children.Add(SearchView = new SearchView(categories));
        }

        private void XMainGrid_OnDoubleTapped(object sender, DoubleTappedRoutedEventArgs e)
        {
            MainPage.Instance.xCanvas.Children.Remove(Instance);
        }
    }
}
