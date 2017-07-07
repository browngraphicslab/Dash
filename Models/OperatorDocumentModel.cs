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
            List<Key> inputKeys = opController.InputKeys;
            List<Key> outputKeys = opController.OutputKeys;
            List<FieldModelController> inputs = opController.GetNewInputFields();
            List<FieldModelController> outputs = opController.GetNewOutputFields();
            Dictionary<Key, FieldModelController> fields = new Dictionary<Key, FieldModelController>();
            fields[OperatorKey] = opController;
            for (int i = 0; i < inputKeys.Count; ++i)
            {
                fields[inputKeys[i]] = inputs[i];
            }
            for (int i = 0; i < outputKeys.Count; ++i)
            {
                fields[outputKeys[i]] = outputs[i];
            }
            
            var doc = new DocumentController(fields, OperatorType);
            ContentController.GetController(doc.GetId());

            var layoutDoc = new CourtesyDocuments.OperatorBox(new ReferenceFieldModelController(doc.GetId(), OperatorKey)).Document;
            var layoutController = new DocumentFieldModelController(layoutDoc);
            doc.SetField(DashConstants.KeyStore.LayoutKey, layoutController, false);

            return doc;
        }
    }
}
