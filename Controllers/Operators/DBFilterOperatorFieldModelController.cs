using System.Collections.Generic;
using System.Diagnostics;
using DashShared;
using System.Linq;
using Dash.Converters;
using System;
using static Dash.NoteDocuments;

namespace Dash.Controllers.Operators
{
    public class DBFilterOperatorFieldModel : OperatorFieldModel
    {
        public DBFilterOperatorFieldModel() : base("DBfilter")
        {
        }
    }
    public class DBFilterOperatorFieldModelController : OperatorFieldModelController
    {
        public static readonly DocumentType DBFilterType = new DocumentType("B6E8FE1B-C7F6-44E8-B574-82542E9B6734", "DBFilter");
        public DBFilterOperatorFieldModel DBFilterOperatorFieldModel { get { return OperatorFieldModel as DBFilterOperatorFieldModel; } }

        static public DocumentController CreateFilter(DocumentController dbDoc, string fieldRef)
        {
            var filterFieldController = new DBFilterOperatorFieldModelController(new DBFilterOperatorFieldModel());
            var filterOp = OperatorDocumentModel.CreateOperatorDocumentModel(filterFieldController);

        
            filterOp.SetField(InputDocsKey,    new ReferenceFieldModelController(dbDoc.GetId(), KeyStore.DataKey), true);
            filterOp.SetField(FilterFieldKey,  new TextFieldModelController(""), true);
            filterOp.SetField(AutoFitKey,      new NumberFieldModelController(0), true);
            filterOp.SetField(SelectedKey,     new ListFieldModelController<NumberFieldModelController>(), true);
            filterOp.SetField(BucketsKey,   new ListFieldModelController<NumberFieldModelController>(
                new NumberFieldModelController[] { new NumberFieldModelController(0), new NumberFieldModelController(0), new NumberFieldModelController(0), new NumberFieldModelController(0) }
                ), true);

            var layoutDoc = new DBFilterOperatorBox(new ReferenceFieldModelController(filterOp.GetId(), OperatorDocumentModel.OperatorKey)).Document;
            filterOp.SetActiveLayout(layoutDoc, true, true);

            return filterOp;
        }

        public DBFilterOperatorFieldModelController() : base(new DBFilterOperatorFieldModel())
        {
        }
        public DBFilterOperatorFieldModelController(DBFilterOperatorFieldModel operatorFieldModel) : base(operatorFieldModel)
        {
            OperatorFieldModel = operatorFieldModel;
        }

        //Output keys
        public static readonly KeyController ResultsKey      = new KeyController("AE54F402-3B8F-4437-A71F-FF8B9B804194", "Results");
        public static readonly KeyController AvgResultKey    = new KeyController("27A7017A-170E-4E4A-8CDC-94983C2A5188", "Avg");
        public static readonly KeyController CountBarsKey    = new KeyController("539D338C-1851-4E45-A6E3-145B3659C237", "CountBars");

        public static readonly KeyController FilterFieldKey  = new KeyController("B98F5D76-55D6-4796-B53C-D7C645094A85", "FilterField");
        public static readonly KeyController BucketsKey      = new KeyController("5F0974E9-08A1-46BD-89E5-6225C1FE40C7", "Buckets");
        public static readonly KeyController SelectedKey     = new KeyController("A1AABEE2-D842-490A-875E-72C509011D86", "Selected");
        public static readonly KeyController InputDocsKey    = new KeyController("0F8FD78F-4B35-4D0B-9CA0-17BAF275FE17", "Dataset");
        public static readonly KeyController AutoFitKey      = new KeyController("79A247CB-CE40-44EA-9EA5-BB295F1F70F5", "AutoFit");
        
        public override ObservableDictionary<KeyController, IOInfo> Inputs { get; } = new ObservableDictionary<KeyController, IOInfo>
        {
            [InputDocsKey]   = new IOInfo(TypeInfo.Collection, true),
            [FilterFieldKey] = new IOInfo(TypeInfo.Text, true),
            [BucketsKey]     = new IOInfo(TypeInfo.List, true),
            [AutoFitKey]     = new IOInfo(TypeInfo.Number, false),
            [SelectedKey]    = new IOInfo(TypeInfo.List, false)
        };
        public override ObservableDictionary<KeyController, TypeInfo> Outputs { get; } = new ObservableDictionary<KeyController, TypeInfo>
        {
            [ResultsKey]    = TypeInfo.Collection,
            [CountBarsKey]  = TypeInfo.Collection,
            [BucketsKey]    = TypeInfo.List,
            [AvgResultKey]  = TypeInfo.Number
        };

