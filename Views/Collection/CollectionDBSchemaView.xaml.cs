using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Diagnostics;
using System.Linq;
using Windows.ApplicationModel.DataTransfer;
using Windows.System;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Data;
using Microsoft.Toolkit.Uwp.UI.Controls;
using CheckBox = Windows.UI.Xaml.Controls.CheckBox;
using FrameworkElement = Windows.UI.Xaml.FrameworkElement;
using Task = System.Threading.Tasks.Task;
using TextBlock = Windows.UI.Xaml.Controls.TextBlock;
using Point = Windows.Foundation.Point;

// The User Control item template is documented at https://go.microsoft.com/fwlink/?LinkId=234236

namespace Dash
{
    public sealed partial class CollectionDBSchemaView : ICollectionView
    {
        private WindowsDictionaryColumn       _sortColumn;
        private ListController<KeyController> _keys;
        private CollectionViewModel           _oldViewModel;

        public CollectionViewModel ViewModel   => DataContext as CollectionViewModel;
        public DataGrid            DataGrid    => xDataGrid;
        public UserControl         UserControl => this;
        public CollectionViewType  ViewType    => CollectionViewType.Schema;

        public CollectionDBSchemaView()
        {
            InitializeComponent();
            DataContextChanged += OnDataContextChanged;

            xDataGrid.MaxWidth = xDataGrid.MaxHeight = 50;
            xDataGrid.AutoGenerateColumns = false;
            xDataGrid.CanUserSortColumns = true;
            xDataGrid.CanUserResizeColumns = true;
            xDataGrid.CanUserReorderColumns = true;
            xDataGrid.ColumnWidth = new DataGridLength(100);
            xDataGrid.GridLinesVisibility = DataGridGridLinesVisibility.All;
            xDataGrid.CellEditEnding += XDataGridOnCellEditEnding;
            xDataGrid.LoadingRow += XDataGrid_LoadingRow;
            xDataGrid.ColumnReordered += XDataGridOnColumnReordered;
            xDataGrid.Sorting += XDataGrid_Sorting;
            xDataGrid.SelectionChanged += XDataGrid_SelectionChanged;

            xAddColumnEntry.AddKeyHandler(VirtualKey.Enter, args => AddNewColumn());
            xGridSplitter.PointerPressed += (s,e) => this.GetDocumentView().ViewModel.DragAllowed = e.GetCurrentPoint(null).Properties.IsRightButtonPressed;

            Loaded += OnLoaded;
        }

        static public void AddDataBoxForKey(KeyController key, DocumentController dvm)
        {
            var proto = dvm.GetDereferencedField<DocumentController>(KeyStore.LayoutPrototypeKey, null) ??  dvm;
            var docs  = proto.GetField<ListController<DocumentController>>(KeyStore.DataKey);

            if (docs != null)
            {
                foreach (var doc in docs.Where((doc) => doc.DocumentType.Equals(DataBox.DocumentType)))
                {
                    var fkey = (doc.GetField(KeyStore.DataKey) as ReferenceController).FieldKey;
                    if (key.Equals(fkey) == true)
                    {
                        return; // document already has a databox view of the added key
                    }
                }

                var newDataBoxCol = new DataBox(proto.GetDataDocument(), key, new Point(0, 35 * docs.Count)).Document;
                CollectionViewModel.RouteDataBoxReferencesThroughCollection(proto, new DocumentController[] { newDataBoxCol }.ToList());
                proto.AddToListField(KeyStore.DataKey, newDataBoxCol);
                newDataBoxCol.SetTitle(key.Name);
            }
        }
        public async Task FillColumn(ActionTextBox sender, KeyController key)
        {
            using (UndoManager.GetBatchHandle())
            {
                for (int i = 0; i < ViewModel.DocumentViewModels.Count; ++i)
                {
                    var doc = ViewModel.DocumentViewModels[i].DataDocument;
                    await CommitEdit(sender.Text, doc, key, i);
                }
            }
        }

