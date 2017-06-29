using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Metadata;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Text;
using Dash.Models;
using DashShared;
using Microsoft.Extensions.DependencyInjection;

namespace Dash
{
    public static class OperatorDocumentModel
    {
        public static Key OperatorKey = new Key("F5B0E5E0-2C1F-4E49-BD26-5F6CBCDE766A", "Operator");
        public static DocumentType OperatorType = new DocumentType("3FF64E84-A614-46AF-9742-FB5F6E2E37CE", "operator");

        public static DocumentController CreateOperatorDocumentModel(OperatorFieldModelController opController)
        {
            List<Key> inputKeys = opController.InputKeys;
            List<Key> outputKeys = opController.OutputKeys;
            List<FieldModel> inputs = opController.GetNewInputFields();
            List<FieldModel> outputs = opController.GetNewOutputFields();
            Dictionary<Key, FieldModel> fields = new Dictionary<Key, FieldModel>();
            fields[OperatorKey] = opController.FieldModel;
            for (int i = 0; i < inputKeys.Count; ++i)
            {
                fields[inputKeys[i]] = inputs[i];
            }
            for (int i = 0; i < outputKeys.Count; ++i)
            {
                fields[outputKeys[i]] = outputs[i];
            }
            
            var doc = new CreateNewDocumentRequest(new CreateNewDocumentRequestArgs(fields, OperatorType))
                .GetReturnedDocumentController();
            ContentController.GetController(doc.GetId());

            var layoutDoc = new MainPage.OperatorBox(new ReferenceFieldModel(doc.GetId(), OperatorKey)).Document.DocumentModel;
            var documentFieldModel = new DocumentModelFieldModel(layoutDoc);
            var layoutController = new DocumentFieldModelController(documentFieldModel);
            ContentController.AddModel(documentFieldModel);
            ContentController.AddController(layoutController);
            doc.SetField(DashConstants.KeyStore.LayoutKey, layoutController, false);

            return doc;
        }
    }
}
