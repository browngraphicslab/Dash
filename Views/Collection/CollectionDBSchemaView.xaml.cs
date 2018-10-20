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
using Telerik.UI.Xaml.Controls.Grid;
using Telerik.UI.Xaml.Controls.Grid.Primitives;
using Windows.UI.Xaml.Data;
using Microsoft.Office.Interop.Word;
using Microsoft.Toolkit.Uwp.UI.Controls;
using NewControls;
using CheckBox = Windows.UI.Xaml.Controls.CheckBox;
using FrameworkElement = Windows.UI.Xaml.FrameworkElement;
using StackPanel = Windows.UI.Xaml.Controls.StackPanel;
using Task = System.Threading.Tasks.Task;
using TextBlock = Windows.UI.Xaml.Controls.TextBlock;

// The User Control item template is documented at https://go.microsoft.com/fwlink/?LinkId=234236

namespace Dash
{
    public sealed partial class CollectionDBSchemaView : ICollectionView
    {
        public CollectionViewModel ViewModel => DataContext as CollectionViewModel;

        private WindowsDictionaryColumn _sortColumn;
        public DataGrid DataGrid => xDataGrid;

        //ICollectionView implementation
        public void SetDropIndicationFill(Brush fill)
        {
        }

        public void SetupContextMenu(MenuFlyout contextMenu)
        {

        }

        public UserControl UserControl => this;

        private ListController<KeyController> Keys { get; set; }

        public CollectionDBSchemaView()
        {
            InitializeComponent();
            DataContextChanged += OnDataContextChanged;

            xDataGrid.MaxWidth = xDataGrid.MaxHeight = 50;
            xDataGrid.AutoGenerateColumns = false;
            xDataGrid.CanUserSortColumns = true;
            xDataGrid.CanUserResizeColumns = true;
            xDataGrid.CanUserReorderColumns = true;
            xDataGrid.ColumnWidth = new DataGridLength(200);
            xDataGrid.GridLinesVisibility = DataGridGridLinesVisibility.All;
            xDataGrid.CellEditEnding += XDataGridOnCellEditEnding;
            xDataGrid.LoadingRow += XDataGrid_LoadingRow;
            xDataGrid.ColumnReordered += XDataGridOnColumnReordered;
            xDataGrid.Sorting += XDataGrid_Sorting;

            XNewColumnEntry.AddKeyHandler(VirtualKey.Enter, args => { AddNewColumn(); });

            Loaded += OnLoaded;
        }

        private void XDataGrid_Sorting(object sender, DataGridColumnEventArgs e)
        {
            var sortColumn = (WindowsDictionaryColumn)e.Column;
            if (_sortColumn != null && sortColumn != _sortColumn)
            {
                _sortColumn.SortDirection = null;
            }

            _sortColumn = sortColumn;
            switch (e.Column.SortDirection)
            {
            case DataGridSortDirection.Ascending:
                _sortColumn.SortDirection = DataGridSortDirection.Descending;
                break;
            case DataGridSortDirection.Descending:
                _sortColumn.SortDirection = null;
                break;
            default:
                _sortColumn.SortDirection = DataGridSortDirection.Ascending;
                break;
            }

            //ViewModel.ContainerDocument.SetField<ListController<TextController>>(KeyStore.ColumnSortingKey, new List<string>(new string[] { _sortColumn.Key.Name, _sortColumn.SortDirection?.ToString() ?? "" }), true);
            UpdateSort();
        }

        private void UpdateSort()
        {
            switch (_sortColumn?.SortDirection)
            {
            case DataGridSortDirection.Ascending:
                this.xDataGrid.ItemsSource = ViewModel.DocumentViewModels.OrderBy<DocumentViewModel, string>((dvm) =>
                    dvm.DocumentController.GetDataDocument().GetDereferencedField(_sortColumn.Key, null)?.ToString() ??
                    "");
                break;
            case DataGridSortDirection.Descending:
                this.xDataGrid.ItemsSource = ViewModel.DocumentViewModels.OrderByDescending<DocumentViewModel, string>(
                    (dvm) => dvm.DocumentController.GetDataDocument().GetDereferencedField(_sortColumn.Key, null)
                                 ?.ToString() ?? "");
                break;
            default:
                this.xDataGrid.ItemsSource = ViewModel.DocumentViewModels.ToArray();
                break;
            }
        }

