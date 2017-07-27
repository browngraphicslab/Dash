using System.Collections.Generic;
using System.Diagnostics;
using DashShared;
using System.Linq;
using Dash.Converters;

namespace Dash.Controllers.Operators
{
    public class DBSearchOperatorFieldModel : OperatorFieldModel
    {
        public string Pattern { get; set; }

        public DBSearchOperatorFieldModel(string type, string pattern) : base(type)
        {
            Pattern = pattern;
        }
    }
    public class DBSearchOperatorFieldModelController : OperatorFieldModelController
    {
        public static void ForceUpdate(DocumentFieldReference docFieldRef)
        {
            var opDoc = ContentController.GetController<DocumentController>(docFieldRef.DocumentId);
            opDoc.Execute(null, true);
        }
        public DBSearchOperatorFieldModel DBSearchOperatorFieldModel {  get { return OperatorFieldModel as DBSearchOperatorFieldModel; } }
        public string Pattern
        {
            get { return DBSearchOperatorFieldModel.Pattern; }
            set { DBSearchOperatorFieldModel.Pattern = value; }
        }
       
        static public DocumentController CreateSearch(DocumentController searchForDoc, string fieldRef)
        {
            var searchFieldModel = new DBSearchOperatorFieldModel("Search", fieldRef);
            var searchFieldController = new DBSearchOperatorFieldModelController(searchFieldModel);
            var searchOp = OperatorDocumentModel.CreateOperatorDocumentModel(searchFieldController);
            searchOp.SetField(SearchForDocKey, new DocumentFieldModelController(searchForDoc), true);
            return searchOp;
        }

        static public DocumentController CreateSearch(FieldModelController fieldContainingSearchForDoc, string fieldRef)
        {
            var searchFieldModel = new DBSearchOperatorFieldModel("Search", fieldRef);
            var searchFieldController = new DBSearchOperatorFieldModelController(searchFieldModel);
            var searchOp = OperatorDocumentModel.CreateOperatorDocumentModel(searchFieldController);
            if (fieldContainingSearchForDoc != null)
                searchOp.SetField(SearchForDocKey, fieldContainingSearchForDoc, true);
            return searchOp;
        }
        public DBSearchOperatorFieldModelController(DBSearchOperatorFieldModel operatorFieldModel) : base(operatorFieldModel)
        {
            OperatorFieldModel = operatorFieldModel;
        }
        //Input keys

        //Output keys
        public static readonly Key ResultsKey = new Key("03A2157E-F03C-46A1-8F52-F59BD226944E", "Results");
        public static readonly Key SearchForDocKey = new Key("C544405C-6389-4F6D-8C17-31DEB14409D4", "SearchForDoc");

        public override ObservableDictionary<Key, TypeInfo> Inputs { get; } = new ObservableDictionary<Key, TypeInfo>
        {
            [SearchForDocKey] = TypeInfo.Document
        };
        public override ObservableDictionary<Key, TypeInfo> Outputs { get; } = new ObservableDictionary<Key, TypeInfo>
        {
            [ResultsKey] = TypeInfo.Collection
        };

        public override void Execute(Dictionary<Key, FieldModelController> inputs, Dictionary<Key, FieldModelController> outputs)
        {
            var pattern = new List<string>((OperatorFieldModel as DBSearchOperatorFieldModel).Pattern.Trim(' ', '\r').Split('.'));
            var searchForDoc = (inputs[SearchForDocKey] as DocumentFieldModelController).Data;// (OperatorFieldModel as SearchOperatorFieldModel).Search;
            
            var docsInSearchScope = findDocsThatReferenceDocument(searchForDoc);

            var textStr = "";
            var documents = new List<DocumentController>();
            foreach (var dmc in docsInSearchScope.ToArray())
                textStr += SearchInDocumentForNamedField(pattern, dmc, ref documents);
            
            if (!outputs.ContainsKey(ResultsKey))
                outputs[ResultsKey] = new DocumentCollectionFieldModelController(documents);
            else (outputs[ResultsKey] as DocumentCollectionFieldModelController).SetDocuments(documents);
        }

        private static IEnumerable<DocumentController> findDocsThatReferenceDocument(DocumentController targetDocument)
        {
            var docsInSearchScope = new List<DocumentController>();
            foreach (var dmc in ContentController.GetControllers<DocumentController>())
                if (!dmc.DocumentType.Type.Contains("Box") && 
                    dmc.DocumentType != StackingPanel.DocumentType && 
                    dmc.DocumentType != GridPanel.GridPanelDocumentType && 
                    dmc.DocumentType != GridViewLayout.DocumentType) {
                    if (targetDocument == null)
                    {
                        docsInSearchScope.Add(dmc);
                    }
                    foreach (var field in dmc.EnumFields())
                        if (field.Value is DocumentFieldModelController)
                        {
                            var dfmc = field.Value as DocumentFieldModelController;
                            if (dfmc.Data == targetDocument)
                            {
                                docsInSearchScope.Add(dmc);
                                break;
                            }
                        }
                }
            return docsInSearchScope;
        }

        private static string SearchInDocumentForNamedField(List<string> pattern, DocumentController dmc, ref List<DocumentController> documents)
        {
            var textStr = "";
            // loop through each field to find on that matches the field name pattern 
            foreach (var pfield in dmc.EnumFields().Where((pf)=>pf.Key.Name == pattern[0] || pattern[0] == ""))
            {
                if (pattern.Count == 1)
                {
                    documents.Add(dmc);
                    textStr += "Document(" + new DocumentControllerToStringConverter().ConvertDataToXaml(dmc) + ")";
                    break;
                }
                else if (pfield.Value is DocumentFieldModelController)
                    foreach (var f in (pfield.Value as DocumentFieldModelController).Data.EnumFields())
                    {
                        if ((pattern[1] != "" && pattern[1][0] == '~' && f.Key.Name.Contains(pattern[1].Substring(1, pattern.Count - 1))) || f.Key.Name == pattern[1])
                        {
                            textStr += f.Value + " ";
                            documents.Add(ContentController.GetController<DocumentController>((pfield.Value as DocumentFieldModelController).Data.DocumentModel.Id));
                        }
                    }
            }

            return textStr;
        }

        public override FieldModelController Copy()
        {
            return new DivideOperatorFieldModelController(OperatorFieldModel);
        }
    }
}
