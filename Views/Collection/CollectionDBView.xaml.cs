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
using Dash.Controllers;

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
            var dbDocs = ParentDocument.GetDereferencedField<ListController<DocumentController>>(ViewModel.CollectionKey, null).TypedData;
            var pattern = ParentDocument.GetDereferencedField<TextController>(DBFilterOperatorController.FilterFieldKey, null)?.Data.Trim(' ', '\r').Split(new char[] { '.' }, StringSplitOptions.RemoveEmptyEntries);
            if (dbDocs != null && pattern != null && pattern.Any())
            {
                var collection = dbDocs.Select((d) =>
                {
                    var key =  testPatternMatch(d.GetDataDocument(null), pattern, term);
                    if (key != null)
                    {
                        var rnote = new NoteDocuments.RichTextNote(NoteDocuments.PostitNote.DocumentType).Document;
                        var derefField = d.GetDataDocument(null).GetDereferencedField(key, null);
                        if (derefField is TextController)
                            rnote.GetDataDocument(null).SetField(RichTextNote.RTFieldKey, new RichTextController(new RichTextModel.RTD((derefField as TextController).Data)), true);
                        else if (derefField is RichTextController)
                            rnote.GetDataDocument(null).SetField(RichTextNote.RTFieldKey, new RichTextController(new RichTextModel.RTD((derefField as RichTextController).Data.ReadableString)), true);
                        rnote.GetDataDocument(null).SetField(DBFilterOperatorController.SelectedKey, new TextController(term), true);
                        return rnote;
                    }
                    return null;
                });
                var collectionDoc = new CollectionNote(new Point(), CollectionView.CollectionViewType.Schema, term, 200, 300, collection.Where((c)=> c != null).ToList()).Document;
                
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
                        _parentDocument = _parentDocument.GetDereferencedField<DocumentController>(KeyStore.DocumentContextKey, null);
                    }
                    ParentDocument.FieldModelUpdated -= ParentDocument_DocumentFieldUpdated;
                    if (ParentDocument.GetField(DBFilterOperatorController.BucketsKey) == null)
                        ParentDocument.SetField(DBFilterOperatorController.BucketsKey, new ListController<NumberController>(new NumberController[] {
                                                        new NumberController(0), new NumberController(0), new NumberController(0), new NumberController(0)}), true);
                    if (ParentDocument.GetField(DBFilterOperatorController.FilterFieldKey) == null)
                        ParentDocument.SetField(DBFilterOperatorController.FilterFieldKey, new TextController(""), true);
                    if (ParentDocument.GetField(DBFilterOperatorController.AutoFitKey) == null)
                        ParentDocument.SetField(DBFilterOperatorController.AutoFitKey, new NumberController(3), true);
                    if (ParentDocument.GetField(DBFilterOperatorController.SelectedKey) == null)
                        ParentDocument.SetField(DBFilterOperatorController.SelectedKey, new ListController<NumberController>(), true);
                    ParentDocument.SetField(DBFilterOperatorController.AvgResultKey, new NumberController(0), true);
                    ParentDocument.FieldModelUpdated += ParentDocument_DocumentFieldUpdated;
                    xParameter.AddFieldBinding(TextBox.TextProperty, new FieldBinding<TextController>()
                    {
                        Mode = BindingMode.TwoWay,
                        Document = ParentDocument,
                        Key = DBFilterOperatorController.FilterFieldKey,
                        Context = new Context(ParentDocument)
                    });
                    xAutoFit.AddFieldBinding(CheckBox.IsCheckedProperty, new FieldBinding<NumberController>()
                    {
                        Mode = BindingMode.TwoWay,
                        Document = ParentDocument,
                        Key = DBFilterOperatorController.AutoFitKey,
                        Converter = new DoubleToBoolConverter(),
                        Context = new Context(ParentDocument)
                    });
                    // shouldn't have to do this but the binding above wasn't working...
                    xAutoFit.Unchecked +=  (sender, args) => ParentDocument.SetField(DBFilterOperatorController.AutoFitKey, new NumberController(0), true);
                    xAutoFit.Checked += (sender, args) => ParentDocument.SetField(DBFilterOperatorController.AutoFitKey, new NumberController(1), true);

                    xAvg.AddFieldBinding(TextBlock.TextProperty, new FieldBinding<NumberController>()
                    {
                        Mode = BindingMode.TwoWay,
                        Document = ParentDocument,
                        Key = DBFilterOperatorController.AvgResultKey,
                        Converter = new StringToDoubleConverter(),
                        Context = new Context(ParentDocument)
                    });
                }
            }
        }
        public BaseCollectionViewModel ViewModel { get; private set; }

        private void ParentDocument_DocumentFieldUpdated(FieldControllerBase sender, FieldUpdatedEventArgs args, Context context)
        {
            var dargs = (DocumentController.DocumentFieldUpdatedEventArgs) args;
            var autofit = ParentDocument.GetDereferencedField<NumberController>(DBFilterOperatorController.AutoFitKey, context)?.Data != 0;

            if (dargs.Reference.FieldKey.Equals( DBFilterOperatorController.AutoFitKey) ||
                dargs.Reference.FieldKey.Equals(DBFilterOperatorController.FilterFieldKey) ||
                dargs.Reference.FieldKey.Equals(DBFilterOperatorController.BucketsKey) && !autofit ||
                dargs.Reference.FieldKey.Equals(DBFilterOperatorController.SelectedKey) ||
                dargs.Reference.FieldKey.Equals(ViewModel.CollectionKey))
                updateChart(new Context(ParentDocument));
        }

        public void UpdateBucket(int changedBucket, double maxDomain)
        {
            xAutoFit.IsChecked = false;
            var buckets = (ParentDocument.GetDereferencedField<ListController<NumberController>>(DBFilterOperatorController.BucketsKey, new Context(ParentDocument))).Data.Select((fm) => fm as NumberController).ToList();
            (buckets[changedBucket] as NumberController).Data = maxDomain;
            ParentDocument.SetField(DBFilterOperatorController.BucketsKey, new ListController<NumberController>(buckets), true);
        }
        public void UpdateSelection(int changedBar, bool selected)
        {
            var selectedBars = (ParentDocument.GetDereferencedField<ListController<NumberController>>(DBFilterOperatorController.SelectedKey, new Context(ParentDocument))).Data.Select((fm) => fm as NumberController).ToList();
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
                selectedBars.Add(new NumberController(changedBar));
            ParentDocument.SetField(DBFilterOperatorController.SelectedKey, new ListController<NumberController>(selectedBars), true);
        }

        void updateChart(Context context, bool updateViewOnly=false)
        {
            var dbDocs  = ParentDocument?.GetDereferencedField<ListController<DocumentController>>(ViewModel.CollectionKey, context)?.TypedData;
            var buckets = ParentDocument?.GetDereferencedField<ListController<NumberController>>(DBFilterOperatorController.BucketsKey, context)?.Data;
            var pattern = ParentDocument?.GetDereferencedField<TextController>(DBFilterOperatorController.FilterFieldKey, context)?.Data?.Trim(' ', '\r')?.Split(new char[] { '.' }, StringSplitOptions.RemoveEmptyEntries); ;
            var autofit = ParentDocument?.GetDereferencedField<NumberController>(DBFilterOperatorController.AutoFitKey, context)?.Data != 0;
            var selectedBars = ParentDocument?.GetDereferencedField<ListController<NumberController>>(DBFilterOperatorController.SelectedKey, context)?.Data;
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

        double updateBars(List<FieldControllerBase> buckets, List<double> barCounts)
        {
            var xBars = xBarChart.Children.Select((c) => (c as CollectionDBChartBar)).ToList();
            if (xBars.Count == barCounts.Count) // have all of the bars already, just need to change their heights
            {
                for (int i = 0; i < buckets.Count; i++)
                {
                    xBars[i].MaxDomain = (buckets[i] as NumberController).Data;
                }
            }
            else // otherwise, need to build all the bars
            {
                var selectedBars = ParentDocument?.GetDereferencedField<ListController<NumberController>>(DBFilterOperatorController.SelectedKey, null)?.Data ?? new List<FieldControllerBase>();
                var selectedInds = selectedBars.Select((f) => (f as NumberController)?.Data);
                this.xBarChart.Children.Clear();
                this.xBarChart.ColumnDefinitions.Clear();
                this.xBarChart.ColumnDefinitions.Add(new ColumnDefinition() { Width = GridLength.Auto });
                for (int i = 0; i < buckets.Count; i++)
                {
                    this.xBarChart.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(10, GridUnitType.Star) }); // add bar spacing
                    this.xBarChart.ColumnDefinitions.Add(new ColumnDefinition() { Width = GridLength.Auto });                       // add bar
                    var g = new CollectionDBChartBar() { FilterChart = this, BucketIndex = i, MaxDomain = (buckets[i] as NumberController).Data, IsSelected = selectedInds.Contains(i) };
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

        List<FieldControllerBase> autoFitBuckets(List<DocumentController> dbDocs, List<string> pattern, int numBars)
        {
            double minValue = double.MaxValue;
            double maxValue = double.MinValue;
            foreach (var dmc in dbDocs.ToArray())
            {
                var visited = new List<DocumentController>();
                visited.Add(dmc);
                var refField = searchInDocumentForNamedField(pattern, dmc, visited);
                var field = refField?.GetDocumentController(new Context(dmc)).GetDereferencedField<NumberController>(refField.FieldKey, new Context(dmc));
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
            var barDomains = new List<NumberController>();

            for (int i = 0; i < numBars; i++)
            {
                barDomains.Add(new NumberController(barStart));
                barStart += barDomain;
            }
            var newBuckets = barDomains.Select((b) => b as FieldControllerBase).ToList();
            ParentDocument.SetField(DBFilterOperatorController.BucketsKey, new ListController<NumberController>(newBuckets.Select((b) => b as NumberController)), true);

            return newBuckets;
        }

        static KeyController testPatternMatch(DocumentController dmc, string[] pattern, string term)
        {
            if ((pattern != null && pattern.Count() == 0) || dmc == null || dmc.GetField(KeyStore.AbstractInterfaceKey, true) != null)
                return null;
            // loop through each field to find on that matches the field name pattern 
            foreach (var pfield in dmc.EnumFields().Where((pf) => !pf.Key.IsUnrenderedKey() && (pattern == null || pf.Key.Name == pattern[0])))
            {
                if (pattern == null || pfield.Key.Name == pattern.First())
                {
                    var pvalue = pfield.Value.DereferenceToRoot(new Context(dmc));
                    if (pvalue is DocumentController)
                    {
                        var nestedDoc = pvalue as DocumentController;
                        return testPatternMatch(nestedDoc, null, term);
                    }
                    else if (pvalue is ListController<DocumentController>)
                    {
                        foreach (var nestedDoc in (pvalue as ListController<DocumentController>).TypedData.Select((d) => d.GetDataDocument(null)))
                            if (testPatternMatch(nestedDoc, null, term) != null)
                                return pfield.Key;
                    }
                    else if (pvalue is RichTextController)
                    {
                        var text = (pvalue as RichTextController).Data.ReadableString;
                        if (text != null && text.Contains(term))
                            return pfield.Key;
                    }
                    else if (pvalue is TextController)
                    {
                        var text = (pvalue as TextController).Data;
                        if (text != null && text.Contains(term))
                            return pfield.Key;
                    }
                }
            }
            return null;
        }

        List<double> filterDocuments(List<DocumentController> dbDocs, List<FieldControllerBase> bars, List<string> pattern,
                                     List<FieldControllerBase> selectedBars, bool updateViewOnly, ref string rawText)
        {
            bool keepAll = selectedBars.Count == 0;

            var collection = new List<DocumentController>();
            var countBars = new List<double>();
            foreach (var b in bars)
                countBars.Add(0);

            var sumOfFields = 0.0;
            if (dbDocs != null && (pattern == null || pattern.Count() != 0))
            {
                int count = 0;
                foreach (var dmc in dbDocs)
                {
                    count++;
                    //Debug.WriteLine("Count = " + count + " rawText = " + rawText.Length);
                    var visited = new List<DocumentController>();
                    visited.Add(dmc);
                    if (pattern == null)
                    {
                        var dataDoc = dmc.GetDataDocument(null);
                        foreach (var f in dataDoc.EnumFields().Where((f) => !f.Key.IsUnrenderedKey()))
                        {
                            var refField = new DocumentReferenceController(dataDoc.GetId(), f.Key);
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
                if (!ViewModel.CollectionKey.Equals(KeyStore.CollectionOutputKey)) // avoid inifinite loop -- input is output.  this happens when something tries to display the CollectionOutputKey field (e.g., Interface Builder showing all document fields)
                    ParentDocument.SetField(KeyStore.CollectionOutputKey, new ListController<DocumentController>(collection), true);
                ParentDocument.SetField(DBFilterOperatorController.AvgResultKey, new NumberController(sumOfFields / dbDocs.Count), true);
            }
            return countBars;
        }

        void inspectField(List<FieldControllerBase> bars, ReferenceController refField, List<FieldControllerBase> selectedBars, bool updateViewOnly, ref string rawText, bool keepAll, List<DocumentController> collection, List<double> countBars, ref double sumOfFields, DocumentController dmc, List<DocumentController> visited)
        {
            var dataDoc = dmc.GetDataDocument(null);
            var field = refField?.GetDocumentController(new Context(dataDoc)).GetDereferencedField(refField.FieldKey, new Context(dataDoc));
            if (field is ListController<DocumentController>)
            {
                var counted = filterDocuments((field as ListController<DocumentController>).TypedData, bars, null, selectedBars, updateViewOnly, ref rawText);
                for (int i = 0; i < counted.Count; i++)
                    countBars[i] += counted[i] > 0 ? 1 : 0;
            }
            else if (field != null)
            {
                if (field is ListController<TextController>)
                {
                    foreach (var tfmc in (field as ListController<TextController>).Data)
                        rawText += " " + tfmc.ToString();
                } else
                    rawText += " " + field.GetValue(new Context(dataDoc)).ToString();
                var numberField = field as NumberController;
                if (numberField != null)
                {
                    sumOfFields += numberField.Data;
                    foreach (var b in bars)
                    {
                        if (numberField.Data <= (b as NumberController).Data)
                        {
                            countBars[bars.IndexOf(b)]++;
                            if (keepAll || selectedBars.Select((fm) => (fm as NumberController).Data).ToList().Contains(bars.IndexOf(b)))
                                collection.Add(dmc);
                            break;
                        }
                    }
                }
                else if (keepAll)
                    collection.Add(dmc);
            }
        }

        static ReferenceController searchInDocumentForNamedField(List<string> pattern, DocumentController srcDoc, List<DocumentController> visited)
        {
            var dmc = srcDoc.GetDataDocument(null);
            if ((pattern != null && pattern.Count == 0) || dmc == null || dmc.GetField(KeyStore.AbstractInterfaceKey, true) != null)
                return null;
            // loop through each field to find on that matches the field name pattern 
            foreach (var pfield in dmc.EnumFields().Where((pf) => !pf.Key.IsUnrenderedKey() && ( pattern == null || pf.Key.Name == pattern[0] || pattern[0] == "")))
            {
                if (pattern != null && pattern.Count == 1)
                {
                    return new DocumentReferenceController(dmc.GetId(), pfield.Key);
                }
            }
            foreach (var pfield in dmc.EnumFields().Where((pf) => !pf.Key.IsUnrenderedKey() && pf.Value is DocumentController))
            {
                var nestedDoc = pfield.Value as DocumentController;
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
    }
}