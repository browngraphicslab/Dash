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
using Dash.Controllers;

// The User Control item template is documented at http://go.microsoft.com/fwlink/?LinkId=234236

namespace Dash
{
    public sealed partial class CollectionDBView : SelectionElement, ICollectionView
    {
        public BaseCollectionViewModel ViewModel { get; private set; }
        public CollectionDBView()
        {
            this.InitializeComponent();
            DataContextChanged += CollectionDBView_DataContextChanged;
            Loaded += CollectionDBView_Loaded;
            SizeChanged += CollectionDBView_SizeChanged;
            xParameter.Style = Application.Current.Resources["xPlainTextBox"] as Style;
            MinWidth = MinHeight = 50;
        }

        private void CollectionDBView_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            if (ParentDocument != null)
                UpdateChart(new Context(ParentDocument));
        }

        private void CollectionDBView_Loaded(object sender, RoutedEventArgs e)
        {
            var dv = VisualTreeHelperExtensions.GetFirstAncestorOfType<DocumentView>(this);
            
            ParentDocument = dv.ViewModel.DocumentController;
            UpdateChart(new Context(ParentDocument));
        }

        private void CollectionDBView_DataContextChanged(FrameworkElement sender, DataContextChangedEventArgs args)
        {
            ViewModel = DataContext as BaseCollectionViewModel;
            ViewModel.OutputKey = DBFilterOperatorFieldModelController.ResultsKey;
            ParentDocument = this.GetFirstAncestorOfType<DocumentView>()?.ViewModel?.DocumentController;
            if (ParentDocument != null)
                UpdateChart(new Context(ParentDocument));
        }
        

