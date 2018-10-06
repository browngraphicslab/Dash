using System;
using System.Collections.Generic;
using System.Linq;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Media;

// The User Control item template is documented at http://go.microsoft.com/fwlink/?LinkId=234236

namespace Dash
{
    public sealed partial class CollectionDBView : ICollectionView
    {
        public UserControl UserControl => this;
        public CollectionDBView()
        {
            this.InitializeComponent();
            Loaded             += CollectionDBView_Loaded;
            Unloaded           += CollectionDBView_Unloaded;
            SizeChanged        += (sender, e) => updateChart(new Context(ParentDocument), true);
            xParameter.Style    = Application.Current.Resources["xSearchTextBox"] as Style;
            MinWidth = MinHeight = 50;
            xTagCloud.TermDragStarting += XTagCloud_TermDragStarting;
        }


        public void SetupContextMenu(MenuFlyout contextMenu)
        {

        }
        private void XTagCloud_TermDragStarting(string term, DragStartingEventArgs args)
        {
            var dbDocs = ParentDocument.GetDataDocument().GetDereferencedField<ListController<DocumentController>>(ViewModel.CollectionKey, null).TypedData;
            var pattern = ParentDocument.GetDereferencedField<KeyController>(CollectionDBView.FilterFieldKey, null);

            if (dbDocs == null || pattern == null || string.IsNullOrEmpty(pattern.Name)) return;

            var collection = dbDocs.Select((d) =>
            {
                KeyController key =  testPatternMatch(d.GetDataDocument(), pattern, term);

                if (key == null) return null;

                string derefField = d.GetDataDocument().GetDereferencedField<TextController>(key, null)?.Data;
                DocumentController rnote = new RichTextNote(derefField ?? "<empty>").Document;
                rnote.GetDataDocument().SetField(SelectedKey, new TextController(term), true);
                return rnote;
            });

            args.Data.SetDragModel(new DragDocumentModel(collection.Where((c) => c != null).ToList(), CollectionView.CollectionViewType.Schema));
        }

        private void CollectionDBView_Loaded(object sender, RoutedEventArgs e)
        {
            DataContextChanged += CollectionDBView_DataContextChanged;
            CollectionDBView_DataContextChanged(sender, null);
        }
        private void CollectionDBView_Unloaded(object sender, RoutedEventArgs e)
        {
            DataContextChanged -= CollectionDBView_DataContextChanged;
            ParentDocument.FieldModelUpdated -= ParentDocument_DocumentFieldUpdated;
        }

        private void CollectionDBView_DataContextChanged(object sender, DataContextChangedEventArgs args)
        {
            ParentDocument = this.GetFirstAncestorOfType<DocumentView>()?.ViewModel?.DocumentController;
            updateChart(new Context(ParentDocument));
        }   
        
        //Input Keys
        public static readonly KeyController FilterFieldKey = new KeyController("_FilterField", "B98F5D76-55D6-4796-B53C-D7C645094A85");
        public static readonly KeyController BucketsKey = new KeyController("_Buckets", "5F0974E9-08A1-46BD-89E5-6225C1FE40C7");
        public static readonly KeyController SelectedKey = new KeyController("Selected", "A1AABEE2-D842-490A-875E-72C509011D86");
        public static readonly KeyController InputDocsKey = new KeyController("Dataset", "0F8FD78F-4B35-4D0B-9CA0-17BAF275FE17");
        public static readonly KeyController AutoFitKey = new KeyController("_AutoFit", "79A247CB-CE40-44EA-9EA5-BB295F1F70F5");
        public static readonly KeyController AvgResultKey = new KeyController("Avg", "27A7017A-170E-4E4A-8CDC-94983C2A5188");


