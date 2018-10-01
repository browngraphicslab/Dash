using DashShared;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using Windows.ApplicationModel.DataTransfer;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;
using static Dash.CollectionDBSchemaHeader;
using Windows.UI.Xaml.Media;
using Telerik.UI.Xaml.Controls.Grid;
using Telerik.UI.Xaml.Controls.Grid.Primitives;
using DataGridColumn = Telerik.UI.Xaml.Controls.Grid.DataGridColumn;
using DataGridTextColumn = Telerik.UI.Xaml.Controls.Grid.DataGridTextColumn;
using Telerik.Data.Core;
using Telerik.UI.Xaml.Controls.Grid.Commands;
using DataGridSelectionMode = Telerik.UI.Xaml.Controls.Grid.DataGridSelectionMode;
using DataGridTemplateColumn = Telerik.UI.Xaml.Controls.Grid.DataGridTemplateColumn;
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

        public CollectionDBSchemaView()
        {
            InitializeComponent();
            DataContextChanged += OnDataContextChanged;
        }

        private void AddRow_OnTapped(object sender, TappedRoutedEventArgs e)
        {
            // Add a new document to the schema view
            ViewModel.AddDocument(Util.BlankNote());
            e.Handled = true;
        }

        private void AddColumn_OnTapped(object sender, TappedRoutedEventArgs e)
        {
            xDataGrid.Columns.Add(new WindowsDictionaryColumn(new KeyController("New Column")));

            e.Handled = true;
        }

        private CollectionViewModel _oldViewModel;
        private void OnDataContextChanged(FrameworkElement sender, DataContextChangedEventArgs args)
        {
            if (ViewModel == _oldViewModel)
            {
                return;
            }

            _oldViewModel = ViewModel;

            if (ViewModel == null)
            {
                return;
            }
            var docs = ViewModel.ContainerDocument.GetDereferencedField<ListController<DocumentController>>(KeyStore.DataKey, null);
            var keys = new HashSet<KeyController>();
            foreach (var doc in docs)
            {
                foreach (var field in doc.GetDataDocument().EnumDisplayableFields())
                {
                    keys.Add(field.Key);
                }
            }

            xDataGrid.AutoGenerateColumns = false;
            //xDataGrid.UserEditMode = DataGridUserEditMode.Inline;
            //xDataGrid.SelectionUnit = DataGridSelectionUnit.Cell;
            //xDataGrid.ColumnResizeHandleDisplayMode = DataGridColumnResizeHandleDisplayMode.Always;

            foreach (var key in keys)
            {
                var column = new WindowsDictionaryColumn(key)
                {
                    Header = key,
                    CanUserResize = true,
                    CanUserReorder = true,
                    CanUserSort = true,
                    Width = new DataGridLength(200)
                };

                xDataGrid.Columns.Add(column);
            }

            xDataGrid.ItemsSource = docs;
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
            var contentPresenter = new MyContentPresenter();
            contentPresenter.SetDocumentAndKey(((DocumentController)dataItem).GetDataDocument(), Key);
            contentPresenter.IsHitTestVisible = false;
            return contentPresenter;
        }

        protected override object PrepareCellForEdit(FrameworkElement editingElement, RoutedEventArgs editingEventArgs)
        {
            var tb = (TextBox) editingElement;

            var doc = (DocumentController)editingElement.DataContext;
            var dataDoc = doc.GetDataDocument();

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
