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
            filterOp.SetField(FieldPatternKey, new TextFieldModelController(fieldRef), true);
            filterOp.SetField(InputDocsKey,    new ReferenceFieldModelController(dbDoc.GetId(), KeyStore.DataKey), true);

            var layoutDoc = new DBFilterOperatorBox(new ReferenceFieldModelController(filterOp.GetId(), OperatorDocumentModel.OperatorKey)).Document;
            filterOp.SetActiveLayout(layoutDoc, true, true);

            var outDOc = new DocumentController(new Dictionary<KeyController, FieldModelController>(), DashConstants.DocumentTypeStore.CollectionDocument);
            outDOc.SetField(ResultsKey, new ReferenceFieldModelController(filterOp.GetId(), ResultsKey), true);
            filterOp.SetField(KeyStore.DocumentContextKey, new DocumentFieldModelController(outDOc), true);
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
        public static readonly KeyController InputDocsKey    = new KeyController("0F8FD78F-4B35-4D0B-9CA0-17BAF275FE17", "Input Collection");
        public static readonly KeyController FieldPatternKey = new KeyController("7FEF304A-AC8E-4A94-B1C2-1F1D60FFAF62", "Filter Field");

        public override ObservableDictionary<KeyController, IOInfo> Inputs { get; } = new ObservableDictionary<KeyController, IOInfo>
        {
            [FieldPatternKey] = new IOInfo(TypeInfo.Text, true),
            [InputDocsKey] = new IOInfo(TypeInfo.Collection, true)
        };
        public override ObservableDictionary<KeyController, TypeInfo> Outputs { get; } = new ObservableDictionary<KeyController, TypeInfo>
        {
            [ResultsKey] = TypeInfo.Collection
        };

        public override void Execute(Dictionary<KeyController, FieldModelController> inputs, Dictionary<KeyController, FieldModelController> outputs)
        {
            var pattern = new List<string>((inputs[FieldPatternKey] as TextFieldModelController).Data.Trim(' ', '\r').Split('.'));
            var dbDocs = (!inputs.ContainsKey(InputDocsKey)) ? null : (inputs[InputDocsKey] as DocumentCollectionFieldModelController)?.Data;
            if (dbDocs == null)
                return;
            var docsInSearchScope = dbDocs;

            var documents = new List<DocumentController>();
            foreach (var dmc in docsInSearchScope.ToArray())
            {
                var visited = new List<DocumentController>();
                visited.Add(dmc);
                if (SearchInDocumentForNamedField(pattern, dmc, visited))
                {
                     documents.Add(dmc);
                }
            }

            outputs[ResultsKey] = new DocumentCollectionFieldModelController(documents);
        }

        private static bool SearchInDocumentForNamedField(List<string> pattern, DocumentController dmc, List<DocumentController> visited)
        {
            if (dmc == null)
                return false;
            // loop through each field to find on that matches the field name pattern 
            foreach (var pfield in dmc.EnumFields().Where((pf) => pf.Key.Name == pattern[0] || pattern[0] == "" || pf.Value is DocumentFieldModelController))
            {
                if (pfield.Value is DocumentFieldModelController)
                {
                    var nestedDoc = (pfield.Value as DocumentFieldModelController).Data;
                    if (!visited.Contains(nestedDoc))
                    {
                        visited.Add(nestedDoc);
                        if (SearchInDocumentForNamedField(pattern, nestedDoc, visited))
                            return true;
                    }
                } else if (pattern.Count == 1)
                {
                    return true;
                }
                else if (pfield.Value is DocumentFieldModelController)
                    foreach (var f in (pfield.Value as DocumentFieldModelController).Data.EnumFields())
                    {
                        if ((pattern[1] != "" && pattern[1][0] == '~' && f.Key.Name.Contains(pattern[1].Substring(1, pattern.Count - 1))) || f.Key.Name == pattern[1])
                        {
                            return true;
                        }
                    }
            }
            return false;
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
