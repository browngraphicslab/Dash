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
using static Dash.NoteDocuments;

// The User Control item template is documented at http://go.microsoft.com/fwlink/?LinkId=234236

namespace Dash
{
    public sealed partial class CollectionDBView : SelectionElement, ICollectionView
    {
        public CollectionDBView()
        {
            this.InitializeComponent();
            DataContextChanged += CollectionDBView_DataContextChanged;
            Loaded             += CollectionDBView_Loaded;
            SizeChanged        += (sender, e) => updateChart(new Context(ParentDocument), true);
            xParameter.Style    = Application.Current.Resources["xSearchTextBox"] as Style;
            MinWidth = MinHeight = 50;
            xTagCloud.TermDragStarting += XTagCloud_TermDragStarting;
        }

        void XTagCloud_TermDragStarting(string term, DragStartingEventArgs args)
        {
            var dbDocs = ParentDocument.GetDereferencedField<DocumentCollectionFieldModelController>(ViewModel.CollectionKey, null).Data;
            var pattern = ParentDocument.GetDereferencedField<TextFieldModelController>(DBFilterOperatorFieldModelController.FilterFieldKey, null)?.Data.Trim(' ', '\r').Split(new char[] { '.' }, StringSplitOptions.RemoveEmptyEntries); ;
            if (dbDocs != null && pattern != null && pattern.Count() > 0)
            {
                var collection = dbDocs.Where((d) => testPatternMatch(d.GetDataDocument(null), pattern, term));
                
                var collectionDoc = new CollectionNote(new Point(), CollectionView.CollectionViewType.Schema, term, 200, 300, collection.ToList()).Document;
                args.Data.Properties.Add("DocumentControllerList", new List<DocumentController>(new DocumentController[] { collectionDoc }));
            }
        }

        private void CollectionDBView_Loaded(object sender, RoutedEventArgs e)
        {
            var dv = VisualTreeHelperExtensions.GetFirstAncestorOfType<DocumentView>(this);
            
            ParentDocument = dv.ViewModel.DocumentController;
            updateChart(new Context(ParentDocument));
        }

        private void CollectionDBView_DataContextChanged(FrameworkElement sender, DataContextChangedEventArgs args)
        {
            ViewModel = DataContext as BaseCollectionViewModel;
            ViewModel.OutputKey = KeyStore.CollectionOutputKey;
            ParentDocument = this.GetFirstAncestorOfType<DocumentView>()?.ViewModel?.DocumentController;
            updateChart(new Context(ParentDocument));
        }
        
