﻿using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Windows.ApplicationModel.DataTransfer;
using Windows.System;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media.Imaging;
using DashShared;
using Visibility = DashShared.Visibility;

// The User Control item template is documented at https://go.microsoft.com/fwlink/?LinkId=234236

namespace Dash
{
    public sealed partial class KeyValuePane : UserControl
    {
        public static readonly string DragPropertyKey = "key_value_pane_drag_key 1893741";

        private bool _addKVPaneOpen = true;

        private DocumentController _documentControllerDataContext;
        private bool _editKey;

        private KeyFieldContainer _selectedKV;
        private TextBox _tb;

        /// <summary>
        /// The backing collection for items in the list of keys and values
        /// </summary>
        private ObservableCollection<KeyFieldContainer> ListItemSource { get; }

        public GridLength TypeColumnWidth { get; set; } = GridLength.Auto;

        public DocumentController RealDataContext =>
            _documentControllerDataContext.GetField(KeyStore.DocumentContextKey) != null
                ? _documentControllerDataContext
                    .GetDereferencedField<DocumentFieldModelController>(KeyStore.DocumentContextKey, null).Data
                : _documentControllerDataContext;
        
        public KeyValuePane()
        {
            InitializeComponent();

            ListItemSource = new ObservableCollection<KeyFieldContainer>();
            DataContextChanged += KeyValuePane_DataContextChanged;

            //ToggleAddKVPane();
            xTypeComboBox.ItemsSource = Enum.GetValues(typeof(TypeInfo));
        }

        public void DisableInteraction()
        {
            xKeyValueListView.CanDragItems = false;
            xKeyValueListView.SelectionMode = ListViewSelectionMode.None;
            SetHeaderVisibility(DashShared.Visibility.Collapsed); 
        }

        private void SetHeaderVisibility(Visibility vis)
        {
            xHeaderGrid.Visibility = vis == DashShared.Visibility.Visible
                ? Windows.UI.Xaml.Visibility.Visible
                : Windows.UI.Xaml.Visibility.Collapsed;
        }

        public void SetDataContextToDocumentController(DocumentController documentToDisplay)
        {
            var dataContext =
                documentToDisplay.GetDereferencedField<DocumentFieldModelController>(KeyStore.DocumentContextKey, null);
            documentToDisplay = dataContext?.Data ?? documentToDisplay;
            if (_documentControllerDataContext != null)
                _documentControllerDataContext.DocumentFieldUpdated -=
                    _documentControllerDataContext_DocumentFieldUpdated;
            _documentControllerDataContext = documentToDisplay;
            _documentControllerDataContext.DocumentFieldUpdated -= _documentControllerDataContext_DocumentFieldUpdated;
            _documentControllerDataContext.DocumentFieldUpdated += _documentControllerDataContext_DocumentFieldUpdated;
            DataContext = documentToDisplay; // this line fires data context changed
        }

        private void KeyValuePane_DataContextChanged(FrameworkElement sender, DataContextChangedEventArgs args)
        {
            if (DataContext != null)
                SetListItemSourceToCurrentDataContext();
        }

        /// <summary>
        ///     Resets the ListItemSource to fields in the current datacontext (this can be thought of as rebuilding the entire
        ///     list)
        /// </summary>
        private void SetListItemSourceToCurrentDataContext()
        {
            ListItemSource.Clear();
            var keys = _documentControllerDataContext
                           .GetDereferencedField<ListFieldModelController<TextFieldModelController>
                           >(KeyStore.PrimaryKeyKey, null)?.Data?.Select(t => (t as TextFieldModelController).Data)
                           ?.ToList() ?? new List<string>();
            foreach (var keyFieldPair in _documentControllerDataContext.EnumFields())
                if (!keyFieldPair.Key.Name.StartsWith("_"))
                    ListItemSource.Add(new KeyFieldContainer(keyFieldPair.Key,
                        new BoundFieldModelController(keyFieldPair.Value, _documentControllerDataContext),
                        keys.Contains(keyFieldPair.Key.Id), TypeColumnWidth));
        }

        private void _documentControllerDataContext_DocumentFieldUpdated(DocumentController sender,
            DocumentController.DocumentFieldUpdatedEventArgs args)
        {
            // if a field has been replaced or updated then set it's source to be the new element
            // otherwise replcae the entire data source to reflect the new set of fields (due to add or remove)
            if (args.Action == DocumentController.FieldUpdatedAction.Replace ||
                args.Action == DocumentController.FieldUpdatedAction.Update)
                UpdateListItemSourceElement(args.Reference.FieldKey, args.NewValue);
            else SetListItemSourceToCurrentDataContext();
        }

