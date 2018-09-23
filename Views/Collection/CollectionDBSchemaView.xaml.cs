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
        private DocumentController _parentDocument;

        private Dictionary<KeyController, HashSet<TypeInfo>> _typedHeaders;

        /// <summary>
        ///     Each element in the list renders a column in the records list view
        /// </summary>
        private ObservableCollection<CollectionDBSchemaColumnViewModel> ColumnViewModels { get; }

        /// <summary>
        ///     The observable collection of all documents displayed in the schema, a pointer to this is held by every
        ///     columnviewmodel so be very careful about changing it (i.e. don't add a setter)
        /// </summary>
        private ObservableCollection<DocumentController> CollectionDocuments { get; }

        ////bcz: this field isn't used, but if it's not here Field items won't be updated when they're changed.  Why???????
        //public ObservableCollection<CollectionDBSchemaRecordViewModel> Records { get; set; } =
        //    new ObservableCollection<CollectionDBSchemaRecordViewModel>();

        public ObservableCollection<HeaderViewModel> SchemaHeaders { get; }

        public CollectionViewModel ViewModel { get; set; }

        public DocumentController ParentDocument
        {
            get => _parentDocument;
            set
            {
                _parentDocument = value;
                if (value != null)
                    if (ParentDocument.GetField(CollectionDBView.FilterFieldKey) == null)
                        ParentDocument.SetField(CollectionDBView.FilterFieldKey, new KeyController(), true);
            }
        }

        public UserControl UserControl => this;

        public CollectionDBSchemaView()
        {
            InitializeComponent();
            //Warning: code does not work yet -Brandon
            ParentDocument = this.GetFirstAncestorOfType<DocumentView>()?.ViewModel?.DocumentController;
            var docs = ParentDocument.GetDataDocument().
                GetDereferencedField<ListController<DocumentController>>(ViewModel.CollectionKey, null)?.TypedData;
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
                    SizeMode = DataGridColumnSizeMode.Auto
                };

                xDataGrid.Columns.Add(column);
            }

            xDataGrid.ItemsSource = docs;
        }

        private void AddRow_OnTapped(object sender, TappedRoutedEventArgs e)
        {
            // Add a new document to the schema view
            ViewModel.AddDocument(Util.BlankNote());
            e.Handled = true;
        }

        private void AddColumn_OnTapped(object sender, TappedRoutedEventArgs e)
        {
            var newHvm = new HeaderViewModel
            {
                SchemaView = this,
                SchemaDocument = ParentDocument,
                Width = 150,
                FieldKey = new KeyController("New Field", Guid.NewGuid().ToString())
            };
            SchemaHeaders.Add(newHvm);

            //var cvm = new CollectionDBSchemaColumnViewModel(newHvm.FieldKey, CollectionDocuments, newHvm);
            //ColumnViewModels.Add(cvm);
            //cvm.PropertyChanged += Cvm_PropertyChanged;

            e.Handled = true;
        }

        public void SetDropIndicationFill(Brush fill)
        {
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
            var tb = new TextBox();
            return tb;
        }

        public override object CreateContainer(object rowItem)
        {
            var contentPresenter = new ContentPresenter();
            return contentPresenter;
        }

        public override object GetContainerType(object rowItem)
        {
            return typeof(ContentPresenter);
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
            editorContent.DataContext = binding.Source;
        }

        public override void ClearEditorContentVisual(FrameworkElement editorContent)
        {
            editorContent.ClearValue(TextBox.TextProperty);
        }

        //public override object GetValueForInstance(object instance)
        //{
        //    //return null;
        //    return ((Customer)instance).Params.TryGetValue(Key, out var value) ? value : "<null>";
        //}

        public override void PrepareCell(object container, object value, object item)
        {
            base.PrepareCell(container, value, item); //Scrap in favor of Databox.Makeview
            var contentPresenter = (ContentPresenter)container;
            switch (value)
            {
            case string s:
                contentPresenter.Content = new TextBlock { Text = s };
                break;
            case bool b:
                CheckBox checkBox = new CheckBox
                {
                    IsChecked = b,
                };
                contentPresenter.Content = checkBox;
                contentPresenter.HorizontalAlignment = HorizontalAlignment.Center;
                break;
            default:
                contentPresenter.Content = new TextBlock { Text = "Unrecognized data type" };
                break;
            }
        }
    }
}
