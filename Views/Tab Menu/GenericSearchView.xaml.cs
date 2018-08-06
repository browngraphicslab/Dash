using System.Collections.ObjectModel;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;

// The User Control item template is documented at http://go.microsoft.com/fwlink/?LinkId=234236

namespace Dash
{
    public sealed partial class GenericSearchView : UserControl
    {
        private static GenericSearchView instance;
        public static GenericSearchView Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = new GenericSearchView();
                }
                return instance;
            }
        }
        public GenericSearchView()
        {
            this.InitializeComponent();
            this.MakeView();
        }

        // TODO what to do with this file 
        private void MakeView()
        {
            // TODO: get ObservableCollections of all docs, collections, fields... etc
            var docs = new ObservableCollection<object>() { };
            var collections = new ObservableCollection<object>() { };
            var fields = new ObservableCollection<object>() {};
            var all = new ObservableCollection<object>();

            //var categories = new List<SearchCategoryItem>();

            // TODO: create actions to specify what happens when an item in a specific category is selected
            //categories.Add(new SearchCategoryItem(string.Empty, "DOCUMENT", docs, null));
            //categories.Add(new SearchCategoryItem(string.Empty, "COLLECTION", collections, null));
            //categories.Add(new SearchCategoryItem(string.Empty, "FIELD", fields, null));
            //categories.Add(new SearchCategoryItem(string.Empty, "ALL", all, null));
            //var searchView = new SearchView(categories[0]);

            //xMainGrid.Children.Add(searchView);
        }

        private void XMainGrid_OnDoubleTapped(object sender, DoubleTappedRoutedEventArgs e)
        {
            MainPage.Instance.xTabCanvas.Children.Remove(Instance);
        }
    }
}
