using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Windows.ApplicationModel.DataTransfer;
using Windows.System;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;
using DashShared;
using Newtonsoft.Json;
using Visibility = Windows.UI.Xaml.Visibility;

// The User Control item template is documented at https://go.microsoft.com/fwlink/?LinkId=234236

namespace Dash
{
    public sealed partial class CSVImportHelper : WindowTemplate
    {
        private readonly CsvImportHelperViewModel _vm;

        private TaskCompletionSource<CsvImportHelperViewModel> _tcs;


        /// <summary>
        /// Private drag key to determine if the drag operations came from this view
        /// </summary>
        private static string _headerDragKey = "08AFF7A5-823C-4810-93B6-AEFCBB0C37D0";
        private static string _dataDragKey = "199DCDDF-5C62-4FB2-B60C-A9B39943F86C";
        private string _dataListDragKey = "0B586B7A-C835-405A-89DA-B562C0E8D1CE";


        // private variables for header drag and drop
        private object _headerDragSender;
        private object _headerDragReceiver;
        private bool _isDroppedOnOtherHeaderList;

        // private variable for data drag and drop
        private bool _selfDragOnDataList;


        public CSVImportHelper(CsvImportHelperViewModel viewModel)
        {
            InitializeComponent();
            DataContext = viewModel;
            _vm = viewModel;
            _tcs = new TaskCompletionSource<CsvImportHelperViewModel>();

            // event for when the window closes
            OnWindowClosed += OnOnWindowClosed;
        }

        #region HeaderDragAndDrop

        /// <summary>
        /// Called when a header item is dragged from a list
        /// </summary>
        private void XHeaderGridOnDragItemsStarting(object sender, DragItemsStartingEventArgs e)
        {
            // Set the content of the DataPackage
            var items = e.Items.Select(item => item as string).ToList();
            var serializedItems = JsonConvert.SerializeObject(items);
            e.Data.SetText(serializedItems);
            // we want to move items between lists
            e.Data.RequestedOperation = DataPackageOperation.Move;
            e.Data.Properties[_headerDragKey] = true;
            _headerDragSender = sender;
        }

        /// <summary>
        /// Called when a header item has finished being dragged to another list
        /// </summary>
        private void XHeaderGrid_OnDragItemsCompleted(ListViewBase sender, DragItemsCompletedEventArgs args)
        {
            var headerVM = sender.DataContext as IHeaderViewModel;

            // if the drop occured on another list in this importer
            // remove each of the headers that were moved
            if (_isDroppedOnOtherHeaderList)
            {
                if (args.DropResult == DataPackageOperation.Move)
                {
                    foreach (var item in args.Items.ToArray())
                    {
                        headerVM.RemoveHeader(item as string);
                    }
                }
            }

            _isDroppedOnOtherHeaderList = false;
            _headerDragSender = null;
            _headerDragReceiver = null;

        }

        /// <summary>
        /// Called when an item is dragged over a header grid, determines whether or not
        /// we will accept the drag operation
        /// </summary>
        private void XHeaderGrid_OnDragOver(object sender, DragEventArgs e)
        {
            // if it was dragged from another list view in this importer
            if (e.DataView.Properties.ContainsKey(_headerDragKey))
            {
                _headerDragReceiver = sender;

                // and its not the same list view
                if (!ReferenceEquals(_headerDragReceiver, _headerDragSender))
                {
                    e.AcceptedOperation = DataPackageOperation.Move;
                    return;
                }
            }
            e.AcceptedOperation = DataPackageOperation.None;
        }

        /// <summary>
        /// Called when a header item is dropped on a list
        /// </summary>
        private async void XHeaderGrid_OnDrop(object sender, DragEventArgs e)
        {
            var def = e.GetDeferral();
            var serializedItems = await e.DataView.GetTextAsync();
            var deserializedItems = JsonConvert.DeserializeObject<List<string>>(serializedItems);


            var headerVM = (sender as FrameworkElement)?.DataContext as IHeaderViewModel;
            foreach (var header in deserializedItems)
            {
                headerVM?.AddHeader(header);
            }
            _isDroppedOnOtherHeaderList = true;

            def.Complete();
        }

        #endregion

        #region DocumentTypeDragAndDrop

        private void DocumentTypeDragStarting(UIElement sender, DragStartingEventArgs args)
        {
            var tb = sender as TextBlock;
            if (tb != null)
            {
                var docType = _vm.DocToColumnMaps.FirstOrDefault(i => i.DocumentType.Type.Equals(tb.Text))?.DocumentType;
                if (docType != null)
                {
                    args.AllowedOperations = DataPackageOperation.Copy;
                    args.Data.SetText(JsonConvert.SerializeObject(docType));
                    args.Data.Properties[_dataDragKey] = true;
                    return;
                }
            }
            args.AllowedOperations = DataPackageOperation.None;
        }

        private void DocumentType_PointerPressed(object sender, PointerRoutedEventArgs e)
        {
            var pointerPoint = e.GetCurrentPoint(sender as UIElement);
            (sender as UIElement).StartDragAsync(pointerPoint);
        }

        private void XDataGrid_OnDragItemsStarting(object sender, DragItemsStartingEventArgs e)
        {
            // Set the content of the DataPackage
            var items = e.Items.Select(item => item as DocumentType).ToList();
            var serializedItems = JsonConvert.SerializeObject(items);
            e.Data.SetText(serializedItems);
            // we want to move items between lists
            e.Data.RequestedOperation = DataPackageOperation.Copy;
            e.Data.Properties[_dataListDragKey] = true;
        }