        private void UpdateSort()
        {
            var docViewModels = ViewModel.DocumentViewModels;
            switch (_sortColumn?.SortDirection)
            {
            case DataGridSortDirection.Ascending:
                xDataGrid.ItemsSource = docViewModels.OrderBy<DocumentViewModel, string>(dvm =>
                     dvm.DocumentController.GetDataDocument().GetDereferencedField(_sortColumn.Key, null)?.ToString() ?? "");
                break;
            case DataGridSortDirection.Descending:
                xDataGrid.ItemsSource = docViewModels.OrderByDescending<DocumentViewModel, string>(dvm =>
                     dvm.DocumentController.GetDataDocument().GetDereferencedField(_sortColumn.Key, null)?.ToString() ?? "");
                break;
            default:
                xDataGrid.ItemsSource = docViewModels.ToArray();
                break;
            }
        }
        private void AddKey(KeyController key)
        {
            if (!_keys.Contains(key))
            {
                _keys.Add(key);
                xDataGrid.Columns.Add(new WindowsDictionaryColumn(key, this) { HeaderStyle = xHeaderStyle });
                var schemaColumns =
                    ViewModel.ContainerDocument
                        .GetField<ListController<KeyController>>(KeyStore.SchemaDisplayedColumns);

                foreach (var dvm in ViewModel.DocumentViewModels.Where((dvm) =>
                    dvm.DocumentController.DocumentType.Equals(CollectionBox.DocumentType)))
                {
                    AddDataBoxForKey(key, dvm.DocumentController);
                    dvm.LayoutDocument.SetField(KeyStore.SchemaDisplayedColumns, schemaColumns.Copy(), true);
                }
            }
        }
        private void RemoveKey(KeyController key)
        {
            var index = _keys.IndexOf(key);
            if (index != -1)
            {
                _keys.RemoveAt(index);
                xDataGrid.Columns.RemoveAt(index);
                var schemaColumns =
                    ViewModel.ContainerDocument
                        .GetField<ListController<KeyController>>(KeyStore.SchemaDisplayedColumns);

                foreach (var dvm in ViewModel.DocumentViewModels.Where((dvm) =>
                    dvm.DocumentController.DocumentType.Equals(CollectionBox.DocumentType)))
                {
                    RemoveDataBoxForKey(key, dvm);
                    dvm.LayoutDocument.SetField(KeyStore.SchemaDisplayedColumns, schemaColumns.Copy(), true);
                }

            }
        }
        private void RemoveDataBoxForKey(KeyController key, DocumentViewModel dvm)
        {
            var proto =
                dvm.DocumentController.GetDereferencedField<DocumentController>(KeyStore.LayoutPrototypeKey, null) ??
                dvm.DocumentController;
            var docs = proto.GetField<ListController<DocumentController>>(KeyStore.DataKey);
            foreach (var doc in docs.Where((doc) => doc.DocumentType.Equals(DataBox.DocumentType)))
            {
                var fkey = (doc.GetField(KeyStore.DataKey) as ReferenceController).FieldKey;
                if (key.Equals(fkey) == true)
                {
                    proto.RemoveFromListField(KeyStore.DataKey, doc);
                    break;
                }
            }
        }
        private void AddNewColumn()
        {
            if (!string.IsNullOrWhiteSpace(xAddColumnEntry.Text))
            {
                var key = KeyController.Get(xAddColumnEntry.Text);
                if (!_keys.Contains(key))
                {
                    AddKey(key);
                    ((IList<KeyController>)xColumnsList.ItemsSource).Add(key);
                }
            }

            xAddColumnEntry.Text = "";
        }
        private async Task CommitEdit(string script, DocumentController doc, KeyController key, int index)
        {
            try
            {
                var scope = Scope.CreateStateWithThisDocument(doc);
                scope.DeclareVariable("index", new NumberController(index));
                scope.DeclareVariable("table", ViewModel.ContainerDocument.GetDataDocument());
                var field = await DSL.InterpretUserInput(script, scope: scope);
                doc.SetField(key, field, true);
            }
            catch (DSLException e)
            {
            }
        }

        #region ICollection methods
        public void SetDropIndicationFill(Brush fill) { }
        public void SetupContextMenu(MenuFlyout flyout) { }
        public void OnDocumentSelected(bool selected)
        {
            if (!selected)
            {
                xDataGrid.Columns.ToList().ForEach((c) => (c.Header as ColumnHeaderViewModel).IsSelected = Visibility.Collapsed);
            }
        }
        #endregion

        private HashSet<KeyController> InitializeDocs()
        {
            var docs = ViewModel.DocumentViewModels;
            var keys = new HashSet<KeyController>();
            foreach (var doc in docs)
            {
                foreach (var field in doc.DataDocument.EnumDisplayableFields())
                {
                    keys.Add(field.Key);
                }
            }

            return keys;
        }

