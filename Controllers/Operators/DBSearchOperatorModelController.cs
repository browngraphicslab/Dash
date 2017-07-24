using System.Collections.Generic;
using System.Diagnostics;
using DashShared;
using System.Linq;

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
        static public DocumentController GlobalDoc = null;
        static void initGlobalDoc()
        {
            if (GlobalDoc == null)
            {
                GlobalDoc = new DocumentController(new Dictionary<Key, FieldModelController>(), new DocumentType("global", "global"));
                GlobalDoc.SetField(ForceUpdateKey, new NumberFieldModelController(1), true);
            }
        }
        public static void ForceUpdate()
        {
            initGlobalDoc();
            if (GlobalDoc != null)
                GlobalDoc.SetField(ForceUpdateKey, new NumberFieldModelController(2), true);
        }
        public DBSearchOperatorFieldModel DBSearchOperatorFieldModel {  get { return OperatorFieldModel as DBSearchOperatorFieldModel; } }
        public string Pattern
        {
            get { return DBSearchOperatorFieldModel.Pattern; }
            set { DBSearchOperatorFieldModel.Pattern = value; }
        }
       
        static public DocumentController CreateSearch(DocumentController searchForDoc, string fieldRef)
        {
            initGlobalDoc();
            var searchFieldModel = new DBSearchOperatorFieldModel("Search", fieldRef);
            var searchFieldController = new DBSearchOperatorFieldModelController(searchFieldModel);
            var searchOp = OperatorDocumentModel.CreateOperatorDocumentModel(searchFieldController);
            searchOp.SetField(SearchForDocKey, new DocumentFieldModelController(searchForDoc), true);
            searchOp.SetField(ForceUpdateKey, new DocumentReferenceController(GlobalDoc.GetId(), ForceUpdateKey), true);
            return searchOp;
            //return new DocumentReferenceController(searchOp.GetId(), DBSearchOperatorFieldModelController.ResultsKey);
        }

        static public DocumentController CreateSearch(FieldModelController fieldContainingSearchForDoc, string fieldRef)
        {
            initGlobalDoc();
            var searchFieldModel = new DBSearchOperatorFieldModel("Search", fieldRef);
            var searchFieldController = new DBSearchOperatorFieldModelController(searchFieldModel);
            var searchOp = OperatorDocumentModel.CreateOperatorDocumentModel(searchFieldController);
            searchOp.SetField(SearchForDocKey, fieldContainingSearchForDoc, true);
            searchOp.SetField(ForceUpdateKey, new DocumentReferenceController(GlobalDoc.GetId(), ForceUpdateKey), true);
            return searchOp;
            //return new DocumentReferenceController(searchOp.GetId(), DBSearchOperatorFieldModelController.ResultsKey);
        }
        public DBSearchOperatorFieldModelController(DBSearchOperatorFieldModel operatorFieldModel) : base(operatorFieldModel)
        {
            initGlobalDoc();
            OperatorFieldModel = operatorFieldModel;
        }
        //Input keys

        //Output keys
        public static readonly Key ResultsKey = new Key("03A2157E-F03C-46A1-8F52-F59BD226944E", "Results");
        public static readonly Key SearchForDocKey = new Key("C544405C-6389-4F6D-8C17-31DEB14409D4", "SearchForDoc");
        public static readonly Key ForceUpdateKey = new Key("1FA1ABE9-6891-45B6-A845-08E9F0101D19", "ForceUpdate");

        public override ObservableDictionary<Key, TypeInfo> Inputs { get; } = new ObservableDictionary<Key, TypeInfo>
        {
            [SearchForDocKey] = TypeInfo.Document,
            [ForceUpdateKey] = TypeInfo.Number
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
            if (targetDocument == null)
                return ContentController.GetControllers<DocumentController>();
            var docsInSearchScope = new List<DocumentController>();
            foreach (var dmc in ContentController.GetControllers<DocumentController>())
            {
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
                    if (dmc.GetField(DashConstants.KeyStore.DelegatesKey, true) == null)
                    {
                        var del = dmc.MakeDelegate();
                        var layout = del.GetField(DashConstants.KeyStore.ActiveLayoutKey) as DocumentFieldModelController;
                        if (layout != null)
                        {
                            var layoutDel = layout.Data.MakeDelegate();
                            layoutDel.SetField(DashConstants.KeyStore.PositionFieldKey, new PointFieldModelController(0, 0), true);
                            del.SetField(DashConstants.KeyStore.ActiveLayoutKey, new DocumentFieldModelController(layoutDel), true);
                        }
                        documents.Add(del);
                        if (pfield.Value is DocumentFieldModelController)
                        {
                            textStr += "Document(";
                            foreach (var f in (pfield.Value as DocumentFieldModelController).Data.EnumFields().Where((pf) => !pf.Key.Name.StartsWith("_")))
                            {
                                textStr += f.Key.Name + "=" + f.Value + " ";
                            }
                            textStr += ")";

                        }
                        else textStr += pfield.Value + " ";
                    }
                    break;
                }
                else if (pfield.Value is DocumentFieldModelController)  
                    foreach (var f in (pfield.Value as DocumentFieldModelController).Data.EnumFields())
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

            return textStr;
        }

        public override FieldModelController Copy()
        {
            return new DivideOperatorFieldModelController(OperatorFieldModel);
        }
    }
}
