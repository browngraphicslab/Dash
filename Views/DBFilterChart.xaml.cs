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
            OpDoc.SetField(DBFilterOperatorFieldModelController.AutoFitKey, new NumberFieldModelController(2), true);
        }

        DocumentController _opDoc;
        public DocumentController OpDoc
        {
            get { return _opDoc; }
            set
            {
                _opDoc = value;
                xFound.AddFieldBinding(TextBlock.TextProperty, new FieldBinding<DocumentCollectionFieldModelController>()
                {
                    Mode = BindingMode.TwoWay,
                    Document = OpDoc,
                    Key = DBFilterOperatorFieldModelController.InputDocsKey,
                    Converter = new DocumentCollectionToStringConverter(true),
                    Context = new Context(OpDoc)
                });
                xParameter.AddFieldBinding(TextBox.TextProperty, new FieldBinding<TextFieldModelController>()
                {
                    Mode = BindingMode.TwoWay,
                    Document = OpDoc,
                    Key = DBFilterOperatorFieldModelController.FilterFieldKey,
                    Context = new Context(OpDoc)
                });
                xAutoFit.AddFieldBinding(CheckBox.IsCheckedProperty, new FieldBinding<NumberFieldModelController>()
                {
                    Mode = BindingMode.TwoWay,
                    Document = OpDoc,
                    Key = DBFilterOperatorFieldModelController.AutoFitKey,
                    Converter = new DoubleToBoolConverter(),
                    Context = new Context(OpDoc)
                });
                xAvg.AddFieldBinding(TextBlock.TextProperty, new FieldBinding<NumberFieldModelController>()
                {
                    Mode = BindingMode.TwoWay,
                    Document = OpDoc,
                    Key = DBFilterOperatorFieldModelController.SelfAvgResultKey,
                    Converter = new DoubleToStringConverter(),
                    Context = new Context(OpDoc)
                });
            }
        }
        public void UpdateBucket(int changedBucket, double maxDomain)
        {
            xAutoFit.IsChecked = false;
            var buckets = (_opDoc.GetDereferencedField<ListFieldModelController<NumberFieldModelController>>(DBFilterOperatorFieldModelController.BucketsKey, new Context(_opDoc))).Data.Select((fm) => fm as NumberFieldModelController).ToList();
            (buckets[changedBucket] as NumberFieldModelController).Data = maxDomain;
            _opDoc.SetField(DBFilterOperatorFieldModelController.BucketsKey, new ListFieldModelController<NumberFieldModelController>(buckets), true);
        }
        public void UpdateSelection(int changedBar, bool selected)
        {
            var selectedBars = (_opDoc.GetDereferencedField<ListFieldModelController<NumberFieldModelController>>(DBFilterOperatorFieldModelController.SelectedKey, new Context(_opDoc))).Data.Select((fm) => fm as NumberFieldModelController).ToList();
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
            _opDoc.SetField(DBFilterOperatorFieldModelController.SelectedKey, new ListFieldModelController<NumberFieldModelController>(selectedBars), true);
        }
        public void OperatorOutputChanged(Context context)
        {
            FieldControllerBase barcnts, results, buckets, avg;
            if (context.TryDereferenceToRoot(new DocumentFieldReference(_opDoc.GetId(), DBFilterOperatorFieldModelController.CountBarsKey), out barcnts) &&
                context.TryDereferenceToRoot(new DocumentFieldReference(_opDoc.GetId(), DBFilterOperatorFieldModelController.ResultsKey), out results) &&
                context.TryDereferenceToRoot(new DocumentFieldReference(_opDoc.GetId(), DBFilterOperatorFieldModelController.BucketsKey), out buckets) &&
                context.TryDereferenceToRoot(new DocumentFieldReference(_opDoc.GetId(), DBFilterOperatorFieldModelController.AvgResultKey), out avg))
            {
                var xBars = xBarChart.Children.Select((c) => (c as DBFilterChartBar)).ToList();

                var barCounts = (barcnts as ListFieldModelController<NumberFieldModelController>)?.Data;
                if (xBars.Count == barCounts.Count)
                {
                    var barDomains = (buckets as ListFieldModelController<NumberFieldModelController>).Data;
                    for (int i = 0; i < barDomains.Count(); i++)
                    {
                        xBars[i].MaxDomain = (barDomains[i] as NumberFieldModelController).Data;
                    }
                }
                else
                {
                    setupBars((buckets as ListFieldModelController<NumberFieldModelController>)?.Data);
                    xBars = xBarChart.Children.Select((c) => (c as DBFilterChartBar)).ToList();
                }

                {
                    var savedBuckets = (_opDoc.GetDereferencedField<ListFieldModelController<NumberFieldModelController>>(DBFilterOperatorFieldModelController.BucketsKey, new Context(_opDoc))).Data.Select((fm) => fm as NumberFieldModelController).ToList();
                    var anyBucketChanged = false;
                    for (int i = 0; i < (buckets as ListFieldModelController<NumberFieldModelController>)?.Data?.Count; i++)
                    {
                        var newBucket = ((buckets as ListFieldModelController<NumberFieldModelController>).Data[i] as NumberFieldModelController)?.Data ?? 0;
                        if (newBucket != (savedBuckets[i] as NumberFieldModelController).Data)
                            anyBucketChanged = true;
                        (savedBuckets[i] as NumberFieldModelController).Data = newBucket;
                    }
                    if (anyBucketChanged)
                        _opDoc.SetField(DBFilterOperatorFieldModelController.BucketsKey, new ListFieldModelController<NumberFieldModelController>(savedBuckets), true);
                }

                double barSum = 0;
                foreach (var b in barCounts)
                {
                    xBars[barCounts.IndexOf(b)].xBar.Height = xBarChart.ActualHeight * (b as NumberFieldModelController).Data;
                    barSum += (b as NumberFieldModelController).Data;
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
                var g = new DBFilterChartBar() { FilterChart = this, BucketIndex = i, MaxDomain = (buckets[i] as NumberFieldModelController).Data };
                Grid.SetColumn(g, i * 2 + 1);
                this.xBarChart.Children.Add(g);
            }
            this.xBarChart.ColumnDefinitions.Add(new ColumnDefinition() { Width = GridLength.Auto });
        }
    }
}