        DocumentController _parentDocument;
        public DocumentController ParentDocument {
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
                    xParameter.AddFieldBinding(TextBox.TextProperty, new FieldBinding<TextFieldModelController>()
                    {
                        Mode = BindingMode.TwoWay,
                        Document = ParentDocument,
                        Key = DBFilterOperatorFieldModelController.FilterFieldKey,
                        Context = new Context(ParentDocument)
                    });
                    xAutoFit.AddFieldBinding(CheckBox.IsCheckedProperty, new FieldBinding<NumberFieldModelController>()
                    {
                        Mode = BindingMode.TwoWay,
                        Document = ParentDocument,
                        Key = DBFilterOperatorFieldModelController.AutoFitKey,
                        Converter = new DoubleToBoolConverter(),
                        Context = new Context(ParentDocument)
                    });
                    // shouldn't have to do this but the binding above wasn't working...
                    xAutoFit.Unchecked +=  (sender, args) => ParentDocument.SetField(DBFilterOperatorFieldModelController.AutoFitKey, new NumberFieldModelController(0), true);
                    xAutoFit.Checked += (sender, args) => ParentDocument.SetField(DBFilterOperatorFieldModelController.AutoFitKey, new NumberFieldModelController(1), true);

                    xAvg.AddFieldBinding(TextBlock.TextProperty, new FieldBinding<NumberFieldModelController>()
                    {
                        Mode = BindingMode.TwoWay,
                        Document = ParentDocument,
                        Key = DBFilterOperatorFieldModelController.AvgResultKey,
                        Converter = new DoubleToStringConverter(),
                        Context = new Context(ParentDocument)
                    });
                }
            }
        }
        

        private void ParentDocument_DocumentFieldUpdated(DocumentController sender, DocumentController.DocumentFieldUpdatedEventArgs args)
        {
            if (args.Reference.FieldKey == DBFilterOperatorFieldModelController.AutoFitKey ||
                args.Reference.FieldKey == DBFilterOperatorFieldModelController.FilterFieldKey ||
                args.Reference.FieldKey == DBFilterOperatorFieldModelController.BucketsKey ||
                args.Reference.FieldKey == DBFilterOperatorFieldModelController.SelectedKey ||
                args.Reference.FieldKey == ViewModel.CollectionKey)
                UpdateChart(new Context(ParentDocument));
        }

        public void UpdateBucket(int changedBucket, double maxDomain)
        {
            xAutoFit.IsChecked = false;
            var buckets = (ParentDocument.GetDereferencedField<ListFieldModelController<NumberFieldModelController>>(DBFilterOperatorFieldModelController.BucketsKey, new Context(ParentDocument))).Data.Select((fm) => fm as NumberFieldModelController).ToList();
            (buckets[changedBucket] as NumberFieldModelController).Data = maxDomain;
            ParentDocument.SetField(DBFilterOperatorFieldModelController.BucketsKey, new ListFieldModelController<NumberFieldModelController>(buckets), true);
        }
        public void UpdateSelection(int changedBar, bool selected)
        {
            var selectedBars = (ParentDocument.GetDereferencedField<ListFieldModelController<NumberFieldModelController>>(DBFilterOperatorFieldModelController.SelectedKey, new Context(ParentDocument))).Data.Select((fm) => fm as NumberFieldModelController).ToList();
            bool found = false;
            foreach (var sel in selectedBars)
                if (sel.Data == changedBar)
                {
                    found = true;
                    if (!selected)
                    {
                        selectedBars.Remove(sel);
                        break;
                    }
                }
            if (!found && selected)
                selectedBars.Add(new NumberFieldModelController(changedBar));
            ParentDocument.SetField(DBFilterOperatorFieldModelController.SelectedKey, new ListFieldModelController<NumberFieldModelController>(selectedBars), true);
        }


        public void UpdateChart(Context context)
        {
            var dbDocs  = ParentDocument.GetDereferencedField<DocumentCollectionFieldModelController>(ViewModel.CollectionKey, context).Data;
            var buckets = ParentDocument.GetDereferencedField<ListFieldModelController<NumberFieldModelController>>(DBFilterOperatorFieldModelController.BucketsKey, context)?.Data;
            var pattern = ParentDocument.GetDereferencedField<TextFieldModelController>(DBFilterOperatorFieldModelController.FilterFieldKey, context)?.Data.Trim(' ', '\r').Split(new char[] { '.' }, StringSplitOptions.RemoveEmptyEntries); ;
            var autofit = ParentDocument.GetDereferencedField<NumberFieldModelController>(DBFilterOperatorFieldModelController.AutoFitKey, context).Data != 0;
            var selectedBars = ParentDocument.GetDereferencedField<ListFieldModelController<NumberFieldModelController>>(DBFilterOperatorFieldModelController.SelectedKey, context)?.Data;
            if (dbDocs != null && buckets != null)
            {
                if (autofit)
                {
                    var newBuckets = autoFitBuckets(dbDocs, pattern.ToList(), buckets.Count);
                    if (newBuckets != null)
                    {
                        if (newBuckets.Count == buckets.Count)
                            for (int i = 0; i < newBuckets.Count; i++)
                                if ((newBuckets[i] as NumberFieldModelController).Data != (buckets[i] as NumberFieldModelController).Data)
                                {
                                    ParentDocument.SetField(DBFilterOperatorFieldModelController.BucketsKey, new ListFieldModelController<NumberFieldModelController>(newBuckets.Select((b) => b as NumberFieldModelController)), true);
                                    break;
                                }
                        buckets = newBuckets;
                    }
                }

                var barCounts = filterDocuments(dbDocs, buckets, pattern.ToList(), selectedBars);
                var xBars = xBarChart.Children.Select((c) => (c as CollectionDBChartBar)).ToList();

                if (xBars.Count == barCounts.Count)
                {
                    for (int i = 0; i < buckets.Count; i++)
                    {
                        xBars[i].MaxDomain = (buckets[i] as NumberFieldModelController).Data;
                    }
                }
                else
                {
                    setupBars(buckets);
                    xBars = xBarChart.Children.Select((c) => (c as CollectionDBChartBar)).ToList();
                }


                double barSum = 0;
                for (int i = 0; i < barCounts.Count; i++)
                {
                    var b = barCounts[i];
                    xBars[i].xBar.Height = xBarChart.ActualHeight * b;
                    barSum += b;
                }
                for (int i = 0; i < barCounts.Count; i++)
                {
                    var b = barCounts[i];
                    xBars[i].xBar.Height /= Math.Max(1, barSum);
                }
            }
        }
        public void setupBars(List<FieldControllerBase> buckets)
        {
            this.xBarChart.Children.Clear();
            this.xBarChart.ColumnDefinitions.Clear();
            this.xBarChart.ColumnDefinitions.Add(new ColumnDefinition() { Width = GridLength.Auto });
            for (int i = 0; i < buckets.Count; i++)
            {
                this.xBarChart.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(10, GridUnitType.Star) });
                this.xBarChart.ColumnDefinitions.Add(new ColumnDefinition() { Width = GridLength.Auto });
                var g = new CollectionDBChartBar() { FilterChart = this, BucketIndex = i, MaxDomain = (buckets[i] as NumberFieldModelController).Data };
                Grid.SetColumn(g, i * 2 + 1);
                this.xBarChart.Children.Add(g);
            }
            this.xBarChart.ColumnDefinitions.Add(new ColumnDefinition() { Width = GridLength.Auto });
        }

        static List<FieldControllerBase> autoFitBuckets(List<DocumentController> dbDocs, List<string> pattern, int numBars)
        {
            double minValue = double.MaxValue;
            double maxValue = double.MinValue;
            foreach (var dmc in dbDocs.ToArray())
            {
                var visited = new List<DocumentController>();
                visited.Add(dmc);
                var refField = SearchInDocumentForNamedField(pattern, dmc, dmc, visited);
                var field = refField?.GetDocumentController(new Context(dmc)).GetDereferencedField<NumberFieldModelController>(refField.FieldKey, new Context(dmc));
                if (field != null)
                {
                    if (field.Data < minValue) minValue = field.Data;
                    if (field.Data > maxValue) maxValue = field.Data;
                }
            }
            if (minValue == double.MaxValue)
                return null;
            double barDomain = (maxValue - minValue) / numBars;
            double barStart = minValue + barDomain;
            var barDomains = new List<NumberFieldModelController>();

            for (int i = 0; i < numBars; i++)
            {
                barDomains.Add(new NumberFieldModelController(barStart));
                barStart += barDomain;
            }

            return barDomains.Select((b) => b as FieldControllerBase).ToList();
        }

        public List<double> filterDocuments(List<DocumentController> dbDocs, List<FieldControllerBase> bars, List<string> pattern, List<FieldControllerBase> selectedBars)
        {
            bool keepAll = selectedBars.Count == 0;

            var collection = new List<DocumentController>();
            var countBars = new List<double>();
            foreach (var b in bars)
                countBars.Add(0);

            var sumOfFields = 0.0;
            if (dbDocs != null && pattern.Count() != 0)
            {
                foreach (var dmc in dbDocs.ToArray())
                {
                    var visited = new List<DocumentController>();
                    visited.Add(dmc);

                    var refField = SearchInDocumentForNamedField(pattern, dmc, dmc, visited);
                    var field = refField?.GetDocumentController(new Context(dmc)).GetDereferencedField<NumberFieldModelController>(refField.FieldKey, new Context(dmc));
                    if (field != null)
                    {
                        sumOfFields += field.Data;
                        foreach (var b in bars)
                        {
                            if (field.Data <= (b as NumberFieldModelController).Data)
                            {
                                countBars[bars.IndexOf(b)]++;
                                if (keepAll || selectedBars.Select((fm) => (fm as NumberFieldModelController).Data).ToList().Contains(bars.IndexOf(b)))
                                    collection.Add(dmc);
                                break;
                            }
                        }
                    }
                }
            }
            ParentDocument.SetField(DBFilterOperatorFieldModelController.ResultsKey, new DocumentCollectionFieldModelController(collection), true);
            ParentDocument.SetField(DBFilterOperatorFieldModelController.AvgResultKey, new NumberFieldModelController(sumOfFields / dbDocs.Count), true);
            return countBars;
        }

        private static ReferenceFieldModelController SearchInDocumentForNamedField(List<string> pattern, DocumentController srcDoc, DocumentController dmc, List<DocumentController> visited)
        {
            if (pattern.Count == 0 || dmc == null || dmc.GetField(KeyStore.AbstractInterfaceKey, true) != null)
                return null;
            // loop through each field to find on that matches the field name pattern 
            foreach (var pfield in dmc.EnumFields().Where((pf) => pf.Key.Name == pattern[0] || pattern[0] == "" || pf.Value is DocumentFieldModelController))
            {
                if (pfield.Value is DocumentFieldModelController)
                {
                    var nestedDoc = (pfield.Value as DocumentFieldModelController).Data;
                    if (!visited.Contains(nestedDoc))
                    {
                        visited.Add(nestedDoc);
                        var field = SearchInDocumentForNamedField(pattern, nestedDoc, nestedDoc, visited);
                        if (field != null)
                            return field;
                    }
                }
                else if (pattern.Count == 1)
                {
                    return new DocumentReferenceFieldController(srcDoc.GetId(), pfield.Key);
                }
            }
            return null;
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