using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Dash.Controllers.Operators;
using Dash.Views;
using Windows.System;
using Windows.UI.Core;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using DashShared;
using System.Diagnostics;

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
            xHeaderView.ItemsSource = SchemaHeaders;
            Loaded   += CollectionDBSchemaView_Loaded1;
            Unloaded += CollectionDBSchemaView_Unloaded1;
            xEditTextBox.AddHandler(KeyDownEvent, new KeyEventHandler( xEditTextBox_KeyDown), true);
        }

        private void SchemaHeaders_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            var recordSource = xRecordsView.ItemsSource as ObservableCollection<CollectionDBSchemaRecordViewModel>;
            var slist = SchemaHeaders.ToArray().ToList();
            foreach (var r in recordSource)
                if (r.RecordFields.Count == slist.Count)
                {
                    UpdateRecords((xRecordsView.ItemsSource as ObservableCollection<CollectionDBSchemaRecordViewModel>).ToArray().Select((xr)=> xr.Document));
                    var stuff = new ListFieldModelController<TextFieldModelController>();
                    foreach (var s in SchemaHeaders)
                        stuff.Add(new TextFieldModelController(s.FieldKey.Id));
                    ParentDocument.SetField(HeaderListKey, stuff, true);
                    break;
                }
            CollectionDBSchemaHeader.DragModel = null;
        }

        //bcz: this field isn't used, but if it's not here Field items won't be updated when they're changed.  Why???????
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
                    if (_parentDocument.GetField(KeyStore.DocumentContextKey) != null)
                    {
                        _parentDocument = _parentDocument.GetDereferencedField<DocumentFieldModelController>(KeyStore.DocumentContextKey, null).Data;
                    }
                    ParentDocument.DocumentFieldUpdated -= ParentDocument_DocumentFieldUpdated;
                    if (ParentDocument.GetField(DBFilterOperatorFieldModelController.FilterFieldKey) == null)
                        ParentDocument.SetField(DBFilterOperatorFieldModelController.FilterFieldKey,
                            new TextFieldModelController(""), true);
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

        private void CollectionDBSchemaView_Unloaded1(object sender, RoutedEventArgs e)
        {
            CollectionDBSchemaRecordField.FieldTappedEvent -= CollectionDBSchemaRecordField_FieldTappedEvent;
        }

        private void CollectionDBSchemaView_Loaded1(object sender, RoutedEventArgs e)
        {
            CollectionDBSchemaRecordField.FieldTappedEvent += CollectionDBSchemaRecordField_FieldTappedEvent;
        }

        private void CollectionDBSchemaRecordField_FieldTappedEvent(CollectionDBSchemaRecordField fieldView)
        {
            var dc = fieldView.DataContext as CollectionDBSchemaRecordFieldViewModel;
            var column = (xRecordsView.Items[dc.Row] as CollectionDBSchemaRecordViewModel).RecordFields.IndexOf(dc);
            if (column != -1)
            {
                FlyoutBase.SetAttachedFlyout(fieldView, xEditField);
                updateEditBox(dc);
                xEditField.ShowAt(this);
            }
        }

        private void updateEditBox(CollectionDBSchemaRecordFieldViewModel dc)
        {
            xEditTextBox.Tag = dc;
            var field = dc.Document.GetDereferencedField(dc.HeaderViewModel.FieldKey, null);
            xEditTextBox.Text = field?.GetValue(null)?.ToString() ?? "<null>";
            dc.Selected = true;
            xEditTextBox.SelectAll();
        }
        
        private void xEditTextBox_KeyDown(object sender, KeyRoutedEventArgs e)
        {
            if (e.Key == Windows.System.VirtualKey.Enter)
            {
                if (xEditTextBox.Text == "\r")
                {
                    var dc = xEditTextBox.Tag as CollectionDBSchemaRecordFieldViewModel;
                    var field = dc.Document.GetDereferencedField(dc.HeaderViewModel.FieldKey, null);
                    xEditTextBox.Text = field?.GetValue(null)?.ToString() ?? "<null>";
                    dc.Selected = false;
                    var direction = (Window.Current.CoreWindow.GetKeyState(VirtualKey.Shift).HasFlag(CoreVirtualKeyStates.Down)) ? -1 : 1;
                    var column = (xRecordsView.Items[dc.Row] as CollectionDBSchemaRecordViewModel).RecordFields.IndexOf(dc);
                    var recordViewModel = xRecordsView.Items[Math.Max(0, Math.Min(xRecordsView.Items.Count - 1, dc.Row + direction))] as CollectionDBSchemaRecordViewModel;
                    updateEditBox(recordViewModel.RecordFields[column]); 
                }
                e.Handled = true;
            }   
            if (e.Key == Windows.System.VirtualKey.Tab)
            {
                e.Handled = true;
            }
        }

        private void xEditTextBox_KeyUp(object sender, KeyRoutedEventArgs e)
        {
            var direction = (Window.Current.CoreWindow.GetKeyState(VirtualKey.Shift).HasFlag(CoreVirtualKeyStates.Down)) ? -1 : 1;
            if (e.Key == Windows.System.VirtualKey.Down || e.Key == Windows.System.VirtualKey.Up)
            {
                direction = e.Key == Windows.System.VirtualKey.Down ? 1 : e.Key == Windows.System.VirtualKey.Up ? -1 : direction;
                var dc = xEditTextBox.Tag as CollectionDBSchemaRecordFieldViewModel;
                SetFieldValue(dc);
                var column = (xRecordsView.Items[dc.Row] as CollectionDBSchemaRecordViewModel).RecordFields.IndexOf(dc);
                var recordViewModel = xRecordsView.Items[Math.Max(0, Math.Min(xRecordsView.Items.Count - 1, dc.Row + direction))] as CollectionDBSchemaRecordViewModel;
                updateEditBox(recordViewModel.RecordFields[column]);
            }

            if (e.Key == Windows.System.VirtualKey.Tab || e.Key == Windows.System.VirtualKey.Right || e.Key == Windows.System.VirtualKey.Left)
            {
                direction = e.Key == Windows.System.VirtualKey.Right ? 1 : e.Key == Windows.System.VirtualKey.Left ? -1 : direction;
                var dc = xEditTextBox.Tag as CollectionDBSchemaRecordFieldViewModel;
                SetFieldValue(dc);
                var column = (xRecordsView.Items[dc.Row] as CollectionDBSchemaRecordViewModel).RecordFields.IndexOf(dc);
                var recordViewModel = xRecordsView.Items[dc.Row] as CollectionDBSchemaRecordViewModel;
                updateEditBox(recordViewModel.RecordFields[Math.Max(0,Math.Min(recordViewModel.RecordFields.Count - 1, column + direction))]);
            }
            e.Handled = true;
        }


        private void xEditField_Closing(FlyoutBase sender, FlyoutBaseClosingEventArgs args)
        {
            var dc = xEditTextBox.Tag as CollectionDBSchemaRecordFieldViewModel;
            SetFieldValue(dc);
        }

        private void SetFieldValue(CollectionDBSchemaRecordFieldViewModel dc)
        {
            dc.Document.ParseDocField(dc.HeaderViewModel.FieldKey, xEditTextBox.Text, dc.Document.GetDereferencedField(dc.HeaderViewModel.FieldKey, new Context(dc.Document)));
            dc.DataReference = new DocumentReferenceFieldController(dc.Document.GetId(), dc.HeaderViewModel.FieldKey);
            dc.Selected = false;
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
            ParentDocument = this.GetFirstAncestorOfType<DocumentView>().ViewModel.DocumentController;
            if (ViewModel != null)
                UpdateFields(new Context(ParentDocument));
        }

        private void CollectionDBView_DataContextChanged(FrameworkElement sender, DataContextChangedEventArgs args)
        {
            ViewModel = DataContext as BaseCollectionViewModel;
            ViewModel.OutputKey = KeyStore.CollectionOutputKey;
            if (ParentDocument != null)
                ParentDocument.DocumentFieldUpdated -= ParentDocument_DocumentFieldUpdated;
            ParentDocument = this.GetFirstAncestorOfType<DocumentView>()?.ViewModel?.DocumentController;
            if (ParentDocument != null)
                UpdateFields(new Context(ParentDocument));
        }


        private void ParentDocument_DocumentFieldUpdated(DocumentController sender,
            DocumentController.DocumentFieldUpdatedEventArgs args)
        {
            if (args.Reference.FieldKey.Equals(ViewModel.CollectionKey))
                UpdateFields(new Context(ParentDocument));
        }

        /// </summary>
        public static KeyController HeaderListKey = new KeyController("7C3F0C3F-F065-4094-8802-F572B35C4D42", "HeaderList");
        private bool SchemaHeadersContains(KeyController field)
        {
            foreach (var s in SchemaHeaders)
                if (s.FieldKey.Equals(field))
                    return true;
            return false;
        }

        KeyController _lastFieldSortKey = null;
        public void Sort(CollectionDBSchemaHeader.HeaderViewModel viewModel)
        {
            var dbDocs = ParentDocument
                   .GetDereferencedField<DocumentCollectionFieldModelController>(ViewModel.CollectionKey, null)?.Data?.Select((d) => d.GetDereferencedField<DocumentFieldModelController>(KeyStore.DocumentContextKey, null)?.Data ?? d);

            var records = new SortedList<string, DocumentController>();
            foreach (var d in dbDocs)
            {
                var str = d.GetDereferencedField(viewModel.FieldKey, null)?.GetValue(new Context(d))?.ToString() ?? "{}";
                if (records.ContainsKey(str))
                    records.Add(str + Guid.NewGuid(), d);
                else records.Add(str, d);
            }
            if (_lastFieldSortKey != null && _lastFieldSortKey.Equals(viewModel.FieldKey))
                UpdateRecords(records.Select((r) => r.Value).Reverse());
            else UpdateRecords(records.Select((r) => r.Value));
            _lastFieldSortKey = viewModel.FieldKey;
        }
        /// <summary>
        ///     Updates all the fields in the schema view
        /// </summary>
        /// <param name="context"></param>
        public void UpdateFields(Context context)
        {
            var dbDocs = ParentDocument
                .GetDereferencedField<DocumentCollectionFieldModelController>(ViewModel.CollectionKey, context)?.Data?.Select((d) => d.GetDereferencedField<DocumentFieldModelController>(KeyStore.DocumentContextKey, null)?.Data ?? d);
            var headerList = ParentDocument
                .GetDereferencedField<ListFieldModelController<TextFieldModelController>>(HeaderListKey, context)?.Data ?? new List<FieldControllerBase>();
            if (dbDocs != null)
            {
                SchemaHeaders.CollectionChanged -= SchemaHeaders_CollectionChanged;
                SchemaHeaders.Clear();
                foreach (var h in headerList)
                { 
                    SchemaHeaders.Add(new CollectionDBSchemaHeader.HeaderViewModel() { SchemaView = this, SchemaDocument = ParentDocument, Width = 70, 
                                                     FieldKey = ContentController<KeyModel>.GetController<KeyController>((h as TextFieldModelController).Data)  });
                }
                // for each document we add any header we find with a name not matching a current name. This is the UNION of all fields *assuming no collisions
                foreach (var d in dbDocs)
                {
                    //if (d.GetField(RegexOperatorFieldModelController.TextKey) == null &&
                    //    d.GetField(KeyStore.DocumentTextKey) != null)
                    //{
                    //    var rd = OperatorDocumentFactory.CreateOperatorDocument(new RegexOperatorFieldModelController());
                    //    rd.SetField(RegexOperatorFieldModelController.ExpressionKey, new TextFieldModelController("^\\$[0-9.]+$"), true);
                    //    rd.SetField(RegexOperatorFieldModelController.SplitExpressionKey, new TextFieldModelController(" "), true);
                    //    rd.SetField(RegexOperatorFieldModelController.ExpressionKey, new TextFieldModelController(".*"), true);
                    //    rd.SetField(RegexOperatorFieldModelController.SplitExpressionKey, new TextFieldModelController("\\."), true);
                    //    rd.SetField(RegexOperatorFieldModelController.TextKey, new DocumentReferenceFieldController(d.GetId(), KeyStore.DocumentTextKey), true);
                    //    d.SetField(KeyStore.DataKey, new DocumentReferenceFieldController(rd.GetId(), RegexOperatorFieldModelController.MatchesKey), true);
                    //}
                    foreach (var f in d.EnumFields())
                        if (!f.Key.Name.StartsWith("_") && !SchemaHeadersContains(f.Key))
                            SchemaHeaders.Add(new CollectionDBSchemaHeader.HeaderViewModel() { SchemaView = this, SchemaDocument = ParentDocument, Width = 70, FieldKey = f.Key });
                }
                SchemaHeaders.CollectionChanged += SchemaHeaders_CollectionChanged;

                // add all the records
                UpdateRecords(dbDocs);
            }
        }

        private void UpdateRecords(IEnumerable<DocumentController> dbDocs)
        {
            var records = new List<CollectionDBSchemaRecordViewModel>();
            int recordCount = 0;
            foreach (var d in dbDocs)
            {
                records.Add(new CollectionDBSchemaRecordViewModel(
                    d,
                    SchemaHeaders.Select((f) => new CollectionDBSchemaRecordFieldViewModel(d, f, HeaderBorderThickness, recordCount))
                    ));
                recordCount++;
            }
            xRecordsView.ItemsSource = new ObservableCollection<CollectionDBSchemaRecordViewModel>(records);
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
            ParentDocument.SetField(KeyStore.CollectionOutputKey, new DocumentCollectionFieldModelController(collection), true);
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
            Debug.WriteLine("drop event from collection");

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

        private void xOuterGrid_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            this.xRecordsView.Height = xOuterGrid.ActualHeight - xHeaderArea.ActualHeight;
        }

        private void xOuterGrid_Loaded(object sender, RoutedEventArgs e)
        {
            this.xRecordsView.Height = xOuterGrid.ActualHeight - xHeaderArea.ActualHeight;
        }

        private void XRecordsView_OnLoaded(object sender, RoutedEventArgs e)
        {
            Util.FixListViewBaseManipulationDeltaPropagation(xRecordsView);
        }
    }
}