        private void XDataGridOnColumnReordered(object sender, DataGridColumnEventArgs dataGridColumnEventArgs)
        {
            var col = (WindowsDictionaryColumn)dataGridColumnEventArgs.Column;
            var removed = Keys.Remove(col.Key);
            Debug.Assert(removed);
            Keys.Insert(col.DisplayIndex, col.Key);
        }

        private void XDataGrid_LoadingRow(object sender, DataGridRowEventArgs e)
        {
            e.Row.CanDrag = true;
            e.Row.DragStarting -= RowOnDragStarting;
            e.Row.DragStarting += RowOnDragStarting;
        }

        private void RowOnDragStarting(UIElement sender, DragStartingEventArgs args)
        {
            var dvm = (DocumentViewModel)((FrameworkElement)sender).DataContext;
            args.Data.SetDragModel(new DragDocumentModel(dvm.LayoutDocument)
                {DraggedDocCollectionViews = new List<CollectionViewModel> {ViewModel}});
        }

        private void OnLoaded(object sender, RoutedEventArgs routedEventArgs)
        {
            if (ViewModel != null)
            {
                xDataGrid.MaxWidth = xDataGrid.MaxHeight = double.PositiveInfinity;
                //xDataGrid.UpdateLayout();
                xDataGrid.ItemsSource = ViewModel.BindableDocumentViewModels;

                var keys = InitializeDocs().ToList();
                var savedKeys = ViewModel.ContainerDocument
                    .GetField<ListController<KeyController>>(KeyStore.SchemaDisplayedColumns).TypedData;
                foreach (var key in savedKeys ?? keys)
                {
                    AddKey(key);
                }
            }
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

                AddDataBoxForKey(col.Key, dvm.DataDocument);
            }
        }