        private void UpdateListItemSourceElement(KeyController fieldKey, FieldControllerBase fieldValue)
        {
            var keys = _documentControllerDataContext
                           .GetDereferencedField<ListFieldModelController<TextFieldModelController>
                           >(KeyStore.PrimaryKeyKey, null)?.Data?.Select(t => (t as TextFieldModelController).Data)
                           ?.ToList() ?? new List<string>();

            for (var i = 0; i < ListItemSource.Count; i++)
                if (ListItemSource[i].Key.Equals(fieldKey))
                    ListItemSource[i] = new KeyFieldContainer(fieldKey,
                        new BoundFieldModelController(fieldValue, RealDataContext), keys.Contains(fieldKey.Id),
                        TypeColumnWidth);
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
                e.Data.Properties.Add(DragPropertyKey,
                    new KeyValuePair<KeyController, DocumentController>(container.Key, _documentControllerDataContext));
            }
        }

        private void AddButton_Tapped(object sender, TappedRoutedEventArgs e)
        {
            //Debug.Assert(xAddButton.Content != null, "xAddButton.Content != null");
            //var view = (Viewbox)xAddButton.Content;
            //var icon = ((SymbolIcon)view.Child).Symbol;
            //if (icon == Symbol.Accept)
            //{
            // only execute if all fields are specified and reset  
            var type = (TypeInfo) xTypeComboBox.SelectedItem;
            if (xNewKeyField.Text != "" && type != TypeInfo.None &&
                (xNewValueField.Text != "" || type == TypeInfo.Collection || type == TypeInfo.Document))
                if (AddKeyValuePair())
                {
                    xNewKeyField.Text = "";
                    xNewValueField.Text = "";
                    xTypeComboBox.SelectedIndex = 0;
                    ToggleAddKVPane();
                }
            //}
        }

        /// <summary>
        ///     Adds a new row to the KeyValuePane, using user inputed values, returning a boolean depending on whether it is
        ///     successful in adding the pair.
        /// </summary>
        private bool AddKeyValuePair()
        {
            var item = (TypeInfo) xTypeComboBox.SelectedItem;
            var key = new KeyController(Guid.NewGuid().ToString(),
                xNewKeyField.Text); // TODO change this create actual guids 
            FieldControllerBase fmController = new TextFieldModelController("something went wrong");

            //_documentControllerDataContext.ParseDocField(key, xNewValueField.Text);
            //fmController = _documentControllerDataContext.GetField(key);

            // /*                                         // TODO the above doesn't take into account the type users selected, ex) choosing "Text" and inputing 5 will return a Number type field 
            ///                                         // and can't create image fields ? 
            if (item == TypeInfo.Number)
            {
                double number;
                // if specified type is number only add a new keyvalue pair if the value is a number 
                if (double.TryParse(xNewValueField.Text, out number))
                    fmController = new NumberFieldModelController(number);
                else
                    return false;
            }
            else if (item == TypeInfo.Image)
            {
                fmController = new ImageFieldModelController(new Uri(xNewValueField.Text));
            }
            else if (item == TypeInfo.Text)
            {
                fmController = new TextFieldModelController(xNewValueField.Text);
            }
            else if (item == TypeInfo.Collection)
            {
                fmController = new DocumentCollectionFieldModelController();
            }
            else if (item == TypeInfo.Document)
            {
                var fields = new Dictionary<KeyController, FieldControllerBase>
                {
                    [KeyStore.ActiveLayoutKey] =
                    new DocumentFieldModelController(new FreeFormDocument(new List<DocumentController>()).Document)
                };

                fmController =
                    new DocumentFieldModelController(new DocumentController(fields, DocumentType.DefaultType));
            }
            var keys = _documentControllerDataContext
                           .GetDereferencedField<ListFieldModelController<TextFieldModelController>
                           >(KeyStore.PrimaryKeyKey, null)?.Data?.Select(t => (t as TextFieldModelController).Data)
                           ?.ToList() ?? new List<string>();

            ListItemSource.Add(new KeyFieldContainer(key, new BoundFieldModelController(fmController, RealDataContext),
                keys.Contains(key.Id), TypeColumnWidth));
            RealDataContext.SetField(key, fmController, true);
            //*/ 
            return true;
        }

