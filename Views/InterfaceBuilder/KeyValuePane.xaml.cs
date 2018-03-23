using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using Windows.ApplicationModel.DataTransfer;
using Windows.System;
using Windows.UI.Core;
using Windows.UI.Input;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;
using DashShared;
using static Windows.ApplicationModel.Core.CoreApplication;
using Visibility = DashShared.Visibility;
using Dash.Models.DragModels;

// The User Control item template is documented at https://go.microsoft.com/fwlink/?LinkId=234236

namespace Dash
{
    public sealed partial class KeyValuePane : UserControl
    {
        /// <summary>
        /// True if we are editing the key of the selected key value
        /// </summary>
        private bool _editKey;

        /// <summary>
        /// The key value which we are currently editing
        /// </summary>
        private KeyFieldContainer _selectedKV;

        private TextBox _tb;

        /// <summary>
        /// This is a local reference to the DataContext and the Document we render fields for
        /// </summary>
        private DocumentController _dataContextDocument;

        /// <summary>
        ///     The list of fields displayed on the key value pane
        /// </summary>
        private ObservableCollection<KeyFieldContainer> ListItemSource { get; }

        public GridLength TypeColumnWidth { get; set; } = GridLength.Auto;

        public KeyValuePane()
        {
            InitializeComponent();

            ListItemSource = new ObservableCollection<KeyFieldContainer>();
            DataContextChanged += KeyValuePane_DataContextChanged;
            PointerPressed += (sender, e) =>
                this.GetFirstAncestorOfType<DocumentView>().ManipulationMode = e.GetCurrentPoint(this).Properties.IsRightButtonPressed ? ManipulationModes.All : ManipulationModes.None;

            xTypeComboBox.ItemsSource = Enum.GetValues(typeof(TypeInfo));
            Loaded += KeyValuePane_Loaded;
            Unloaded += KeyValuePane_Unloaded;
        }

        void FontIcon_PointerPressed(object sender, PointerRoutedEventArgs e)
        {
            this.GetFirstAncestorOfType<DocumentView>().ManipulationMode = e.GetCurrentPoint(this).Properties.IsRightButtonPressed ? ManipulationModes.All : ManipulationModes.None;
        }

        private void KeyValuePane_Unloaded(object sender, RoutedEventArgs e)
        {
            Loaded -= KeyValuePane_Loaded;
            DataContextChanged -= KeyValuePane_DataContextChanged;

        }

        private void KeyValuePane_Loaded(object sender, RoutedEventArgs e)
        {
            var docView = this.GetFirstAncestorOfType<DocumentView>();
            //docView.DraggerButton.Visibility = Windows.UI.Xaml.Visibility.Collapsed;
            docView?.StyleKeyValuePane();
        }

        /// <summary>
        /// Called whenever the datacontext changes
        /// </summary>
        private void KeyValuePane_DataContextChanged(FrameworkElement sender, DataContextChangedEventArgs args)
        {
            // if the datacontext is a document controller
            if (DataContext is DocumentController dc)
            {
                // remove old events from the previous datacontext
                if (_dataContextDocument != null)
                {
                    _dataContextDocument.FieldModelUpdated -= ViewDocumentFieldUpdated;
                }

                // assign the new datacontext to a variable, and add events
                _dataContextDocument = dc;
                _dataContextDocument.FieldModelUpdated -= ViewDocumentFieldUpdated;
                _dataContextDocument.FieldModelUpdated += ViewDocumentFieldUpdated;

                // set the field list item source to the new datacontext
                SetListItemSourceToCurrentDataContext();
            }
        }

