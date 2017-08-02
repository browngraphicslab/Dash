using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.ApplicationModel.DataTransfer;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Animation;
using Windows.UI.Xaml.Navigation;
using DashShared;
using Visibility = Windows.UI.Xaml.Visibility;

// The User Control item template is documented at http://go.microsoft.com/fwlink/?LinkId=234236

namespace Dash
{
    public sealed partial class KeyValuePane : UserControl
    {
        public static readonly string DragPropertyKey = "key";

        private DocumentController _documentControllerDataContext;

        private ObservableCollection<KeyFieldContainer> ListItemSource { get; }

        /// <summary>
        /// The KeyFieldContainer that is currently selected on the listview
        /// </summary>
        private KeyFieldContainer _selectedKV;

        /// <summary>
        /// Currently visible name editing TextBox
        /// </summary>
        private TextBox _visibleEditBox;

        /// <summary>
        /// Currently visible type ComboBox
        /// </summary>
        private ComboBox _visibleTypeComboBox;

        /// <summary>
        /// The most recently added field
        /// </summary>
        private KeyFieldContainer _newField;

        /// <summary>
        /// List of all Grids around items in the listview
        /// </summary>
        List<Grid> _gridContainers = new List<Grid>();

        /// <summary>
        /// List of all name editing TextBoxes in the listview
        /// </summary>
        private List<TextBox> _textBoxes = new List<TextBox>();

        /// <summary>
        /// List of all type selection ComboBoxes
        /// </summary>
        private List<ComboBox> _typeComboBoxes = new List<ComboBox>();

        public KeyValuePane()
        {
            InitializeComponent();
            ListItemSource = new ObservableCollection<KeyFieldContainer>();
            DataContextChanged += KeyValuePane_DataContextChanged;
        }

        public void SetDataContextToDocumentController(DocumentController documentToDisplay)
        {
            _documentControllerDataContext = documentToDisplay;
            DataContext = documentToDisplay; // this line fires data context changed
        }

        private void KeyValuePane_DataContextChanged(FrameworkElement sender, DataContextChangedEventArgs args)
        {
            if (DataContext == null) return;

            SetListItemSourceToCurrentDataContext();
        }

        private void SetListItemSourceToCurrentDataContext()
        {
            ListItemSource.Clear();
            foreach (var keyFieldPair in _documentControllerDataContext.EnumFields())
                ListItemSource.Add(new KeyFieldContainer(keyFieldPair.Key, keyFieldPair.Value));
            _visibleEditBox = null;
            _selectedKV = null;
            _newField = null;
        }


        private void XKeyValueListView_OnDragItemsStarting(object sender, DragItemsStartingEventArgs e)
        {
            var item = e.Items.FirstOrDefault();

            // item type has to be the same as ListItemSource item type
            if (item is KeyFieldContainer)
            {
                var container = item as KeyFieldContainer;
                e.Data.RequestedOperation = DataPackageOperation.Move;
                e.Data.Properties.Add(DragPropertyKey, new KeyValuePair<Key, DocumentController>(container.Key, _documentControllerDataContext));
            }
        }

        /// <summary>
        /// Shows delete buttons (which only remove items from the list view at the moment and the "deleted" items show up again whenever the datacontext updates)
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void XEditButton_OnTapped(object sender, TappedRoutedEventArgs e)
        {
            foreach (var grid in _gridContainers)
            {
                grid.ColumnDefinitions[0].Width = new GridLength(35);
            }
            xColumnBackground.ColumnDefinitions[0].Width = new GridLength(35);
            xConfirmButton.Visibility = Visibility.Visible;
            xEditButton.Visibility = Visibility.Collapsed;
        }

        /// <summary>
        /// Hide delete buttons
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void XConfirmButton_OnTapped(object sender, TappedRoutedEventArgs e)
        {
            foreach (var grid in _gridContainers)
            {
                grid.ColumnDefinitions[0].Width = new GridLength(0);
            }
            xColumnBackground.ColumnDefinitions[0].Width = new GridLength(0);
            xEditButton.Visibility = Visibility.Visible;
            xConfirmButton.Visibility = Visibility.Collapsed;
        }

        private void xKeyValueListView_ItemClick(object sender, ItemClickEventArgs e)
        {
            var kv = e.ClickedItem as KeyFieldContainer;

            // control visibility of name editing TextBox and save name editing changes accordingly
            if (_visibleEditBox != null)
            {
                // tapping on any item collapses the previously visible name edit text box
                _visibleEditBox.Visibility = Visibility.Collapsed;
                // tapping on any item updates the name of a field if it was in the process of being edited
                if (_selectedKV?.Key.Name != _visibleEditBox.Text)
                {
                    _selectedKV.Key.Name = _visibleEditBox.Text;
                    SetListItemSourceToCurrentDataContext();
                }
                _visibleEditBox = null;
            }
            _selectedKV = kv;

            // Make the type ComboBox of the selected item visible and collapse all other type ComboBoxes
            if (_selectedKV != null)
            foreach (var box in _typeComboBoxes)
            {
                if (box.Tag.Equals(_selectedKV.HashCode))
                {
                    _visibleTypeComboBox = box;
                    box.Visibility = Visibility.Visible;
                }
                else
                {
                    box.Visibility = Visibility.Collapsed;
                }
            }
        }

