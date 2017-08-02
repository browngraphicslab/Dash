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
using Dash.Controllers.Operators;

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
            xTypeComboBox.SelectedItem = TypeInfo.None;
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

        /// <summary>
        /// Updates ListView 
        /// </summary>
        private void SetListItemSourceToCurrentDataContext()
        {
            ListItemSource.Clear();
            foreach (var keyFieldPair in _documentControllerDataContext.EnumFields())
                ListItemSource.Add(new KeyFieldContainer(keyFieldPair.Key, keyFieldPair.Value));
        }

        private void FocusOn(TextBox tb)
        {
            tb.Focus(FocusState.Programmatic);
            tb.SelectAll(); 
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
            if ((string)xAddButton.Content == "↵")
            {
                // only execute if all fields are specified 
                if (xNewKeyField.Text != "" && (TypeInfo)xTypeComboBox.SelectedItem != TypeInfo.None && xNewValueField.Text != "")
                {
                    AddKeyValuePair();
                }
            }
            ToggleAddKVPane();
        }
        /// <summary>
        /// Adds a new row to the KeyValuePane, using user inputed values 
        /// </summary>
        private void AddKeyValuePair()
        {
            var item = (TypeInfo)xTypeComboBox.SelectedItem;
            Key key = new Key((new Random()).Next(0, 100000000).ToString(), xNewKeyField.Text);                 // TODO change this create actual guids 
            FieldModelController fmController = new TextFieldModelController("something went wrong");
            if (item == TypeInfo.Number)
            {
                double number;
                // if specified type is number only add a new keyvalue pair if the value is a number 
                if (double.TryParse(xNewValueField.Text, out number))
                    fmController = new NumberFieldModelController(number);
                else
                    return; 
            }
            else if (item == TypeInfo.Image)
            {
                fmController = new ImageFieldModelController(new Uri(xNewValueField.Text));
            }
            else if (item == TypeInfo.Text)
            {
                fmController = new TextFieldModelController(xNewValueField.Text);
            }
            ListItemSource.Add(new KeyFieldContainer(key, fmController));
            _documentControllerDataContext.SetField(key, fmController, true);
        }

        /// <summary>
        /// Toggles the bottom pane UI for adding new key-value pairs 
        /// </summary>
        private void ToggleAddKVPane()
        {
            _addKVPaneOpen = !_addKVPaneOpen;
            if (_addKVPaneOpen)
            {
                xAddButton.Content = "↵";
                xNewKeyField.Visibility = Windows.UI.Xaml.Visibility.Visible;
                xTypeComboBox.Visibility = Windows.UI.Xaml.Visibility.Visible;
                xNewValueField.Visibility = Windows.UI.Xaml.Visibility.Visible;
                FocusOn(xNewKeyField);
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
                xImageGrid.BorderThickness = new Thickness(0);
            }
        }
        /// <summary>
        /// A container which represents a single row in the list created by the <see cref="KeyValuePane"/>
        /// </summary>
        public class KeyFieldContainer
        {
            public Key Key { get; }
            public FieldModelController Controller { get; }
            // Type of field, ex) Text, Image, Number  
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
                xImageGrid.BorderThickness = new Thickness(0);
                xTypeComboBox.IsEnabled = false;
            }
        }

        private void xTypeComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var item = (TypeInfo)xTypeComboBox.SelectedItem;

            if (item == TypeInfo.Image)
            {
                xNewValueField.IsEnabled = true;
                xDefaultImage.Visibility = Windows.UI.Xaml.Visibility.Visible;
                xImageGrid.BorderThickness = new Thickness(0, 3, 0, 0);
                xNewValueField.Text = "ms-appx://Dash/Assets/DefaultImage.png"; 
                FocusOn(xNewValueField); 
            }
            else if (item == TypeInfo.Text || item == TypeInfo.Number)
            {
                xNewValueField.IsEnabled = true;
                xDefaultImage.Visibility = Windows.UI.Xaml.Visibility.Collapsed;
                xImageGrid.BorderThickness = new Thickness(0);
                FocusOn(xNewValueField);
            }
            else
            {
                xNewValueField.IsEnabled = false;
                return;
            }

        }

        /// <summary>
        /// If a new Image field is being added, update the preview (xDefaultImage) with the inputed url  
        /// </summary>
        private void xNewValueField_TextChanged(object sender, TextChangedEventArgs e)
        {
            string value = xNewValueField.Text;

            if (value != "" && (TypeInfo)xTypeComboBox.SelectedItem == TypeInfo.Image)
            {
                Uri outUri; 
                if (Uri.TryCreate(value, UriKind.Absolute, out outUri))
                {
                    xDefaultImage.Source = new BitmapImage(outUri);
                } else
                {
                    xDefaultImage.Source = new BitmapImage(new Uri("ms-appx://Dash/Assets/DefaultImage.png")); 
                }
            }
        }
        
        /// <summary>
        /// when item in keyvaluepane is clicked, show a textbox used to edit keys / values at clicked position 
        /// </summary>
        private void Grid_Tapped(object sender, Windows.UI.Xaml.Input.TappedRoutedEventArgs e)
        {
            // if there's already an editing textbox, get rid of it
            if (_tb != null)
            {
                RemoveEditingTextBox();
                return;
            }

            FrameworkElement tappedSource = e.OriginalSource as FrameworkElement;
            var posInKVPane = e.GetPosition(xOuterGrid);

            // make sure you can only edit the key or values; don't edit the type 
            if (posInKVPane.X < xHeaderGrid.ColumnDefinitions[0].ActualWidth)
                _editKey = true;
            else if (posInKVPane.X > xHeaderGrid.ColumnDefinitions[0].ActualWidth + xHeaderGrid.ColumnDefinitions[1].ActualWidth)
                _editKey = false;
            else
                return;

            //get position of mouse in screenspace 
            var containerGrid = xOuterGrid.GetFirstAncestorOfType<Grid>();
            var p = Util.PointTransformFromVisual(posInKVPane, containerGrid); 

            _tb = new TextBox();

            //set the editing textbox's initial value appropriately 
            if (tappedSource is TextBlock)
                _tb.Text = (tappedSource as TextBlock).Text;
            else if (tappedSource is Image)
                _tb.Text = (tappedSource as Image).BaseUri.AbsoluteUri;
            else throw new NotImplementedException();

            //add textbox graphically and set up events 
            Canvas.SetLeft(_tb, p.X);
            Canvas.SetTop(_tb, p.Y);
            MainPage.Instance.xCanvas.Children.Add(_tb);
            SetTextBoxEvents();
            FocusOn(_tb); 
        }

        /// <summary>
        /// Textbox will update keyvaluepane on enter 
        /// </summary>
        private void SetTextBoxEvents()
        {
            _tb.LostFocus += (s, e) => RemoveEditingTextBox();

            // if key was pressed, just edit the key value (don't have to update the fieldmodelcontrollers) 
            if (_editKey)
            {
                _tb.KeyDown += (s, e) =>
                {
                    if (e.Key == Windows.System.VirtualKey.Enter)
                    {
                        _selectedKV.Key.Name = _tb.Text;
                        SetListItemSourceToCurrentDataContext();
                        RemoveEditingTextBox();
                    }
                };
                return;
            }

            TypeInfo type = _selectedKV.Controller.TypeInfo;
            if (type == TypeInfo.Text)
            {
                _tb.KeyDown += (s, e) =>
                {
                    if (e.Key == Windows.System.VirtualKey.Enter)
                    {
                        var textCont = _selectedKV.Controller as TextFieldModelController;
                        textCont.Data = _tb.Text;
                        SetListItemSourceToCurrentDataContext();
                        RemoveEditingTextBox();
                    }
                };
            }
            else if (type == TypeInfo.Number)
            {
                _tb.KeyDown += (s, e) =>
                {
                    if (e.Key == Windows.System.VirtualKey.Enter)
                    {
                        var textCont = _selectedKV.Controller as NumberFieldModelController;
                        double number;
                        if (double.TryParse(_tb.Text, out number))
                            textCont.Data = number;

                        SetListItemSourceToCurrentDataContext();
                        RemoveEditingTextBox();
                    }
                };
            }
            else if (type == TypeInfo.Image)
            {
                _tb.KeyDown += (s, e) =>
                {
                    if (e.Key == Windows.System.VirtualKey.Enter)
                    {
                        var textCont = _selectedKV.Controller as ImageFieldModelController;
                        (textCont.Data as BitmapImage).UriSource = new Uri(_tb.Text);

                        SetListItemSourceToCurrentDataContext();
                        RemoveEditingTextBox();
                    }
                };
            }
            else
                throw new NotImplementedException();
            
        }

        private void RemoveEditingTextBox()
        {
            MainPage.Instance.xCanvas.Children.Remove(_tb);
            _tb = null;
        }

        private KeyFieldContainer _selectedKV = null;
        private TextBox _tb = null;           
        private bool _editKey = false;

        /// <summary>
        /// Identifies the row that is being modified currently 
        /// </summary>
        private void xKeyValueListView_ItemClick(object sender, ItemClickEventArgs e)
        {
            _selectedKV = e.ClickedItem as KeyFieldContainer;
        }

        
    }
}