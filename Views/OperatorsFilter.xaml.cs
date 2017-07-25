using Dash.Controllers.Operators;
using DashShared;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
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
using static Dash.Controllers.Operators.DBSearchOperatorFieldModelController;

// The User Control item template is documented at http://go.microsoft.com/fwlink/?LinkId=234236

namespace Dash
{
    public sealed partial class OperatorsFilter : UserControl
    {
        List<string> arithmetics = new List<string>() {"Divide"};
        List<string> sets = new List<string>() { "Union", "Intersection" };
        List<string> maps = new List<string> () { "ImageToUri" };
        List<string> all = new List<string>() { "Divide", "Union", "Intersection", "ImageToUri"};
        public Canvas OverlayParent { get; set; }
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
            RenderTransform = new CompositeTransform();
            OverlayParent = MainPage.Instance.xCanvas;
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
            ManipulationDelta += delegate (object sender, ManipulationDeltaRoutedEventArgs e)
            {
                var transform = (CompositeTransform) RenderTransform;
                Debug.Assert(transform != null);
                transform.TranslateX += e.Delta.Translation.X;
                transform.TranslateY += e.Delta.Translation.Y;
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
            var freeForm =
                MainPage.Instance.MainDocView.GetFirstDescendantOfType<CollectionView>()
                    .CurrentView as CollectionFreeformView;
            DocumentController opModel = null;
            var border = this.GetFirstDescendantOfType<Border>();
            var position = new Point(Canvas.GetLeft(border), Canvas.GetTop(border));
            var translate = new Point();
            if (freeForm != null)
            {
                var r = TransformToVisual(freeForm.xItemsControl.ItemsPanelRoot);
                Debug.Assert(r != null);
                translate = r.TransformPoint(new Point(position.X, position.Y));
            }
            if (type == null) return null;
            if (type == "Divide")
            {
                opModel =
<<<<<<< HEAD
                OperatorDocumentModel.CreateOperatorDocumentModel(
                    new DivideOperatorFieldModelController(new OperatorFieldModel(type)));
=======
                    OperatorDocumentModel.CreateOperatorDocumentModel(
                        new DivideOperatorFieldModelController(new OperatorFieldModel(type)));
                var view = new DocumentView
                {
                    Width = 200,
                    Height = 200
                };

                var opvm = new DocumentViewModel(opModel)
                {
                    GroupTransform = new TransformGroupData(translate, new Point(), new Point(1, 1))
                };
                //OperatorDocumentViewModel opvm = new OperatorDocumentViewModel(opModel);
                view.DataContext = opvm;
>>>>>>> origin/master
            }
            else if (type == "Union")
            {
                opModel =
                    OperatorDocumentModel.CreateOperatorDocumentModel(
                        new UnionOperatorFieldModelController(new OperatorFieldModel(type)));
<<<<<<< HEAD
            } else if (type == "Intersection")
=======
                var unionView = new DocumentView
                {
                    Width = 200,
                    Height = 200
                };
                var unionOpvm = new DocumentViewModel(opModel)
                {
                    GroupTransform = new TransformGroupData(translate, new Point(), new Point(1, 1))
                };
                unionView.DataContext = unionOpvm;
            }
            else if (type == "Intersection")
>>>>>>> origin/master
            {
                // add union operator for testing 
                opModel =
                    OperatorDocumentModel.CreateOperatorDocumentModel(
                        new IntersectionOperatorModelController(new OperatorFieldModel(type)));
<<<<<<< HEAD
            } else if (type == "ImageToUri")
=======
                var intersectView = new DocumentView
                {
                    Width = 200,
                    Height = 200
                };
                var intersectOpvm = new DocumentViewModel(opModel)
                {
                    GroupTransform = new TransformGroupData(translate, new Point(), new Point(1, 1))
                };
                intersectView.DataContext = intersectOpvm;
            }
            else if (type == "ImageToUri")
>>>>>>> origin/master
            {
                // add image url -> image operator for testing
                opModel =
                    OperatorDocumentModel.CreateOperatorDocumentModel(
                        new ImageOperatorFieldModelController(new OperatorFieldModel(type)));
<<<<<<< HEAD
=======
                var imgOpView = new DocumentView
                {
                    Width = 200,
                    Height = 200
                };
                var imgOpvm = new DocumentViewModel(opModel)
                {
                    GroupTransform = new TransformGroupData(translate, new Point(), new Point(1, 1))
                };
                imgOpView.DataContext = imgOpvm;
>>>>>>> origin/master
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
