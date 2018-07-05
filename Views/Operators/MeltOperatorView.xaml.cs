using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using Windows.ApplicationModel.DataTransfer;
using Windows.System;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;
using DashShared;
using Newtonsoft.Json;

// The User Control item template is documented at https://go.microsoft.com/fwlink/?LinkId=234236

namespace Dash
{
    public sealed partial class MeltOperatorView : UserControl
    {
        private static readonly string _headerDragKey = "18B33733-8BAC-4D21-9153-A24BEDFB93D0";
        private object _dragSourceHeaderList;
        private object _dragTargetHeaderList;
        private bool _isDroppedOnOtherHeaderList;

        /// <summary>
        ///     The document controller which contains the <see cref="MeltOperatorController" /> that this
        ///     view is associated with
        /// </summary>
        private DocumentController _operatorDoc;

        /// <summary>
        ///     The input headers
        /// </summary>
        private readonly ObservableCollection<KeyController> InputHeaders = new ObservableCollection<KeyController>();

        /// <summary>
        ///     The output headers
        /// </summary>
        private readonly ObservableCollection<KeyController> OutputHeaders = new ObservableCollection<KeyController>();


        public MeltOperatorView()
        {
            InitializeComponent();
            DataContextChanged += OnDataContextChanged;
        }

        /// <summary>
        ///     Called whenever the datacontext is changed
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="args"></param>
        private void OnDataContextChanged(FrameworkElement sender, DataContextChangedEventArgs args)
        {
            // datacontext is a reference to the operator field
            var refToOp = DataContext as FieldReference;

            // get the document containing the operator
            _operatorDoc = refToOp?.GetDocumentController(null);

            // listen for when the input collection is changed
            _operatorDoc?.AddFieldUpdatedListener(MeltOperatorController.InputCollection,
                OnInputCollectionChanged);
            _operatorDoc?.AddFieldUpdatedListener(MeltOperatorController.VariableName,
                OnNewVariableNameChanged);
            _operatorDoc?.AddFieldUpdatedListener(MeltOperatorController.ValueName, OnNewValueNameChanged);

            // TODO - Luke, set initial values for the variable and value names if they exist already
        }

        /// <summary>
        ///     Called whenever the input collection to the melt operator is changed
        /// </summary>
        private void OnInputCollectionChanged(DocumentController sender,
            DocumentController.DocumentFieldUpdatedEventArgs args, Context context)
        {
            // get all the headers from a collection
            var collection = args.NewValue.DereferenceToRoot<ListController<DocumentController>>(null);
            var typedHeaders = Util.GetTypedHeaders(collection);

            // reset all the headers
            InputHeaders.Clear();
            OutputHeaders.Clear();
            foreach (var key in typedHeaders.Keys.OrderBy(k => k.Name))
                InputHeaders.Add(key);
        }

        /// <summary>
        /// When any textbox key down is pressed we capture the <see cref="VirtualKey.Enter"/> and <see cref="VirtualKey.Tab"/>
        /// to avoid any unwanted side effects (i.e. tab menu opening)
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void XAnyTextBoxOnKeyDown(object sender, KeyRoutedEventArgs e)
        {
            if (e.Key == VirtualKey.Enter || e.Key == VirtualKey.Tab)
            {
                e.Handled = true;
            }

        }

        /// <summary>
        /// When the enter key is released on a textbox we update the backend with the new value
        /// in the textbox
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void XAnyTextBoxOnKeyUp(object sender, KeyRoutedEventArgs e)
        {
            // if it wasn't the enter key don't update anything
            if (e.Key != VirtualKey.Enter)
            {
                return;
            }

            // otherwise update the correct backend based on the sender
            if (sender == xNewValueTextBox)
            {
                _operatorDoc.SetField<TextController>(MeltOperatorController.ValueName,xNewValueTextBox.Text, true);
            } else if (sender == xNewVariableTextBox)
            {
                _operatorDoc.SetField<TextController>(MeltOperatorController.VariableName, xNewVariableTextBox.Text, true);
            }
        }

