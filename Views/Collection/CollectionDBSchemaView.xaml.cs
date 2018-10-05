using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Windows.System;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Telerik.UI.Xaml.Controls.Grid;
using Telerik.UI.Xaml.Controls.Grid.Primitives;
using Windows.UI.Xaml.Data;
using Microsoft.Toolkit.Uwp.UI.Controls;

// The User Control item template is documented at https://go.microsoft.com/fwlink/?LinkId=234236

namespace Dash
{
    public sealed partial class CollectionDBSchemaView : ICollectionView
    {
        public CollectionViewModel ViewModel => DataContext as CollectionViewModel;
        public DataGrid DataGrid => xDataGrid;

        //ICollectionView implementation
        public void SetDropIndicationFill(Brush fill)
        {
        }

        public UserControl UserControl => this;

        public HashSet<KeyController> Keys { get; } = new HashSet<KeyController>();

        public CollectionDBSchemaView()
        {
            InitializeComponent();
            DataContextChanged += OnDataContextChanged;

            xDataGrid.AutoGenerateColumns = false;
            xDataGrid.CanUserSortColumns = true;
            xDataGrid.CanUserResizeColumns = true;
            xDataGrid.CanUserReorderColumns = true;
            xDataGrid.ColumnWidth = new DataGridLength(200);
            xDataGrid.GridLinesVisibility = DataGridGridLinesVisibility.All;
            xDataGrid.CellEditEnding += XDataGridOnCellEditEnding;

            Loaded += OnLoaded;
        }

        private void OnLoaded(object sender, RoutedEventArgs routedEventArgs)
        {
            if (ViewModel != null)
            {
                xDataGrid.UpdateLayout();
                xDataGrid.ItemsSource = ViewModel.BindableDocumentViewModels;
                AddKeys();
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
            catch (DSLException e) { }
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
                var doc = ((DocumentViewModel)box.DataContext).DataDocument;
                var col = (WindowsDictionaryColumn)args.Column;
                using (UndoManager.GetBatchHandle())
                {
                    await CommitEdit(box.Text, doc, col.Key, args.Row.GetIndex());//TODO This index might be wrong with sorting/filtering, etc.
                }
            }
        }

        private void AddRow_OnTapped(object sender, TappedRoutedEventArgs e)
        {
            // Add a new document to the schema view
            ViewModel.AddDocument(Util.BlankNote());
            e.Handled = true;
        }

        private void AddColumn_OnTapped(object sender, TappedRoutedEventArgs e)
        {
            AddKey(new KeyController("New Column"));

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
                foreach (var doc in _oldViewModel.DocumentViewModels)
                {
                    doc.DataDocument.FieldModelUpdated -= DataDocumentOnFieldModelUpdated;
                }
                _oldViewModel.DocumentViewModels.CollectionChanged -= DocumentViewModelsOnCollectionChanged;
            }

            _oldViewModel = ViewModel;

            if (ViewModel == null)
            {
                return;
            }

            ViewModel.DocumentViewModels.CollectionChanged += DocumentViewModelsOnCollectionChanged;

            //Keys.Clear();
            //xDataGrid.Columns.Clear();
            //var docs = ViewModel.DocumentViewModels;
            //var keys = new HashSet<KeyController>();
            //foreach (var doc in docs)
            //{
            //    foreach (var field in doc.DataDocument.EnumDisplayableFields())
            //    {
            //        keys.Add(field.Key);
            //    }
            //    doc.DataDocument.FieldModelUpdated += DataDocumentOnFieldModelUpdated;
            //}

            //foreach (var key in keys)
            //{
            //    AddKey(key);
            //}

            //if (ViewModel.IsLoaded && xDataGrid.ItemsSource == null)
            //{
            //    xDataGrid.ItemsSource = ViewModel.BindableDocumentViewModels;
            //}
        }

        private void DataDocumentOnFieldModelUpdated(FieldControllerBase sender, FieldUpdatedEventArgs args, Context context)
        {
            if (args is DocumentController.DocumentFieldUpdatedEventArgs dargs)
            {
                AddKey(dargs.Reference.FieldKey);
            }
        }

        private void AddKey(KeyController key)
        {
            if (Keys.Contains(key))
            {
                return;
            }

            Keys.Add(key);

            var column = new WindowsDictionaryColumn(key, this)
            {
                Header = key,
            };

            xDataGrid.Columns.Add(column);
        }

        private void DocumentViewModelsOnCollectionChanged(object sender, NotifyCollectionChangedEventArgs args)
        {
            switch (args.Action)
            {
            case NotifyCollectionChangedAction.Add:
                foreach (var argsNewItem in args.NewItems)
                {
                    var documentViewModel = (DocumentViewModel)argsNewItem;
                    var dataDoc = documentViewModel.DataDocument;
                    dataDoc.FieldModelUpdated += DataDocumentOnFieldModelUpdated;
                    foreach (var field in documentViewModel.DataDocument.EnumDisplayableFields())
                    {
                        AddKey(field.Key);
                    }
                }

                break;
            case NotifyCollectionChangedAction.Remove:
                foreach (var argsNewItem in args.OldItems)
                {
                    ((DocumentViewModel)argsNewItem).DataDocument.FieldModelUpdated -= DataDocumentOnFieldModelUpdated;
                }
                break;
            default:
                throw new ArgumentOutOfRangeException();
            }
        }

        private void Flyout_OnTapped(object sender, TappedRoutedEventArgs e)
        {

        }

        private void ColumnVisibility_Changed(object sender, RoutedEventArgs e)
        {
            var checkBox = sender as CheckBox;
            var key = checkBox.DataContext as KeyController;
            if ((bool)checkBox.IsChecked)
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
            if (!Keys.Contains(key))
            {
                return;
            }

            Keys.Remove(key);

            var column = xDataGrid.Columns.First(col => col.Header.Equals(key));
            xDataGrid.Columns.Remove(column);
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

            xColumnsList.ItemsSource = keys;
        }

        private void CheckBox_OnLoaded(object sender, RoutedEventArgs e)
        {
            if ((sender as CheckBox).DataContext != null)
            {
                var checkBox = sender as CheckBox;
                var key = checkBox.DataContext as KeyController;
                if (Keys.Contains(key))
                {
                    checkBox.IsChecked = true;
                }
                else
                {
                    checkBox.IsChecked = false;
                }

                checkBox.GetFirstAncestorOfType<Grid>().GetFirstDescendantOfType<TextBlock>().Tapped += (s, args) =>
                    {
                        checkBox.IsChecked = !(bool)checkBox.IsChecked;
                    };

                checkBox.Checked += ColumnVisibility_Changed;
                checkBox.Unchecked += ColumnVisibility_Changed;
            }
        }

        private void AddKeys()
        {
            Keys.Clear();
            xDataGrid.Columns.Clear();
            var docs = ViewModel.DocumentViewModels;
            var keys = new HashSet<KeyController>();
            foreach (var doc in docs)
            {
                foreach (var field in doc.DataDocument.EnumDisplayableFields())
                {
                    keys.Add(field.Key);
                }
                doc.DataDocument.FieldModelUpdated += DataDocumentOnFieldModelUpdated;
            }

            foreach (var key in keys)
            {
                AddKey(key);
            }

            if (ViewModel.IsLoaded && xDataGrid.ItemsSource == null)
            {
                xDataGrid.ItemsSource = ViewModel.BindableDocumentViewModels;
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

            var thisDoc = (DocumentController)item;//This should be data doc I think
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

        protected override object PrepareCellForEdit(FrameworkElement editingElement, RoutedEventArgs editingEventArgs)
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
