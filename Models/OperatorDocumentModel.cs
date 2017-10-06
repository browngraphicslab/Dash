using System.Collections.Generic;
using Dash.Controllers;
using DashShared;
using Dash.Controllers.Operators;

namespace Dash
{
    /// <summary>
    /// Provides static utilities for creating Documents that contain an OperatorFieldModel.
    /// </summary>
    public static class OperatorDocumentModel
    {
        public static KeyController OperatorKey = new KeyController("F5B0E5E0-2C1F-4E49-BD26-5F6CBCDE766A", "Operator");
        public static DocumentType OperatorType = new DocumentType("3FF64E84-A614-46AF-9742-FB5F6E2E37CE", "operator");

        public static DocumentController CreateOperatorDocumentModel(OperatorFieldModelController opController, string title="")
        {
            Dictionary<KeyController, FieldControllerBase> fields = new Dictionary<KeyController, FieldControllerBase>();
            fields[OperatorKey] = opController;
            if(title != "") fields[KeyStore.TitleKey] = new TextFieldModelController(title);
            
            var doc = new DocumentController(fields, OperatorType);
            ContentController<DocumentModel>.GetController(doc.GetId());

            var layoutDoc = new OperatorBox(new DocumentReferenceFieldController(doc.GetId(), OperatorKey)).Document;
            var layoutController = new DocumentFieldModelController(layoutDoc);
            doc.SetField(KeyStore.ActiveLayoutKey, layoutController, true);

            return doc;
        }

        public static DocumentController CreateDBFilterDocumentController()
        {
            return DBFilterOperatorFieldModelController.CreateFilter(new DocumentReferenceFieldController(DBTest.DBDoc.GetId(), KeyStore.DataKey), "");
        }
        public static DocumentController CreateFilterDocumentController()
        {
            Dictionary<KeyController, FieldControllerBase> fields = new Dictionary<KeyController, FieldControllerBase>();
            fields[OperatorKey] = new FilterOperatorFieldModelController();
            var doc = new DocumentController(fields, FilterOperatorFieldModelController.FilterType);

            var layoutDoc = new FilterOperatorBox(new DocumentReferenceFieldController(doc.GetId(), OperatorKey)).Document;
            doc.SetActiveLayout(layoutDoc, true, true);

            return doc;
        }
        public static DocumentController CreateMapDocumentController()
        {
            Dictionary<KeyController, FieldControllerBase> fields = new Dictionary<KeyController, FieldControllerBase>();
            fields[OperatorKey] = new CollectionMapOperator();
            var doc = new DocumentController(fields, CollectionMapOperator.MapType);

            var layoutDoc = new CollectionMapOperatorBox(new DocumentReferenceFieldController(doc.GetId(), OperatorKey)).Document;
            doc.SetActiveLayout(layoutDoc, true, true);

            return doc;
        }

        public static DocumentController CreateApiDocumentController()
        {
            Dictionary<KeyController, FieldControllerBase> fields = new Dictionary<KeyController, FieldControllerBase>();
            fields[OperatorKey] = new ApiOperatorController();
            var doc = new DocumentController(fields, ApiOperatorController.ApiType);

            var layoutDoc = new ApiOperatorBox(new DocumentReferenceFieldController(doc.GetId(), OperatorKey)).Document;
            doc.SetActiveLayout(layoutDoc, true, true);

            return doc;
        }

        public static DocumentController CreateCompoundController()
        {
            var fields = new Dictionary<KeyController, FieldControllerBase>
            {
                [OperatorKey] = new CompoundOperatorFieldController()
            };
            var doc = new DocumentController(fields, CompoundOperatorFieldController.MapType);

            var layoutDoc = new OperatorBox(new DocumentReferenceFieldController(doc.GetId(), OperatorKey)).Document;
            doc.SetActiveLayout(layoutDoc, true, true);

            OperationCreationHelper.AddOperator(doc.GetId(), () => doc.GetCopy(), () => doc.GetField(OperatorKey).DereferenceToRoot(null) as OperatorFieldModelController);

            return doc;
        }
    }
}