        private void AddRow_OnTapped(object sender, TappedRoutedEventArgs e)
        {
            var newDoc = new CollectionNote(new Windows.Foundation.Point(), CollectionView.CollectionViewType.Stacking,
                200, 200).Document;
            var docs = newDoc.GetFieldOrCreateDefault<ListController<DocumentController>>(KeyStore.DataKey);
            int placement = 0;
            foreach (var key in Keys)
            {
                //newDoc.GetDataDocument().SetField<TextController>(key, "<empty>", true);
                docs.Add(new DataBox(new DocumentReferenceController(newDoc.GetDataDocument(), key), 0, placement, 100,
                    double.NaN).Document);
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

        private CollectionViewModel _oldViewModel;

        private void OnDataContextChanged(FrameworkElement sender, DataContextChangedEventArgs args)
        {
            if (ViewModel == _oldViewModel)
            {
                return;
            }

            if (_oldViewModel != null)
            {

                _oldViewModel.DocumentViewModels.CollectionChanged -= DocumentViewModels_CollectionChanged;
            }

            _oldViewModel = ViewModel;

            if (ViewModel == null)
            {
                return;
            }

            xDataGrid.Columns.Clear();
            Keys = ViewModel.ContainerDocument.GetField<ListController<KeyController>>(KeyStore.SchemaDisplayedColumns);
            if (Keys == null)
            {
                Keys = new ListController<KeyController> {KeyStore.TitleKey};
                ViewModel.ContainerDocument.SetField(KeyStore.SchemaDisplayedColumns, Keys, true);
            }

            foreach (var key in Keys)
            {
                xDataGrid.Columns.Add(new WindowsDictionaryColumn(key, this) {Header = key, HeaderStyle = xHeaderStyle });
            }

            if (ViewModel.IsLoaded && xDataGrid.ItemsSource == null)
            {
                xDataGrid.ItemsSource = ViewModel.BindableDocumentViewModels;
            }

            ViewModel.DocumentViewModels.CollectionChanged -= DocumentViewModels_CollectionChanged;
            ViewModel.DocumentViewModels.CollectionChanged += DocumentViewModels_CollectionChanged;
        }

        private void DocumentViewModels_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (ViewModel != null)
            {
                UpdateSort();
            }
            else if (_oldViewModel != null)
            {
                _oldViewModel.DocumentViewModels.CollectionChanged -= DocumentViewModels_CollectionChanged;
            }
        }

        private void AddKey(KeyController key)
        {
            if (!Keys.Contains(key))
            {
                Keys.Add(key);
                xDataGrid.Columns.Add(new WindowsDictionaryColumn(key, this) {Header = key, HeaderStyle = xHeaderStyle});
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

        static public void AddDataBoxForKey(KeyController key, DocumentController dvm)
        {
            var proto = dvm.GetDereferencedField<DocumentController>(KeyStore.LayoutPrototypeKey, null) ?? dvm;
            var docs = proto.GetField<ListController<DocumentController>>(KeyStore.DataKey);

            foreach (var doc in docs.Where((doc) => doc.DocumentType.Equals(DataBox.DocumentType)))
            {
                var fkey = (doc.GetField(KeyStore.DataKey) as ReferenceController).FieldKey;
                if (key.Equals(fkey) == true)
                {
                    return; // document already has a databox view of the added key
                }
            }

            var newDataBoxCol = new DataBox(new DocumentReferenceController(proto.GetDataDocument(), key), 0,
                35 * docs.Count, double.NaN, double.NaN).Document;
            CollectionViewModel.RouteDataBoxReferencesThroughCollection(proto,
                new List<DocumentController>(new DocumentController[] {newDataBoxCol}));
            proto.AddToListField(KeyStore.DataKey, newDataBoxCol);
            newDataBoxCol.SetTitle(key.Name);
        }

        private void ColumnVisibility_Changed(object sender, RoutedEventArgs e)
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

        private void RemoveKey(KeyController key)
        {
            var index = Keys.IndexOf(key);
            if (index != -1)
            {
                Keys.RemoveAt(index);
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

        private void XColumnFlyout_OnOpening(object sender, object e)
        {
            xColumnsList.ItemsSource = null;
            if (ViewModel == null)
            {
                return;
            }

            var keys = new HashSet<KeyController>();
            foreach (var dvm in ViewModel.DocumentViewModels)
            {
                foreach (var enumDisplayableField in dvm.DataDocument.EnumDisplayableFields())
                {
                    keys.Add(enumDisplayableField.Key);
                }
            }

            foreach (var keyController in Keys
            ) //Need to do this to include added columns that haven't been filled in yet
            {
                keys.Add(keyController);
            }

            xColumnsList.ItemsSource = new ObservableCollection<KeyController>(keys);
        }

        private void CheckBox_OnLoaded(object sender, RoutedEventArgs e)
        {
            if ((sender as CheckBox).DataContext != null)
            {
                var checkBox = sender as CheckBox;
                checkBox.IsChecked = Keys.Contains(checkBox.DataContext as KeyController);

                checkBox.Checked += ColumnVisibility_Changed;
                checkBox.Unchecked += ColumnVisibility_Changed;
            }
        }

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

        private void AddNewColumn()
        {
            if (!string.IsNullOrWhiteSpace(XNewColumnEntry.Text))
            {
                var key = new KeyController(XNewColumnEntry.Text);
                if (!Keys.Contains(key))
                {
                    AddKey(key);
                    ((IList<KeyController>)xColumnsList.ItemsSource).Add(key);
                }
            }

            XNewColumnEntry.Text = "";
        }

        private void XNewColumnButton_OnTapped(object sender, TappedRoutedEventArgs e)
        {
            AddNewColumn();
        }

        private void UserControl_PointerWheelChanged(object sender, PointerRoutedEventArgs e)
        {
            e.Handled = true;
        }

        private void Join_DragStarting(UIElement sender, DragStartingEventArgs args)
        {
            args.Data.RequestedOperation = DataPackageOperation.Move;

            var docs = new List<DocumentController>();
            foreach (var doc in ViewModel.DocumentViewModels)
            {
                docs.Add(doc.DocumentController);
            }

            args.Data.SetDragModel(new DragDocumentModel(docs, CollectionView.CollectionViewType.DB)
                {DraggingJoinButton = true, DraggedKey = (sender as FrameworkElement).DataContext as KeyController});
        }

        private async void CollectionDBSchemaView_OnDrop(object sender, DragEventArgs e)
        {
            if (e.DataView.GetDragModel() is DragDocumentModel ddm && ddm.DraggingJoinButton)
            {
                e.Handled = true;
                var allKeys = new List<KeyController>();
                foreach (var doc in ddm.DraggedDocuments)
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

                (KeyController comparisonKey, List<KeyController> keysToJoin) = await MainPage.Instance.PromptJoinTables(comparisonKeys, allKeys);
                if (comparisonKey == null) return;
                foreach (var doc in ddm.DraggedDocuments)
                {
                    var draggedKey = ddm.DraggedKey;
                    var comparisonField = doc.GetDataDocument().GetDereferencedField(draggedKey, null).GetValue(null);
                    var matchingDoc = ViewModel.DocumentViewModels.FirstOrDefault(dvm =>
                        dvm.DocumentController.GetDataDocument().GetDereferencedField(comparisonKey, null).GetValue(null).Equals(comparisonField));
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

        public class DataGridDictionaryColumn : DataGridTypedColumn
        {
            public KeyController Key { get; set; }

            public DataGridDictionaryColumn(KeyController key)
            {
                PropertyName = key.Name;
                Key = key;
            }

            public override object GetEditorType(object item)
            {
                return typeof(TextBox);
            }

            public override FrameworkElement CreateEditorContentVisual()
            {
                return new TextBox();
            }

            public override object CreateContainer(object rowItem)
            {
                return new MyContentPresenter();
            }

            public override object GetContainerType(object rowItem)
            {
                return typeof(MyContentPresenter);
            }

            protected override DataGridFilterControlBase CreateFilterControl()
            {
                return new DataGridTextFilterControl()
                {
                    PropertyName = PropertyName
                };
            }

            public override void PrepareEditorContentVisual(FrameworkElement editorContent, Binding binding)
            {
                var doc = ((DocumentController)binding.Source).GetDataDocument();
                var field = doc.GetField(Key);
                ((TextBox)editorContent).Text = DSL.GetScriptForField(field, doc);
            }

            public override void ClearEditorContentVisual(FrameworkElement editorContent)
            {
                editorContent.ClearValue(TextBox.TextProperty);
            }

            public override void PrepareCell(object container, object value, object item)
            {
                base.PrepareCell(container, value, item);

                var thisDoc = (DocumentController)item; //This should be data doc I think
                var cp = (MyContentPresenter)container;
                cp.SetDocumentAndKey(thisDoc, Key);
            }
        }

        public class WindowsDictionaryColumn : Microsoft.Toolkit.Uwp.UI.Controls.DataGridColumn
        {
            public KeyController Key { get; private set; }
            public CollectionDBSchemaView Parent { get; }

            public WindowsDictionaryColumn(KeyController key, CollectionDBSchemaView parent)
            {
                Key = key;
                Parent = parent;
            }

            protected override FrameworkElement GenerateEditingElement(DataGridCell cell, object dataItem)
            {
                var atb = new ActionTextBox
                {
                    IsSpellCheckEnabled = false
                };
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
                var doc = ((DocumentViewModel)dataItem).DataDocument;
                var textblock = new TextBlock
                {
                    IsDoubleTapEnabled = false
                };
                textblock.DataContextChanged += Textblock_DataContextChanged;
                var binding = new FieldBinding<FieldControllerBase>
                {
                    Document = doc,
                    Key = Key,
                    Converter = new ObjectToStringConverter(),
                    Mode = BindingMode.OneWay,
                    FallbackValue = "<null>"
                };
                textblock.AddFieldBinding(TextBlock.TextProperty, binding);
                return textblock;
                //var contentPresenter = new MyContentPresenter();
                //contentPresenter.SetDocumentAndKey(((DocumentViewModel)dataItem).DataDocument, Key);
                //contentPresenter.IsHitTestVisible = false;
                //return contentPresenter;
            }

            private void Textblock_DataContextChanged(FrameworkElement sender, DataContextChangedEventArgs args)
            {
                var binding = new FieldBinding<FieldControllerBase>
                {
                    Document = (sender.DataContext as DocumentViewModel).DataDocument,
                    Key = Key,
                    Converter = new ObjectToStringConverter(),
                    Mode = BindingMode.OneWay,
                };
                sender.AddFieldBinding(TextBlock.TextProperty, binding);
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

        public class MyContentPresenter : ContentPresenter
        {
            public DocumentController Document { get; private set; }
            public KeyController Key { get; private set; }

            public void SetDocumentAndKey(DocumentController doc, KeyController key)
            {
                if (Document == doc && Key == key)
                {
                    return;
                }

                Document = doc;
                Key = key;
                TableBox.BindContent(this, Document, Key, null);
            }
        }
    }
}