        /// <summary>
        ///     Toggles the bottom pane UI for adding new key-value pairs
        /// </summary>
        private void ToggleAddKVPane()
        {
            _addKVPaneOpen = !_addKVPaneOpen;
            if (_addKVPaneOpen)
            {
                xNewFieldPanel.Visibility = Windows.UI.Xaml.Visibility.Collapsed;
                xCreateFieldButton.Visibility = Windows.UI.Xaml.Visibility.Visible;
            }
            else
            {
                xNewFieldPanel.Visibility = Windows.UI.Xaml.Visibility.Visible;
                xCreateFieldButton.Visibility = Windows.UI.Xaml.Visibility.Collapsed;
            }
            //if (_addKVPaneOpen)
            //{
            //    xAddButton.Content = new Viewbox { Child = new SymbolIcon(Symbol.Accept) };
            //    xNewKeyField.Visibility = Windows.UI.Xaml.Visibility.Visible;
            //    xTypeComboBox.Visibility = Windows.UI.Xaml.Visibility.Visible;
            //    xNewValueField.Visibility = Windows.UI.Xaml.Visibility.Visible;
            //    FocusOn(xNewKeyField);
            //}
            //else
            //{
            //    xAddButton.Content = new Viewbox { Child = new SymbolIcon(Symbol.Add) };
            //    xNewKeyField.Visibility = Windows.UI.Xaml.Visibility.Collapsed;
            //    xTypeComboBox.IsEnabled = false;
            //    xTypeComboBox.Visibility = Windows.UI.Xaml.Visibility.Collapsed;
            //    xNewValueField.IsEnabled = false;
            //    xNewValueField.Visibility = Windows.UI.Xaml.Visibility.Collapsed;
            //    xDefaultImage.Visibility = Windows.UI.Xaml.Visibility.Collapsed;
            //    xImageGrid.BorderThickness = new Thickness(0);
            //}
        }

        private void CheckBox_Checked(object sender, RoutedEventArgs e)
        {
            var kf = (sender as CheckBox).Tag as KeyFieldContainer;
            var keys = _documentControllerDataContext
                           .GetDereferencedField<ListFieldModelController<TextFieldModelController>
                           >(KeyStore.PrimaryKeyKey, null)?.Data?.Select(t => t as TextFieldModelController)
                           ?.ToList() ?? new List<TextFieldModelController>();
            if (kf != null && keys.Where(k => k.Data == kf.Key.Id).Count() == 0)
            {
                keys.Add(new TextFieldModelController(kf.Key.Id));
                _documentControllerDataContext.SetField(KeyStore.PrimaryKeyKey,
                    new ListFieldModelController<TextFieldModelController>(keys), false);
            }
        }

        private void CheckBox_Unchecked(object sender, RoutedEventArgs e)
        {
            var kf = (sender as CheckBox).Tag as KeyFieldContainer;
            var keys = _documentControllerDataContext
                           .GetDereferencedField<ListFieldModelController<TextFieldModelController>
                           >(KeyStore.PrimaryKeyKey, null)?.Data?.Select(t => t as TextFieldModelController)
                           ?.ToList() ?? new List<TextFieldModelController>();
            if (kf != null && keys.Where(k => k.Data == kf.Key.Id).Count() != 0)
            {
                keys.RemoveAt(keys.FindIndex(k => k.Data == kf.Key.Id));
                _documentControllerDataContext.SetField(KeyStore.PrimaryKeyKey,
                    new ListFieldModelController<TextFieldModelController>(keys), false);
            }
        }

