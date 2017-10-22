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
using DashShared;

// The User Control item template is documented at http://go.microsoft.com/fwlink/?LinkId=234236

namespace Dash
{
    public sealed partial class TabMenu : UserControl
    {
        private static TabMenu _instance;
        public static TabMenu Instance => _instance ?? (_instance = new TabMenu());


        public SearchView SearchView { get; private set; }
        public static CollectionFreeformView AddsToThisCollection = MainPage.Instance.GetMainCollectionView().CurrentView as CollectionFreeformView;

        private TabMenu()
        {
            this.InitializeComponent();
            this.MakeView();
            this.LostFocus += OnLostFocus;
        }

        private void OnLostFocus(object sender, RoutedEventArgs routedEventArgs)
        {
            MainPage.Instance.xCanvas.Children.Remove(this);
        }

        private void MakeView()
        {
            var categories = new List<string> {
                "Add", "Subtract", "Multiply",
                "Divide", "Union", "Intersection",
                "Zip", "UriToImage", "Map", "Api",
                "Concat", "Append", "Filter", "Compound"
            }; 

            var all = new Dictionary<string, Func<DocumentController>>();
            all["Document"] = Util.BlankDoc;
            all["Collection"] = Util.BlankCollection;
            all["Note"] = Util.BlankNote;
            foreach (string s in categories)
                all[s] = OperationCreationHelper.Operators[s].OperationDocumentConstructor; 

            xMainGrid.Children.Add(SearchView = new SearchView(new SearchCategoryItem("∀", "ALL", all)));
        }
        

        public void SetTextBoxFocus()
        {
            SearchView?.SetTextBoxFocus();
        }

        private void XMainGrid_OnDoubleTapped(object sender, DoubleTappedRoutedEventArgs e)
        {
            MainPage.Instance.xCanvas.Children.Remove(Instance);
        }

        public static void ShowAt(Canvas canvas, Point position, bool isTouch = false)
        {
            if (Instance != null)
            {
                if (!canvas.Children.Contains(Instance))
                {
                    canvas.Children.Add(Instance);
                }
                //Instance.SearchView.UpdateCategories(GetSearchCategories());
                if (isTouch) Instance.SearchView.ConfigureForTouch();
                Canvas.SetLeft(Instance, position.X);
                Canvas.SetTop(Instance, position.Y);
                Instance.SearchView.SetNoSelection();
            }
        }
    }
}