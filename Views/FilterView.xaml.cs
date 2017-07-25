using System;
using System.Collections.Generic;
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
using Dash.Models;
using Dash.StaticClasses;

// The User Control item template is documented at https://go.microsoft.com/fwlink/?LinkId=234236

namespace Dash
{
    public sealed partial class FilterView : UserControl
    {
        private bool _isHasFieldPreviouslySelected;

        public FilterView()

        {
            this.InitializeComponent();

            DataContextChanged += OnDataContextChanged;
        }

        private DocumentController _filterParams;

        private void OnDataContextChanged(FrameworkElement sender, DataContextChangedEventArgs args)
        {
            var refToOp = args.NewValue as FieldReference;
            var doc = refToOp.GetDocumentController(null);
            _filterParams =
                (doc.GetDereferencedField(FilterOperator.FilterParameterKey, null) as DocumentFieldModelController)
                .Data;
            doc.AddFieldUpdatedListener(FilterOperator.InputCollection,
                delegate(DocumentController controller, DocumentController.DocumentFieldUpdatedEventArgs eventArgs)
                {
                    Documents = eventArgs.NewValue.DereferenceToRoot<DocumentCollectionFieldModelController>(null)
                        .GetDocuments();
                });
        }

        private void UpdateParams()
        {
            if (_filterParams == null)
            {
                return;
            }
            if (xComboBox.SelectedItem == xHasField)
            {
                _filterParams.SetField(FilterOperator.FilterTypeKey, new TextFieldModelController(FilterModel.FilterType.containsKey.ToString()), true);
                _filterParams.SetField(FilterOperator.KeyNameKey, new TextFieldModelController(xSearchFieldBox.Text), true);
                _filterParams.SetField(FilterOperator.FilterValueKey, new TextFieldModelController(""), true);
            }
            else if (xComboBox.SelectedItem == xFieldContains)
            {
                _filterParams.SetField(FilterOperator.FilterTypeKey, new TextFieldModelController(FilterModel.FilterType.valueContains.ToString()), true);
                _filterParams.SetField(FilterOperator.KeyNameKey, new TextFieldModelController(xFieldBox.Text), true);
                _filterParams.SetField(FilterOperator.FilterValueKey, new TextFieldModelController(xSearchBox.Text), true);
            }
            else if (xComboBox.SelectedItem == xFieldEquals)
            {
                _filterParams.SetField(FilterOperator.FilterTypeKey, new TextFieldModelController(FilterModel.FilterType.valueEquals.ToString()), true);
                _filterParams.SetField(FilterOperator.KeyNameKey, new TextFieldModelController(xFieldBox.Text), true);
                _filterParams.SetField(FilterOperator.FilterValueKey, new TextFieldModelController(xSearchBox.Text), true);
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
                xFilterButton.IsEnabled = true;
            }


            if (xFieldBox.Text != "")

            {
                xSearchFieldBox.Text = xFieldBox.Text;

                xFieldBox.Text = "";
            }
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


        /// <summary>
        /// Animate expansion of xMainGrid when the "Field contains" or "Field equals" option is
        /// selected in the combobox (and the previously selected option is "Has field")
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void fieldContainsOrEquals_Tapped(object sender, TappedRoutedEventArgs e)

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
                xFilterButton.IsEnabled = false;

                // case where field option is selected after the text boxes are filled in
            }

            else if (xFieldBox.Text != "" && xSearchBox.Text != "")

            {
                xFilterButton.IsEnabled = true;
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
            UpdateParams();
            //EnableOrDisableFilterButton();
        }


        ///// <summary>
        ///// Generate autosuggestions according to available fields when user types into the autosuggestionbox to prevent mispelling
        ///// </summary>
        ///// <param name="sender"></param>
        ///// <param name="args"></param>
        //private void XFieldBox_OnTextChanged(AutoSuggestBox sender, AutoSuggestBoxTextChangedEventArgs args)

        //{
        //    if (args.Reason == AutoSuggestionBoxTextChangeReason.UserInput)

        //    {
        //        if (sender.Text.Length > 0)

        //        {
        //            sender.ItemsSource = FilterUtils.GetKeySuggestions(Documents, sender.Text.ToLower());
        //        }

        //        else

        //        {
        //            sender.ItemsSource = new string[] {"No suggestions..."};
        //        }
        //    }

        //    // enable and disable button accordingly

        //    EnableOrDisableFilterButton();
        //}

        public List<DocumentController> Documents { get; set; }


        /// <summary>
        /// Specify conditions for the FILTER button to enable or disable
        /// </summary>
        private void EnableOrDisableFilterButton()

        {
            if (xComboBox.SelectedItem == xHasField && xSearchFieldBox.Text != "" ||
                xComboBox.SelectedItem != xHasField && xComboBox.SelectedItem != null && xSearchBox.Text != "" &&
                xFieldBox.Text != "")

            {
                xFilterButton.IsEnabled = true;
            }

            else

            {
                xFilterButton.IsEnabled = false;
            }
        }

        /// <summary>
        /// Remove entire filter view from its parent when the animation finishes playing
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void FadeOutThemeAnimation_Completed(object sender, object e)

        {
            ((Grid) this.Parent).Children.Remove(this);
        }


        /// <summary>
        /// Create FilterModel when the xFilterButton is tapped on
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void XFilterButton_OnTapped(object sender, TappedRoutedEventArgs e)

        {
            FilterModel filterModel = null;


            // generate FilterModels accordingly

            if (xComboBox.SelectedItem == xHasField)

            {
                filterModel = new FilterModel(FilterModel.FilterType.containsKey, xSearchFieldBox.Text, string.Empty);
            }

            else if (xComboBox.SelectedItem == xFieldContains)

            {
                filterModel = new FilterModel(FilterModel.FilterType.valueContains, xFieldBox.Text, xSearchBox.Text);
            }

            else if (xComboBox.SelectedItem == xFieldEquals)

            {
                filterModel = new FilterModel(FilterModel.FilterType.valueEquals, xFieldBox.Text, xSearchBox.Text);
            }


            var list = FilterUtils.Filter(Documents, filterModel);

            // bind gridview to list of DocumentControllers

            //TODO: Do something with list
        }

        private void XComboBox_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            UpdateParams();
        }

        private void XFieldBox_OnTextChanged(AutoSuggestBox sender, AutoSuggestBoxTextChangedEventArgs args)
        {
            UpdateParams();
        }
    }
}
