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
        #region STATIC VARIABLES 
        private static TabMenu _instance;
        public static TabMenu Instance => _instance ?? (_instance = new TabMenu());

        public static CollectionFreeformView AddsToThisCollection = MainPage.Instance.GetMainCollectionView().CurrentView as CollectionFreeformView;
        public static Point WhereToAdd;
        #endregion

        public static void Configure(CollectionFreeformView col, Point p)
        {
            AddsToThisCollection = col;
            WhereToAdd = p;
        }

        public List<ITabItemViewModel> TabItems { get; private set; }

        private TabMenu()
        {
            InitializeComponent();
            LostFocus += OnLostFocus;
            GetSearchItems();

            xSearch.TextChanged += XSearch_TextChanged;
            xSearch.QuerySubmitted += XSearch_QuerySubmitted;
            xSearch.Loaded += (sender, args) => SetTextBoxFocus();
        }

        private void OnLostFocus(object sender, RoutedEventArgs routedEventArgs)
        {
            MainPage.Instance.xCanvas.Children.Remove(this);
        }


        private void GetSearchItems()
        {
            var list = new List<ITabItemViewModel>();

            list.Add(new CreateOpTabItemViewModel("Document", Util.BlankDoc));
            list.Add(new CreateOpTabItemViewModel("Collection", Util.BlankCollection));
            list.Add(new CreateOpTabItemViewModel("Note", Util.BlankNote));

            foreach (var op in OperationCreationHelper.Operators)
            {
                list.Add(new CreateOpTabItemViewModel(op.Key, op.Value.OperationDocumentConstructor));
            }

            TabItems = list;
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
            //var results = GetMatches(query);                                                                                         // TODO fix 
            //xListView.ItemsSource = results;
        }

        /// <summary>
        /// Returns results that match the query
        /// </summary>
        private ObservableCollection<object> GetMatches(string searchInput)
        {
            var suggestions = new ObservableCollection<object>();
            //var docNames = _searchList.ListContent;                                                                           // TODO fix 
            //if (docNames != null)
            //{
            //    foreach (var name in docNames)
            //    {
            //        if (name.ToLower().Contains(searchInput.ToLower()) || searchInput == string.Empty)
            //            suggestions.Add(name);
            //    }
            //}
            return suggestions;
        }

        #endregion


        private void XMainGrid_OnDoubleTapped(object sender, DoubleTappedRoutedEventArgs e)
        {
            MainPage.Instance.xCanvas.Children.Remove(Instance);
        }

        public static void ShowAt(Canvas canvas, bool isTouch = false)
        {
            if (Instance != null)
            {
                if (!canvas.Children.Contains(Instance))
                    canvas.Children.Add(Instance);

                if (isTouch) Instance.ConfigureForTouch();
                Canvas.SetLeft(Instance, WhereToAdd.X);
                Canvas.SetTop(Instance, WhereToAdd.Y);
                Instance.SetNoSelection();
            }
        }

        public void ConfigureForTouch()
        {
            xListView.ItemContainerStyle = this.Resources["TouchStyle"] as Style;
        }

        public void MoveSelectedDown()                                                                                     // TODO fix 
        {
            //    if (xListView.SelectedIndex < 0)
            //    {
            //        xListView.SelectedIndex = 0;

            //    }
            //    else if (xListView.SelectedIndex != xListView.Items.Count - 1)
            //    {
            //        xListView.SelectedIndex = xListView.SelectedIndex + 1;
            //        xListView.ScrollIntoView(xListView.SelectedItem);

            //    }
            //    _searchList.SelectedItem = xListView.Items[xListView.SelectedIndex];
            //    xListView.ScrollIntoView(_searchList.SelectedItem);

        }

        public void ActivateItem()
        {
            //_searchList.ActivateItem(_searchList.SelectedItem);                                                  // TODO fix 
        }

        public void SetNoSelection()
        {

            xListView.SelectedIndex = -1;
            //_searchList.SelectedItem = null;                                              // TODO fix
            xSearch.Text = string.Empty;
            UpdateList(string.Empty);
        }

        public void MoveSelectedUp()
        {
            //if (xListView.SelectedIndex <= 0)
            //{
            //    xListView.SelectedIndex = 0;

            //}
            //else
            //{
            //    xListView.SelectedIndex = xListView.SelectedIndex - 1;
            //}
            //_searchList.SelectedItem = xListView.Items[xListView.SelectedIndex];
            //xListView.ScrollIntoView(_searchList.SelectedItem);
        }

        private void xListView_Tapped(object sender, TappedRoutedEventArgs e)
        {
            if (xListView.SelectedIndex == -1) return;
            var name = xListView.SelectedItem as ITabItemViewModel;
            name.ExecuteFunc(); 
            MainPage.Instance.xCanvas.Children.Remove(Instance);
        }
    }
}