        private void       XDataGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var added = e.AddedItems.FirstOrDefault() as DocumentViewModel;
            XDocDisplay.DataContext = added != null ? new DocumentViewModel(added.DocumentController) { IsDimensionless = true } : added;
        }
        private void       XDataGrid_Sorting(object sender, DataGridColumnEventArgs e)
        {
            var sortColumn  = (WindowsDictionaryColumn)e.Column;
            var colHeaderVM = sortColumn.Header as ColumnHeaderViewModel;
            if (colHeaderVM.IsSelected != Visibility.Visible)
            {
                xDataGrid.Columns.ToList().ForEach((c) => (c.Header as ColumnHeaderViewModel).IsSelected = Visibility.Collapsed);
                colHeaderVM.IsSelected = Visibility.Visible;
            }
            else
            {
                if (_sortColumn != null && sortColumn != _sortColumn)
                {
                    _sortColumn.SortDirection = null;
                }

                _sortColumn = sortColumn;
                switch (e.Column.SortDirection)
                {
                case DataGridSortDirection.Ascending:  _sortColumn.SortDirection = DataGridSortDirection.Descending; break;
                case DataGridSortDirection.Descending: _sortColumn.SortDirection = null; break;
                default:                               _sortColumn.SortDirection = DataGridSortDirection.Ascending; break;
                }

                //ViewModel.ContainerDocument.SetField<ListController<TextController>>(KeyStore.ColumnSortingKey, new List<string>(new string[] { _sortColumn.Key.Name, _sortColumn.SortDirection?.ToString() ?? "" }), true);
                UpdateSort();
            }
        }
        private void       XDataGridOnColumnReordered(object sender, DataGridColumnEventArgs dataGridColumnEventArgs)
        {
            var col     = (WindowsDictionaryColumn)dataGridColumnEventArgs.Column;
            var removed = _keys.Remove(col.Key);
            Debug.Assert(removed);
            _keys.Insert(col.DisplayIndex, col.Key);
        }
        private void       XDataGrid_LoadingRow(object sender, DataGridRowEventArgs e)
        {
            e.Row.CanDrag = true;
            e.Row.DragStarting -= RowOnDragStarting;
            e.Row.DragStarting += RowOnDragStarting;
        }
        private async void XDataGridOnCellEditEnding(object sender, DataGridCellEditEndingEventArgs args)
        {
            if (args.EditAction == DataGridEditAction.Commit)
            {
                var box = (ActionTextBox)args.EditingElement;
                var dvm = ((DocumentViewModel)box.DataContext);
                var col = (WindowsDictionaryColumn)args.Column;

                using (UndoManager.GetBatchHandle())
                {
                    await CommitEdit(box.Text, dvm.DataDocument, col.Key,
                        args.Row.GetIndex()); //TODO This index might be wrong with sorting/filtering, etc.
                }

                AddDataBoxForKey(col.Key, dvm.DocumentController);
            }
        }

        private void       RowOnDragStarting(UIElement sender, DragStartingEventArgs args)
        {
            var dvm = (DocumentViewModel)((FrameworkElement)sender).DataContext;
            args.Data.SetDragModel(new DragDocumentModel(dvm.LayoutDocument)
                {DraggedDocCollectionViews = new List<CollectionViewModel> {ViewModel}});
        }
        private void       Join_DragStarting(UIElement sender, DragStartingEventArgs args)
        {
            var docViewModels = ViewModel.DocumentViewModels;
            var columnKey     = ((sender as FrameworkElement).DataContext as ColumnHeaderViewModel).Key;
            var docs          = docViewModels.Select(dvm => dvm.DocumentController).ToList();

            args.Data.RequestedOperation = DataPackageOperation.Move;
            args.Data.SetJoinModel(new JoinDragModel(ViewModel.ContainerDocument, docs, columnKey));

            var grouped       = docViewModels.GroupBy(dvm => dvm.DocumentController.GetDataDocument().GetDereferencedField(columnKey, null)?.ToString() ?? "");
            var collections   = grouped.Select(val => {
                var cnote = new CollectionNote(new Point(), CollectionViewType.Stacking, 300, 200, val.Select(dvm => dvm.DocumentController)).Document;
                cnote.SetTitle(columnKey.ToString()+"="+val.Key);
                return cnote;
            });
            ViewModel.ContainerDocument.SetField<ListController<DocumentController>>(KeyController.Get("DBGroupings"), collections.ToList(), true);
            var dfm = new DragFieldModel(new DocumentFieldReference(ViewModel.ContainerDocument, KeyController.Get("DBGroupings")))
            {
                LayoutFields = new Dictionary<KeyController, FieldControllerBase>()
                {
                    [KeyStore.CollectionViewTypeKey] = new TextController(CollectionViewType.Stacking.ToString())
                }
            };
            args.Data.SetDragModel(dfm);
        }

