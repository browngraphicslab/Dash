using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Diagnostics;
using Windows.System;
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

        //ICollectionView implementation
        public void SetDropIndicationFill(Brush fill)
        {
        }

        public UserControl UserControl => this;

        private HashSet<KeyController> Keys { get; } = new HashSet<KeyController>();

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
            }
        }

        private void XDataGridOnCellEditEnding(object sender, DataGridCellEditEndingEventArgs args)
        {
            if (args.EditAction == DataGridEditAction.Commit)
            {
                var box = (TextBox)args.EditingElement;
                var doc = ((DocumentViewModel)box.DataContext).DataDocument;
                var col = (WindowsDictionaryColumn)args.Column;
                using (UndoManager.GetBatchHandle())
                {
                    try
                    {
                        var field = DSL.InterpretUserInput(box.Text, scope: Scope.CreateStateWithThisDocument(doc));
                        doc.SetField(col.Key, field, true);
                    }
                    catch (DSLException e)
                    {
                    }
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

            var column = new WindowsDictionaryColumn(key)
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

        public WindowsDictionaryColumn(KeyController key)
        {
            Key = key;
        }

        protected override FrameworkElement GenerateEditingElement(DataGridCell cell, object dataItem)
        {
            return new TextBox();
        }

        protected override FrameworkElement GenerateElement(DataGridCell cell, object dataItem)
        {
            var doc = ((DocumentViewModel)dataItem).DataDocument;
            var textblock = new TextBlock();
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

        protected override object PrepareCellForEdit(FrameworkElement editingElement, RoutedEventArgs editingEventArgs)
        {
            var tb = (TextBox) editingElement;

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
            DataBox.BindContent(this, new DataBox(new DocumentReferenceController(Document, Key)).Document, null);
        }
    }
}
