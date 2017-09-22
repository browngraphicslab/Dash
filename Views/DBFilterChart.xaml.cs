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
            xParameter.Style = Application.Current.Resources["xPlainTextBox"] as Style;
            xParameter.TextChanged += (sender, args) => UpdateChart(); 
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
            }
        }

        public void UpdateFilter()
        {
            var bars = new List<DBFilterChartBar>();
            bool keepAll = true;
            foreach (var b in xBarChart.Children.Select((c) => c as DBFilterChartBar))
            {
                bars.Add(b);
                if (b.IsSelected)
                    keepAll = false;
            }

            var dbDocs = (OpDoc.GetDereferencedField(DBFilterOperatorFieldModelController.InputDocsKey, new Context(OpDoc)) as DocumentCollectionFieldModelController)?.Data;
            var pattern = new List<string>(xParameter.Text.Trim(' ', '\r').Split(new char[] { '.' }, StringSplitOptions.RemoveEmptyEntries));
            var collection = new DocumentCollectionFieldModelController(new DocumentController[] { });
            if (dbDocs != null &&  pattern.Count != 0) {
                foreach (var dmc in dbDocs.ToArray())
                {
                    var visited = new List<DocumentController>();
                    visited.Add(dmc);
                    var refField = SearchInDocumentForNamedField(pattern, dmc, dmc, visited);
                    var field = refField?.GetDocumentController(new Context(dmc)).GetDereferencedField<NumberFieldModelController>(refField.FieldKey, null);
                    if (field != null)
                        foreach (var b in bars)
                            if (field.Data <= b.MaxDomain)
                            {
                                if (b.IsSelected || keepAll)
                                    collection.AddDocument(dmc);
                                break;
                            }
                }
            }
            OpDoc.SetField(DBFilterOperatorFieldModelController.FilterKey, collection, true);
        }

        public void UpdateChart(bool autoFit = false)
        {
            var dbDocs = (OpDoc.GetDereferencedField(DBFilterOperatorFieldModelController.InputDocsKey, new Context(OpDoc)) as DocumentCollectionFieldModelController)?.Data;
            var pattern = new List<string>(xParameter.Text.Trim(' ', '\r').Split(new char[] { '.' }, StringSplitOptions.RemoveEmptyEntries));
            if (dbDocs == null || pattern.Count == 0)
                return;
            
            var bars = new List<DBFilterChartBar>();
            foreach (var b in xBarChart.Children.Select((c) => c as DBFilterChartBar))
            {
                bars.Add(b);
                b.ItemCount = 0;
            }
            if (autoFit || true)
            {
                AutoFitBars(dbDocs, pattern, bars);
            }
            foreach (var dmc in dbDocs.ToArray())
            {
                var visited = new List<DocumentController>();
                visited.Add(dmc);
                var refField = SearchInDocumentForNamedField(pattern, dmc, dmc, visited);
                var field = refField?.GetDocumentController(new Context(dmc)).GetDereferencedField<NumberFieldModelController>(refField.FieldKey, null);
                if (field != null)
                {
                    foreach (var b in bars)
                        if (field.Data <= b.MaxDomain)
                        {
                            b.ItemCount++;
                            break;
                        }

                }
                else (xBarChart.Children[0] as DBFilterChartBar).ItemCount++;
            }
            
            double barSum = 0;
            foreach (var b in bars)
            {
                b.xBar.Height = xBarChart.ActualHeight * b.ItemCount;
                barSum += b.ItemCount;
            }
            foreach (var b in bars)
                b.xBar.Height /= Math.Max(1, barSum);
        }

        static void AutoFitBars(List<DocumentController> dbDocs, List<string> pattern, List<DBFilterChartBar> bars)
        {
            double minValue = double.MaxValue;
            double maxValue = double.MinValue;
            foreach (var dmc in dbDocs.ToArray())
            {
                var visited = new List<DocumentController>();
                visited.Add(dmc);
                var refField = SearchInDocumentForNamedField(pattern, dmc, dmc, visited);
                var field = refField?.GetDocumentController(null).GetDereferencedField<NumberFieldModelController>(refField.FieldKey, null);
                if (field != null)
                {
                    if (field.Data < minValue) minValue = field.Data;
                    if (field.Data > maxValue) maxValue = field.Data;
                }
            }
            double barDomain = (maxValue - minValue) / bars.Count;
            double barStart = minValue + barDomain;
            foreach (var b in bars)
            {
                b.MaxDomain = barStart;
                b.xDomain.Text = barStart.ToString();
                barStart += barDomain;
            }
        }

        private static ReferenceFieldModelController SearchInDocumentForNamedField(List<string> pattern, DocumentController srcDoc, DocumentController dmc, List<DocumentController> visited)
        {
            if (dmc == null || dmc.GetField(KeyStore.AbstractInterfaceKey, true) != null)
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
                    return new ReferenceFieldModelController(srcDoc.GetId(), pfield.Key);
                }
            }
            return null;
        }
        public int NumberBars
        {
            set
            {
                this.xBarChart.Children.Clear();
                this.xBarChart.ColumnDefinitions.Clear();
                this.xBarChart.ColumnDefinitions.Add(new ColumnDefinition() { Width = GridLength.Auto });
                for (int i = 0; i < value; i++)
                {
                    this.xBarChart.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(10, GridUnitType.Star) });
                    this.xBarChart.ColumnDefinitions.Add(new ColumnDefinition() { Width = GridLength.Auto });
                    var g = new DBFilterChartBar() { FilterChart = this };
                    Grid.SetColumn(g, i * 2 + 1);
                    this.xBarChart.Children.Add(g);
                }
                this.xBarChart.ColumnDefinitions.Add(new ColumnDefinition() { Width = GridLength.Auto });
            }
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            this.UpdateChart(true);
        }
    }
}
