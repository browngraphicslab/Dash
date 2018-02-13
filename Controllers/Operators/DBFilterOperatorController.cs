using System.Collections.Generic;
using System.Diagnostics;
using DashShared;
using System.Linq;
using Dash.Converters;
using System;
using static Dash.NoteDocuments;

namespace Dash.Controllers.Operators
{
    public class DBFilterOperatorFieldModel : OperatorModel
    {
        public DBFilterOperatorFieldModel() : base(OperatorType.DBfilter)
        {
        }
    }
    public class DBFilterOperatorController : OperatorController
    {
        public DBFilterOperatorFieldModel DBFilterOperatorFieldModel { get { return OperatorFieldModel as DBFilterOperatorFieldModel; } }
        

        //

        public override ObservableDictionary<KeyController, IOInfo> Inputs { get; } = new ObservableDictionary<KeyController, IOInfo>
        {
            [InputDocsKey]   = new IOInfo(TypeInfo.List, true),
            [FilterFieldKey] = new IOInfo(TypeInfo.Key, true),
            [BucketsKey]     = new IOInfo(TypeInfo.List, true),
            [AutoFitKey]     = new IOInfo(TypeInfo.Number, true),
            [SelectedKey]    = new IOInfo(TypeInfo.List, false)
        };
        public override ObservableDictionary<KeyController, TypeInfo> Outputs { get; } = new ObservableDictionary<KeyController, TypeInfo>
        {
            [KeyStore.CollectionOutputKey]    = TypeInfo.List,
            [CountBarsKey]  = TypeInfo.List,
            [BucketsKey]    = TypeInfo.List,
            [AvgResultKey]  = TypeInfo.Number
        };

        public override void Execute(Dictionary<KeyController, FieldControllerBase> inputs, Dictionary<KeyController, FieldControllerBase> outputs)
        {
            var idocs        = (!inputs.ContainsKey(InputDocsKey)) ? null : (inputs[InputDocsKey]);
            var dbDocs       = (idocs as ListController<DocumentController>)?.TypedData ?? (idocs as DocumentController)?.GetDereferencedField<ListController<DocumentController>>(KeyStore.CollectionKey, null).TypedData;
            var selectedBars = (!inputs.ContainsKey(SelectedKey))  ? null : (inputs[SelectedKey]    as ListController<NumberController>)?.Data;
            var pattern      =  !inputs.ContainsKey(FilterFieldKey)? null : (inputs[FilterFieldKey] as KeyController);
            var autofit      =  !inputs.ContainsKey(AutoFitKey)    ? false :(inputs[AutoFitKey]    as NumberController).Data != 0;
            var buckets      =  !inputs.ContainsKey(BucketsKey)    ? null : (inputs[BucketsKey]  as ListController<NumberController>)?.Data;

            if (dbDocs != null)
                filterDocuments(dbDocs, autofit ? autoFitBuckets(dbDocs, pattern, buckets.Count) : buckets, pattern, selectedBars, outputs);
        }
        

        static List<FieldControllerBase> autoFitBuckets(List<DocumentController> dbDocs, KeyController pattern, int numBars)
        {
            double minValue = double.MaxValue;
            double maxValue = double.MinValue;
            foreach (var dmc in dbDocs.ToArray())
            {
                var visited = new List<DocumentController>();
                visited.Add(dmc);
                var refField = SearchInDocumentForNamedField(pattern, dmc, dmc, visited);
                var field = refField?.GetDocumentController(new Context(dmc)).GetDereferencedField<NumberController>(refField.FieldKey, new Context(dmc));
                if (field != null)
                {
                    if (field.Data < minValue) minValue = field.Data;
                    if (field.Data > maxValue) maxValue = field.Data;
                }
            }
            double barDomain = (maxValue - minValue) / numBars;
            double barStart = minValue + barDomain;
            var barDomains = new List<NumberController>();

            for (int i = 0; i < numBars; i++)
            {
                barDomains.Add(new NumberController(barStart));
                barStart += barDomain;
            }

            return barDomains.Select((b) => b as FieldControllerBase).ToList();
        }

        public void filterDocuments(List<DocumentController> dbDocs, List<FieldControllerBase> bars, KeyController pattern, List<FieldControllerBase> selectedBars, Dictionary<KeyController, FieldControllerBase> outputs)
        {
            bool keepAll = selectedBars.Count == 0;

            var collection = new List<DocumentController>();
            var countBars = new List<NumberController>();
            foreach (var b in bars)
                countBars.Add(new NumberController(0));

            var sumOfFields = 0.0;
            if (dbDocs != null && !string.IsNullOrEmpty(pattern?.Name))
            {
                foreach (var dmc in dbDocs.ToArray())
                {
                    var visited = new List<DocumentController>();
                    visited.Add(dmc);

                    var refField = SearchInDocumentForNamedField(pattern, dmc, dmc, visited);
                    var field = refField?.GetDocumentController(new Context(dmc)).GetDereferencedField<NumberController>(refField.FieldKey, new Context(dmc));
                    if (field != null)
                    {
                        sumOfFields += field.Data;
                        foreach (var b in bars)
                        {
                            if (field.Data <= (b as NumberController).Data)
                            {
                                (countBars[bars.IndexOf(b)] as NumberController).Data++;
                                if (keepAll || selectedBars.Select((fm) => (fm as NumberController).Data).ToList().Contains(bars.IndexOf(b)))
                                    collection.Add(dmc);
                                break;
                            }
                        }
                    }
                }
            }
            outputs[CountBarsKey] = new ListController<NumberController>(countBars);
            outputs[KeyStore.CollectionOutputKey]   = new ListController<DocumentController>(collection);
            outputs[BucketsKey]   = new ListController<NumberController>(bars.Select((b) => b as NumberController));
            outputs[AvgResultKey] = new NumberController(sumOfFields / dbDocs.Count);
        }

        private static ReferenceController SearchInDocumentForNamedField(KeyController pattern, DocumentController srcDoc, DocumentController dmc, List<DocumentController> visited)
        {
            if (string.IsNullOrEmpty(pattern?.Name) || dmc == null || dmc.GetField(KeyStore.AbstractInterfaceKey, true) != null)
                return null;
            // loop through each field to find on that matches the field name pattern 
            if (dmc.GetField(pattern) != null)
                return new DocumentReferenceController(srcDoc.GetId(), pattern);
            foreach (var pfield in dmc.EnumFields().Where((pf) => pf.Value is DocumentController))
            {
                var nestedDoc = pfield.Value as DocumentController;
                if (!visited.Contains(nestedDoc))
                {
                    visited.Add(nestedDoc);
                    var field = SearchInDocumentForNamedField(pattern, nestedDoc, nestedDoc, visited);
                    if (field != null)
                        return field;
                }
            }
            return null;
        }
        public override object GetValue(Context context)
        {
            throw new System.NotImplementedException();
        }
        public override bool SetValue(object value)
        {
            return false;
        }
    }
}