        private void       OnLoaded(object sender, RoutedEventArgs routedEventArgs)
        {
            if (ViewModel != null)
            {
                xDataGrid.MaxWidth = xDataGrid.MaxHeight = double.PositiveInfinity;
                xDataGrid.ItemsSource = ViewModel.BindableDocumentViewModels;

                var keys = (IList<KeyController>)InitializeDocs().ToList();
                var savedKeys = ViewModel.ContainerDocument
                    .GetField<ListController<KeyController>>(KeyStore.SchemaDisplayedColumns);
                foreach (var key in savedKeys ?? keys)
                {
                    AddKey(key);
                }
            }
        }
        private void       OnDataContextChanged(FrameworkElement sender, DataContextChangedEventArgs args)
        {
            if (ViewModel == _oldViewModel)
            {
                return;
            }

            if (_oldViewModel != null)
            {

                _oldViewModel.DocumentViewModels.CollectionChanged -= OnDocumentViewModelsCollectionChanged;
            }

            _oldViewModel = ViewModel;

            if (ViewModel == null)
            {
                return;
            }

            xDataGrid.Columns.Clear();
            _keys = ViewModel.ContainerDocument.GetField<ListController<KeyController>>(KeyStore.SchemaDisplayedColumns);
            if (_keys == null)
            {
                _keys = new ListController<KeyController> { KeyStore.TitleKey };
                ViewModel.ContainerDocument.SetField(KeyStore.SchemaDisplayedColumns, _keys, true);
            }

            foreach (var key in _keys)
            {
                xDataGrid.Columns.Add(new WindowsDictionaryColumn(key, this) { HeaderStyle = xHeaderStyle });
            }

            //TODO tfs Events
            if (/*ViewModel.IsLoaded &&*/ xDataGrid.ItemsSource == null)
            {
                xDataGrid.ItemsSource = ViewModel.BindableDocumentViewModels;
            }

            ViewModel.DocumentViewModels.CollectionChanged -= OnDocumentViewModelsCollectionChanged;
            ViewModel.DocumentViewModels.CollectionChanged += OnDocumentViewModelsCollectionChanged;
        }
        private void       OnDocumentViewModelsCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (ViewModel != null)
            {
                UpdateSort();
            }
            else if (_oldViewModel != null)
            {
                _oldViewModel.DocumentViewModels.CollectionChanged -= OnDocumentViewModelsCollectionChanged;
            }
        }
        private void       OnPointerWheelChanged(object sender, PointerRoutedEventArgs e)
        {
            if (this.IsCtrlPressed())
            {
                var delta = e.GetCurrentPoint(null).Properties.MouseWheelDelta;
                ViewModel.CellFontSize = Math.Max(2, ViewModel.CellFontSize * (delta > 0 ? 0.95 : 1.05));
            }
            e.Handled = true;
        }
        private async void OnDrop(object sender, DragEventArgs e)
        {
            if (e.DataView.GetJoinDragModel() is JoinDragModel jdm)
            {
                e.Handled = true;
                var allKeys = new List<KeyController>();
                foreach (var doc in jdm.DraggedDocuments)
                {
                    foreach (var kvp in doc.GetDataDocument().EnumDisplayableFields())
                    {
                        if (!allKeys.Contains(kvp.Key))
                        {
                            allKeys.Add(kvp.Key);
                        }
                    }
                }

                var comparisonKeys = new List<KeyController>();
                foreach (var doc in ViewModel.DocumentViewModels.Select(dvm => dvm.DocumentController))
                {
                    foreach (var kvp in doc.GetDataDocument().EnumDisplayableFields())
                    {
                        if (!comparisonKeys.Contains(kvp.Key))
                        {
                            comparisonKeys.Add(kvp.Key);
                        }
                    }
                }

                (KeyController comparisonKey, List<KeyController> keysToJoin) = await MainPage.Instance.PromptJoinTables(comparisonKeys, allKeys, new KeyController[] { jdm.DraggedKey }.ToList());
                if (comparisonKey == null) return;
                foreach (var doc in jdm.DraggedDocuments)
                {
                    var draggedKey = jdm.DraggedKey;
                    var comparisonField = doc.GetDataDocument().GetDereferencedField(draggedKey, null)?.GetValue();
                    var matchingDoc = ViewModel.DocumentViewModels.FirstOrDefault(dvm =>
                        dvm.DocumentController.GetDataDocument().GetDereferencedField(comparisonKey, null).GetValue().Equals(comparisonField));
                    if (matchingDoc != null)
                    {
                        foreach (var key in keysToJoin)
                        {
                            if (doc.GetDataDocument().GetField(key) != null)
                            {
                                matchingDoc.DataDocument.SetField(key, doc.GetDataDocument().GetField(key), true);
                            }
                        }
                    }
                }

                foreach (var key in keysToJoin)
                {
                    AddKey(key);
                }
            }
        }

        private void       xOuterGrid_PointerPressed(object sender, PointerRoutedEventArgs e)
        {
            var header = VisualTreeHelper.FindElementsInHostCoordinates(e.GetCurrentPoint(null).Position, xOuterGrid).OfType<Microsoft.Toolkit.Uwp.UI.Controls.Primitives.DataGridColumnHeader>();
            this.GetDocumentView().ViewModel.DragAllowed = header == null || e.GetCurrentPoint(null).Properties.IsRightButtonPressed;
            xDataGrid.SelectedIndex = -1;
        }

        private void       xColumnFlyout_ColumnVisibilityChanged(object sender, RoutedEventArgs e)
        {
            var checkBox = sender as CheckBox;
            var key = checkBox.DataContext as KeyController;
            if (checkBox.IsChecked is bool b && b)
            {
                AddKey(key);
            }
            else
            {
                RemoveKey(key);
            }
        }
        private void       xColumnFlyout_OnOpening(object sender, object e)
        {
            xColumnsList.ItemsSource = null;
            if (ViewModel != null)
            {
                var keys = new HashSet<KeyController>();
                foreach (var dvm in ViewModel.DocumentViewModels)
                {
                    foreach (var enumDisplayableField in dvm.DataDocument.EnumDisplayableFields())
                    {
                        keys.Add(enumDisplayableField.Key);
                    }
                }

                foreach (var keyController in _keys) //Need to do this to include added columns that haven't been filled in yet
                {
                    keys.Add(keyController);
                }

                xColumnsList.ItemsSource = new ObservableCollection<KeyController>(keys);
            }
        }
        private void       xColumnFlyoutCheckBox_OnLoaded(object sender, RoutedEventArgs e)
        {
            if (sender is CheckBox checkBox && checkBox.DataContext != null)
            {
                checkBox.IsChecked = _keys.Contains(checkBox.DataContext as KeyController);

                checkBox.Checked   += xColumnFlyout_ColumnVisibilityChanged;
                checkBox.Unchecked += xColumnFlyout_ColumnVisibilityChanged;
            }
        }

        private void       xAddColumnButton_OnTapped(object sender, TappedRoutedEventArgs e) { AddNewColumn(); }
        private void       xAddRow_OnTapped(object sender, TappedRoutedEventArgs e)
        {
            var newDoc = new CollectionNote(new Point(), CollectionViewType.Stacking, 200, 200).Document;
            var docs = newDoc.GetFieldOrCreateDefault<ListController<DocumentController>>(KeyStore.DataKey);
            int placement = 0;
            foreach (var key in _keys)
            {
                //newDoc.GetDataDocument().SetField<TextController>(key, "<empty>", true);
                docs.Add(new DataBox(newDoc.GetDataDocument(), key, new Point(0, placement), 100, double.NaN).Document);
                docs.Last().SetTitle(key.Name);
                placement += 35;
            }

            newDoc.SetField(KeyStore.SchemaDisplayedColumns,
                ViewModel.ContainerDocument.GetField<ListController<KeyController>>(KeyStore.SchemaDisplayedColumns)
                    .Copy(), true);
            // Add a new document to the schema view
            ViewModel.AddDocument(newDoc);
            e.Handled = true;
        }
        private void       xColumnHeaderBorder_Tapped(object sender, TappedRoutedEventArgs e)
        {
            xDataGrid.Columns.ToList().ForEach((c) => (c.Header as ColumnHeaderViewModel).IsSelected = Visibility.Collapsed);
            ((sender as Border).DataContext as ColumnHeaderViewModel).IsSelected = Visibility.Visible;
        }
    }

    public class BindingHelper
    {
        public static readonly DependencyProperty FontSizeProperty =
        DependencyProperty.RegisterAttached("FontSize", typeof(string), typeof(BindingHelper),
            new PropertyMetadata(null, new PropertyChangedCallback(FontSizePropertyChanged)));

        public static string GetFontSize(DependencyObject obj)
        {
            return (string)obj.GetValue(FontSizeProperty);
        }

        public static void   SetFontSize(DependencyObject obj, string value)
        {
            obj.SetValue(FontSizeProperty, value);
        }

        private static void  FontSizePropertyChanged(DependencyObject obj, DependencyPropertyChangedEventArgs e)
        {
            if (e.NewValue is string propertyPath)
            {
                // ugh ... the textBlock's dataContext is it's row's document view model, so we need to get the collection view model from the tag
                if (obj is TextBlock textBlock && textBlock.Tag is CollectionViewModel collectionViewModel)
                {
                    BindingOperations.SetBinding(textBlock,  TextBlock.FontSizeProperty,
                                           new Binding { Source = collectionViewModel, Path = new PropertyPath(propertyPath)});
                }
                if (obj is ActionTextBox aTextBox && aTextBox.Tag is CollectionViewModel collectionViewModel2)
                {
                    BindingOperations.SetBinding(aTextBox, ActionTextBox.FontSizeProperty,
                                           new Binding { Source = collectionViewModel2, Path = new PropertyPath(propertyPath) });
                }
            }
        }
    }
    public class ColumnHeaderViewModel :ViewModelBase
    {
        private Visibility          _isSelected = Visibility.Collapsed;
        public Visibility            IsSelected
        {
            get => _isSelected;
            set
            {
                SetProperty<Visibility>(ref _isSelected, value);
            }
        }
        public KeyController         Key                 { get; private set; }
        public CollectionViewModel   CollectionViewModel { get; private set; }
        public ColumnHeaderViewModel(CollectionViewModel cvm, KeyController key)
        {
            CollectionViewModel = cvm;
            Key = key;
            IsSelected = Visibility.Collapsed;
        }

        public override string ToString()
        {
            return Key?.ToString();
        }
    }


    public class WindowsDictionaryColumn : DataGridColumn
    {
        public KeyController Key => (Header as ColumnHeaderViewModel).Key;
        public CollectionDBSchemaView Parent { get; }
        public WindowsDictionaryColumn(KeyController key, CollectionDBSchemaView parent)
        {
            Header = new ColumnHeaderViewModel(parent.ViewModel, key);
            Parent = parent;
        }

        protected override FrameworkElement GenerateEditingElement(DataGridCell cell, object dataItem)
        {
            var atb = new ActionTextBox {  IsSpellCheckEnabled = false, Tag = Parent.ViewModel }; // must set Tag so that BindingHelper can find CollectionViewModel
            atb.AddKeyHandler(VirtualKey.Enter, async args =>
            {
                if (cell.IsCtrlPressed())
                {
                    await Parent.FillColumn(atb, Key);
                    Parent.DataGrid.CancelEdit();
                    args.Handled = true;
                }
            });
            return atb;
        }
        protected override FrameworkElement GenerateElement(DataGridCell cell, object dataItem)
        {
            var textblock = new TextBlock { IsDoubleTapEnabled = false, Tag = Parent.ViewModel }; // must set Tag so that BindingHelper can find CollectionViewModel
            textblock.DataContextChanged += Textblock_DataContextChanged;
            return textblock;
        }

        private void Textblock_DataContextChanged(FrameworkElement sender, DataContextChangedEventArgs args)
        {
            if (sender.DataContext is DocumentViewModel)
            {
                var binding = new FieldBinding<FieldControllerBase, TextController>
                {
                    Document = (sender.DataContext as DocumentViewModel).DataDocument,
                    Key = Key,
                    Converter = new ObjectToStringConverter(),
                    Mode = BindingMode.OneWay,
                };
                sender.AddFieldBinding(TextBlock.TextProperty, binding);
            }
        }

        protected override object PrepareCellForEdit(FrameworkElement editingElement,
            RoutedEventArgs editingEventArgs)
        {
            var tb = (ActionTextBox)editingElement;

            var doc = (DocumentViewModel)editingElement.DataContext;
            var dataDoc = doc.DataDocument;

            tb.Text = DSL.GetScriptForField(dataDoc.GetField(Key), dataDoc);

            return string.Empty;
        }
    }
}