        /// <summary>
        ///     Resets the ListItemSource to fields in the current datacontext (this can be thought of as rebuilding the entire
        ///     list)
        /// </summary>
        private void SetListItemSourceToCurrentDataContext()
        {
            ListItemSource.Clear();
            if (_dataContextDocument != null)
            {
                foreach (var keyFieldPair in _dataContextDocument.EnumFields())
                    if (!keyFieldPair.Key.Name.StartsWith("_"))
                        ListItemSource.Add(new KeyFieldContainer(keyFieldPair.Key,
                            new BoundController(keyFieldPair.Value, _dataContextDocument), TypeColumnWidth));
            }
        }

        /// <summary>
        /// Called whenever the list of fields attached to the document changes
        /// </summary>
        private void ViewDocumentFieldUpdated(FieldControllerBase sender, FieldUpdatedEventArgs args, Context context)
        {
            // if a field has been replaced or updated then set it's source to be the new element
            // otherwise replcae the entire data source to reflect the new set of fields (due to add or remove)
            var dargs = (DocumentController.DocumentFieldUpdatedEventArgs) args;
            if (args.Action == DocumentController.FieldUpdatedAction.Replace ||
                args.Action == DocumentController.FieldUpdatedAction.Update)
                UpdateListItemSourceElement(dargs.Reference.FieldKey, dargs.NewValue);
            else SetListItemSourceToCurrentDataContext();
        }

        private void UpdateListItemSourceElement(KeyController fieldKey, FieldControllerBase fieldValue)
        {
            for (var i = 0; i < ListItemSource.Count; i++)
                if (ListItemSource[i].Key.Equals(fieldKey))
                    ListItemSource[i] = new KeyFieldContainer(fieldKey,
                        new BoundController(fieldValue, _dataContextDocument), TypeColumnWidth);
        }

        /// <summary>
        /// Button tapped to add a new key value pair to the document and the list
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void AddField_Tapped(object sender, TappedRoutedEventArgs e)
        {
            AddKeyValuePair();
        }


        /// <summary>
        /// Returns true if the user input for the new key value pair is valid. 
        /// Should be called before any yser input is processed.
        /// </summary>
        /// <returns></returns>
        private bool UserInputIsValid()
        {
            var type = (TypeInfo)xTypeComboBox.SelectedItem;

            return xNewKeyField.Text != "" && type != TypeInfo.None &&
                   (xNewValueField.Text != "" || type == TypeInfo.List || type == TypeInfo.Document);
        }

        /// <summary>
        ///     Adds a new row to the KeyValuePane, using user inputed values, returning a boolean depending on whether it is
        ///     successful in adding the pair.
        /// </summary>
        private void AddKeyValuePair()
        {

            if (!UserInputIsValid()) return;

            var item = (TypeInfo) xTypeComboBox.SelectedItem;
            var key = KeyController.LookupKeyByName(xNewKeyField.Text) ?? new KeyController(Guid.NewGuid().ToString(), xNewKeyField.Text);
            FieldControllerBase fmController = new TextController("something went wrong");
            var stringValue = xNewValueField.Text;

            _dataContextDocument.ParseDocField(key, xNewValueField.Text);
            fmController = _dataContextDocument.GetField(key);

            if (fmController == null)
            {
                switch (item)
                {
                    case TypeInfo.Number:
                        fmController = new NumberController(new DoubleToStringConverter().ConvertXamlToData(stringValue));
                        break;
                    case TypeInfo.Image:
                        // TODO check to see if the uri is valid
                        fmController = new ImageController(new UriToStringConverter().ConvertXamlToData(stringValue));
                        break;
                    case TypeInfo.Text:
                        fmController = new TextController(xNewValueField.Text);
                        break;
                    case TypeInfo.List:
                        //TODO tfs: this can only create lists of docs(collections), not lists of other things
                        fmController = new ListController<DocumentController>();
                        break;
                    case TypeInfo.Point:
                        fmController = new PointController(new PointToStringConverter().ConvertXamlToData(stringValue));
                        break;
                    case TypeInfo.Document:
                        fmController = new Converters.DocumentControllerToStringConverter(null).ConvertXamlToData(stringValue);
                        break;
                    case TypeInfo.None:
                    case TypeInfo.PointerReference:
                    case TypeInfo.DocumentReference:
                    case TypeInfo.Operator:
                    case TypeInfo.Ink:
                    case TypeInfo.RichText:
                    case TypeInfo.Rectangle:
                    case TypeInfo.Key:
                    case TypeInfo.Reference:
                    case TypeInfo.Any:
                    default:
                        throw new ArgumentOutOfRangeException();
                }
                _dataContextDocument.SetField(key, fmController, true);
            }
            
            // TODO check if adding was succesful
            // reset the fields to the empty values
            xNewKeyField.Text = "";
            xNewValueField.Text = "";
            xTypeComboBox.SelectedIndex = 0;
            ToggleAddKVPane(false);
            xFieldsScroller.ChangeView(null, xFieldsScroller.MaxHeight, null);


            return;
        }

