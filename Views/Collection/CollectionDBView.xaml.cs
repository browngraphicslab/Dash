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
        public CollectionViewType ViewType => CollectionViewType.DB;
        public CollectionDBView()
        {
            InitializeComponent(); 
            Loaded             += CollectionDBView_Loaded;
            Unloaded           += CollectionDBView_Unloaded;
            SizeChanged        += (sender, e) => UpdateChart(new Context(ParentDocument), true);
            //xParameter.Style    = Application.Current.Resources["xSearchTextBox"] as Style;
            MinWidth = MinHeight = 50;
            xTagCloud.TermDragStarting += XTagCloud_TermDragStarting;
        }


        public void SetupContextMenu(MenuFlyout contextMenu)
        {

        }
        private void XTagCloud_TermDragStarting(string term, DragStartingEventArgs args)
        {
            var dbDocs = ParentDocument.GetDataDocument().GetDereferencedField<ListController<DocumentController>>(ViewModel.CollectionKey, null);
            var pattern = ParentDocument.GetDereferencedField<KeyController>(FilterFieldKey, null);

            if (dbDocs == null || pattern == null || string.IsNullOrEmpty(pattern.Name))
            {
                return;
            }

            var collection = dbDocs.Select(d =>
            {
                KeyController key =  TestPatternMatch(d.GetDataDocument(), pattern, term);

                if (key == null)
                {
                    return null;
                }

                string derefField = d.GetDataDocument().GetDereferencedField<TextController>(key, null)?.Data;
                DocumentController rnote = new RichTextNote(derefField ?? "<empty>").Document;
                rnote.GetDataDocument().SetField(SelectedKey, new TextController(term), true);
                return rnote;
            });

            args.Data.SetDragModel(new DragDocumentModel(collection.Where(c => c != null).ToList(), CollectionViewType.Schema));
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
            UpdateChart(new Context(ParentDocument));
        }
        public void OnDocumentSelected(bool selected)
        {
        }

        //Input Keys
        public static readonly KeyController FilterFieldKey = KeyController.Get("DBChartField");
        public static readonly KeyController BucketsKey = KeyController.Get("DBChartBuckets");
        public static readonly KeyController SelectedKey = KeyController.Get("DBChartSelected");
        public static readonly KeyController AutoFitKey = KeyController.Get("DBChartAutoFit");
        public static readonly KeyController AvgResultKey = KeyController.Get("DBChartAvg");


        private DocumentController _parentDocument;
        public DocumentController ParentDocument {
            get => _parentDocument;
            set
            {
                if (ParentDocument != null)
                {
                    ParentDocument.FieldModelUpdated -= ParentDocument_DocumentFieldUpdated;
                }

                _parentDocument = value;
                if (value != null)
                {
                    if (_parentDocument.GetField(BucketsKey) == null)
                    {
                        _parentDocument.SetField(BucketsKey, new ListController<NumberController>(new[] {
                                                        new NumberController(0), new NumberController(0), new NumberController(0), new NumberController(0)}), true);
                    }

                    if (_parentDocument.GetField(AutoFitKey) == null)
                    {
                        _parentDocument.SetField(AutoFitKey, new NumberController(3), true);
                    }

                    if (_parentDocument.GetField(SelectedKey) == null)
                    {
                        _parentDocument.SetField(SelectedKey, new ListController<NumberController>(), true);
                    }

                    _parentDocument.SetField(AvgResultKey, new NumberController(0), true);
                    _parentDocument.FieldModelUpdated += ParentDocument_DocumentFieldUpdated;
                    xParameter.AddFieldBinding(TextBlock.TextProperty, new FieldBinding<KeyController, TextController>
                    {
                        Mode = BindingMode.TwoWay,
                        Document = ParentDocument,
                        Key = FilterFieldKey,
                        Converter=new ObjectToStringConverter(),
                        Context = new Context(ParentDocument)
                    });
                    xAutoFit.AddFieldBinding(CheckBox.IsCheckedProperty, new FieldBinding<NumberController>
                    {
                        Mode = BindingMode.TwoWay,
                        Document = ParentDocument,
                        Key = AutoFitKey,
                        Converter = new DoubleToBoolConverter(),
                        Context = new Context(ParentDocument)
                    });
                    // shouldn't have to do this but the binding above wasn't working...
                    xAutoFit.Unchecked +=  (sender, args) => ParentDocument.SetField(AutoFitKey, new NumberController(0), true);
                    xAutoFit.Checked += (sender, args) => ParentDocument.SetField(AutoFitKey, new NumberController(1), true);

                    xAvg.AddFieldBinding(TextBlock.TextProperty, new FieldBinding<NumberController>
                    {
                        Mode = BindingMode.TwoWay,
                        Document = ParentDocument,
                        Key = AvgResultKey,
                        Converter = new DoubleToStringConverter(),
                        Context = new Context(ParentDocument)
                    });
                }
            }
        }
        public CollectionViewModel ViewModel => DataContext as CollectionViewModel;

        private void ParentDocument_DocumentFieldUpdated(FieldControllerBase sender, FieldUpdatedEventArgs args, Context context)
        {
            var dargs = (DocumentController.DocumentFieldUpdatedEventArgs) args;
            bool autofit = ParentDocument?.GetDereferencedField<NumberController>(AutoFitKey, context)?.Data != 0;

            if (dargs.Reference.FieldKey.Equals(AutoFitKey) ||
                dargs.Reference.FieldKey.Equals(FilterFieldKey) ||
                dargs.Reference.FieldKey.Equals(BucketsKey) && !autofit ||
                dargs.Reference.FieldKey.Equals(SelectedKey) ||
                dargs.Reference.FieldKey.Equals(ViewModel?.CollectionKey))
            {
                UpdateChart(new Context(ParentDocument));
            }
        }

        public void UpdateBucket(int changedBucket, double maxDomain)
        {
            xAutoFit.IsChecked = false;
            var buckets =
                ParentDocument.GetDereferencedField<ListController<NumberController>>(BucketsKey, new Context(ParentDocument));
            buckets[changedBucket].Data = maxDomain;
        }

        public void UpdateSelection(int changedBar, bool selected)
        {
            var selectedBars =
                ParentDocument.GetDereferencedField<ListController<NumberController>>(SelectedKey, new Context(ParentDocument));
            bool found = false;
            foreach (var sel in selectedBars.ToList())
            {
                if (sel.Data == changedBar)
                {
                    found = true;
                    if (!selected)
                    {
                        selectedBars.Remove(sel);
                        break;
                    }
                }
            }

            if (!found && selected)
            {
                selectedBars.Add(new NumberController(changedBar));
            }
        }

        private void UpdateChart(Context context, bool updateViewOnly=false)
        {
            ParentDocument = this.GetFirstAncestorOfType<DocumentView>().ViewModel.DocumentController;
            var dbDocs  = ParentDocument?.GetDataDocument().GetDereferencedField<ListController<DocumentController>>(ViewModel.CollectionKey, context);
            IList<NumberController> buckets = ParentDocument?.GetDereferencedField<ListController<NumberController>>(BucketsKey, context);
            var pattern = ParentDocument?.GetDereferencedField<KeyController>(FilterFieldKey, context);
            bool autofit = ParentDocument?.GetDereferencedField<NumberController>(AutoFitKey, context)?.Data != 0;
            var selectedBars = ParentDocument?.GetDereferencedField<ListController<NumberController>>(SelectedKey, context);
            if (dbDocs != null && buckets != null && pattern != null)
            {
                if (autofit)
                {
                    buckets = AutoFitBuckets(dbDocs.Select(d => d.GetDataDocument()).ToList(), pattern, buckets.Count) ?? buckets;
                }

                string rawText   = "";
                var    barCounts = FilterDocuments(dbDocs, buckets, pattern, selectedBars, updateViewOnly, ref rawText);
                double barSum    = UpdateBars(buckets, barCounts);

                if (barSum == 0 && dbDocs.Count > 0)
                {
                    xTagCloud.TheText = rawText;
                    xNumberContent.Visibility = Visibility.Collapsed;
                    xTextContent.Visibility = Visibility.Visible;
                }
            }
        }

        private double UpdateBars(IList<NumberController> buckets, IList<double> barCounts)
        {
            var xBars = xBarChart.Children.Select(c => (c as CollectionDBChartBar)).ToList();
            if (xBars.Count == barCounts.Count) // have all of the bars already, just need to change their heights
            {
                for (int i = 0; i < buckets.Count; i++)
                {
                    xBars[i].MaxDomain = buckets[i].Data;
                }
            }
            else // otherwise, need to build all the bars
            {
                var selectedBars = ParentDocument?.GetDereferencedField<ListController<NumberController>>(SelectedKey, null);
                var selectedInds = selectedBars?.Select(f => f.Data).ToList() ?? new List<double>();
                xBarChart.Children.Clear();
                xBarChart.ColumnDefinitions.Clear();
                xBarChart.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
                for (int i = 0; i < buckets.Count; i++)
                {
                    xBarChart.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(10, GridUnitType.Star) }); // add bar spacing
                    xBarChart.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });                       // add bar
                    var g = new CollectionDBChartBar { FilterChart = this, BucketIndex = i, MaxDomain = buckets[i].Data, IsSelected = selectedInds.Contains(i) };
                    Grid.SetColumn(g, i * 2 + 1);
                    xBarChart.Children.Add(g);
                }
                xBars = xBarChart.Children.Select(c => (c as CollectionDBChartBar)).ToList();
            }


            double barSum = barCounts.Aggregate((v, b) => v + b);
            for (int i = 0; i < barCounts.Count; i++)
            {
                xBars[i].xBar.Height = xBarChart.ActualHeight * barCounts[i] / Math.Max(1, barSum);
            }

            return barSum;
        }

        private IList<NumberController> AutoFitBuckets(List<DocumentController> dbDocs, KeyController pattern, int numBars)
        {
            double minValue = double.MaxValue;
            double maxValue = double.MinValue;
            foreach (var dmc in dbDocs.ToArray())
            {
                var visited = new List<DocumentController> {dmc};
                var refField = SearchInDocumentForNamedField(pattern, dmc, visited);
                var field = refField?.GetDocumentController(new Context(dmc)).GetDereferencedField<NumberController>(refField.FieldKey, new Context(dmc));
                if (field != null)
                {
                    minValue = Math.Min(minValue, field.Data);
                    maxValue = Math.Max(maxValue, field.Data);
                }
            }
            if (minValue == double.MaxValue)
            {
                return null;
            }

            double barDomain = (maxValue - minValue) / numBars;
            double barStart = minValue + barDomain;
            var barDomains = new List<NumberController>();

            for (int i = 0; i < numBars; i++)
            {
                barDomains.Add(new NumberController(barStart));
                barStart += barDomain;
            }
            ParentDocument.SetField(BucketsKey, new ListController<NumberController>(barDomains), true);

            return barDomains;
        }

        private static KeyController TestPatternMatch(DocumentController dmc, KeyController pattern, string term)
        {
            if (string.IsNullOrEmpty(pattern?.Name) || dmc == null || dmc.GetField(KeyStore.AbstractInterfaceKey, true) != null)
            {
                return null;
            }
            // loop through each field to find on that matches the field name pattern 
            foreach (var pfield in dmc.EnumFields().Where(pf => !pf.Key.IsUnrenderedKey() && pf.Key.Equals(pattern)))
            {
                if (pfield.Key.Equals(pattern))
                {
                    var pvalue = pfield.Value.DereferenceToRoot(new Context(dmc));
                    if (pvalue is DocumentController)
                    {
                        var nestedDoc = pvalue as DocumentController;
                        return TestPatternMatch(nestedDoc, null, term);
                    }

                    if (pvalue is ListController<DocumentController>)
                    {
                        foreach (var nestedDoc in (pvalue as ListController<DocumentController>).Select(d => d.GetDataDocument()))
                        {
                            if (TestPatternMatch(nestedDoc, null, term) != null)
                            {
                                return pfield.Key;
                            }
                        }
                    }
                    else if (pvalue is TextController)
                    {
                        string text = (pvalue as TextController).Data;
                        if (text.Contains(term))
                        {
                            return pfield.Key;
                        }
                    }
                }
            }
            return null;
        }

        private IList<double> FilterDocuments(IList<DocumentController> dbDocs, IList<NumberController> bars, KeyController pattern,
                                     IList<NumberController> selectedBars, bool updateViewOnly, ref string rawText)
        {
            bool keepAll = selectedBars.Count == 0;

            var collection = new List<DocumentController>();
            var countBars = bars.Select(b => 0.0).ToList();

            double sumOfFields = 0.0;
            if (dbDocs != null && !string.IsNullOrEmpty(pattern?.Name))
            {
                foreach (var dmc in dbDocs)
                {
                    //Debug.WriteLine("Count = " + count + " rawText = " + rawText.Length);
                    var visited = new List<DocumentController> {dmc};
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
                        var refField = SearchInDocumentForNamedField(pattern, dmc, visited);
                        InspectField(bars, refField, selectedBars, updateViewOnly, ref rawText, keepAll, collection, countBars, ref sumOfFields, dmc);
                    }
                }
            }
            if (!updateViewOnly)
            {
                if (!ViewModel.CollectionKey.Equals(KeyStore.CollectionOutputKey)) // avoid inifinite loop -- input is output.  this happens when something tries to display the CollectionOutputKey field (e.g., Interface Builder showing all document fields)
                {
                    ParentDocument.SetField(KeyStore.CollectionOutputKey, new ListController<DocumentController>(collection), true);
                }

                ParentDocument.SetField(AvgResultKey, new NumberController(sumOfFields / dbDocs.Count), true);
            }
            return countBars;
        }

        private void InspectField(IList<NumberController> bars, ReferenceController refField, IList<NumberController> selectedBars, bool updateViewOnly, ref string rawText, bool keepAll, IList<DocumentController> collection, IList<double> countBars, ref double sumOfFields, DocumentController dmc)
        {
            var dataDoc = dmc.GetDataDocument();
            var field = refField?.GetDocumentController(new Context(dataDoc)).GetDereferencedField(refField.FieldKey, new Context(dataDoc));
            if (field is ListController<DocumentController>)
            {
                var counted = FilterDocuments(field as ListController<DocumentController>, bars, null, selectedBars, updateViewOnly, ref rawText);
                for (int i = 0; i < counted.Count; i++)
                {
                    countBars[i] += counted[i] > 0 ? 1 : 0;
                }
            }
            else if (field != null)
            {
                if (field is ListController<TextController> textList)
                {
                    foreach (var tfmc in textList)
                    {
                        rawText += " " + tfmc.Data;
                    }
                } else
                {
                    rawText += " " + field.GetValue(new Context(dataDoc));
                }

                var numberField = field as NumberController;
                if (numberField != null)
                {
                    sumOfFields += numberField.Data;
                    foreach (var b in bars)
                    {
                        if (numberField.Data <= b.Data)
                        {
                            countBars[bars.IndexOf(b)]++;
                            if (keepAll || selectedBars.Select(fm => (int)fm.Data).Contains(bars.IndexOf(b)))
                            {
                                collection.Add(dmc);
                            }

                            break;
                        }
                    }
                }
                else if (keepAll)
                {
                    collection.Add(dmc);
                }
            }
        }

        private static ReferenceController SearchInDocumentForNamedField(KeyController pattern, DocumentController srcDoc, List<DocumentController> visited)
        {
            var dmc = srcDoc.GetDataDocument();
            if (string.IsNullOrEmpty(pattern?.Name) || dmc == null || dmc.GetField(KeyStore.AbstractInterfaceKey, true) != null)
            {
                return null;
            }
            // loop through each field to find on that matches the field name pattern 
            if (dmc.GetField(pattern) != null)
            {
                return new DocumentReferenceController(dmc, pattern);
            }

            foreach (var pfield in dmc.EnumFields().Where(pf => !pf.Key.IsUnrenderedKey() && pf.Value is DocumentController))
            {
                var nestedDoc = pfield.Value as DocumentController;
                if (!visited.Contains(nestedDoc))
                {
                    visited.Add(nestedDoc);
                    var field = SearchInDocumentForNamedField(pattern, nestedDoc, visited);
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