        DocumentController _parentDocument;
        public DocumentController ParentDocument {
            get { return _parentDocument; }
            set
            {
                if (ParentDocument != null)
                    ParentDocument.FieldModelUpdated -= ParentDocument_DocumentFieldUpdated;
                _parentDocument = value;
                if (value != null)
                {
                    if (ParentDocument.GetField(BucketsKey) == null)
                        ParentDocument.SetField(BucketsKey, new ListController<NumberController>(new NumberController[] {
                                                        new NumberController(0), new NumberController(0), new NumberController(0), new NumberController(0)}), true);
                    if (ParentDocument.GetField(FilterFieldKey) == null)
                        ParentDocument.SetField(FilterFieldKey, new KeyController(), true);
                    if (ParentDocument.GetField(AutoFitKey) == null)
                        ParentDocument.SetField(AutoFitKey, new NumberController(3), true);
                    if (ParentDocument.GetField(SelectedKey) == null)
                        ParentDocument.SetField(SelectedKey, new ListController<NumberController>(), true);
                    ParentDocument.SetField(AvgResultKey, new NumberController(0), true);
                    ParentDocument.FieldModelUpdated += ParentDocument_DocumentFieldUpdated;
                    xParameter.AddFieldBinding(TextBox.TextProperty, new FieldBinding<KeyController>()
                    {
                        Mode = BindingMode.TwoWay,
                        Document = ParentDocument,
                        Key = CollectionDBView.FilterFieldKey,
                        Converter=new ObjectToStringConverter(),
                        Context = new Context(ParentDocument)
                    });
                    xAutoFit.AddFieldBinding(CheckBox.IsCheckedProperty, new FieldBinding<NumberController>()
                    {
                        Mode = BindingMode.TwoWay,
                        Document = ParentDocument,
                        Key = CollectionDBView.AutoFitKey,
                        Converter = new DoubleToBoolConverter(),
                        Context = new Context(ParentDocument)
                    });
                    // shouldn't have to do this but the binding above wasn't working...
                    xAutoFit.Unchecked +=  (sender, args) => ParentDocument.SetField(CollectionDBView.AutoFitKey, new NumberController(0), true);
                    xAutoFit.Checked += (sender, args) => ParentDocument.SetField(CollectionDBView.AutoFitKey, new NumberController(1), true);

                    xAvg.AddFieldBinding(TextBlock.TextProperty, new FieldBinding<NumberController>()
                    {
                        Mode = BindingMode.TwoWay,
                        Document = ParentDocument,
                        Key = CollectionDBView.AvgResultKey,
                        Converter = new DoubleToStringConverter(),
                        Context = new Context(ParentDocument)
                    });
                }
            }
        }
        public CollectionViewModel ViewModel { get => DataContext as CollectionViewModel; }

        private void ParentDocument_DocumentFieldUpdated(FieldControllerBase sender, FieldUpdatedEventArgs args, Context context)
        {
            var dargs = (DocumentController.DocumentFieldUpdatedEventArgs) args;
            var autofit = ParentDocument?.GetDereferencedField<NumberController>(CollectionDBView.AutoFitKey, context)?.Data != 0;

            if (dargs.Reference.FieldKey.Equals(CollectionDBView.AutoFitKey) ||
                dargs.Reference.FieldKey.Equals(CollectionDBView.FilterFieldKey) ||
                dargs.Reference.FieldKey.Equals(CollectionDBView.BucketsKey) && !autofit ||
                dargs.Reference.FieldKey.Equals(CollectionDBView.SelectedKey) ||
                dargs.Reference.FieldKey.Equals(ViewModel?.CollectionKey))
                updateChart(new Context(ParentDocument));
        }

        public void UpdateBucket(int changedBucket, double maxDomain)
        {
            xAutoFit.IsChecked = false;
            var buckets = (ParentDocument.GetDereferencedField<ListController<NumberController>>(CollectionDBView.BucketsKey, new Context(ParentDocument))).Data.Select((fm) => fm as NumberController).ToList();
            (buckets[changedBucket] as NumberController).Data = maxDomain;
            ParentDocument.SetField(CollectionDBView.BucketsKey, new ListController<NumberController>(buckets), true);
        }
        public void UpdateSelection(int changedBar, bool selected)
        {
            var selectedBars = (ParentDocument.GetDereferencedField<ListController<NumberController>>(CollectionDBView.SelectedKey, new Context(ParentDocument))).Data.Select((fm) => fm as NumberController).ToList();
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
            ParentDocument.SetField(CollectionDBView.SelectedKey, new ListController<NumberController>(selectedBars), true);
        }

