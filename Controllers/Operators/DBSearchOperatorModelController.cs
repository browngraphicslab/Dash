using System.Collections.Generic;
using System.Diagnostics;
using DashShared;
using System.Linq;
using Dash.Converters;
using System;

namespace Dash.Controllers.Operators
{
    public class DBSearchOperatorFieldModel : OperatorFieldModel
    {
        public DBSearchOperatorFieldModel() : base("search")
        {
        }
    }
    public class DBSearchOperatorFieldModelController : OperatorFieldModelController
    {
        public static void ForceUpdate(DocumentFieldReference docFieldRef)
        {
            var opDoc = ContentController<DocumentModel>.GetController<DocumentController>(docFieldRef.DocumentId);
            opDoc.Execute(null, true);
        }
        public DBSearchOperatorFieldModel DBSearchOperatorFieldModel {  get { return OperatorFieldModel as DBSearchOperatorFieldModel; } }
       
        static public DocumentController CreateSearch(DocumentController searchForDoc, DocumentController dbDoc, string fieldRef, string retPath)
        {
            var searchFieldController = new DBSearchOperatorFieldModelController(new DBSearchOperatorFieldModel());
            var searchOp = OperatorDocumentModel.CreateOperatorDocumentModel(searchFieldController);
            searchOp.SetField(FieldPatternKey, new TextFieldModelController(fieldRef), true);
            searchOp.SetField(ReturnDocKey, new TextFieldModelController(retPath), true);
            searchOp.SetField(SearchForDocKey, new DocumentFieldModelController(searchForDoc), true);
            searchOp.SetField(InputDocsKey, new DocumentReferenceFieldController(dbDoc.GetId(), KeyStore.DataKey), true);


            var layoutDoc = new DBSearchOperatorBox(new DocumentReferenceFieldController(searchOp.GetId(), OperatorDocumentModel.OperatorKey)).Document;
            searchOp.SetActiveLayout(layoutDoc, true, true);
            return searchOp;
        }

        static public DocumentController CreateSearch(FieldControllerBase fieldContainingSearchForDoc, DocumentController dbDoc, string fieldRef, string retPath)
        {
            var searchFieldController = new DBSearchOperatorFieldModelController(new DBSearchOperatorFieldModel());
            var searchOp = OperatorDocumentModel.CreateOperatorDocumentModel(searchFieldController);
            searchOp.SetField(FieldPatternKey, new TextFieldModelController(fieldRef), true);
            searchOp.SetField(ReturnDocKey, new TextFieldModelController(retPath), true);
            if (fieldContainingSearchForDoc != null)
                searchOp.SetField(SearchForDocKey, fieldContainingSearchForDoc, true);
            else
                searchOp.SetField(SearchForDocKey, fieldContainingSearchForDoc, true);
            searchOp.SetField(InputDocsKey, new DocumentReferenceFieldController(dbDoc.GetId(), KeyStore.DataKey), true);

            var layoutDoc = new DBSearchOperatorBox(new DocumentReferenceFieldController(searchOp.GetId(), OperatorDocumentModel.OperatorKey)).Document;
            searchOp.SetActiveLayout(layoutDoc, true, true);
            return searchOp;
        }
        public DBSearchOperatorFieldModelController(DBSearchOperatorFieldModel operatorFieldModel) : base(operatorFieldModel)
        {
            OperatorFieldModel = operatorFieldModel;
        }

        //Output keys
        public static readonly KeyController ResultsKey      = new KeyController("03A2157E-F03C-46A1-8F52-F59BD226944E", "Results");
        public static readonly KeyController InputDocsKey    = new KeyController("4181DD2A-2258-4BB7-BE0C-725B8E27FA4A", "Input Collection");
        public static readonly KeyController FieldPatternKey = new KeyController("863F89AD-0FAF-42F4-9FBC-BF45457B8A3C", "Has Field");
        public static readonly KeyController ReturnDocKey    = new KeyController("DB03F66F-350D-49D9-B8EC-D6E8D54E9AB6", "[Return Doc]");
        public static readonly KeyController SearchForDocKey = new KeyController("C544405C-6389-4F6D-8C17-31DEB14409D4", "[Contains Doc]");

        public override ObservableDictionary<KeyController, IOInfo> Inputs { get; } = new ObservableDictionary<KeyController, IOInfo>
        {
            [FieldPatternKey] = new IOInfo(TypeInfo.Text, true),
            [ReturnDocKey]    = new IOInfo(TypeInfo.Text, true),
            [SearchForDocKey] = new IOInfo(TypeInfo.Document, true),
            [InputDocsKey]    = new IOInfo(TypeInfo.Collection, true)
        };
        public override ObservableDictionary<KeyController, TypeInfo> Outputs { get; } = new ObservableDictionary<KeyController, TypeInfo>
        {
            [ResultsKey] = TypeInfo.Collection
        };
        
