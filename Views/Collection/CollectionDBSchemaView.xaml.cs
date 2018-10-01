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
            xDataGrid.Columns.Add(new DataGridDictionaryColumn(new KeyController("New Column")));

            e.Handled = true;
        }

        private void OnDataContextChanged(FrameworkElement sender, DataContextChangedEventArgs args)
        {
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
            xDataGrid.UserEditMode = DataGridUserEditMode.Inline;
            xDataGrid.SelectionUnit = DataGridSelectionUnit.Cell;
            xDataGrid.ColumnResizeHandleDisplayMode = DataGridColumnResizeHandleDisplayMode.Always;

            foreach (var key in keys)
            {
                var column = new DataGridDictionaryColumn(key)
                {
                    Header = key,
                    CanUserEdit = true,
                    CanUserResize = true,
                    SizeMode = DataGridColumnSizeMode.Fixed,
                    Width = 200
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

            var thisDoc = (DocumentController)item;
            var cp = (MyContentPresenter)container;
            cp.SetDocumentAndKey(thisDoc, Key);
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
            DataBox.BindContent(this, Document, Key, null);
        }
    }
}