        void updateChart(Context context, bool updateViewOnly=false)
        {
            var dbDocs  = ParentDocument?.GetDataDocument().GetDereferencedField<ListController<DocumentController>>(ViewModel.CollectionKey, context)?.TypedData;
            var buckets = ParentDocument?.GetDereferencedField<ListController<NumberController>>(CollectionDBView.BucketsKey, context)?.Data;
            var pattern = ParentDocument?.GetDereferencedField<KeyController>(CollectionDBView.FilterFieldKey, context);
            var autofit = ParentDocument?.GetDereferencedField<NumberController>(CollectionDBView.AutoFitKey, context)?.Data != 0;
            var selectedBars = ParentDocument?.GetDereferencedField<ListController<NumberController>>(CollectionDBView.SelectedKey, context)?.Data;
            if (dbDocs != null && buckets != null)
            {
                if (autofit)
                {
                    buckets = autoFitBuckets(dbDocs.Select((d) => d.GetDataDocument()).ToList(), pattern, buckets.Count) ?? buckets;
                }

                string rawText   = "";
                var    barCounts = filterDocuments(dbDocs, buckets, pattern, selectedBars, updateViewOnly, ref rawText);
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
                var selectedBars = ParentDocument?.GetDereferencedField<ListController<NumberController>>(CollectionDBView.SelectedKey, null)?.Data ?? new List<FieldControllerBase>();
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

        List<FieldControllerBase> autoFitBuckets(List<DocumentController> dbDocs, KeyController pattern, int numBars)
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
            ParentDocument.SetField(CollectionDBView.BucketsKey, new ListController<NumberController>(newBuckets.Select((b) => b as NumberController)), true);

            return newBuckets;
        }

        static KeyController testPatternMatch(DocumentController dmc, KeyController pattern, string term)
        {
            if (string.IsNullOrEmpty(pattern?.Name) || dmc == null || dmc.GetField(KeyStore.AbstractInterfaceKey, true) != null)
                return null;
            // loop through each field to find on that matches the field name pattern 
            foreach (var pfield in dmc.EnumFields().Where((pf) => !pf.Key.IsUnrenderedKey() && pf.Key.Equals(pattern)))
            {
                if (pfield.Key.Equals(pattern))
                {
                    var pvalue = pfield.Value.DereferenceToRoot(new Context(dmc));
                    if (pvalue is DocumentController)
                    {
                        var nestedDoc = pvalue as DocumentController;
                        return testPatternMatch(nestedDoc, null, term);
                    }
                    else if (pvalue is ListController<DocumentController>)
                    {
                        foreach (var nestedDoc in (pvalue as ListController<DocumentController>).TypedData.Select((d) => d.GetDataDocument()))
                            if (testPatternMatch(nestedDoc, null, term) != null)
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

        List<double> filterDocuments(List<DocumentController> dbDocs, List<FieldControllerBase> bars, KeyController pattern,
                                     List<FieldControllerBase> selectedBars, bool updateViewOnly, ref string rawText)
        {
            bool keepAll = selectedBars.Count == 0;

            var collection = new List<DocumentController>();
            var countBars = new List<double>();
            foreach (var b in bars)
                countBars.Add(0);

            var sumOfFields = 0.0;
            if (dbDocs != null && !string.IsNullOrEmpty(pattern?.Name))
            {
                int count = 0;
                foreach (var dmc in dbDocs)
                {
                    count++;
                    //Debug.WriteLine("Count = " + count + " rawText = " + rawText.Length);
                    var visited = new List<DocumentController>();
                    visited.Add(dmc);
                    //if (pattern == null)
                    //{
                    //    var dataDoc = dmc.GetDataDocument(null);
                    //    foreach (var f in dataDoc.EnumFields().Where((f) => !f.Key.IsUnrenderedKey()))
                    //    {
                    //        var refField = new DocumentReferenceController(dataDoc.GetId(), f.Key);
                    //        inspectField(bars, refField, selectedBars, updateViewOnly, ref rawText, keepAll, collection, countBars, ref sumOfFields, dmc, visited);
                    //    }
                    //}
                    //else
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
                ParentDocument.SetField(CollectionDBView.AvgResultKey, new NumberController(sumOfFields / dbDocs.Count), true);
            }
            return countBars;
        }

        void inspectField(List<FieldControllerBase> bars, ReferenceController refField, List<FieldControllerBase> selectedBars, bool updateViewOnly, ref string rawText, bool keepAll, List<DocumentController> collection, List<double> countBars, ref double sumOfFields, DocumentController dmc, List<DocumentController> visited)
        {
            var dataDoc = dmc.GetDataDocument();
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

        static ReferenceController searchInDocumentForNamedField(KeyController pattern, DocumentController srcDoc, List<DocumentController> visited)
        {
            var dmc = srcDoc.GetDataDocument();
            if (string.IsNullOrEmpty(pattern?.Name) || dmc == null || dmc.GetField(KeyStore.AbstractInterfaceKey, true) != null)
                return null;
            // loop through each field to find on that matches the field name pattern 
            if (dmc.GetField(pattern) != null)
                return new DocumentReferenceController(dmc, pattern);
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
        

        #region DragAndDrop
        

        public void SetDropIndicationFill(Brush fill)
        {
        }
        #endregion
    }
}
