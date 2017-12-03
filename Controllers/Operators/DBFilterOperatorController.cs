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
        public static readonly DocumentType DBFilterType = new DocumentType("B6E8FE1B-C7F6-44E8-B574-82542E9B6734", "DBFilter");
        public DBFilterOperatorFieldModel DBFilterOperatorFieldModel { get { return OperatorFieldModel as DBFilterOperatorFieldModel; } }
        static DocumentType DBFilterOperatorType = new DocumentType("52AC96D1-0102-4930-A555-67D8B20C7BE2", "DBFilterOperator");
        static public DocumentController CreateFilter(ReferenceController inputDocs, string fieldRef)
        {
            var filterFieldController = new DBFilterOperatorController(new DBFilterOperatorFieldModel());
            var filterOp = OperatorDocumentFactory.CreateOperatorDocument(filterFieldController);
            filterOp.DocumentType = DBFilterOperatorType;
            
            filterOp.SetField(InputDocsKey,    inputDocs, true);
            filterOp.SetField(FilterFieldKey,  new TextController(fieldRef), true);
            filterOp.SetField(AutoFitKey,      new NumberController(1), true);
            filterOp.SetField(ClassKey,        new TextController("Filter"), true);
            filterOp.SetField(SelectedKey,     new ListController<NumberController>(), true);
            filterOp.SetField(BucketsKey,      new ListController<NumberController>(
                new NumberController[] { new NumberController(0), new NumberController(0), new NumberController(0), new NumberController(0) }
                ), true);


            var layoutDoc = new DBFilterOperatorBox(new DocumentReferenceController(filterOp.GetId(), KeyStore.OperatorKey)).Document;

            filterOp.SetActiveLayout(layoutDoc, true, true); 
            
            // this field stores the Avg so that the operator view can have something to bind to.
            filterOp.SetField(SelfAvgResultKey, new DocumentReferenceController(filterOp.GetId(), DBFilterOperatorController.AvgResultKey), true);
            filterOp.SetField(KeyStore.PrimaryKeyKey, new ListController<TextController>(new TextController[] { new TextController(ClassKey.Id), new TextController(FilterFieldKey.Id) }), true);

            return filterOp;
        }

        public DBFilterOperatorController() : base(new DBFilterOperatorFieldModel())
        {
        }
        public DBFilterOperatorController(DBFilterOperatorFieldModel operatorFieldModel) : base(operatorFieldModel)
        {
            OperatorFieldModel = operatorFieldModel;
        }

        //

        //Output keys
        public static readonly KeyController AvgResultKey    = new KeyController("27A7017A-170E-4E4A-8CDC-94983C2A5188", "Avg");
        public static readonly KeyController CountBarsKey    = new KeyController("539D338C-1851-4E45-A6E3-145B3659C237", "CountBars");

        //Self-stored output value keys
        public static readonly KeyController SelfAvgResultKey = new KeyController("D8CCB9B7-C934-4ECC-8588-C68C67B8A88B", "Avg");
        //class primary key
        public static readonly KeyController ClassKey         = new KeyController("018E279A-119B-42E6-9AAF-00A5F76A08F1", "Filter");

        //Input Keys
        public static readonly KeyController FilterFieldKey  = new KeyController("B98F5D76-55D6-4796-B53C-D7C645094A85", "_FilterField");
        public static readonly KeyController BucketsKey      = new KeyController("5F0974E9-08A1-46BD-89E5-6225C1FE40C7", "_Buckets");
        public static readonly KeyController SelectedKey     = new KeyController("A1AABEE2-D842-490A-875E-72C509011D86", "Selected");
        public static readonly KeyController InputDocsKey    = new KeyController("0F8FD78F-4B35-4D0B-9CA0-17BAF275FE17", "Dataset");
        public static readonly KeyController AutoFitKey      = new KeyController("79A247CB-CE40-44EA-9EA5-BB295F1F70F5", "_AutoFit");
        
        public override ObservableDictionary<KeyController, IOInfo> Inputs { get; } = new ObservableDictionary<KeyController, IOInfo>
        {
            [InputDocsKey]   = new IOInfo(TypeInfo.List, true),
            [FilterFieldKey] = new IOInfo(TypeInfo.Text, true),
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
            var dbDocs       = (idocs as ListController<DocumentController>)?.TypedData ?? (idocs as DocumentController)?.GetDereferencedField<ListController<DocumentController>>(CollectionNote.CollectedDocsKey, null).TypedData;
            var selectedBars = (!inputs.ContainsKey(SelectedKey))  ? null : (inputs[SelectedKey]    as ListController<NumberController>)?.Data;
            var pattern      =  !inputs.ContainsKey(FilterFieldKey)? null : (inputs[FilterFieldKey] as TextController)?.Data.Trim(' ', '\r').Split(new char[] { '.' }, StringSplitOptions.RemoveEmptyEntries);
            var autofit      =  !inputs.ContainsKey(AutoFitKey)    ? false :(inputs[AutoFitKey]    as NumberController).Data != 0;
            var buckets      =  !inputs.ContainsKey(BucketsKey)    ? null : (inputs[BucketsKey]  as ListController<NumberController>)?.Data;

            if (dbDocs != null)
                filterDocuments(dbDocs, autofit ? autoFitBuckets(dbDocs, pattern.ToList(), buckets.Count) : buckets, pattern.ToList(), selectedBars, outputs);
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

        public void filterDocuments(List<DocumentController> dbDocs, List<FieldControllerBase> bars, List<string> pattern, List<FieldControllerBase> selectedBars, Dictionary<KeyController, FieldControllerBase> outputs)
        {
            bool keepAll = selectedBars.Count == 0;

            var collection = new List<DocumentController>();
            var countBars = new List<NumberController>();
            foreach (var b in bars)
                countBars.Add(new NumberController(0));

            var sumOfFields = 0.0;
            if (dbDocs != null && pattern.Count() != 0)
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

        private static ReferenceController SearchInDocumentForNamedField(List<string> pattern, DocumentController srcDoc, DocumentController dmc, List<DocumentController> visited)
        {
            if (pattern.Count == 0 || dmc == null || dmc.GetField(KeyStore.AbstractInterfaceKey, true) != null)
                return null;
            // loop through each field to find on that matches the field name pattern 
            foreach (var pfield in dmc.EnumFields().Where((pf) => pf.Key.Name == pattern[0] || pattern[0] == "" || pf.Value is DocumentController))
            {
                if (pfield.Value is DocumentController)
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
                else if (pattern.Count == 1)
                {
                    return new DocumentReferenceController(srcDoc.GetId(), pfield.Key);
                }
            }
            return null;
        }

        public override FieldModelController<OperatorModel> Copy()
        {
            return new DBFilterOperatorController(OperatorFieldModel as DBFilterOperatorFieldModel);
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
