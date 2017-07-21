using System.Collections.Generic;
using System.Diagnostics;
using DashShared;

namespace Dash.Controllers.Operators
{
    public class DBSearchOperatorFieldModelController : OperatorFieldModelController
    {
        public class SearchOperatorFieldModel : OperatorFieldModel
        {

            public string Pattern { get; set; }

            public SearchOperatorFieldModel(string type,string pattern) : base(type)
            {
                Pattern = pattern;
            }
        }
        static public FieldModelController CreateSearch(DocumentController contextDoc, string fieldRef)
        {
            var searchFieldModel = new SearchOperatorFieldModel("Search", fieldRef);
            var searchFieldController = new DBSearchOperatorFieldModelController(searchFieldModel);
            var searchOp = OperatorDocumentModel.CreateOperatorDocumentModel(searchFieldController);
            searchOp.SetField(ContextDocKey, new DocumentFieldModelController(contextDoc), true);
            return new DocumentReferenceController(searchOp.GetId(), DBSearchOperatorFieldModelController.ResultsKey);
        }

        static public FieldModelController CreateSearch(FieldModelController contextDocFieldController, string fieldRef)
        {
            var searchFieldModel = new SearchOperatorFieldModel("Search", fieldRef);
            var searchFieldController = new DBSearchOperatorFieldModelController(searchFieldModel);
            var searchOp = OperatorDocumentModel.CreateOperatorDocumentModel(searchFieldController);
            searchOp.SetField(ContextDocKey, contextDocFieldController, true);
            return new DocumentReferenceController(searchOp.GetId(), DBSearchOperatorFieldModelController.ResultsKey);
        }
        public DBSearchOperatorFieldModelController(SearchOperatorFieldModel operatorFieldModel) : base(operatorFieldModel)
        {
            OperatorFieldModel = operatorFieldModel;
        }
        //Input keys

        //Output keys
        public static readonly Key ResultsKey = new Key("03A2157E-F03C-46A1-8F52-F59BD226944E", "Results");
        public static readonly Key ContextDocKey = new Key("C544405C-6389-4F6D-8C17-31DEB14409D4", "This");

        public override ObservableDictionary<Key, TypeInfo> Inputs { get; } = new ObservableDictionary<Key, TypeInfo>
        {
            [ContextDocKey] = TypeInfo.Document
        };
        public override ObservableDictionary<Key, TypeInfo> Outputs { get; } = new ObservableDictionary<Key, TypeInfo>
        {
            [ResultsKey] = TypeInfo.Text
        };

        public override void Execute(Dictionary<Key, FieldModelController> inputs, Dictionary<Key, FieldModelController> outputs)
        {
            var pattern = new List<string>((OperatorFieldModel as SearchOperatorFieldModel).Pattern.Trim(' ', '\r').Split('.'));
            DocumentController Container = (inputs[ContextDocKey] as DocumentFieldModelController).Data;// (OperatorFieldModel as SearchOperatorFieldModel).Search;

            var documents = new List<DocumentController>();
            var textStr = "";
            foreach (var dmc in ContentController.GetControllers<DocumentController>())
            {
                foreach (var field in dmc.EnumFields())
                    if (field.Value is DocumentFieldModelController)
                    {
                        var dfmc = field.Value as DocumentFieldModelController;
                        if (dfmc.Data == Container)
                        {
                            foreach (var pfield in dmc.EnumFields())
                                if (pfield.Key.Name == pattern[0])
                                {
                                    if (pfield.Value is DocumentFieldModelController)
                                        foreach (var f in (pfield.Value as DocumentFieldModelController).Data.EnumFields())
                                        {
                                            if (pattern.Count == 1)
                                            {
                                                if (!f.Key.Name.StartsWith("_"))
                                                    textStr += f.Key.Name + "=" + f.Value+ " ";
                                            } else
                                            {
                                                if (pattern[1][0] == '~')
                                                {
                                                    if (f.Key.Name.Contains(pattern[1].Substring(1, pattern.Count - 1)))
                                                        textStr += f.Value + " ";
                                                }
                                                else
                                                if (f.Key.Name == pattern[1])
                                                    textStr += f.Value + " ";

                                            }
                                        }
                                    else
                                        textStr += pfield.Value + " ";
                                    documents.Add(dmc);
                                }
                        }
                    }
            }
            outputs[ResultsKey] = new TextFieldModelController(textStr);
        }

        public override FieldModelController Copy()
        {
            return new DivideOperatorFieldModelController(OperatorFieldModel);
        }
    }
}
