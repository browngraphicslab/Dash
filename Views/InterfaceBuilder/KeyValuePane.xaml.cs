using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using Windows.ApplicationModel.DataTransfer;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using DashShared;
using System;

// The User Control item template is documented at https://go.microsoft.com/fwlink/?LinkId=234236

namespace Dash
{
    public sealed partial class KeyValuePane : UserControl
    {
        public static readonly string DragPropertyKey = "key";

        private DocumentController _documentControllerDataContext;
        private ObservableCollection<KeyFieldContainer> ListItemSource { get; }

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

        private void ValueField_KeyDown(object sender, Windows.UI.Xaml.Input.KeyRoutedEventArgs e)
        {
            if (e.Key == Windows.System.VirtualKey.Enter)
            {
                if (!xNewKeyField.IsEnabled && _selectedKV != null)
                {
                    TextFieldModelController textCont = _selectedKV.Controller as TextFieldModelController;
                    (_selectedKV.Controller as TextFieldModelController).Data = xNewValueField.Text;

                    SetListItemSourceToCurrentDataContext();
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
            if (!xNewValueField.IsEnabled && (e.Key == Windows.System.VirtualKey.Enter || e.Key == Windows.System.VirtualKey.Tab))
            {
                if (_selectedKV != null) _selectedKV.Key.Name = xNewKeyField.Text;

                var textCont = _selectedKV.Controller as TextFieldModelController;
                if (textCont != null)
                {
                    xNewValueField.IsEnabled = true; 
                    xNewValueField.Text = textCont.Data;
                    xNewKeyField.IsEnabled = false;
                }
                else
                {
                    SetListItemSourceToCurrentDataContext();
                    ResetKeyValueModifier();
                }
            }
        }

        private KeyFieldContainer _selectedKV;

        private void xKeyValueListView_ItemClick(object sender, ItemClickEventArgs e)
        {
            _selectedKV = e.ClickedItem as KeyFieldContainer;

            string keyField = _selectedKV.Key.Name;

            xNewValueField.Text = "< Enter new key";
            xNewKeyField.Text = keyField;
            xNewValueField.IsEnabled = false;
        }

        private void ResetKeyValueModifier()
        {
            xNewKeyField.IsEnabled = true;
            xNewValueField.IsEnabled = true;
            xNewValueField.Text = "";
            xNewKeyField.Text = "";
            _selectedKV = null;
        }
    }

    /// <summary>
    /// A container which represents a single row in the list created by the <see cref="KeyValuePane"/>
    /// </summary>
    public class KeyFieldContainer
    {
        public Key Key { get; }
        public FieldModelController Controller { get; }

        public KeyFieldContainer(Key key, FieldModelController controller)
        {
            Key = key;
            Controller = controller;
        }
    }
}