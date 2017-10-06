using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Dash.Controllers.Operators;
using Dash.Views;

// The User Control item template is documented at https://go.microsoft.com/fwlink/?LinkId=234236

namespace Dash
{
    public sealed partial class CollectionDBSchemaView : SelectionElement, ICollectionView
    {
        private DocumentController _parentDocument;

        public CollectionDBSchemaView()
        {
            this.InitializeComponent();
            Unloaded += CollectionDBSchemaView_Unloaded;
            Loaded += CollectionDBSchemaView_Loaded;
            MinWidth = MinHeight = 50;
            xGridView.ItemsSource = SchemaHeaders;
        }

        public ObservableCollection<CollectionDBSchemaRecordViewModel> Records { get; set; } =
            new ObservableCollection<CollectionDBSchemaRecordViewModel>();

        public ObservableCollection<CollectionDBSchemaHeader.HeaderViewModel> SchemaHeaders { get; set; } =
            new ObservableCollection<CollectionDBSchemaHeader.HeaderViewModel>();

        public DocumentController ParentDocument
        {
            get => _parentDocument;
            set
            {
                _parentDocument = value;
                if (value != null)
                {
                    ParentDocument.DocumentFieldUpdated -= ParentDocument_DocumentFieldUpdated;
                    if (ParentDocument.GetField(DBFilterOperatorFieldModelController.BucketsKey) == null)
                        ParentDocument.SetField(DBFilterOperatorFieldModelController.BucketsKey,
                            new ListFieldModelController<NumberFieldModelController>(new[]
                            {
                                new NumberFieldModelController(0), new NumberFieldModelController(0),
                                new NumberFieldModelController(0), new NumberFieldModelController(0)
                            }), true);
                    if (ParentDocument.GetField(DBFilterOperatorFieldModelController.FilterFieldKey) == null)
                        ParentDocument.SetField(DBFilterOperatorFieldModelController.FilterFieldKey,
                            new TextFieldModelController(""), true);
                    if (ParentDocument.GetField(DBFilterOperatorFieldModelController.AutoFitKey) == null)
                        ParentDocument.SetField(DBFilterOperatorFieldModelController.AutoFitKey,
                            new NumberFieldModelController(3), true);
                    if (ParentDocument.GetField(DBFilterOperatorFieldModelController.SelectedKey) == null)
                        ParentDocument.SetField(DBFilterOperatorFieldModelController.SelectedKey,
                            new ListFieldModelController<NumberFieldModelController>(), true);
                    ParentDocument.SetField(DBFilterOperatorFieldModelController.AvgResultKey,
                        new NumberFieldModelController(0), true);
                    ParentDocument.DocumentFieldUpdated += ParentDocument_DocumentFieldUpdated;
                }
            }
        }

        public BaseCollectionViewModel ViewModel { get; private set; }

        #region ItemSelection

        public void ToggleSelectAllItems()
        {
        }

        #endregion

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
            ParentDocument = this.GetFirstAncestorOfType<DocumentView>().ViewModel.DocumentController;
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


        private void ParentDocument_DocumentFieldUpdated(DocumentController sender,
            DocumentController.DocumentFieldUpdatedEventArgs args)
        {
            if (args.Reference.FieldKey == ViewModel.CollectionKey ||
                args.Reference.FieldKey == DBFilterOperatorFieldModelController.SelectedKey)
                UpdateFields(new Context(ParentDocument));
        }

        private bool SchemaHeadersContains(string field)
        {
            foreach (var s in SchemaHeaders)
                if (s.Key.Name == field)
                    return true;
            return false;
        }

        /// <summary>
        ///     Updates all the fields in the schema view
        /// </summary>
        /// <param name="context"></param>
        public void UpdateFields(Context context)
        {
            var dbDocs = ParentDocument
                .GetDereferencedField<DocumentCollectionFieldModelController>(ViewModel.CollectionKey, context)?.Data;
            var selectedBars = ParentDocument
                .GetDereferencedField<ListFieldModelController<NumberFieldModelController>>(
                    DBFilterOperatorFieldModelController.SelectedKey, context)?.Data;
            if (dbDocs != null)
            {
                // for each document we add any header we find with a name not matching a current name. This is the UNION of all fields *assuming no collisions
                foreach (var d in dbDocs)
                foreach (var f in d.EnumFields())
                    if (!f.Key.Name.StartsWith("_") && !SchemaHeadersContains(f.Key.Name))
                        SchemaHeaders.Add(new CollectionDBSchemaHeader.HeaderViewModel
                        {
                            SchemaDocument = ParentDocument,
                            Width = 70,
                            Key = f.Key,
                            Selected = false
                        });
                // remove possible infinite loops
                filterDocuments(dbDocs,
                    selectedBars.Select(b => SchemaHeaders[(int) (b as NumberFieldModelController).Data].Key.Name)
                        .ToList());

                // add all the records
                var records = new List<CollectionDBSchemaRecordViewModel>();
                foreach (var document in dbDocs)
                    records.Add(new CollectionDBSchemaRecordViewModel(
                        document,
                        SchemaHeaders.Select(headerViewModel => new CollectionDBSchemaRecordFieldViewModel(
                            headerViewModel.Width + HeaderBorderThickness.BorderThickness.Left + HeaderBorderThickness.BorderThickness.Right, 
                            document, headerViewModel.Key,
                            HeaderBorderThickness.BorderThickness))
                    ));
                xRecordsView.ItemsSource = new ObservableCollection<CollectionDBSchemaRecordViewModel>(records);
            }
        }

        /// <summary>
        ///     removes any documents which would lead to infinite loops from dbDocs
        /// </summary>
        /// <param name="dbDocs"></param>
        /// <param name="selectedBars"></param>
        public void filterDocuments(List<DocumentController> dbDocs, List<string> selectedBars)
        {
            var keepAll = selectedBars.Count == 0;

            var collection = new List<DocumentController>();

            foreach (var dmc in dbDocs.ToArray())
            {
                var visited = new List<DocumentController>();
                visited.Add(dmc);

                if (SearchInDocumentForNamedField(dmc, selectedBars, visited))
                    collection.Add(dmc);
            }
            ParentDocument.SetField(DBFilterOperatorFieldModelController.ResultsKey,
                new DocumentCollectionFieldModelController(collection), true);
        }

        private static bool SearchInDocumentForNamedField(DocumentController dmc, List<string> selectedBars,
            List<DocumentController> visited)
        {
            if (dmc == null)
                return false;
            // loop through each field to find on that matches the field name pattern 
            foreach (var pfield in dmc.EnumFields()
                .Where(pf => selectedBars.Contains(pf.Key.Name) || pf.Value is DocumentFieldModelController))
                if (pfield.Value is DocumentFieldModelController)
                {
                    var nestedDoc = (pfield.Value as DocumentFieldModelController).Data;
                    if (!visited.Contains(nestedDoc))
                    {
                        visited.Add(nestedDoc);
                        var field = SearchInDocumentForNamedField(nestedDoc, selectedBars, visited);
                        if (field)
                            return true;
                    }
                }
                else
                {
                    return true;
                }
            return false;
        }

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