using System.Collections.Generic;
using DashShared;

namespace Dash
{
    /// <summary>
    /// Provides static utilities for creating Documents that contain an OperatorFieldModel.
    /// </summary>
    public static class OperatorDocumentModel
    {
        public static KeyController OperatorKey = new KeyController("F5B0E5E0-2C1F-4E49-BD26-5F6CBCDE766A", "Operator");
        public static DocumentType OperatorType = new DocumentType("3FF64E84-A614-46AF-9742-FB5F6E2E37CE", "operator");

        public static DocumentController CreateOperatorDocumentModel(OperatorFieldModelController opController)
        {
            IDictionary<KeyController, TypeInfo> inputs = opController.Inputs;
            IDictionary<KeyController, TypeInfo> outputs = opController.Outputs;
            Dictionary<KeyController, FieldModelController> fields = new Dictionary<KeyController, FieldModelController>();
            fields[OperatorKey] = opController;
            
            var doc = new DocumentController(fields, OperatorType);
            ContentController.GetController(doc.GetId());

            var layoutDoc = new OperatorBox(new ReferenceFieldModelController(doc.GetId(), OperatorKey)).Document;
            var layoutController = new DocumentFieldModelController(layoutDoc);
            doc.SetField(KeyStore.ActiveLayoutKey, layoutController, true);

            return doc;
        }

        public static DocumentController CreateFilterDocumentController()
        {
            Dictionary<KeyController, FieldModelController> fields = new Dictionary<KeyController, FieldModelController>();
            fields[OperatorKey] = new FilterOperatorFieldModelController();
            var doc = new DocumentController(fields, FilterOperatorFieldModelController.FilterType);

            var layoutDoc = new FilterOperatorBox(new ReferenceFieldModelController(doc.GetId(), OperatorKey)).Document;
            doc.SetActiveLayout(layoutDoc, true, true);

            return doc;
        }
        public static DocumentController CreateMapDocumentController()
        {
            Dictionary<KeyController, FieldModelController> fields = new Dictionary<KeyController, FieldModelController>();
            fields[OperatorKey] = new CollectionMapOperator();
            var doc = new DocumentController(fields, CollectionMapOperator.MapType);

            var layoutDoc = new CollectionMapOperatorBox(new ReferenceFieldModelController(doc.GetId(), OperatorKey)).Document;
            doc.SetActiveLayout(layoutDoc, true, true);

            return doc;
        }

        //public static DocumentController CreateApiDocumentController()
        //{
        //    Dictionary<KeyController, FieldModelController> fields = new Dictionary<KeyController, FieldModelController>();
        //    var doc = new ApiDocumentModel().Document;
        //    doc.SetField(OperatorKey, new ApiOperator(new OperatorFieldModel("Api")), true );
        //    doc.DocumentType = ApiOperator.ApiType;


        //    var layoutDoc = new ApiOperatorBox(new ReferenceFieldModelController(doc.GetId(), OperatorKey)).Document;
        //    doc.SetActiveLayout(layoutDoc, true, true);

        //    return doc;
        //}

        public static DocumentController CreateApiDocumentController()
        {
            Dictionary<KeyController, FieldModelController> fields = new Dictionary<KeyController, FieldModelController>();
            fields[OperatorKey] = new ApiOperatorController();
            var doc = new DocumentController(fields, ApiOperatorController.ApiType);

            var layoutDoc = new ApiOperatorBox(new ReferenceFieldModelController(doc.GetId(), OperatorKey)).Document;
            doc.SetActiveLayout(layoutDoc, true, true);

            return doc;
        }

        public static DocumentController CreateCompoundController()
        {
            var fields = new Dictionary<KeyController, FieldModelController>
            {
                [OperatorKey] = new CompoundOperatorFieldController()
            };
            var doc = new DocumentController(fields, CollectionMapOperator.MapType);

            var layoutDoc = new OperatorBox(new ReferenceFieldModelController(doc.GetId(), OperatorKey)).Document;
            doc.SetActiveLayout(layoutDoc, true, true);

            OperationCreationHelper.AddOperator(doc.GetId(), () => doc.GetCopy(), () => doc.GetField(OperatorKey).DereferenceToRoot(null) as OperatorFieldModelController);

            return doc;
        }
    }
}