        public override void Execute(Dictionary<KeyController, FieldModelController> inputs, Dictionary<KeyController, FieldModelController> outputs)
        {
            var idocs        = (!inputs.ContainsKey(InputDocsKey)) ? null : (inputs[InputDocsKey]);
            var dbDocs       = (idocs as DocumentCollectionFieldModelController)?.Data ?? (idocs as DocumentFieldModelController)?.Data.GetDereferencedField<DocumentCollectionFieldModelController>(CollectionNote.CollectedDocsKey, null).Data;
            var selectedBars = (!inputs.ContainsKey(SelectedKey))  ? null : (inputs[SelectedKey]    as ListFieldModelController<NumberFieldModelController>)?.Data;
            var pattern      =  !inputs.ContainsKey(FilterFieldKey)? null : (inputs[FilterFieldKey] as TextFieldModelController)?.Data.Trim(' ', '\r').Split(new char[] { '.' }, StringSplitOptions.RemoveEmptyEntries);
            var autofit      =  !inputs.ContainsKey(AutoFitKey)    ? false :(inputs[AutoFitKey]    as NumberFieldModelController).Data != 0;
            var buckets      =  !inputs.ContainsKey(BucketsKey)    ? null : (inputs[BucketsKey]  as ListFieldModelController<NumberFieldModelController>)?.Data;

            if (dbDocs != null)
                filterDocuments(dbDocs, autofit ? autoFitBuckets(dbDocs, pattern.ToList(), buckets.Count, inputs) : buckets, pattern.ToList(), selectedBars, inputs, outputs);
        }
        

        static List<FieldModelController> autoFitBuckets(List<DocumentController> dbDocs, List<string> pattern, int numBars, Dictionary<KeyController, FieldModelController> inputs)
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
            double barDomain = (maxValue - minValue) / numBars;
            double barStart = minValue + barDomain;
            var barDomains = new List<NumberFieldModelController>();

            for (int i = 0; i < numBars; i++)
            {
                barDomains.Add(new NumberFieldModelController(barStart));
                barStart += barDomain;
            }

            return barDomains.Select((b) => b as FieldModelController).ToList();
        }

        public void filterDocuments(List<DocumentController> dbDocs, List<FieldModelController> bars, List<string> pattern, List<FieldModelController> selectedBars, Dictionary<KeyController, FieldModelController> inputs, Dictionary<KeyController, FieldModelController> outputs)
        {
            bool keepAll = selectedBars.Count == 0;

            var collection = new List<DocumentController>();
            var countBars = new List<NumberFieldModelController>();
            foreach (var b in bars)
                countBars.Add(new NumberFieldModelController(0));

            var sumOfFields = 0.0;
            if (dbDocs != null && pattern.Count() != 0)
            {
                foreach (var dmc in dbDocs.ToArray())
                {
                    var visited = new List<DocumentController>();
                    visited.Add(dmc);

                    var refField = SearchInDocumentForNamedField(pattern, dmc, dmc, visited);
                    var field = refField?.GetDocumentController(new Context(dmc)).GetDereferencedField<NumberFieldModelController>(refField.FieldKey, null);
                    if (field != null)
                    {
                        sumOfFields += field.Data;
                        foreach (var b in bars)
                        {
                            if (field.Data <= (b as NumberFieldModelController).Data)
                            {
                                (countBars[bars.IndexOf(b)] as NumberFieldModelController).Data++;
                                if (keepAll || selectedBars.Select((fm) => (fm as NumberFieldModelController).Data).ToList().Contains(bars.IndexOf(b)))
                                    collection.Add(dmc);
                                break;
                            }
                        }
                    }
                }
            }
            outputs[CountBarsKey] = new ListFieldModelController<NumberFieldModelController>(countBars);
            outputs[ResultsKey]   = new DocumentCollectionFieldModelController(collection);
            outputs[BucketsKey]   = new ListFieldModelController<NumberFieldModelController>(bars.Select((b) => b as NumberFieldModelController));
            outputs[AvgResultKey] = new NumberFieldModelController(sumOfFields / dbDocs.Count);
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
                    return new ReferenceFieldModelController(srcDoc.GetId(), pfield.Key);
                }
            }
            return null;
        }

        public override FieldModelController Copy()
        {
            return new DBFilterOperatorFieldModelController(OperatorFieldModel as DBFilterOperatorFieldModel);
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
