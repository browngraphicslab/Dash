using System.Collections.Generic;
using System.Diagnostics;
using DashShared;
using System.Linq;
using Dash.Converters;
using System;

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
        public static readonly KeyController FilterKey       = new KeyController("A1AABEE2-D842-490A-875E-72C509011D86", "Filter");
        public static readonly KeyController InputDocsKey    = new KeyController("0F8FD78F-4B35-4D0B-9CA0-17BAF275FE17", "Dataset");
        
        public override ObservableDictionary<KeyController, IOInfo> Inputs { get; } = new ObservableDictionary<KeyController, IOInfo>
        {
            [InputDocsKey] = new IOInfo(TypeInfo.Collection, true),
            [FilterKey]    = new IOInfo(TypeInfo.Collection, true)
        };
        public override ObservableDictionary<KeyController, TypeInfo> Outputs { get; } = new ObservableDictionary<KeyController, TypeInfo>
        {
            [ResultsKey] = TypeInfo.Collection
        };

        public override void Execute(Dictionary<KeyController, FieldModelController> inputs, Dictionary<KeyController, FieldModelController> outputs)
        {
            var dbDocs     = (!inputs.ContainsKey(InputDocsKey)) ? null : (inputs[InputDocsKey] as DocumentCollectionFieldModelController)?.Data;
            var filterDocs = (!inputs.ContainsKey(FilterKey)) ? null : (inputs[FilterKey] as DocumentCollectionFieldModelController)?.Data;
            if (dbDocs == null)
                return;
            var docsInSearchScope = dbDocs;

            var documents = filterDocs == null ? dbDocs : new List<DocumentController>();
            if (filterDocs != null)
                foreach (var d in dbDocs)
                {
                    if (filterDocs.Contains(d))
                        documents.Add(d);
                }
            
            // perform filter based on filter inputs specified in DBFilterChart UI 

            outputs[ResultsKey] = new DocumentCollectionFieldModelController(documents);
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
