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
using Dash.Controllers;
using DashShared;

// The User Control item template is documented at http://go.microsoft.com/fwlink/?LinkId=234236

namespace Dash
{
    public sealed partial class TabMenu : UserControl
    {
        private static TabMenu _instance;
        public static TabMenu Instance => _instance ?? (_instance = new TabMenu());


        //public SearchView SearchView { get; private set; }
        public static CollectionFreeformView AddsToThisCollection = MainPage.Instance.GetMainCollectionView().CurrentView as CollectionFreeformView;

        public Point AddHere { get; set; }

        private List<TabItemViewModel> _tabItems;
        private SearchCategoryItem _searchList;

        private TabMenu()
        {
            InitializeComponent();
            

            LostFocus += OnLostFocus;
            //_tabItems = ??? 


            _searchList = GetSearchCategories(); 
            ListGrid.Children.Add(_searchList);
            _searchList.Margin = new Thickness(0);
            OuterGrid.Width = _searchList.List.Width;

            xSearch.TextChanged += XSearch_TextChanged;
            xSearch.QuerySubmitted += XSearch_QuerySubmitted;
            xSearch.Loaded += (sender, args) => SetTextBoxFocus();
        }

        private void OnLostFocus(object sender, RoutedEventArgs routedEventArgs)
        {
            MainPage.Instance.xCanvas.Children.Remove(this);
        }


        private static SearchCategoryItem GetSearchCategories()
        {
            var all = new ObservableCollection<Func<DocumentController>>
            {
                Util.BlankDoc,
                Util.BlankCollection,
                Util.BlankNote
            };

            foreach (var op in OperationCreationHelper.Operators)
            {
                all.Add(op.Value.OperationDocumentConstructor);
            }

            return new SearchCategoryItem("", "", all);
        }

        #region xSEARCH
        public void SetTextBoxFocus()
        {
            xSearch.Focus(FocusState.Programmatic);
        }

        private void XSearch_QuerySubmitted(AutoSuggestBox sender, AutoSuggestBoxQuerySubmittedEventArgs args)
        {
            UpdateList(args.QueryText);
        }

        /// <summary>
        /// Generates suggestions for searchbox
        /// </summary>
        private void XSearch_TextChanged(AutoSuggestBox sender, AutoSuggestBoxTextChangedEventArgs args)
        {
            if (args.Reason == AutoSuggestionBoxTextChangeReason.UserInput)
                UpdateList(sender.Text);
        }

        /// <summary>
        /// Updates items source of the current listview to reflect search results within the current category
        /// </summary>
        private void UpdateList(string query)
        {
            var results = GetMatches(query);
            _searchList.List.ItemsSource = results;
        }

        /// <summary>
        /// Returns results that match the query
        /// </summary>
        private ObservableCollection<object> GetMatches(string searchInput)
        {
            var suggestions = new ObservableCollection<object>();
            var docNames = _searchList.ListContent;
            if (docNames != null)
            {
                foreach (var name in docNames)
                {
                    if (name.ToLower().Contains(searchInput.ToLower()) || searchInput == string.Empty)
                        suggestions.Add(name);
                }
            }
            return suggestions;
        }

        #endregion



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
                if (isTouch) Instance.ConfigureForTouch();
                Canvas.SetLeft(Instance, position.X);
                Canvas.SetTop(Instance, position.Y);
                Instance.SetNoSelection();
            }
        }

        public void ConfigureForTouch()
        {
            _searchList.List.ItemContainerStyle = this.Resources["TouchStyle"] as Style;
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
    }
}