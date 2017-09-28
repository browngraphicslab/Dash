using Dash.Controllers.Operators;
using Dash.Converters;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

// The User Control item template is documented at https://go.microsoft.com/fwlink/?LinkId=234236

namespace Dash
{
    public sealed partial class CollectionDBSchemaView : SelectionElement, ICollectionView
    {
        public class SchemaField
        {
            public string Name;
            public bool Selected = true;
            public override string ToString()
            {
                return Name;
            }

        }
        bool SchemaFieldContains(string field)
        {
            foreach (var s in SchemaFields)
                if (s.Name == field)
                    return true;
            return false;
        }
        public ObservableCollection<SchemaField> SchemaFields { get; set; } = new ObservableCollection<SchemaField>();
        public BaseCollectionViewModel ViewModel { get; private set; }
        public CollectionDBSchemaView()
        {
            this.InitializeComponent();
            Unloaded += CollectionDBSchemaView_Unloaded;
            Loaded += CollectionDBSchemaView_Loaded;
            MinWidth = MinHeight = 50;
            xGridView.ItemsSource = SchemaFields;
        }

        private void CollectionDBSchemaView_Unloaded(object sender, RoutedEventArgs e)
        {
            DataContextChanged -= CollectionDBView_DataContextChanged;
            if (ParentDocument != null)
                ParentDocument.DocumentFieldUpdated -= ParentDocument_DocumentFieldUpdated;
            ParentDocument = null;
        }
        

        private void CollectionDBSchemaView_Loaded(object sender, RoutedEventArgs e)
        {
            DataContextChanged += CollectionDBView_DataContextChanged;
            ViewModel = DataContext as BaseCollectionViewModel;
            ParentDocument = VisualTreeHelperExtensions.GetFirstAncestorOfType<DocumentView>(this).ViewModel.DocumentController;
            if (ViewModel != null)
                UpdateFields(new Context(ParentDocument));
        }

        private void CollectionDBView_DataContextChanged(FrameworkElement sender, DataContextChangedEventArgs args)
        {
            ViewModel = DataContext as BaseCollectionViewModel;
            ViewModel.OutputKey = DBFilterOperatorFieldModelController.ResultsKey;
            if (ParentDocument != null)
                ParentDocument.DocumentFieldUpdated -= ParentDocument_DocumentFieldUpdated;
            ParentDocument = this.GetFirstAncestorOfType<DocumentView>()?.ViewModel?.DocumentController;
            if (ParentDocument != null)
                UpdateFields(new Context(ParentDocument));
        }
        

        DocumentController _parentDocument;
        public DocumentController ParentDocument
        {
            get { return _parentDocument; }
            set
            {
                _parentDocument = value;
                if (value != null)
                {
                    ParentDocument.DocumentFieldUpdated -= ParentDocument_DocumentFieldUpdated;
                    if (ParentDocument.GetField(DBFilterOperatorFieldModelController.BucketsKey) == null)
                        ParentDocument.SetField(DBFilterOperatorFieldModelController.BucketsKey, new ListFieldModelController<NumberFieldModelController>(new NumberFieldModelController[] {
                                                        new NumberFieldModelController(0), new NumberFieldModelController(0), new NumberFieldModelController(0), new NumberFieldModelController(0)}), true);
                    if (ParentDocument.GetField(DBFilterOperatorFieldModelController.FilterFieldKey) == null)
                        ParentDocument.SetField(DBFilterOperatorFieldModelController.FilterFieldKey, new TextFieldModelController(""), true);
                    if (ParentDocument.GetField(DBFilterOperatorFieldModelController.AutoFitKey) == null)
                        ParentDocument.SetField(DBFilterOperatorFieldModelController.AutoFitKey, new NumberFieldModelController(3), true);
                    if (ParentDocument.GetField(DBFilterOperatorFieldModelController.SelectedKey) == null)
                        ParentDocument.SetField(DBFilterOperatorFieldModelController.SelectedKey, new ListFieldModelController<NumberFieldModelController>(), true);
                    ParentDocument.SetField(DBFilterOperatorFieldModelController.AvgResultKey, new NumberFieldModelController(0), true);
                    ParentDocument.DocumentFieldUpdated += ParentDocument_DocumentFieldUpdated;
                   
                 }
            }
        }


        private void ParentDocument_DocumentFieldUpdated(DocumentController sender, DocumentController.DocumentFieldUpdatedEventArgs args)
        {
            if (args.Reference.FieldKey == ViewModel.CollectionKey ||
                args.Reference.FieldKey == DBFilterOperatorFieldModelController.SelectedKey)
                UpdateFields(new Context(ParentDocument));
        }

