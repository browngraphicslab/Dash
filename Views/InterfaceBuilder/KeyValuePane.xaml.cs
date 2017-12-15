using System;
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
using static Windows.ApplicationModel.Core.CoreApplication;
using Windows.Foundation;

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
                    .GetDereferencedField<DocumentController>(KeyStore.DocumentContextKey, null)
                : _documentControllerDataContext;
        
        public KeyValuePane()
        {
            MainView.CoreWindow.PointerPressed -= CoreWindow_PointerPressed;
            MainView.CoreWindow.PointerPressed += CoreWindow_PointerPressed;
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

        public void SetHeaderVisibility(Visibility vis)
        {
            xHeaderGrid.Visibility = vis == DashShared.Visibility.Visible
                ? Windows.UI.Xaml.Visibility.Visible
                : Windows.UI.Xaml.Visibility.Collapsed;
        }

        public void SetDataContextToDocumentController(DocumentController documentToDisplay)
        {
            var dataContext = documentToDisplay.GetDereferencedField<DocumentController>(KeyStore.DocumentContextKey, null);
            documentToDisplay = dataContext ?? documentToDisplay;
            if (_documentControllerDataContext != null)
                _documentControllerDataContext.FieldModelUpdated -=
                    _documentControllerDataContext_DocumentFieldUpdated;
            _documentControllerDataContext = documentToDisplay;
            _documentControllerDataContext.FieldModelUpdated -= _documentControllerDataContext_DocumentFieldUpdated;
            _documentControllerDataContext.FieldModelUpdated += _documentControllerDataContext_DocumentFieldUpdated;
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
            if (_documentControllerDataContext != null)
            {
                var keys = _documentControllerDataContext.GetDereferencedField<ListController<KeyController>>(KeyStore.PrimaryKeyKey, null)?.TypedData?.ToList() ?? new List<KeyController>();
                foreach (var keyFieldPair in _documentControllerDataContext.EnumFields())
                    if (!keyFieldPair.Key.Name.StartsWith("_"))
                        ListItemSource.Add(new KeyFieldContainer(keyFieldPair.Key,
                            new BoundController(keyFieldPair.Value, _documentControllerDataContext),
                            keys.Contains(keyFieldPair.Key), TypeColumnWidth));
            } else
            {

            }
        }

        private void _documentControllerDataContext_DocumentFieldUpdated(FieldControllerBase sender, FieldUpdatedEventArgs args, Context context)
        {
            // if a field has been replaced or updated then set it's source to be the new element
            // otherwise replcae the entire data source to reflect the new set of fields (due to add or remove)
            var dargs = (DocumentController.DocumentFieldUpdatedEventArgs) args;
            if (args.Action == DocumentController.FieldUpdatedAction.Replace || args.Action == DocumentController.FieldUpdatedAction.Update)
                UpdateListItemSourceElement(dargs.Reference.FieldKey, dargs.NewValue);
            else SetListItemSourceToCurrentDataContext();
        }

        private void UpdateListItemSourceElement(KeyController fieldKey, FieldControllerBase fieldValue)
        {
            var keys = _documentControllerDataContext.GetDereferencedField<ListController<KeyController>>(KeyStore.PrimaryKeyKey, null)?.TypedData?.ToList() ?? new List<KeyController>();

            for (var i = 0; i < ListItemSource.Count; i++)
                if (ListItemSource[i].Key.Equals(fieldKey))
                    ListItemSource[i] = new KeyFieldContainer(fieldKey,
                        new BoundController(fieldValue, RealDataContext), keys.Contains(fieldKey),TypeColumnWidth);
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
            var type = (TypeInfo)xTypeComboBox.SelectedItem;
            if (xNewKeyField.Text != "" && type != TypeInfo.None && (xNewValueField.Text != "" || type == TypeInfo.List || type == TypeInfo.Document))
            {
                if (AddKeyValuePair())
                {
                    xNewKeyField.Text = "";
                    xNewValueField.Text = "";
                    xTypeComboBox.SelectedIndex = 0;
                    ToggleAddKVPane();
                }
            }
        }

        /// <summary>
        ///     Adds a new row to the KeyValuePane, using user inputed values, returning a boolean depending on whether it is
        ///     successful in adding the pair.
        /// </summary>
        private bool AddKeyValuePair()
        {
            var item = (TypeInfo)xTypeComboBox.SelectedItem;
            KeyController key = new KeyController(Guid.NewGuid().ToString(), xNewKeyField.Text);
            FieldControllerBase fmController = new TextController("something went wrong");

            //_documentControllerDataContext.ParseDocField(key, xNewValueField.Text);
            //fmController = _documentControllerDataContext.GetField(key);

            // /*                                         // TODO the above doesn't take into account the type users selected, ex) choosing "Text" and inputing 5 will return a Number type field 
            ///                                         // and can't create image fields ? 
            if (item == TypeInfo.Number)
            {
                double number;
                // if specified type is number only add a new keyvalue pair if the value is a number 
                if (double.TryParse(xNewValueField.Text, out number))
                    fmController = new NumberController(number);
                else
                    return false;
            }
            else if (item == TypeInfo.Image)
            {
                fmController = new ImageController(new Uri(xNewValueField.Text));
            }
            else if (item == TypeInfo.Text)
            {
                fmController = new TextController(xNewValueField.Text);
            }
            else if (item == TypeInfo.List)
            {
                //TODO tfs: this can only create lists of docs(collections), not lists of other things
                fmController = new ListController<DocumentController>();
            }
            else if (item == TypeInfo.Document)
            {
                var fields = new Dictionary<KeyController, FieldControllerBase>
                {
                    [KeyStore.ActiveLayoutKey] = new FreeFormDocument(new List<DocumentController>()).Document
                };

                fmController = new DocumentController(fields, DocumentType.DefaultType);
            }
            var keys = _documentControllerDataContext.GetDereferencedField<ListController<KeyController>>(KeyStore.PrimaryKeyKey, null)?.TypedData?.ToList() ?? new List<KeyController>();

            ListItemSource.Add(new KeyFieldContainer(key, new BoundController(fmController, RealDataContext), keys.Contains(key), TypeColumnWidth));
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
        }

        private void CheckBox_Checked(object sender, RoutedEventArgs e)
        {
            var kf = (sender as CheckBox).Tag as KeyFieldContainer;
            if (kf == null)
            {
                return;
            }
            var primaryKeys =
                _documentControllerDataContext.GetDereferencedField<ListController<KeyController>>( KeyStore.PrimaryKeyKey, null);
            if (primaryKeys == null)
            {
                _documentControllerDataContext.SetField(KeyStore.PrimaryKeyKey, new ListController<KeyController>(kf.Key), false);
            }
            else
            {
                if (!primaryKeys.TypedData.Contains(kf.Key))
                {
                    primaryKeys.Add(kf.Key);
                }
            }
        }

        private void CheckBox_Unchecked(object sender, RoutedEventArgs e)
        {
            var kf = (sender as CheckBox).Tag as KeyFieldContainer;
            if (kf == null)
            {
                return;
            }
            var primaryKeys =
                _documentControllerDataContext.GetDereferencedField<ListController<KeyController>>( KeyStore.PrimaryKeyKey, null);
            if(primaryKeys != null)
            {
                if (primaryKeys.TypedData.Contains(kf.Key))
                {
                    primaryKeys.Remove(kf.Key);
                }
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
            
            _tb.MaxHeight = _tb.MaxWidth = 500;
            var srcText = "";
            //set the editing textbox's initial value appropriately 
            if (tappedSource is TextBlock)
                srcText = (tappedSource as TextBlock).Text;
            else if (tappedSource is Image)
                srcText = (tappedSource as Image).BaseUri.AbsoluteUri;
            else if (tappedSource is Grid)
                return;
            else throw new NotImplementedException();
            _tb.AcceptsReturn = !srcText.Contains("\r");
            _lastTbText = _tb.Text = srcText;
            _tb.TextChanged += _tb_TextChanged;

            //add textbox graphically and set up events 
            Canvas.SetLeft(_tb, p.X);
            Canvas.SetTop(_tb, p.Y);
            MainPage.Instance.xCanvas.Children.Add(_tb);
            SetTextBoxEvents();
            FocusOn(_tb);
        }
        string _lastTbText = "";
        private void _tb_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (_tb.AcceptsReturn && _tb.Text.StartsWith(_lastTbText) && _tb.Text.EndsWith("\r") &&
                Window.Current.CoreWindow.GetKeyState(VirtualKey.Shift).HasFlag(Windows.UI.Core.CoreVirtualKeyStates.Down))
            {
                //DBTest.ResetCycleDetection();
                var field = _documentControllerDataContext.GetDereferencedField<FieldControllerBase>(
                    _selectedKV.Key, new Context(_documentControllerDataContext));
                _documentControllerDataContext.ParseDocField(_selectedKV.Key, _tb.Text, field);
                RemoveEditingTextBox();
            } else
                _lastTbText = _tb.Text;
        }

        /// <summary>
        ///     Textbox will update keyvaluepane on enter
        /// </summary>
        private void SetTextBoxEvents()
        {
            _tb.LostFocus += (s, e) => RemoveEditingTextBox();

            // if key was pressed, just edit the key value (don't have to update the Controllers) 
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
                if (e.Key == VirtualKey.Enter && !_tb.AcceptsReturn)
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
                    (xNewValueField.Text != "" || type == TypeInfo.List || type == TypeInfo.Document))
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
            //var newField = new KeyFieldContainer(new KeyController(),
            //    new BoundController(new TextController(""), RealDataContext), false,
            //    TypeColumnWidth);
            //ListItemSource.Add(newField);
        }

        public void SetUpForDocumentBox(DocumentController dc)
        {
            xKeyValueListView.CanDragItems = false;
            xKeyValueListView.SelectionMode = ListViewSelectionMode.None;
            SetHeaderVisibility(DashShared.Visibility.Collapsed);
            SetDataContextToDocumentController(dc);
        }

        public class HeaderDragData
        {
            public DocumentController Document;
            public KeyFieldContainer FieldKey;
            public CollectionView.CollectionViewType ViewType;
        }
        static public HeaderDragData DragModel = null;
        static Windows.UI.Input.PointerPoint IgnoreE;
        private void KeyDragPointerPressed(object sender, PointerRoutedEventArgs e)
        {
            var header = new HeaderDragData()
            {
                Document = _documentControllerDataContext,
                FieldKey = (sender as FrameworkElement).DataContext as KeyFieldContainer
            };
            IgnoreE = e.GetCurrentPoint(this);
            DragModel = header;
            var c = (sender as UIElement).GetFirstAncestorOfType<ContentPresenter>();
            c.StartDragAsync(e.GetCurrentPoint(sender as UIElement));
            e.Handled = true;
        }
        static void CoreWindow_PointerPressed(Windows.UI.Core.CoreWindow sender, Windows.UI.Core.PointerEventArgs args)
        {
            if (IgnoreE?.FrameId != args.CurrentPoint.FrameId)
                DragModel =  null;
        }
    }
}