        /// <summary>
        /// Fired when the value name is changed elsewhere, updates the text in the view
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="args"></param>
        private void OnNewValueNameChanged(DocumentController sender,
            DocumentController.DocumentFieldUpdatedEventArgs args, Context context)
        {
            var tfmc = args.NewValue as TextController;
            Debug.Assert(tfmc != null);
            if (tfmc.Data.Equals(xNewValueTextBox.Text))
                return;

            xNewValueTextBox.Text = tfmc.Data;
        }

        /// <summary>
        /// Fired when the variable name is changed elsewhere, updates the text in the view
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="args"></param>
        private void OnNewVariableNameChanged(DocumentController sender,
            DocumentController.DocumentFieldUpdatedEventArgs args, Context context)
        {
            var tfmc = args.NewValue as TextController;
            Debug.Assert(tfmc != null);
            if (tfmc.Data.Equals(xNewVariableTextBox.Text))
                return;

            xNewVariableTextBox.Text = tfmc.Data;
        }

        #region HeaderlistDragandDrop

        private void xHeaderListOnDragItemsStarting(object sender, DragItemsStartingEventArgs e)
        {
            // Set the content of the DataPackage to the models of the key controllers
            var items = e.Items.Select(item => (item as KeyController)?.Model).ToList();
            var serializedItems = JsonConvert.SerializeObject(items);
            e.Data.SetText(serializedItems);
            // we want to move items between lists
            e.Data.RequestedOperation = DataPackageOperation.Move;
            e.Data.Properties[_headerDragKey] = true;
            _dragSourceHeaderList = sender;
        }

        private void xHeaderListOnDragItemsCompleted(ListViewBase sender, DragItemsCompletedEventArgs args)
        {
            if (_isDroppedOnOtherHeaderList && args.DropResult == DataPackageOperation.Move)
            {
                var listToRemoveHeadersFrom = ReferenceEquals(sender, xInputHeaderList) ? InputHeaders : OutputHeaders;
                foreach (var item in args.Items.Select(item => item as KeyController))
                    listToRemoveHeadersFrom.Remove(item);

                _operatorDoc.SetField(MeltOperatorController.ColumnVariables,
                    new ListController<TextController>(
                        OutputHeaders.Select(h => new TextController(h.Id))), true);
            }
            _isDroppedOnOtherHeaderList = false;
            _dragTargetHeaderList = null;
            _dragSourceHeaderList = null;
        }

        private async void xHeaderListOnDrop(object sender, DragEventArgs e)
        {
            var def = e.GetDeferral();
            var serializedItems = await e.DataView.GetTextAsync();
            var deserializedItems = JsonConvert.DeserializeObject<List<KeyModel>>(serializedItems);

            var listToAddHeadersTo = ReferenceEquals(sender, xInputHeaderList) ? InputHeaders : OutputHeaders;

            foreach (var header in deserializedItems.Select(km => new KeyController(km)))
                listToAddHeadersTo.Add(header);
            _isDroppedOnOtherHeaderList = true;

            def.Complete();
        }

        private void xHeaderListOnDragOver(object sender, DragEventArgs e)
        {
            // if it was dragged from another list view in this importer
            if (e.DataView.Properties.ContainsKey(_headerDragKey))
            {
                _dragTargetHeaderList = sender;
                if (!ReferenceEquals(_dragTargetHeaderList, _dragSourceHeaderList))
                {
                    e.AcceptedOperation = DataPackageOperation.Move;
                    return;
                }
            }
            e.AcceptedOperation = DataPackageOperation.None;
        }

        private void xHeaderListManipulationStarting(object sender, ManipulationStartingRoutedEventArgs e)
        {
            e.Handled = true;
        }


        private void xHeaderListOnManipulationDelta(object sender, ManipulationDeltaRoutedEventArgs e)
        {
            e.Handled = true;
        }

        #endregion


    }
}