        public override void Execute(Dictionary<KeyController, FieldControllerBase> inputs, Dictionary<KeyController, FieldControllerBase> outputs)
        {
            var retPathString = (!inputs.ContainsKey(ReturnDocKey)) ? "" :  (inputs[ReturnDocKey] as TextFieldModelController).Data.Trim(' ','\r');
            var pattern      = new List<string>((inputs[FieldPatternKey] as TextFieldModelController).Data.Trim(' ', '\r').Split('.'));
            var returnPath   = new List<string>(retPathString.Split('.'));
            var searchForDoc = (!inputs.ContainsKey(SearchForDocKey)) ? null : (inputs[SearchForDocKey] as DocumentFieldModelController).Data;
            if (searchForDoc == DBTest.DBNull)
                searchForDoc = null;
            var dbDocs       = (!inputs.ContainsKey(InputDocsKey)) ? null : (inputs[InputDocsKey] as DocumentCollectionFieldModelController)?.Data;
            if (dbDocs == null)
                return;
            if (returnPath == null)
                returnPath = pattern;
            var docsInSearchScope = findDocsThatReferenceDocument(searchForDoc, dbDocs);
            
            var documents = new List<DocumentController>();
            foreach (var dmc in docsInSearchScope.ToArray())
            {
                if (SearchInDocumentForNamedField(pattern, dmc))
                {
                    var retDoc = retPathString == "" ? dmc : GetReturnDoc(dmc, returnPath);
                    if (retDoc != null)
                        documents.Add(retDoc);
                }
            }
            
            outputs[ResultsKey] = new DocumentCollectionFieldModelController(documents);
        }

        private void D_DocumentFieldUpdated()
        {
            throw new System.NotImplementedException();
        }
        
        private static IEnumerable<DocumentController> findDocsThatReferenceDocument(DocumentController targetDocument, List<DocumentController> dbDocs)
        {
            var docsInSearchScope = new List<DocumentController>();
            foreach (var dmc in dbDocs)
                if (!dmc.DocumentType.Type.Contains("Box") && 
                    dmc.DocumentType != StackLayout.DocumentType && 
                    dmc.DocumentType != GridLayout.GridPanelDocumentType && 
                    dmc.DocumentType != GridViewLayout.DocumentType)
                {
                    if (targetDocument == null)
                    {
                        docsInSearchScope.Add(dmc);
                    }
                    else if (CheckForFieldReferencingTarget(targetDocument, dmc) != null)
                        docsInSearchScope.Add(dmc);
                }
            return docsInSearchScope;
        }

        private static ReferenceFieldModelController CheckForFieldReferencingTarget(DocumentController targetDocument, DocumentController dmc)
        {
            foreach (var field in dmc.EnumFields()) 
                if (field.Key != KeyStore.ThisKey) {
                    foreach (var docRef in field.Value.GetReferences())
                        if (docRef.GetId() == targetDocument.GetId())
                            return new DocumentReferenceFieldController(dmc.GetId(), field.Key);
                }
            return null;
        }

        private static bool SearchInDocumentForNamedField(List<string> pattern, DocumentController dmc)
        {
            // loop through each field to find on that matches the field name pattern 
            foreach (var pfield in dmc.EnumFields().Where((pf)=>pf.Key.Name == pattern[0] || pattern[0] == ""))
            {
                if (pattern.Count == 1)
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
        private static DocumentController GetReturnDoc(DocumentController dmc, List<string> pattern)
        {
            // loop through each field to find on that matches the field name pattern 
            foreach (var pfield in dmc.EnumFields().Where((pf) => pf.Key.Name == pattern[0] || pattern[0] == ""))
            {
                var pfieldDoc = (pfield.Value as DocumentFieldModelController)?.Data;
                if (pattern.Count == 1)
                {
                    if (pfieldDoc != null)
                        return pfieldDoc;
                    return dmc;
                }
                else if (pfieldDoc != null)
                    foreach (var f in pfieldDoc.EnumFields())
                    {
                        if ((pattern[1] != "" && pattern[1][0] == '~' && f.Key.Name.Contains(pattern[1].TrimStart('~'))) || f.Key.Name == pattern[1])
                        {
                            if (f.Value is DocumentFieldModelController)
                                return (f.Value as DocumentFieldModelController).Data;
                            return pfieldDoc;
                        }
                    }
            }

            return null;
        }

        public override FieldModelController<OperatorFieldModel> Copy()
        {
            return new DBSearchOperatorFieldModelController(OperatorFieldModel as DBSearchOperatorFieldModel);
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
