using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Input;

// The User Control item template is documented at http://go.microsoft.com/fwlink/?LinkId=234236

namespace Dash
{
    public sealed partial class CollectionView : UserControl
    {
      
        public CollectionViewModel ViewModel;
        private bool _isHasFieldPreviouslySelected;
        public Grid OuterGrid
        {
            get { return Grid; }
            set { Grid = value; }
        }

        public CollectionView(CollectionViewModel vm)
        {
            this.InitializeComponent();
            DataContext = ViewModel = vm;
            SetEventHandlers();

        }

        private void SetEventHandlers()
        {
            GridOption.Tapped += ViewModel.GridViewButton_Tapped;
            ListOption.Tapped += ViewModel.ListViewButton_Tapped;
            CloseButton.Tapped += ViewModel.CloseButton_Tapped;
            SelectButton.Tapped += ViewModel.SelectButton_Tapped;
            DeleteSelected.Tapped += ViewModel.DeleteSelected_Tapped;
            Filter.Tapped += ViewModel.FilterSelection_Tapped;
            ClearFilter.Tapped += ViewModel.ClearFilter_Tapped;
            //CancelSoloDisplayButton.Tapped += ViewModel.CancelSoloDisplayButton_Tapped;

            HListView.SelectionChanged += ViewModel.SelectionChanged;
            GridView.SelectionChanged += ViewModel.SelectionChanged;
            

            DraggerButton.Holding += ViewModel.DraggerButtonHolding;
            DraggerButton.ManipulationDelta += ViewModel.Dragger_OnManipulationDelta;
            DraggerButton.ManipulationCompleted += ViewModel.Dragger_ManipulationCompleted;

            Grid.DoubleTapped += ViewModel.OuterGrid_DoubleTapped;

            SingleDocDisplayGrid.Tapped += ViewModel.SingleDocDisplayGrid_Tapped;

            xFilterExit.Tapped += ViewModel.FilterExit_Tapped;
            xFilterButton.Tapped += ViewModel.FilterButton_Tapped;


            xSearchBox.TextCompositionEnded += ViewModel.SearchBox_TextEntered;
            xSearchBox.TextChanged += ViewModel.xSearchBox_TextChanged;

            xFieldBox.TextChanged += ViewModel.FilterFieldBox_OnTextChanged;
            xFieldBox.SuggestionChosen += ViewModel.FilterFieldBox_SuggestionChosen;
            xFieldBox.QuerySubmitted += ViewModel.FilterFieldBox_QuerySubmitted;
            
            xSearchFieldBox.TextChanged += ViewModel.xSearchFieldBox_TextChanged;






        }

        private void UIElement_OnDoubleTapped(object sender, DoubleTappedRoutedEventArgs e)
        {
            ViewModel.DocumentView_OnDoubleTapped(sender, e);
            e.Handled = true;
        }

        private void SoloDocument_OnTapped(object sender, TappedRoutedEventArgs e)
        {
            e.Handled = true;
        }

        private void Grid_OnManipulationStarted(object sender, ManipulationStartedRoutedEventArgs e)
        {
            if (e.Container is ScrollBar || e.Container is ScrollViewer)
            {
                e.Complete();
                e.Handled = true;
            }
        }

