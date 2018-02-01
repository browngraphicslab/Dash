using Dash.Controllers.Operators;
using Dash.Converters;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI;
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
    public sealed partial class DBFilterChart : UserControl
    {
        public DBFilterChart()
        {
            this.InitializeComponent();
            this.Loaded += DBFilterChart_Loaded;
            xParameter.Style = Application.Current.Resources["xPlainTextBox"] as Style;
        }

        private void DBFilterChart_Loaded(object sender, RoutedEventArgs e)
        {
            // hack to force operator to execute once when loaded...
            OpDoc.SetField(DBFilterOperatorController.AutoFitKey, new NumberController(2), true);
        }

        DocumentController _opDoc;
        public DocumentController OpDoc
        {
            get { return _opDoc; }
            set
            {
                _opDoc = value;
                xFound.AddFieldBinding(TextBlock.TextProperty, new FieldBinding<ListController<DocumentController>>()
                {
                    Mode = BindingMode.TwoWay,
                    Document = OpDoc,
                    Key = DBFilterOperatorController.InputDocsKey,
                    Converter = new DocumentCollectionToStringConverter(true),
                    Context = new Context(OpDoc)
                });
                xParameter.AddFieldBinding(TextBox.TextProperty, new FieldBinding<KeyController>()
                {
                    Mode = BindingMode.TwoWay,
                    Document = OpDoc,
                    Key = DBFilterOperatorController.FilterFieldKey,
                    Context = new Context(OpDoc),
                    Converter=new ObjectToStringConverter()
                });
                xAutoFit.AddFieldBinding(CheckBox.IsCheckedProperty, new FieldBinding<NumberController>()
                {
                    Mode = BindingMode.TwoWay,
                    Document = OpDoc,
                    Key = DBFilterOperatorController.AutoFitKey,
                    Converter = new DoubleToBoolConverter(),
                    Context = new Context(OpDoc)
                });
                xAvg.AddFieldBinding(TextBlock.TextProperty, new FieldBinding<NumberController>()
                {
                    Mode = BindingMode.TwoWay,
                    Document = OpDoc,
                    Key = DBFilterOperatorController.SelfAvgResultKey,
                    Converter = new DoubleToStringConverter(),
                    Context = new Context(OpDoc)
                });
            }
        }
        public void UpdateBucket(int changedBucket, double maxDomain)
        {
            xAutoFit.IsChecked = false;
            var buckets = (_opDoc.GetDereferencedField<ListController<NumberController>>(DBFilterOperatorController.BucketsKey, new Context(_opDoc))).Data.Select((fm) => fm as NumberController).ToList();
            (buckets[changedBucket] as NumberController).Data = maxDomain;
            _opDoc.SetField(DBFilterOperatorController.BucketsKey, new ListController<NumberController>(buckets), true);
        }
        public void UpdateSelection(int changedBar, bool selected)
        {
            var selectedBars = (_opDoc.GetDereferencedField<ListController<NumberController>>(DBFilterOperatorController.SelectedKey, new Context(_opDoc))).Data.Select((fm) => fm as NumberController).ToList();
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
            _opDoc.SetField(DBFilterOperatorController.SelectedKey, new ListController<NumberController>(selectedBars), true);
        }
        public void OperatorOutputChanged(Context context)
        {
            FieldControllerBase barcnts, results, buckets, avg;
            if (context.TryDereferenceToRoot(new DocumentFieldReference(_opDoc.GetId(), DBFilterOperatorController.CountBarsKey), out barcnts) &&
                context.TryDereferenceToRoot(new DocumentFieldReference(_opDoc.GetId(), KeyStore.CollectionOutputKey), out results) &&
                context.TryDereferenceToRoot(new DocumentFieldReference(_opDoc.GetId(), DBFilterOperatorController.BucketsKey), out buckets) &&
                context.TryDereferenceToRoot(new DocumentFieldReference(_opDoc.GetId(), DBFilterOperatorController.AvgResultKey), out avg))
            {
                var xBars = xBarChart.Children.Select((c) => (c as DBFilterChartBar)).ToList();

                var barCounts = (barcnts as ListController<NumberController>)?.Data;
                if (xBars.Count == barCounts.Count)
                {
                    var barDomains = (buckets as ListController<NumberController>).Data;
                    for (int i = 0; i < barDomains.Count(); i++)
                    {
                        xBars[i].MaxDomain = (barDomains[i] as NumberController).Data;
                    }
                }
                else
                {
                    setupBars((buckets as ListController<NumberController>)?.Data);
                    xBars = xBarChart.Children.Select((c) => (c as DBFilterChartBar)).ToList();
                }

                {
                    var savedBuckets = (_opDoc.GetDereferencedField<ListController<NumberController>>(DBFilterOperatorController.BucketsKey, new Context(_opDoc))).Data.Select((fm) => fm as NumberController).ToList();
                    var anyBucketChanged = false;
                    for (int i = 0; i < (buckets as ListController<NumberController>)?.Data?.Count; i++)
                    {
                        var newBucket = ((buckets as ListController<NumberController>).Data[i] as NumberController)?.Data ?? 0;
                        if (newBucket != (savedBuckets[i] as NumberController).Data)
                            anyBucketChanged = true;
                        (savedBuckets[i] as NumberController).Data = newBucket;
                    }
                    if (anyBucketChanged)
                        _opDoc.SetField(DBFilterOperatorController.BucketsKey, new ListController<NumberController>(savedBuckets), true);
                }

                double barSum = 0;
                foreach (var b in barCounts)
                {
                    xBars[barCounts.IndexOf(b)].xBar.Height = xBarChart.ActualHeight * (b as NumberController).Data;
                    barSum += (b as NumberController).Data;
                }
                foreach (var b in barCounts)
                    xBars[barCounts.IndexOf(b)].xBar.Height /= Math.Max(1, barSum);
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
                var g = new DBFilterChartBar() { FilterChart = this, BucketIndex = i, MaxDomain = (buckets[i] as NumberController).Data };
                Grid.SetColumn(g, i * 2 + 1);
                this.xBarChart.Children.Add(g);
            }
            this.xBarChart.ColumnDefinitions.Add(new ColumnDefinition() { Width = GridLength.Auto });
        }
    }
}
