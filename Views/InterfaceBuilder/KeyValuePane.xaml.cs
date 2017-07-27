using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using Windows.ApplicationModel.DataTransfer;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using DashShared;
using System;
using Windows.UI.Xaml.Media.Imaging;

// The User Control item template is documented at https://go.microsoft.com/fwlink/?LinkId=234236

namespace Dash
{
    public sealed partial class KeyValuePane : UserControl
    {
        public static readonly string DragPropertyKey = "key";

        private DocumentController _documentControllerDataContext;
        private ObservableCollection<KeyFieldContainer> ListItemSource { get; }

        private bool _addKVPaneOpen = true;

        public KeyValuePane()
        {
            InitializeComponent();
            ListItemSource = new ObservableCollection<KeyFieldContainer>();
            DataContextChanged += KeyValuePane_DataContextChanged;

            ToggleAddKVPane();
            xTypeComboBox.ItemsSource = Enum.GetValues(typeof(TypeInfo));
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

        private void AddButton_Tapped(object sender, Windows.UI.Xaml.Input.TappedRoutedEventArgs e)
        {
            ToggleAddKVPane();
        }

        private void ToggleAddKVPane()
        {
            _addKVPaneOpen = !_addKVPaneOpen;
            if (_addKVPaneOpen)
            {
                xAddButton.Content = "⏎";
                xNewKeyField.Visibility = Windows.UI.Xaml.Visibility.Visible;
                xTypeComboBox.Visibility = Windows.UI.Xaml.Visibility.Visible;
                xNewValueField.Visibility = Windows.UI.Xaml.Visibility.Visible;
            }
            else
            {
                xAddButton.Content = "+";
                xNewKeyField.Visibility = Windows.UI.Xaml.Visibility.Collapsed;
                xTypeComboBox.IsEnabled = false;
                xTypeComboBox.Visibility = Windows.UI.Xaml.Visibility.Collapsed;
                xNewValueField.IsEnabled = false;
                xNewValueField.Visibility = Windows.UI.Xaml.Visibility.Collapsed;
                xDefaultImage.Visibility = Windows.UI.Xaml.Visibility.Collapsed;
            }
        }
        /// <summary>
        /// A container which represents a single row in the list created by the <see cref="KeyValuePane"/>
        /// </summary>
        public class KeyFieldContainer
        {
            public Key Key { get; }
            public FieldModelController Controller { get; }
            public string Type { get; }

            public KeyFieldContainer(Key key, FieldModelController controller)
            {
                Key = key;
                Controller = controller;
                Type = (controller.TypeInfo).ToString();
            }
        }


        private void xNewKeyField_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (xNewKeyField.Text != "") xTypeComboBox.IsEnabled = true;
            else
            {
                xDefaultImage.Visibility = Windows.UI.Xaml.Visibility.Collapsed;
                xTypeComboBox.IsEnabled = false;
            }
        }

        private void xTypeComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var item = (TypeInfo)xTypeComboBox.SelectedItem;
            if (item == TypeInfo.None)
            {
                xNewValueField.IsEnabled = false;
                return;
            }

            xNewValueField.IsEnabled = true;
            if (item == TypeInfo.Image)
            {
                xDefaultImage.Visibility = Windows.UI.Xaml.Visibility.Visible;
                xNewValueField.Text = ImageBox.DefaultImageUri.AbsoluteUri; 
            }
            else if (item == TypeInfo.Text || item == TypeInfo.Number)
            {
                xDefaultImage.Visibility = Windows.UI.Xaml.Visibility.Collapsed;
            }
        }

        private void xNewValueField_TextChanged(object sender, TextChangedEventArgs e)
        {
            string value = xNewValueField.Text; 
            if (value == "") return;
            var item = (TypeInfo)xTypeComboBox.SelectedItem;
            if (item == TypeInfo.Image)
            {
                xDefaultImage.Source = new BitmapImage(new Uri(value)); 
            }
            else if (item == TypeInfo.Number)
            {
                double num;
                if (!double.TryParse(value, out num))                             // if it's not a number
                {
                    return; 
                }
            }
        }


        /* 
        private void xKeyValueListView_ItemClick(object sender, ItemClickEventArgs e)
        {

            var kv = e.ClickedItem as KeyFieldContainer;
            if (kv == _selectedKV)
            {
                ResetKeyValueModifier();
                return;
            }
            _selectedKV = kv;

            xNewValueField.Text = "< Enter new key";
            xNewKeyField.Text = _selectedKV.Key.Name;
            xNewValueField.IsEnabled = false;
        }
        private void ValueField_KeyDown(object sender, Windows.UI.Xaml.Input.KeyRoutedEventArgs e)
        {
            if (e.Key == Windows.System.VirtualKey.Enter)
            {
                if (!xNewKeyField.IsEnabled && _selectedKV != null)
                {
                    TextFieldModelController textCont = _selectedKV.Controller as TextFieldModelController;
                    if ((_selectedKV.Controller as TextFieldModelController).Data != xNewValueField.Text || _selectedKV.Key.Name != xNewKeyField.Text)
                    {
                        _selectedKV.Key.Name = xNewKeyField.Text;
                        (_selectedKV.Controller as TextFieldModelController).Data = xNewValueField.Text;
                        SetListItemSourceToCurrentDataContext();
                    }
                    ResetKeyValueModifier();

                    return;
                }
                if (xNewKeyField.Text == "" || xNewValueField.Text == "")
                    return;

                //var key = new Key(Guid.NewGuid().ToString(), (xNewKeyField as TextBox).Text); // TODO commented out cos i didn't want to waste guids on testing 
                var key = new Key((new Random()).Next(0, 100000000).ToString(), (xNewKeyField as TextBox).Text);
                var cont = new TextFieldModelController((sender as TextBox).Text);
                ListItemSource.Add(new KeyFieldContainer(key, cont));

                _documentControllerDataContext.SetField(key, cont, true);
                xNewKeyField.Text = "";
                xNewValueField.Text = "";
            }
            else if (e.Key == Windows.System.VirtualKey.Tab)
            {
                ResetKeyValueModifier();
            }
        }

        private void KeyField_KeyDown(object sender, Windows.UI.Xaml.Input.KeyRoutedEventArgs e)
        {
            if (_selectedKV == null) return;
            if (!xNewValueField.IsEnabled && (e.Key == Windows.System.VirtualKey.Enter || e.Key == Windows.System.VirtualKey.Tab))
            {
                var textCont = _selectedKV.Controller as TextFieldModelController;
                if (textCont != null)
                {
                    xNewValueField.IsEnabled = true;
                    xNewValueField.Text = textCont.Data;
                    xNewKeyField.IsEnabled = false;
                }
                else
                {
                    if (_selectedKV.Key.Name != xNewKeyField.Text)
                    {
                        _selectedKV.Key.Name = xNewKeyField.Text; 
                        SetListItemSourceToCurrentDataContext();
                    }
                    ResetKeyValueModifier();
                }
            }
        }

        private KeyFieldContainer _selectedKV;

        

        private void ResetKeyValueModifier()
        {
            xNewKeyField.IsEnabled = true;
            xNewValueField.IsEnabled = true;
            xNewValueField.Text = "";
            xNewKeyField.Text = "";
            _selectedKV = null;
        }
    }
    */ // the old editable keyvalue insert thingy 
    }
}