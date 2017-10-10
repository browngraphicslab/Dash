using System;
using System.Collections.Generic;
using System.Linq;
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

        /// <summary>
        /// Private drag key to determine if the drag operations came from this view
        /// </summary>
        private static string _csvImportDragKey = "fd3f2da7-1a44-4b3f-ad14-d90e3206ce32";

        private object _dragSender;
        private object _dragReceiver;
        private bool _succesfulDrop;

        public CSVImportHelper(CsvImportHelperViewModel viewModel)
        {
            InitializeComponent();
            DataContext = viewModel;
            _vm = viewModel;
        }

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
            e.Data.Properties[_csvImportDragKey] = true;
            _dragSender = sender;
        }

        private void XHeaderGrid_OnDragItemsCompleted(ListViewBase sender, DragItemsCompletedEventArgs args)
        {
            var headerVM = sender.DataContext as IHeaderViewModel;

            if (_succesfulDrop)
            {
                if (args.DropResult == DataPackageOperation.Move)
                {
                    foreach (var item in args.Items)
                    {
                        headerVM.RemoveHeader(item as string);
                    }
                }
            }

            _succesfulDrop = false;
            _dragSender = null;
            _dragReceiver = null;

        }

        /// <summary>
        /// Called when an item is dragged over a header grid, determines whether or not
        /// we will accept the drag operation
        /// </summary>
        private void XHeaderGrid_OnDragOver(object sender, DragEventArgs e)
        {
            if (e.DataView.Properties.ContainsKey(_csvImportDragKey))
            {
                _dragReceiver = sender;

                if (!ReferenceEquals(_dragReceiver, _dragSender))
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
            _succesfulDrop = true;

            def.Complete();
        }

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
            _vm.DocumentTypeMaps.Add(
                new DocumentTypeToColumnMapViewModel(
                    new DocumentType(DashShared.Util.GenerateNewId(), docType)
                )
            );

            ToggleAddDocTypeUI(false);
            xNewDocTypeTextBox.Text = string.Empty;
        }

        #endregion


    }
}