        private void xNewKeyField_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (xNewKeyField.Text != "")
            {
                xTypeComboBox.IsEnabled = true;
            }
            else
            {
                xDefaultImage.Visibility = Windows.UI.Xaml.Visibility.Collapsed;
                xImageGrid.BorderThickness = new Thickness(0);
                //xTypeComboBox.IsEnabled = false;
            }
        }

        private void xTypeComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var item = (TypeInfo) xTypeComboBox.SelectedItem;

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
            }
        }

        /// <summary>
        ///     If a new Image field is being added, update the preview (xDefaultImage) with the inputed url
        /// </summary>
        private void xNewValueField_TextChanged(object sender, TextChangedEventArgs e)
        {
            var value = xNewValueField.Text;

            if (value != "" && (TypeInfo) xTypeComboBox.SelectedItem == TypeInfo.Image)
            {
                Uri outUri;
                if (Uri.TryCreate(value, UriKind.Absolute, out outUri))
                    xDefaultImage.Source = new BitmapImage(outUri);
                else
                    xDefaultImage.Source = new BitmapImage(new Uri("ms-appx://Dash/Assets/DefaultImage.png"));
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

            var tappedSource = e.OriginalSource as FrameworkElement;
            var posInKVPane = e.GetPosition(xOuterGrid);
            var item = xKeyValueListView.ContainerFromIndex(0) as ListViewItem;
            if (item == null)
                return;
            var col0Width = (item.ContentTemplateRoot as Grid).ColumnDefinitions[0].ActualWidth;
            var col1Width = (item.ContentTemplateRoot as Grid).ColumnDefinitions[1].ActualWidth;
            var col2Width = (item.ContentTemplateRoot as Grid).ColumnDefinitions[2].ActualWidth;
            // make sure you can only edit the key or values; don't edit the type 
            if (posInKVPane.X > col0Width && posInKVPane.X < col1Width)
                _editKey = true;
            else if (posInKVPane.X > col1Width && posInKVPane.X < col1Width + col2Width)
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
            else if (tappedSource is Grid)
                return;
            else throw new NotImplementedException();

            //add textbox graphically and set up events 
            Canvas.SetLeft(_tb, p.X);
            Canvas.SetTop(_tb, p.Y);
            MainPage.Instance.xCanvas.Children.Add(_tb);
            SetTextBoxEvents();
            FocusOn(_tb);
        }

        /// <summary>
        ///     Textbox will update keyvaluepane on enter
        /// </summary>
        private void SetTextBoxEvents()
        {
            _tb.LostFocus += (s, e) => RemoveEditingTextBox();

            // if key was pressed, just edit the key value (don't have to update the fieldmodelcontrollers) 
            if (_editKey)
            {
                _tb.KeyDown += (s, e) =>
                {
                    if (e.Key == VirtualKey.Enter)
                    {
                        //DBTest.ResetCycleDetection();
                        _selectedKV.Key.Name = _tb.Text;
                        SetListItemSourceToCurrentDataContext();
                        RemoveEditingTextBox();
                    }
                };
                return;
            }

            _tb.KeyDown += (s, e) =>
            {
                if (e.Key == VirtualKey.Enter)
                {
                    //DBTest.ResetCycleDetection();
                    var field = _documentControllerDataContext.GetDereferencedField<FieldControllerBase>(
                        _selectedKV.Key, new Context(_documentControllerDataContext));
                    _documentControllerDataContext.ParseDocField(_selectedKV.Key, _tb.Text, field);
                    RemoveEditingTextBox();
                }
            };
        }

        private void RemoveEditingTextBox()
        {
            MainPage.Instance.xCanvas.Children.Remove(_tb);
            _tb = null;
        }

        /// <summary>
        ///     Identifies the row that is being modified currently
        /// </summary>
        private void xKeyValueListView_ItemClick(object sender, ItemClickEventArgs e)
        {
            _selectedKV = e.ClickedItem as KeyFieldContainer;
        }

        private void ShowCreateFieldOptions(object sender, RoutedEventArgs e)
        {
            ToggleAddKVPane();
        }

        private void CreateFieldPaneClose(object sender, TappedRoutedEventArgs e)
        {
            ToggleAddKVPane();
        }

        private void XNewValueField_OnKeyDown(object sender, KeyRoutedEventArgs e)
        {
            if (e.Key == VirtualKey.Enter)
            {
                var type = (TypeInfo) xTypeComboBox.SelectedItem;
                if (xNewKeyField.Text != "" && type != TypeInfo.None &&
                    (xNewValueField.Text != "" || type == TypeInfo.Collection || type == TypeInfo.Document))
                {
                    AddKeyValuePair();
                    xNewKeyField.Text = "";
                    xNewValueField.Text = "";
                    //xTypeComboBox.SelectedIndex = 0;
                    FocusOn(xNewKeyField);
                }
            }
        }

        private void xKeyValueListView_Tapped(object sender, TappedRoutedEventArgs e)
        {
            var newField = new KeyFieldContainer(new KeyController(),
                new BoundFieldModelController(new TextFieldModelController(""), RealDataContext), false,
                TypeColumnWidth);
            ListItemSource.Add(newField);
        }
    }
}