using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;
using DashShared;
using Visibility = Windows.UI.Xaml.Visibility;

// The User Control item template is documented at https://go.microsoft.com/fwlink/?LinkId=234236

namespace Dash
{
    public sealed partial class CSVImportHelper : UserControl
    {
        private readonly CsvImportHelperViewModel _vm;

        public CSVImportHelper(CsvImportHelperViewModel viewModel)
        {
            InitializeComponent();
            DataContext = viewModel;
            _vm = viewModel;
        }

        private void XHeaderGridOnDragItemsStarting(object sender, DragItemsStartingEventArgs e)
        {
        }

        private void XHeaderGrid_OnDrop(object sender, DragEventArgs e)
        {
        }

        private void XCancelNewDocTypeButton_OnTapped(object sender, TappedRoutedEventArgs e)
        {
            ToggleAddDocTypeUI(false);
        }

        private void XYesNewDocTypeButton_OnTapped(object sender, TappedRoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(xNewDocTypeTextBox.Text))
                return;

            _vm.DocumentTypeMaps.Add(
                new DocumentTypeToColumnMapViewModel(
                    new DocumentType(DashShared.Util.GenerateNewId(), xNewDocTypeTextBox.Text)
                )
            );


            ToggleAddDocTypeUI(false);
        }

        private void XAddNewDocTypeButton_OnTapped(object sender, TappedRoutedEventArgs e)
        {
            ToggleAddDocTypeUI(true);
        }

        private void ToggleAddDocTypeUI(bool showEditView)
        {
            xAddNewDocTypeButton.Visibility = showEditView ? Visibility.Collapsed : Visibility.Visible;
            xNewDocTypeTextBox.Visibility = showEditView ? Visibility.Visible : Visibility.Collapsed;
            xYesNewDocTypeButton.Visibility = showEditView ? Visibility.Visible : Visibility.Collapsed;
            xCancelNewDocTypeButton.Visibility = showEditView ? Visibility.Visible : Visibility.Collapsed;
        }
    }
}