        /// <summary>
        /// Animate fadeout of the xFieldBox and the collapsing of the xMainGrid
        /// when the "Has field" option is selected in the combobox
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void hasField_Tapped(object sender, TappedRoutedEventArgs e)
        {
            _isHasFieldPreviouslySelected = true;

            ViewModel.CollectionFilterMode = CollectionViewModel.FilterMode.HasField;

            // collapse only if the grid that the xFieldBox is located in is expanded
            if (xFieldBoxColumn.Width > 0)
            {
                xHideFieldBox.Begin();
                xCollapseMainGrid.Begin();
            }

            xSearchBox.Visibility = Visibility.Collapsed;
            xSearchFieldBox.Visibility = Visibility.Visible;

            // case where xSearchBox is filled in before user clicks on xHasField
            if (xSearchFieldBox.Text != "")
            {
                xFilterButton.Visibility = Visibility.Visible;
            }

            if (xFieldBox.Text != "")
            {
                xSearchFieldBox.Text = xFieldBox.Text;
                xFieldBox.Text = "";
            }
        }
        /// <summary>
        /// Animate expansion of xMainGrid when the "Field contains" or "Field equals" option is
        /// selected in the combobox (and the previously selected option is "Has field")
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void fieldContainsOrEuqals_Tapped(object sender, TappedRoutedEventArgs e)
        {
            // expand only if the grid that the xFieldBox is located in is collapsed
            if (xFieldBoxColumn.Width == 0)
            {
                // resize actual grid column
                xFieldBoxColumn.Width = 165;
                xExpandMainGrid.Begin();
            }

            xSearchBox.Visibility = Visibility.Visible;
            xSearchFieldBox.Visibility = Visibility.Collapsed;

            // xFieldBox is cleared when xFieldContains or xFieldEquals is selected, button must be disabled
            if (xFieldBox.Text == "")
            {
                xFilterButton.Visibility = Visibility.Collapsed;
                // case where field option is selected after the text boxes are filled in
            }
            else if (xFieldBox.Text != "" && xSearchBox.Text != "")
            {
                xFilterButton.Visibility = Visibility.Visible;
            }

            if (xSearchFieldBox.Text != "" && _isHasFieldPreviouslySelected)
            {
                xFieldBox.Text = xSearchFieldBox.Text;
                xSearchFieldBox.Text = "";
            }

            _isHasFieldPreviouslySelected = false;
        }

        /// <summary>
        /// Animate fadein of the xFieldBox when the animation that expands the xMainGrid finishes playing
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void xExpandMainGrid_Completed(object sender, object e)
        {
            xShowFieldBox.Begin();
        }

        /// <summary>
        /// Ensure that the filter button is only responsive when all available combo and text boxes are filled in
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void xSearchBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            EnableOrDisableFilterButton();
        }

        /// <summary>
        /// Generate autosuggestions according to available fields when user types into the autosuggestionbox to prevent mispelling
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="args"></param>


        /// <summary>
        /// Specify conditions for the FILTER button to enable or disable
        /// </summary>
        private void EnableOrDisableFilterButton()
        {
            if (xComboBox.SelectedItem == xHasField && xSearchFieldBox.Text != "" || xComboBox.SelectedItem != xHasField && xComboBox.SelectedItem != null && xSearchBox.Text != "" && xFieldBox.Text != "")
            {
                xFilterButton.Visibility = Visibility.Visible;
            }
            else
            {
                xFilterButton.Visibility = Visibility.Collapsed;
            }
        }

        /// <summary>
        /// Remove entire filter view from its parent when the animation finishes playing
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void FadeOutThemeAnimation_Completed(object sender, object e)
        {
            ((Grid)this.Parent).Children.Remove(this);
        }

        /// <summary>
        /// Resize the grid column that the xFieldBox is located in when the animation that collapses
        /// the xMainGrid and fades out the xFieldBox finishes playing
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void xCollapseMainGrid_Completed(object sender, object e)
        {
            xFieldBoxColumn.Width = 0;
        }

        private void XFieldBox_OnTextChanged(AutoSuggestBox sender, AutoSuggestBoxTextChangedEventArgs args)
        {
            
            // enable and disable button accordingly
            EnableOrDisableFilterButton();
        }

        private void fieldContains_Tapped(object sender, TappedRoutedEventArgs e)
        {
            ViewModel.CollectionFilterMode = CollectionViewModel.FilterMode.FieldContains;
            fieldContainsOrEuqals_Tapped(sender, e);
        }

        private void fieldEquals_Tapped(object sender, TappedRoutedEventArgs e)
        {
            ViewModel.CollectionFilterMode = CollectionViewModel.FilterMode.FieldEquals;
            fieldContainsOrEuqals_Tapped(sender, e);
        }
    }
}
