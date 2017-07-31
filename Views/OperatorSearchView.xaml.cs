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


        public SearchView SearchView { get; set; }
        public static CollectionView AddsToThisCollection = MainPage.Instance.MainDocView.GetFirstDescendantOfType<CollectionView>();

        private OperatorSearchView()
        {
            this.InitializeComponent();
            this.MakeView();
        }

        private void MakeView()
        {
            // set up the operators, these are all functions which produces new operator documents
            var divide = new Func<DocumentController>(
                () => OperatorDocumentModel.CreateOperatorDocumentModel(new DivideOperatorFieldModelController()));
            var union = new Func<DocumentController>(
                () => OperatorDocumentModel.CreateOperatorDocumentModel(new UnionOperatorFieldModelController()));
            var intersection = new Func<DocumentController>(
                () => OperatorDocumentModel.CreateOperatorDocumentModel(new IntersectionOperatorModelController()));
            var filter = new Func<DocumentController>(OperatorDocumentModel.CreateFilterDocumentController);
            var imagetoUri = new Func<DocumentController>(
                () => OperatorDocumentModel.CreateOperatorDocumentModel(new ImageOperatorFieldModelController()));
            var api = new Func<DocumentController>(OperatorDocumentModel.CreateApiDocumentController);
            var map = new Func<DocumentController>(OperatorDocumentModel.CreateMapDocumentController);


            var arithmetics = new ObservableCollection<KeyValuePair<string, object>>
            {
                new KeyValuePair<string, object>("divide", divide)
            };
            var sets = new ObservableCollection<KeyValuePair<string, object>> {
                new KeyValuePair<string, object>("union", union),
                new KeyValuePair<string, object>("intersection", intersection),
                new KeyValuePair<string, object>("filter", filter)
            };
            var maps = new ObservableCollection<KeyValuePair<string, object>>
            {
                new KeyValuePair<string, object>("imageToUri", imagetoUri),
                new KeyValuePair<string, object>("map", map)
            };
            var all = new ObservableCollection<KeyValuePair<string, object>>
            {
                new KeyValuePair<string, object>("divide", divide),
                new KeyValuePair<string, object>("union", union),
                new KeyValuePair<string, object>("intersection", intersection),
                new KeyValuePair<string, object>("imageToUri", imagetoUri),
                new KeyValuePair<string, object>("filter", filter),
                new KeyValuePair<string, object>("api", api),
            };

            var categories = new List<SearchCategoryItem>();
            categories.Add(new SearchCategoryItem("∀", "ALL",all, Actions.AddOperator));
            categories.Add(new SearchCategoryItem("÷", "ARITHMETIC",arithmetics, Actions.AddOperator));
            categories.Add(new SearchCategoryItem("→","MAP", maps, Actions.AddOperator));
            categories.Add(new SearchCategoryItem("∈","SET", sets, Actions.AddOperator));
            categories.Add(new SearchCategoryItem(string.Empty,"CUSTOM",null,Actions.AddOperator));

            xMainGrid.Children.Add(SearchView = new SearchView(categories));
        }

        private void XMainGrid_OnDoubleTapped(object sender, DoubleTappedRoutedEventArgs e)
        {
            MainPage.Instance.xCanvas.Children.Remove(Instance);
        }
    }
}
