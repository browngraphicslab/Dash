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
        private static OperatorSearchView instance;
        public static OperatorSearchView Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = new OperatorSearchView();
                }
                return instance;
            }
        }

        public OperatorSearchView()
        {
            this.InitializeComponent();
            this.MakeView();
        }

        private void MakeView()
        {
            var arithmetics = new ObservableCollection<object>() { "Divide" };
            var sets = new ObservableCollection<object>() { "Union", "Intersection" };
            var maps = new ObservableCollection<object>() { "ImageToUri" };
            var all = new ObservableCollection<object>(){ "Divide", "Union", "Intersection", "ImageToUri" };

            var categories = new List<SearchCategoryItem>();
            categories.Add(new SearchCategoryItem("∀", "ALL",all, Actions.AddOperator));
            categories.Add(new SearchCategoryItem("÷", "ARITHMETIC",arithmetics, Actions.AddOperator));
            categories.Add(new SearchCategoryItem("→","MAP", maps, Actions.AddOperator));
            categories.Add(new SearchCategoryItem("∈","SET", sets, Actions.AddOperator));
            categories.Add(new SearchCategoryItem(string.Empty,"CUSTOM",null,Actions.AddOperator));
            var searchView = new SearchView(categories);

            xMainGrid.Children.Add(searchView);
        }
    }
}