        DocumentController _parentDocument;
        public DocumentController ParentDocument {
            get { return _parentDocument; }
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
        public BaseCollectionViewModel ViewModel { get; private set; }

        private void ParentDocument_DocumentFieldUpdated(DocumentController sender, DocumentController.DocumentFieldUpdatedEventArgs args)
        {
            var autofit = ParentDocument.GetDereferencedField<NumberFieldModelController>(DBFilterOperatorFieldModelController.AutoFitKey, args.Context)?.Data != 0;

            if (args.Reference.FieldKey == DBFilterOperatorFieldModelController.AutoFitKey ||
                args.Reference.FieldKey == DBFilterOperatorFieldModelController.FilterFieldKey ||
                (args.Reference.FieldKey == DBFilterOperatorFieldModelController.BucketsKey  && !autofit) ||
                args.Reference.FieldKey == DBFilterOperatorFieldModelController.SelectedKey ||
                args.Reference.FieldKey == ViewModel.CollectionKey)
                updateChart(new Context(ParentDocument));
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

        void updateChart(Context context, bool updateViewOnly=false)
        {
            var dbDocs  = ParentDocument?.GetDereferencedField<DocumentCollectionFieldModelController>(ViewModel.CollectionKey, context)?.Data;
            var buckets = ParentDocument?.GetDereferencedField<ListFieldModelController<NumberFieldModelController>>(DBFilterOperatorFieldModelController.BucketsKey, context)?.Data;
            var pattern = ParentDocument?.GetDereferencedField<TextFieldModelController>(DBFilterOperatorFieldModelController.FilterFieldKey, context)?.Data?.Trim(' ', '\r')?.Split(new char[] { '.' }, StringSplitOptions.RemoveEmptyEntries); ;
            var autofit = ParentDocument?.GetDereferencedField<NumberFieldModelController>(DBFilterOperatorFieldModelController.AutoFitKey, context)?.Data != 0;
            var selectedBars = ParentDocument?.GetDereferencedField<ListFieldModelController<NumberFieldModelController>>(DBFilterOperatorFieldModelController.SelectedKey, context)?.Data;
            if (dbDocs != null && buckets != null)
            {
                if (autofit)
                {
                    buckets = autoFitBuckets(dbDocs.Select((d) => d.GetDataDocument(null)).ToList(), pattern.ToList(), buckets.Count) ?? buckets;
                }

                string rawText   = "";
                var    barCounts = filterDocuments(dbDocs, buckets, pattern.ToList(), selectedBars, updateViewOnly, ref rawText);
                double barSum    = updateBars(buckets, barCounts);

                if (barSum == 0 && dbDocs.Count > 0)
                {
                    this.xTagCloud.TheText = rawText;
                    this.xNumberContent.Visibility = Visibility.Collapsed;
                    this.xTextContent.Visibility = Visibility.Visible;
                }
            }
        }

        double updateBars(List<FieldModelController> buckets, List<double> barCounts)
        {
            var xBars = xBarChart.Children.Select((c) => (c as CollectionDBChartBar)).ToList();
            if (xBars.Count == barCounts.Count) // have all of the bars already, just need to change their heights
            {
                for (int i = 0; i < buckets.Count; i++)
                {
                    xBars[i].MaxDomain = (buckets[i] as NumberFieldModelController).Data;
                }
            }
            else // otherwise, need to build all the bars
            {
                var selectedBars = ParentDocument?.GetDereferencedField<ListFieldModelController<NumberFieldModelController>>(DBFilterOperatorFieldModelController.SelectedKey, null)?.Data ?? new List<FieldModelController>();
                var selectedInds = selectedBars.Select((f) => (f as NumberFieldModelController)?.Data);
                this.xBarChart.Children.Clear();
                this.xBarChart.ColumnDefinitions.Clear();
                this.xBarChart.ColumnDefinitions.Add(new ColumnDefinition() { Width = GridLength.Auto });
                for (int i = 0; i < buckets.Count; i++)
                {
                    this.xBarChart.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(10, GridUnitType.Star) }); // add bar spacing
                    this.xBarChart.ColumnDefinitions.Add(new ColumnDefinition() { Width = GridLength.Auto });                       // add bar
                    var g = new CollectionDBChartBar() { FilterChart = this, BucketIndex = i, MaxDomain = (buckets[i] as NumberFieldModelController).Data, IsSelected = selectedInds.Contains(i) };
                    Grid.SetColumn(g, i * 2 + 1);
                    this.xBarChart.Children.Add(g);
                }
                xBars = xBarChart.Children.Select((c) => (c as CollectionDBChartBar)).ToList();
            }


            double barSum = barCounts.Aggregate((v, b) => v + b);
            for (int i = 0; i < barCounts.Count; i++)
            {
                xBars[i].xBar.Height = xBarChart.ActualHeight * barCounts[i] / Math.Max(1, barSum);
            }

            return barSum;
        }