        /// <summary>
        ///     Toggles the bottom pane UI for adding new key-value pairs
        /// </summary>
        private void ToggleAddKVPane(bool showAddMenu)
        {
            if (!showAddMenu)
            {
                xNewFieldPanel.Visibility = Windows.UI.Xaml.Visibility.Collapsed;
                xCreateFieldButton.Visibility = Windows.UI.Xaml.Visibility.Visible;
            }
            else
            {
                xNewFieldPanel.Visibility = Windows.UI.Xaml.Visibility.Visible;
                xCreateFieldButton.Visibility = Windows.UI.Xaml.Visibility.Collapsed;
                // set type selection box to text by default
                xTypeComboBox.SelectedIndex = 2;
            }
        }
        

        /// <summary>
        ///     when item in keyvaluepane is clicked, show a textbox used to edit keys / values at clicked position
        /// </summary>
        private void Grid_Tapped(object sender, TappedRoutedEventArgs e)
        {
            e.Handled = true;

            // if there's already an editing textbox, get rid of it
            if (_tb != null)
            {
                RemoveEditingTextBox();
                return;
            }

            // check to see if we're editing a key or a value and set _editKey to true if we're editing a key
            var posInKvPane = e.GetPosition(xOuterGrid);
            var columnDefinitions = ((xKeyValueListView.ContainerFromIndex(0) as ListViewItem)?.ContentTemplateRoot as Grid)?.ColumnDefinitions;
            if (columnDefinitions == null)
            {
                return;
            }
            var keyColumnWidth = columnDefinitions[0].ActualWidth;
            if (posInKvPane.X > 0 && posInKvPane.X < keyColumnWidth)
                _editKey = true;
            else
                _editKey = false;


            // set the selectedKV pair, a bunch of textbox methods rely on this field being set
            _selectedKV = (sender as FrameworkElement)?.DataContext as KeyFieldContainer;
            if (_selectedKV == null) return;

            // set the text for the textbox to the key name, or the field converted to a string
            var srcText = _editKey ? _selectedKV.Key.Name : FieldConversion.ConvertFieldToString(_selectedKV.Controller.FieldModelController);
            _tb = new TextBox
            {
                MaxHeight = 500,
                MaxWidth = 500,
                TextWrapping=Windows.UI.Xaml.TextWrapping.Wrap,
                Text = srcText,
                AcceptsReturn = true //!srcText.Contains("\r") // TODO make this a better heuristic
            };
            _tb.BeforeTextChanging += _tb_BeforeTextChanging;
            _tb.LostFocus += _tb_LostFocus;

            //add textbox graphically
            var p = Util.PointTransformFromVisual(posInKvPane, xOuterGrid.GetFirstAncestorOfType<Grid>());
            Canvas.SetLeft(_tb, p.X);
            Canvas.SetTop(_tb, p.Y);
            MainPage.Instance.xCanvas.Children.Add(_tb);

            
            _tb.Focus(FocusState.Programmatic);

        }

