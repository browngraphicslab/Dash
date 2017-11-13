using System.Collections.Generic;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;
using Dash.Models;
using Dash.StaticClasses;

// The User Control item template is documented at https://go.microsoft.com/fwlink/?LinkId=234236

namespace Dash
{
    public sealed partial class FilterView : UserControl
    {
        // TODO Galen comment this and rename it or remove it
        private bool _isHasFieldPreviouslySelected;

        /// <summary>
        /// The document containing the Filter Operator that this view is associated with
        /// </summary>
        private DocumentController _operatorDoc;

        /// <summary>
        /// List of the documents in the input collection, set when the datacontext is changed
        /// </summary>
        public List<DocumentController> Documents { get; set; }

        public FilterView()
        {
            InitializeComponent();
            DataContextChanged += OnDataContextChanged;
        }

        private void OnDataContextChanged(FrameworkElement sender, DataContextChangedEventArgs args)
        {
            // datacontext is a reference to the operator field
            var refToOp = DataContext as FieldReference;

            // get the document containing the operator
            _operatorDoc = refToOp?.GetDocumentController(null);

            // listen for when the input collection is changed
            _operatorDoc?.AddFieldUpdatedListener(FilterOperatorFieldModelController.InputCollection,
                delegate(DocumentController controller, DocumentController.DocumentFieldUpdatedEventArgs eventArgs)
                {
                    Documents = eventArgs.NewValue.DereferenceToRoot<DocumentCollectionFieldModelController>(null)
                        .GetDocuments();
                });
        }

        private void UpdateParams()
        {
            if (_operatorDoc == null)
                return;
            if (xComboBox.SelectedItem == xHasField)
            {
                _operatorDoc.SetField(FilterOperatorFieldModelController.FilterTypeKey,
                    new TextFieldModelController(FilterModel.FilterType.containsKey.ToString()), true);
                _operatorDoc.SetField(FilterOperatorFieldModelController.KeyNameKey, new TextFieldModelController(xSearchFieldBox.Text),
                    true);
                _operatorDoc.SetField(FilterOperatorFieldModelController.FilterValueKey, new TextFieldModelController(""), true);
            }
            else if (xComboBox.SelectedItem == xFieldContains)
            {
                _operatorDoc.SetField(FilterOperatorFieldModelController.FilterTypeKey,
                    new TextFieldModelController(FilterModel.FilterType.valueContains.ToString()), true);
                _operatorDoc.SetField(FilterOperatorFieldModelController.KeyNameKey, new TextFieldModelController(xFieldBox.Text), true);
                _operatorDoc.SetField(FilterOperatorFieldModelController.FilterValueKey, new TextFieldModelController(xSearchBox.Text),
                    true);
            }
            else if (xComboBox.SelectedItem == xFieldEquals)
            {
                _operatorDoc.SetField(FilterOperatorFieldModelController.FilterTypeKey,
                    new TextFieldModelController(FilterModel.FilterType.valueEquals.ToString()), true);
                _operatorDoc.SetField(FilterOperatorFieldModelController.KeyNameKey, new TextFieldModelController(xFieldBox.Text), true);
                _operatorDoc.SetField(FilterOperatorFieldModelController.FilterValueKey, new TextFieldModelController(xSearchBox.Text),
                    true);
            }
        }

        /// <summary>
        ///     Animate fadeout of the xFieldBox and the collapsing of the xMainGrid
        ///     when the "Has field" option is selected in the combobox
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void hasField_Tapped(object sender, TappedRoutedEventArgs e)
        {
            _isHasFieldPreviouslySelected = true;

            // collapse only if the grid that the xFieldBox is located in is expanded
            if (xFieldBoxGrid.Visibility == Visibility.Visible)
            {
                xHideFieldBox.Begin();
                xCollapseMainGrid.Begin();
            }
            xSearchBox.Visibility = Visibility.Collapsed;
            xSearchFieldBox.Visibility = Visibility.Visible;

            // case where xSearchBox is filled in before user clicks on xHasField
            if (xFieldBox.Text != "")
            {
                xSearchFieldBox.Text = xFieldBox.Text;
                xFieldBox.Text = "";
            }
        }

        /// <summary>
        ///     Resize the grid column that the xFieldBox is located in when the animation that collapses
        ///     the xMainGrid and fades out the xFieldBox finishes playing
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void xCollapseMainGrid_Completed(object sender, object e)
        {
            xFieldBoxGrid.Visibility = Visibility.Collapsed;
        }

        /// <summary>
        ///     Animate expansion of xMainGrid when the "Field contains" or "Field equals" option is
        ///     selected in the combobox (and the previously selected option is "Has field")
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void fieldContainsOrEquals_Tapped(object sender, TappedRoutedEventArgs e)
        {
            // expand only if the grid that the xFieldBox is located in is collapsed
            if (xFieldBoxGrid.Visibility == Visibility.Collapsed)
            {
                // resize actual grid column
                xFieldBoxGrid.Visibility = Visibility.Visible;
                xExpandMainGrid.Begin();
            }
            xSearchBox.Visibility = Visibility.Visible;
            xSearchFieldBox.Visibility = Visibility.Collapsed;
            if (xSearchFieldBox.Text != "" && _isHasFieldPreviouslySelected)
            {
                xFieldBox.Text = xSearchFieldBox.Text;
                xSearchFieldBox.Text = "";
            }
            _isHasFieldPreviouslySelected = false;
        }

        /// <summary>
        ///     Animate fadein of the xFieldBox when the animation that expands the xMainGrid finishes playing
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void xExpandMainGrid_Completed(object sender, object e)
        {
            xShowFieldBox.Begin();
        }

        /// <summary>
        ///     Ensure that the filter button is only responsive when all available combo and text boxes are filled in
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void xSearchBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            UpdateParams();
        }

        private void XComboBox_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            UpdateParams();
        }

        private void XFieldBox_OnTextChanged(AutoSuggestBox sender, AutoSuggestBoxTextChangedEventArgs args)
        {
            if (args.Reason == AutoSuggestionBoxTextChangeReason.UserInput)
                if (sender.Text.Length > 0)
                    sender.ItemsSource = FilterUtils.GetKeySuggestions(Documents, sender.Text.ToLower());
                else
                    sender.ItemsSource = new[] {"No suggestions..."};

            // enable and disable button accordingly
            UpdateParams();
        }
    }
}