        private void TextBlock_Tapped(object sender, TappedRoutedEventArgs e)
        {
            var collection = VisualTreeHelperExtensions.GetFirstAncestorOfType<CollectionView>(this);
            if (collection != null)
            {
                ParentDocument.SetField(DBFilterOperatorFieldModelController.FilterFieldKey, new TextFieldModelController((sender as TextBlock).Text), true);

                collection.SetDBView();
                return;
            }

            var tbock = sender as TextBlock;
            for (int i = 0; i < SchemaFields.Count; i++)
            {
                var s = SchemaFields[i];
                if (s.Name == tbock.Text)
                {
                    s.Selected = !s.Selected;
                    SchemaFields.RemoveAt(i);
                    SchemaFields.Insert(i, s);
                }
            }

            var selectedBars = new List<NumberFieldModelController>();
            for (int i = 0; i < xGridView.Items.Count; i++)
            {
                if ((xGridView.Items[i] as SchemaField).Selected)
                    selectedBars.Add(new NumberFieldModelController(i));
            }

            ParentDocument.SetField(DBFilterOperatorFieldModelController.SelectedKey, new ListFieldModelController<NumberFieldModelController>(selectedBars), true);
        }

        public void UpdateFields(Context context)
        {
            var dbDocs = ParentDocument.GetDereferencedField<DocumentCollectionFieldModelController>(ViewModel.CollectionKey, context)?.Data;
            var selectedBars = ParentDocument.GetDereferencedField<ListFieldModelController<NumberFieldModelController>>(DBFilterOperatorFieldModelController.SelectedKey, context)?.Data;
            if (dbDocs != null)
            {
                foreach (var d in dbDocs)
                {
                    foreach (var f in d.EnumFields())
                        if (!f.Key.Name.StartsWith("_") && !SchemaFieldContains(f.Key.Name))
                            SchemaFields.Add(new SchemaField() { Name = f.Key.Name, Selected = false } );
                }
                filterDocuments(dbDocs, selectedBars.Select((b) => SchemaFields[(int)(b as NumberFieldModelController).Data].Name).ToList());
            }
        }

        public void filterDocuments(List<DocumentController> dbDocs, List<string> selectedBars)
        {
            bool keepAll = selectedBars.Count == 0;

            var collection = new List<DocumentController>();
            
            foreach (var dmc in dbDocs.ToArray())
            {
                var visited = new List<DocumentController>();
                visited.Add(dmc);

                if (SearchInDocumentForNamedField(dmc, selectedBars, visited))
                    collection.Add(dmc);
            }
            ParentDocument.SetField(DBFilterOperatorFieldModelController.ResultsKey, new DocumentCollectionFieldModelController(collection), true);
        }

        private static bool SearchInDocumentForNamedField(DocumentController dmc, List<string> selectedBars, List<DocumentController> visited)
        {
            if (dmc == null)
                return false;
            // loop through each field to find on that matches the field name pattern 
            foreach (var pfield in dmc.EnumFields().Where((pf) => selectedBars.Contains(pf.Key.Name) || pf.Value is DocumentFieldModelController))
            {
                if (pfield.Value is DocumentFieldModelController)
                {
                    var nestedDoc = (pfield.Value as DocumentFieldModelController).Data;
                    if (!visited.Contains(nestedDoc))
                    {
                        visited.Add(nestedDoc);
                        var field = SearchInDocumentForNamedField(nestedDoc, selectedBars, visited);
                        if (field )
                            return true;
                    }
                }
                else 
                {
                    return true;
                }
            }
            return false;
        }
        #region ItemSelection

        public void ToggleSelectAllItems()
        {
        }

        #endregion

        #region DragAndDrop


        private void CollectionViewOnDragEnter(object sender, DragEventArgs e)
        {
            ViewModel.CollectionViewOnDragEnter(sender, e);
        }

        private void CollectionViewOnDrop(object sender, DragEventArgs e)
        {
            ViewModel.CollectionViewOnDrop(sender, e);
        }

        private void CollectionViewOnDragLeave(object sender, DragEventArgs e)
        {
            ViewModel.CollectionViewOnDragLeave(sender, e);
        }

        public void SetDropIndicationFill(Brush fill)
        {
        }
        #endregion

        #region Activation

        protected override void OnActivated(bool isSelected)
        {
            ViewModel.SetSelected(this, isSelected);
        }

        protected override void OnLowestActivated(bool isLowestSelected)
        {
            ViewModel.SetLowestSelected(this, isLowestSelected);
        }
        private void OnTapped(object sender, TappedRoutedEventArgs e)
        {
            e.Handled = true;
            if (ViewModel.IsInterfaceBuilder)
                return;
            OnSelected();
        }
        #endregion
    }
}