        private async void XDataGrid_OnDrop(object sender, DragEventArgs e)
        {
            var def = e.GetDeferral();

            if (e.DataView.Properties.ContainsKey(_dataListDragKey))
            {
                _selfDragOnDataList = true;
            }
            else
            {
                var serializedItems = await e.DataView.GetTextAsync();
                var docType = JsonConvert.DeserializeObject<DocumentType>(serializedItems);

                var dataVM = (sender as FrameworkElement)?.DataContext as IDataDocTypeViewModel;
                dataVM?.AddDataDocType(docType);
            }

            def.Complete();
        }

        private void XDataGrid_OnDragItemsCompleted(ListViewBase sender, DragItemsCompletedEventArgs args)
        {
            if (_selfDragOnDataList)
            {
                _selfDragOnDataList = false;
                return;
            }

            var dataVM = sender.DataContext as IDataDocTypeViewModel;
            foreach (var item in args.Items)
            {
                dataVM.RemoveDataDocType(item as DocumentType);
            }
        }

        private void XDataGrid_OnDragOver(object sender, DragEventArgs e)
        {
            // if it was dragged from a data view
            if (e.DataView.Properties.ContainsKey(_dataDragKey) || e.DataView.Properties.ContainsKey(_dataListDragKey))
            {
                e.AcceptedOperation = DataPackageOperation.Copy;
                e.Handled = true;
                return;           
            }
            e.AcceptedOperation = DataPackageOperation.None;
        }

        #endregion

        #region AddDocType

        /// <summary>
        /// Called when a button is tapped
        /// </summary>
        private void XAddNewDocTypeButton_OnTapped(object sender, TappedRoutedEventArgs e)
        {
            ToggleAddDocTypeUI(true);
            xNewDocTypeTextBox.Focus(FocusState.Programmatic); // set focus on the textbox to enter the new document
        }


        /// <summary>
        /// Called when the accept new doc type button is pressed
        /// </summary>
        private void XYesNewDocTypeButton_OnTapped(object sender, TappedRoutedEventArgs e)
        {
            if (NewDocTypeIsValid())
            {
                AddNewDocType(xNewDocTypeTextBox.Text);
            }
        }

        /// <summary>
        /// Called when the cancel new doc type button is tapped
        /// </summary>
        private void XCancelNewDocTypeButton_OnTapped(object sender, TappedRoutedEventArgs e)
        {
            ToggleAddDocTypeUI(false);
        }

        /// <summary>
        /// Toggles add new doc type ui so that it either shows the textbox with buttons to accept
        /// or cancel the operation or shows the button to add a new doctype
        /// </summary>
        private void ToggleAddDocTypeUI(bool showEditView)
        {
            xAddNewDocTypeButton.Visibility = showEditView ? Visibility.Collapsed : Visibility.Visible;
            xNewDocTypeTextBox.Visibility = showEditView ? Visibility.Visible : Visibility.Collapsed;
            xYesNewDocTypeButton.Visibility = showEditView ? Visibility.Visible : Visibility.Collapsed;
            xCancelNewDocTypeButton.Visibility = showEditView ? Visibility.Visible : Visibility.Collapsed;
        }

        /// <summary>
        /// Called when a key is pressed in the add doc type textbox, detects
        /// enter key so that it can be used for data input
        /// </summary>
        private void XNewDocTypeTextBox_OnKeyDown(object sender, KeyRoutedEventArgs e)
        {
            if (e.Key == VirtualKey.Enter)
            {
                if (NewDocTypeIsValid())
                {
                    AddNewDocType(xNewDocTypeTextBox.Text);
                }
            }
        }

        /// <summary>
        /// Helper method to determine if the new doc type is a valid
        /// one
        /// </summary>
        private bool NewDocTypeIsValid()
        {
            return !string.IsNullOrWhiteSpace(xNewDocTypeTextBox.Text);
        }

        /// <summary>
        /// Adds a new doc type to the view model
        /// </summary>
        /// <param name="docType"></param>
        private void AddNewDocType(string docType)
        {

            var docTypes = docType.Split(new []{','}, StringSplitOptions.RemoveEmptyEntries);
            foreach (var type in docTypes)
            {
                _vm.DocToColumnMaps.Add(
                    new DocumentTypeToColumnMapViewModel(
                        new DocumentType(DashShared.UtilShared.GenerateNewId(), type.Trim())
                    )
                );
            }

            ToggleAddDocTypeUI(false);
            xNewDocTypeTextBox.Text = string.Empty;
        }

        #endregion

        private void xParseCSVButtonPressed(object sender, TappedRoutedEventArgs e)
        {
            // don't do anything if there is no data document
            if (_vm.DataDocTypes.Count == 0)
            {
                return;
            }
            if (_tcs != null)
            {
                _tcs.SetResult(_vm);
                _tcs = null;
            }

            CloseWindow();

        }

        private void OnOnWindowClosed()
        {
            _tcs?.SetResult(null);
            _tcs = null;
        }

        public async Task<CsvImportHelperViewModel> GetConfigFromUser()
        {
            return await _tcs.Task;
        }
    }
}