        private void _tb_BeforeTextChanging(TextBox sender, TextBoxBeforeTextChangingEventArgs args)
        {
            var shiftState = Window.Current.CoreWindow.GetKeyState(VirtualKey.Shift).HasFlag(CoreVirtualKeyStates.Down);
            var enterState = Window.Current.CoreWindow.GetKeyState(VirtualKey.Enter).HasFlag(CoreVirtualKeyStates.Down);

            if (shiftState && enterState)
            {
                // bcz: make sure we are writing the field to this instance, and not a prototype.
                //     so we are copying the field in case it came from the prototype.
                var field = _dataContextDocument.GetDereferencedField<FieldControllerBase>(
                    _selectedKV.Key, new Context(_dataContextDocument)).GetCopy();
                FieldConversion.SetFieldtoString(field, _tb.Text, new Context(_dataContextDocument));
                _dataContextDocument.SetField(_selectedKV.Key, field, true);
                //_dataContextDocument.ParseDocField(_selectedKV.Key, _tb.Text, field);
                RemoveEditingTextBox();
                args.Cancel = true;
            }
        }

        private void _tb_LostFocus(object sender, RoutedEventArgs e)
        {
            RemoveEditingTextBox();
        }

        private void RemoveEditingTextBox()
        {
            _tb.LostFocus -= _tb_LostFocus;
            _tb.BeforeTextChanging -= _tb_BeforeTextChanging;
            _selectedKV = null;
            _editKey = false;
            MainPage.Instance.xCanvas.Children.Remove(_tb);
            _tb = null;
        }

        private void ShowCreateFieldOptions(object sender, RoutedEventArgs e)
        {
            ToggleAddKVPane(true);
            // focus on the combo box by defalt
            xTypeComboBox.Focus(FocusState.Programmatic);
        }

        private void CancelField_Tapped(object sender, TappedRoutedEventArgs e)
        {
            ToggleAddKVPane(false);
        }

        private void CloseButton_Tapped(object sender, TappedRoutedEventArgs e)
        {
            var docView = this.GetFirstAncestorOfType<DocumentView>();
            docView.DeleteDocument();
        }

        private void Icon_OnDragStarting(UIElement sender, DragStartingEventArgs args)
        {
            var container = (KeyFieldContainer) ((FrameworkElement) sender).DataContext;
            args.AllowedOperations = DataPackageOperation.Link | DataPackageOperation.Move;
            args.Data.RequestedOperation = DataPackageOperation.Link;
            args.Data.Properties[nameof(DragDocumentModel)] = new DragDocumentModel(_dataContextDocument, container.Key);
        }

        private void XNewKeyField_OnKeyUp(object sender, KeyRoutedEventArgs e)
        {
            // focus on the value field if the user hits the tab key
            if (e.Key == VirtualKey.Tab)
            {
                e.Handled = true; // stop the operator menu frum shoing up
            }
        }

        private void XNewValueField_OnKeyUp(object sender, KeyRoutedEventArgs e)
        {
            // focus on the button if the user hits the tab key
            if (e.Key == VirtualKey.Tab)
            {
                e.Handled = true;
            }

            // add the field if the user hits enter
            if (e.Key == VirtualKey.Enter)
            {
                AddKeyValuePair();
            }
        }

        private void XTypeComboBox_OnKeyUp(object sender, KeyRoutedEventArgs e)
        {
            // focus on the key field if the user hits enter
            if (e.Key == VirtualKey.Enter)
            {
                xNewKeyField.Focus(FocusState.Programmatic);
                e.Handled = true;
            }
        }

        private void XAddButton_OnKeyUp(object sender, KeyRoutedEventArgs e)
        {
            if (e.Key == VirtualKey.Tab)
            {
                e.Handled = true;
            }

            // add the field if the user hits enter
            if (e.Key == VirtualKey.Enter)
            {
                AddKeyValuePair();
            }
        }
    }
}