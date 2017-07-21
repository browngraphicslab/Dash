using System.Collections.Generic;
using System.Diagnostics;
using DashShared;

namespace Dash.Controllers.Operators
{
    public class DBSearchOperatorFieldModelController : OperatorFieldModelController
    {
        public class SearchOperatorFieldModel : OperatorFieldModel
        {
            /// <summary>
            /// Type of operator it is; to be used by the server to determine what controller to use for operations 
            /// This should probably eventually be an enum
            /// </summary>
            public DocumentController Search { get; set; }

            public string Pattern { get; set; }

            public SearchOperatorFieldModel(string type, DocumentController search, string pattern) : base(type)
            {
                Pattern = pattern;
                Search = search;
            }
        }

        static public FieldModelController CreateSearch(DocumentController searchDoc, string fieldRef)
        {
            var searchOp = OperatorDocumentModel.CreateOperatorDocumentModel(new DBSearchOperatorFieldModelController(new SearchOperatorFieldModel("Search", searchDoc, fieldRef)));

            return new DocumentReferenceController(searchOp.GetId(), DBSearchOperatorFieldModelController.ResultsKey);
        }
        public DBSearchOperatorFieldModelController(SearchOperatorFieldModel operatorFieldModel) : base(operatorFieldModel)
        {
            OperatorFieldModel = operatorFieldModel;
        }
        //Input keys

        //Output keys
        public static readonly Key ResultsKey = new Key("03A2157E-F03C-46A1-8F52-F59BD226944E", "Results");

        public override ObservableDictionary<Key, TypeInfo> Inputs { get; } = new ObservableDictionary<Key, TypeInfo>
        {
        };
        public override ObservableDictionary<Key, TypeInfo> Outputs { get; } = new ObservableDictionary<Key, TypeInfo>
        {
            [ResultsKey] = TypeInfo.Text
        };

        public override void Execute(Dictionary<Key, FieldModelController> inputs, Dictionary<Key, FieldModelController> outputs)
        {
            var pattern = (OperatorFieldModel as SearchOperatorFieldModel).Pattern.Trim(' ', '\r');
            DocumentController Container = (OperatorFieldModel as SearchOperatorFieldModel).Search;

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
                                if (pfield.Key.Name == pattern)
                                    textStr += pfield.Value + " ";
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
