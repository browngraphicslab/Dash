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
    public sealed partial class OperatorsFilter : UserControl
    {
        List<string> arithmetics = new List<string>() {"Divide"};
        List<string> sets = new List<string>() { "Union", "Intersection" };
        List<string> maps = new List<string> () { "ImageToUri" };
        List<string> all = new List<string>() { "Divide", "Union", "Intersection", "ImageToUri"};
        private static OperatorsFilter instance;
        public static OperatorsFilter Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = new OperatorsFilter();
                }
                return instance;
            }
        }
        private OperatorsFilter()
        {
            this.InitializeComponent();
            //this.SetManipulation();
            xAllList.Tapped += delegate { this.ItemsSelected(xAllList); };
            xArithmeticList.Tapped += delegate { this.ItemsSelected(xArithmeticList); };
            xMapList.Tapped += delegate { this.ItemsSelected(xMapList); };
            xSetList.Tapped += delegate { this.ItemsSelected(xSetList); };
            xSearch.TextChanged += XSearch_TextChanged;
            xSearch.QuerySubmitted += XSearch_QuerySubmitted;
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
            var listView = GetCurrentListView();
            listView.ItemsSource = results;
        }

        private void XSearch_TextChanged(AutoSuggestBox sender, AutoSuggestBoxTextChangedEventArgs args)
        {
            if (args.Reason == AutoSuggestionBoxTextChangeReason.UserInput)
            {
                if (sender.Text.Length > 0)
                {
                    sender.ItemsSource = GetMatches(sender.Text);
                } else
                {
                    sender.ItemsSource = null;
                    xAllList.ItemsSource = all;
                    xArithmeticList.ItemsSource = arithmetics;
                    xSetList.ItemsSource = sets;
                    xMapList.ItemsSource = maps;
                }
            }
        }

        /// <summary>
        /// Returns the list view in the current pivot
        /// </summary>
        /// <returns></returns>
        private ListView GetCurrentListView()
        {
            ListView currentListView = null;
            var selectedItem = xRootPivot.SelectedItem;
            currentListView = selectedItem == xAllTab ? xAllList : currentListView;
            currentListView = selectedItem == xArithmeticTab ? xArithmeticList : currentListView;
            currentListView = selectedItem == xSetTab ? xSetList : currentListView;
            currentListView = selectedItem == xMapTab ? xMapList : currentListView;
            return currentListView; 
        }

        /// <summary>
        /// Returns operator types that match the query
        /// </summary>
        /// <param name="searchInput"></param>
        /// <returns></returns>
        private List<string> GetMatches(string searchInput)
        {
            List<string> list = null;
            list = xRootPivot.SelectedItem == xAllTab ? all:list;
            list = xRootPivot.SelectedItem == xArithmeticTab ? arithmetics:list;
            list = xRootPivot.SelectedItem == xMapTab ? maps : list;
            list = xRootPivot.SelectedItem == xSetTab ? sets : list;
            List<string> suggestions = new List<string>();
            if (list != null)
            {
                foreach(var item in list)
                {
                    var type = item.ToLower();
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
        /// Creates and adds operator to workspace
        /// </summary>
        /// <param name="listView"></param>
        private void ItemsSelected(ListView listView)
        {
            var model = (string)listView.SelectedItem;
            var docController = this.CreateOperator(model);
            if (docController != null)
                MainPage.Instance.DisplayDocument(docController);
        }

        /// <summary>
        /// Generates operator according to the item selected in the list view
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        private DocumentController CreateOperator(string type)
        {
            DocumentController opModel = null;
            if (type == null) return null;
            if (type == "Divide")
            {
                opModel =
                OperatorDocumentModel.CreateOperatorDocumentModel(
                    new DivideOperatorFieldModelController(new OperatorFieldModel(type)));
                var view = new DocumentView
                {
                    Width = 200,
                    Height = 200
                };
                var opvm = new DocumentViewModel(opModel);
                //OperatorDocumentViewModel opvm = new OperatorDocumentViewModel(opModel);
                view.DataContext = opvm;
            } else if (type == "Union")
            {
                opModel =
                    OperatorDocumentModel.CreateOperatorDocumentModel(
                        new UnionOperatorFieldModelController(new OperatorFieldModel(type)));
                var unionView = new DocumentView
                {
                    Width = 200,
                    Height = 200
                };
                var unionOpvm = new DocumentViewModel(opModel);
                unionView.DataContext = unionOpvm;
            } else if (type == "Intersection")
            {
                // add union operator for testing 
                opModel =
                    OperatorDocumentModel.CreateOperatorDocumentModel(
                        new IntersectionOperatorModelController(new OperatorFieldModel(type)));
                var intersectView = new DocumentView
                {
                    Width = 200,
                    Height = 200
                };
                var intersectOpvm = new DocumentViewModel(opModel);
                intersectView.DataContext = intersectOpvm;
            } else if (type == "ImageToUri")
            {
                // add image url -> image operator for testing
                opModel =
                    OperatorDocumentModel.CreateOperatorDocumentModel(
                        new ImageOperatorFieldModelController(new OperatorFieldModel(type)));
                var imgOpView = new DocumentView
                {
                    Width = 200,
                    Height = 200
                };
                var imgOpvm = new DocumentViewModel(opModel);
                imgOpView.DataContext = imgOpvm;
            }
            return opModel;
        }

        /// <summary>
        /// Hightlights selected pivot
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void xRootPivot_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            xAllBorder.Background = xRootPivot.SelectedItem == xAllTab ? new SolidColorBrush(Colors.SteelBlue) : new SolidColorBrush(Colors.Gray);
            xArithmeticBorder.Background = xRootPivot.SelectedItem == xArithmeticTab ? new SolidColorBrush(Colors.SteelBlue) : new SolidColorBrush(Colors.Gray);
            xMapBorder.Background = xRootPivot.SelectedItem == xMapTab ? new SolidColorBrush(Colors.SteelBlue) : new SolidColorBrush(Colors.Gray);
            xSetBorder.Background = xRootPivot.SelectedItem == xSetTab ? new SolidColorBrush(Colors.SteelBlue) : new SolidColorBrush(Colors.Gray);
            xCustomBorder.Background = xRootPivot.SelectedItem == xCustomTab ? new SolidColorBrush(Colors.SteelBlue) : new SolidColorBrush(Colors.Gray);
            // makes sure that no suggestions from previous searches in other categories would show up
            xSearch.ItemsSource = null;
        }
    }

}
