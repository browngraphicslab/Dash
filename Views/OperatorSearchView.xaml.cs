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
        public static CollectionFreeformView AddsToThisCollection = MainPage.Instance.GetMainCollectionView().CurrentView as CollectionFreeformView;

        private OperatorSearchView()
        {
            this.InitializeComponent();
            this.MakeView();
        }

        private void MakeView()
        {


            var divide = OperationCreationHelper.Operators.FirstOrDefault(ob => ob.Name == "divide");
            var union = OperationCreationHelper.Operators.FirstOrDefault(ob => ob.Name == "union");
            var intersection = OperationCreationHelper.Operators.FirstOrDefault(ob => ob.Name == "intersection");
            var uriToImage = OperationCreationHelper.Operators.FirstOrDefault(ob => ob.Name == "uriToImage");
            var map = OperationCreationHelper.Operators.FirstOrDefault(ob => ob.Name == "map");
            var api = OperationCreationHelper.Operators.FirstOrDefault(ob => ob.Name == "api");
            var filter = OperationCreationHelper.Operators.FirstOrDefault(ob => ob.Name == "filter");
            var compound = OperationCreationHelper.Operators.FirstOrDefault(ob => ob.Name == "compound");

            var arithmetics = new ObservableCollection<OperatorBuilder>
            {
                divide
            };
            var sets = new ObservableCollection<OperatorBuilder> {
                union,
                intersection,
                filter
            };
            var maps = new ObservableCollection<OperatorBuilder>
            {
                uriToImage,
                map
            };
            var all = new ObservableCollection<OperatorBuilder>
            {
                divide,
                union,
                intersection,
                filter,
                api, 
                compound,
                map
            };

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