        /// <summary>
        /// Removes item from listview (does not remove field from document)
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void XDeleteButton_OnTapped(object sender, TappedRoutedEventArgs e)
        {
            var button = sender as Button;
            KeyFieldContainer targetKV = null;
            if (button != null)
            foreach (var item in ListItemSource)
            {
                if (button.Tag.Equals(item.HashCode))
                {
                    // the item whose hashcode matches the tag of the delete button is the target item to be removed from the list view
                    targetKV = item;
                }
            }
            ListItemSource.Remove(targetKV);
            // TODO: Actually remove field from doc?
        }

        /// <summary>
        /// Adds field to document
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void XAddButton_OnTapped(object sender, TappedRoutedEventArgs e)
        {
            //var key = new Key(Guid.NewGuid().ToString(), (xNewKeyField as TextBox).Text); // TODO commented out cos i didn't want to waste guids on testing 
            var key = new Key((new Random()).Next(0, 100000000).ToString(), "");
            key.Name = "";
            var cont = new TextFieldModelController(string.Empty);
            _newField = new KeyFieldContainer(key, cont);
            ListItemSource.Add(_newField);
            _documentControllerDataContext.SetField(key, cont, true);
        }

        /// <summary>
        /// Keeps track of all Grids in the ListView to control the visibility of the delete Buttons
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void XItemGridContainer_OnLoaded(object sender, RoutedEventArgs e)
        {
            _gridContainers.Add(sender as Grid);
        }

        /// <summary>
        /// Keeps track of all name editing TextBoxes in the ListView
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void XEditBox_OnLoaded(object sender, RoutedEventArgs e)
        {
            var textBox = sender as TextBox;
            _textBoxes.Add(textBox);

            // opens up name editing textbox and forces focus on it programmatically to prompt user to name the new field
            if (textBox.Tag.Equals(_newField?.HashCode))
            {
                textBox.Visibility = Visibility.Visible;
                textBox.Focus(FocusState.Programmatic);
                foreach (var item in ListItemSource)
                {
                    if (item.HashCode.Equals(textBox.Tag))
                    {
                        _selectedKV = item;
                    }
                }
                if (_visibleEditBox != null)
                {
                    _visibleEditBox.Visibility = Visibility.Collapsed;
                }
                if (_visibleTypeComboBox != null)
                {
                    _visibleTypeComboBox.Visibility = Visibility.Collapsed;
                }
                _visibleEditBox = textBox;
                _visibleTypeComboBox = null;
            }
        }

        /// <summary>
        /// Updates name of field on enter/tab
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void xNameEditBox_KeyDown(object sender, KeyRoutedEventArgs e)
        {
            if (e.Key == Windows.System.VirtualKey.Enter || e.Key == Windows.System.VirtualKey.Tab)
            {
                if (_selectedKV.Key.Name != (sender as TextBox).Text)
                {
                    _selectedKV.Key.Name = (sender as TextBox).Text;
                    _visibleEditBox.Visibility = Visibility.Collapsed;
                    _visibleEditBox = null;
                    SetListItemSourceToCurrentDataContext();
                }
            }
        }

        /// <summary>
        /// Make visible the name editing TextBox of the item whose name TextBlock is being tapped on and collapse 
        /// the previously visible TextBox (without updating the name even if changes were made)
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void xNameTextBlock_OnTapped(object sender, TappedRoutedEventArgs e)
        {
            foreach (var textBox in _textBoxes)
            {
                if (textBox.Tag.Equals(_selectedKV?.HashCode))
                {
                    // collapse the textbox that was previously visible
                    if (_visibleEditBox != null)
                    {
                        _visibleEditBox.Visibility = Visibility.Collapsed;
                    }
                    // keeps track of the textbox that is currently visible
                    _visibleEditBox = textBox;
                    textBox.Visibility = Visibility.Visible;
                }
            }
        }

        /// <summary>
        /// Keeps track of all type selection ComboBoxes to control their visibility (visible only when item is selected)
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void XTypeCombo_OnLoaded(object sender, RoutedEventArgs e)
        {
            _typeComboBoxes.Add(sender as ComboBox);
        }

        /// <summary>
        /// Changes the type of a field (not exactly working, throws errors when trying to change type of preexisting fields)
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Selector_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var selectedItem = (sender as ComboBox).SelectedValue;
            if (selectedItem != null)
            {
                if (selectedItem.Equals(TypeInfo.Image))
                {
                    _selectedKV.Controller = new ImageFieldModelController(null);
                }
                else if (selectedItem.Equals(TypeInfo.Text))
                {
                    _selectedKV.Controller = new TextFieldModelController(string.Empty);
                }
                else if (selectedItem.Equals(TypeInfo.Number))
                {
                    _selectedKV.Controller = new NumberFieldModelController(double.NaN);
                }
                _documentControllerDataContext.SetField(_selectedKV.Key, _selectedKV.Controller, true);
                SetListItemSourceToCurrentDataContext();
            }
        }
    }

    /// <summary>
    /// A container which represents a single row in the list created by the <see cref="KeyValuePane"/>
    /// </summary>
    public class KeyFieldContainer
    {
        public Key Key { get; }
        /// <summary>
        /// Something for controls in the list to bind to to make them identifiable from the datatemplate
        /// </summary>
        public int HashCode { get { return Key.GetHashCode(); } }
        public FieldModelController Controller { get; set; }
        public Array Types { get; }
        

        public KeyFieldContainer(Key key, FieldModelController controller)
        {
            Key = key;
            Controller = controller;
            Types = Enum.GetValues(typeof(TypeInfo));
        }
    }
}
