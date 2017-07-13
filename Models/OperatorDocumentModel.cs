﻿using System.Collections.Generic;
using DashShared;

namespace Dash
{
    /// <summary>
    /// Provides static utilities for creating Documents that contain an OperatorFieldModel.
    /// </summary>
    public static class OperatorDocumentModel
    {
        public static Key OperatorKey = new Key("F5B0E5E0-2C1F-4E49-BD26-5F6CBCDE766A", "Operator");
        public static DocumentType OperatorType = new DocumentType("3FF64E84-A614-46AF-9742-FB5F6E2E37CE", "operator");

        public static DocumentController CreateOperatorDocumentModel(OperatorFieldModelController opController)
        {
            IDictionary<Key, TypeInfo> inputs = opController.Inputs;
            IDictionary<Key, TypeInfo> outputs = opController.Outputs;
            Dictionary<Key, FieldModelController> fields = new Dictionary<Key, FieldModelController>();
            fields[OperatorKey] = opController;
            //TODO These loops make overloading not possible
            foreach (var typeInfo in inputs)
            {
                fields[typeInfo.Key] = TypeInfoHelper.CreateFieldModelController(typeInfo.Value);
            }
            foreach (var typeInfo in outputs)
            {
                fields[typeInfo.Key] = TypeInfoHelper.CreateFieldModelController(typeInfo.Value);
            }
            
            var doc = new DocumentController(fields, OperatorType);
            ContentController.GetController(doc.GetId());

            var layoutDoc = new CourtesyDocuments.OperatorBox(new ReferenceFieldModelController(doc.GetId(), OperatorKey)).Document;
            var layoutController = new DocumentFieldModelController(layoutDoc);
            doc.SetField(DashConstants.KeyStore.ActiveLayoutKey, layoutController, false);

            return doc;
        }
    }
}
