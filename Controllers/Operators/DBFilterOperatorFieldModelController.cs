﻿using System.Collections.Generic;
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
        static DocumentType DBFilterOperatorType = new DocumentType("52AC96D1-0102-4930-A555-67D8B20C7BE2", "DBFilterOperator");
        static public DocumentController CreateFilter(ReferenceFieldModelController inputDocs, string fieldRef)
        {
            var filterFieldController = new DBFilterOperatorFieldModelController(new DBFilterOperatorFieldModel());
            var filterOp = OperatorDocumentModel.CreateOperatorDocumentModel(filterFieldController);
            filterOp.DocumentType = DBFilterOperatorType;
            
            filterOp.SetField(InputDocsKey,    inputDocs, true);
            filterOp.SetField(FilterFieldKey,  new TextFieldModelController(fieldRef), true);
            filterOp.SetField(AutoFitKey,      new NumberFieldModelController(1), true);
            filterOp.SetField(ClassKey,        new TextFieldModelController("Filter"), true);
            filterOp.SetField(SelectedKey,     new ListFieldModelController<NumberFieldModelController>(), true);
            filterOp.SetField(BucketsKey,      new ListFieldModelController<NumberFieldModelController>(
                new NumberFieldModelController[] { new NumberFieldModelController(0), new NumberFieldModelController(0), new NumberFieldModelController(0), new NumberFieldModelController(0) }
                ), true);

            var layoutDoc = new DBFilterOperatorBox(new DocumentReferenceFieldController(filterOp.GetId(), OperatorDocumentModel.OperatorKey)).Document;
            filterOp.SetActiveLayout(layoutDoc, true, true); 
            
            // this field stores the Avg so that the operator view can have something to bind to.
            filterOp.SetField(SelfAvgResultKey, new DocumentReferenceFieldController(filterOp.GetId(), DBFilterOperatorFieldModelController.AvgResultKey), true);
            filterOp.SetField(KeyStore.PrimaryKeyKey, new ListFieldModelController<TextFieldModelController>(new TextFieldModelController[] { new TextFieldModelController(ClassKey.Id), new TextFieldModelController(FilterFieldKey.Id) }), true);

            return filterOp;
        }

        public DBFilterOperatorFieldModelController() : base(new DBFilterOperatorFieldModel())
        {
        }
        public DBFilterOperatorFieldModelController(DBFilterOperatorFieldModel operatorFieldModel) : base(operatorFieldModel)
        {
            OperatorFieldModel = operatorFieldModel;
        }

        //

        //Output keys
        public static readonly KeyController ResultsKey      = new KeyController("AE54F402-3B8F-4437-A71F-FF8B9B804194", "Results");
        public static readonly KeyController AvgResultKey    = new KeyController("27A7017A-170E-4E4A-8CDC-94983C2A5188", "Avg");
        public static readonly KeyController CountBarsKey    = new KeyController("539D338C-1851-4E45-A6E3-145B3659C237", "CountBars");

        //Self-stored output value keys
        public static readonly KeyController SelfAvgResultKey = new KeyController("D8CCB9B7-C934-4ECC-8588-C68C67B8A88B", "Avg");
        //class primary key
        public static readonly KeyController ClassKey         = new KeyController("018E279A-119B-42E6-9AAF-00A5F76A08F1", "Filter");

        //Input Keys
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
            [AutoFitKey]     = new IOInfo(TypeInfo.Number, true),
            [SelectedKey]    = new IOInfo(TypeInfo.List, false)
        };
        public override ObservableDictionary<KeyController, TypeInfo> Outputs { get; } = new ObservableDictionary<KeyController, TypeInfo>
        {
            [ResultsKey]    = TypeInfo.Collection,
            [CountBarsKey]  = TypeInfo.Collection,
            [BucketsKey]    = TypeInfo.List,
            [AvgResultKey]  = TypeInfo.Number
        };

        public override void Execute(Dictionary<KeyController, FieldControllerBase> inputs, Dictionary<KeyController, FieldControllerBase> outputs)
        {
            var idocs        = (!inputs.ContainsKey(InputDocsKey)) ? null : (inputs[InputDocsKey]);
            var dbDocs       = (idocs as DocumentCollectionFieldModelController)?.Data ?? (idocs as DocumentFieldModelController)?.Data.GetDereferencedField<DocumentCollectionFieldModelController>(CollectionNote.CollectedDocsKey, null).Data;
            var selectedBars = (!inputs.ContainsKey(SelectedKey))  ? null : (inputs[SelectedKey]    as ListFieldModelController<NumberFieldModelController>)?.Data;
            var pattern      =  !inputs.ContainsKey(FilterFieldKey)? null : (inputs[FilterFieldKey] as TextFieldModelController)?.Data.Trim(' ', '\r').Split(new char[] { '.' }, StringSplitOptions.RemoveEmptyEntries);
            var autofit      =  !inputs.ContainsKey(AutoFitKey)    ? false :(inputs[AutoFitKey]    as NumberFieldModelController).Data != 0;
            var buckets      =  !inputs.ContainsKey(BucketsKey)    ? null : (inputs[BucketsKey]  as ListFieldModelController<NumberFieldModelController>)?.Data;

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
                var field = refField?.GetDocumentController(new Context(dmc)).GetDereferencedField<NumberFieldModelController>(refField.FieldKey, new Context(dmc));
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

            return barDomains.Select((b) => b as FieldControllerBase).ToList();
        }

        public void filterDocuments(List<DocumentController> dbDocs, List<FieldControllerBase> bars, List<string> pattern, List<FieldControllerBase> selectedBars, Dictionary<KeyController, FieldControllerBase> outputs)
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
                    var field = refField?.GetDocumentController(new Context(dmc)).GetDereferencedField<NumberFieldModelController>(refField.FieldKey, new Context(dmc));
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
                    return new DocumentReferenceFieldController(srcDoc.GetId(), pfield.Key);
                }
            }
            return null;
        }

        public override FieldModelController<OperatorFieldModel> Copy()
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