        List<FieldModelController> autoFitBuckets(List<DocumentController> dbDocs, List<string> pattern, int numBars)
        {
            double minValue = double.MaxValue;
            double maxValue = double.MinValue;
            foreach (var dmc in dbDocs.ToArray())
            {
                var visited = new List<DocumentController>();
                visited.Add(dmc);
                var refField = searchInDocumentForNamedField(pattern, dmc, visited);
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
            var newBuckets = barDomains.Select((b) => b as FieldModelController).ToList();
            ParentDocument.SetField(DBFilterOperatorFieldModelController.BucketsKey, new ListFieldModelController<NumberFieldModelController>(newBuckets.Select((b) => b as NumberFieldModelController)), true);

            return newBuckets;
        }

        static bool testPatternMatch(DocumentController dmc, string[] pattern, string term)
        {
            if ((pattern != null && pattern.Count() == 0) || dmc == null || dmc.GetField(KeyStore.AbstractInterfaceKey, true) != null)
                return false;
            // loop through each field to find on that matches the field name pattern 
            foreach (var pfield in dmc.EnumFields().Where((pf) => !pf.Key.IsUnrenderedKey() && (pattern == null || pf.Key.Name == pattern[0])))
            {
                if (pattern == null || pfield.Key.Name == pattern.First())
                {
                    var pvalue = pfield.Value.DereferenceToRoot(new Context(dmc));
                    if (pvalue is DocumentFieldModelController)
                    {
                        var nestedDoc = (pvalue as DocumentFieldModelController).Data;
                        return testPatternMatch(nestedDoc, null, term);
                    }
                    else if (pvalue is DocumentCollectionFieldModelController)
                    {
                        foreach (var nestedDoc in (pvalue as DocumentCollectionFieldModelController).Data.Select((d) => d.GetDataDocument(null)))
                            if (testPatternMatch(nestedDoc, null, term))
                                return true;
                    }
                    else if (pvalue is TextFieldModelController)
                    {
                        var text = (pvalue as TextFieldModelController).Data;
                        if (text != null && text.Contains(term))
                            return true;
                    }
                }
            }
            return false;
        }
        List<double> filterDocuments(List<DocumentController> dbDocs, List<FieldModelController> bars, List<string> pattern,
                                     List<FieldModelController> selectedBars, bool updateViewOnly, ref string rawText)
        {
            bool keepAll = selectedBars.Count == 0;

            var collection = new List<DocumentController>();
            var countBars = new List<double>();
            foreach (var b in bars)
                countBars.Add(0);

            var sumOfFields = 0.0;
            if (dbDocs != null && (pattern == null || pattern.Count() != 0))
            {
                foreach (var dmc in dbDocs.Select((d) => d))
                {
                    var visited = new List<DocumentController>();
                    visited.Add(dmc);
                    if (pattern == null)
                    {
                        var dataDoc = dmc.GetDataDocument(null);
                        foreach (var f in dataDoc.EnumFields().Where((f) => !f.Key.IsUnrenderedKey()))
                        {
                            var refField = new ReferenceFieldModelController(dataDoc.GetId(), f.Key);
                            inspectField(bars, refField, selectedBars, updateViewOnly, ref rawText, keepAll, collection, countBars, ref sumOfFields, dmc, visited);
                        }
                    }
                    else
                    {
                        var refField = searchInDocumentForNamedField(pattern, dmc, visited);
                        inspectField(bars, refField, selectedBars, updateViewOnly, ref rawText, keepAll, collection, countBars, ref sumOfFields, dmc, visited);
                    }
                }
            }
            if (!updateViewOnly)
            {
                if (ViewModel.CollectionKey != KeyStore.CollectionOutputKey) // avoid inifinite loop -- input is output.  this happens when something tries to display the CollectionOutputKey field (e.g., Interface Builder showing all document fields)
                    ParentDocument.SetField(KeyStore.CollectionOutputKey, new DocumentCollectionFieldModelController(collection), true);
                ParentDocument.SetField(DBFilterOperatorFieldModelController.AvgResultKey, new NumberFieldModelController(sumOfFields / dbDocs.Count), true);
            }
            return countBars;
        }

        void inspectField(List<FieldModelController> bars, ReferenceFieldModelController refField, List<FieldModelController> selectedBars, bool updateViewOnly, ref string rawText, bool keepAll, List<DocumentController> collection, List<double> countBars, ref double sumOfFields, DocumentController dmc, List<DocumentController> visited)
        {
            var dataDoc = dmc.GetDataDocument(null);
            var field = refField?.GetDocumentController(new Context(dataDoc)).GetDereferencedField(refField.FieldKey, new Context(dataDoc));
            if (field is DocumentCollectionFieldModelController)
            {
                var counted = filterDocuments((field as DocumentCollectionFieldModelController).Data, bars, null, selectedBars, updateViewOnly, ref rawText);
                for (int i = 0; i < counted.Count; i++)
                    countBars[i] += counted[i] > 0 ? 1 : 0;
            }
            else if (field != null)
            {
                rawText += " " + field.GetValue(new Context(dataDoc)).ToString();
                var numberField = field as NumberFieldModelController;
                if (numberField != null)
                {
                    sumOfFields += numberField.Data;
                    foreach (var b in bars)
                    {
                        if (numberField.Data <= (b as NumberFieldModelController).Data)
                        {
                            countBars[bars.IndexOf(b)]++;
                            if (keepAll || selectedBars.Select((fm) => (fm as NumberFieldModelController).Data).ToList().Contains(bars.IndexOf(b)))
                                collection.Add(dmc);
                            break;
                        }
                    }
                }
                else if (keepAll)
                    collection.Add(dmc);
            }
        }

        static ReferenceFieldModelController searchInDocumentForNamedField(List<string> pattern, DocumentController srcDoc, List<DocumentController> visited)
        {
            var dmc = srcDoc.GetDataDocument(null);
            if ((pattern != null && pattern.Count == 0) || dmc == null || dmc.GetField(KeyStore.AbstractInterfaceKey, true) != null)
                return null;
            // loop through each field to find on that matches the field name pattern 
            foreach (var pfield in dmc.EnumFields().Where((pf) => !pf.Key.IsUnrenderedKey() && ( pattern == null || pf.Key.Name == pattern[0] || pattern[0] == "" || pf.Value is DocumentFieldModelController)))
            {
                if (pfield.Value is DocumentFieldModelController)
                {
                    var nestedDoc = (pfield.Value as DocumentFieldModelController).Data;
                    if (!visited.Contains(nestedDoc))
                    {
                        visited.Add(nestedDoc);
                        var field = searchInDocumentForNamedField(pattern, nestedDoc, visited);
                        if (field != null)
                        {
                            return field;
                        }
                    }
                }
                else if (pattern != null && pattern.Count == 1)
                {
                    return new ReferenceFieldModelController(dmc.GetId(), pfield.Key);
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