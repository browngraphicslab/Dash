using System;
using System.Collections.Generic;
using System.Linq;
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

        public static DocumentModel CreateOperatorDocumentModel(OperatorFieldModel operatorField)
        {
            throw new NotImplementedException();

            //List<Key> inputKeys = operatorField.InputKeys;
            //List<Key> outputKeys = operatorField.OutputKeys;
            //List<FieldModel> inputs = operatorField.GetNewInputFields();
            //List<FieldModel> outputs = operatorField.GetNewOutputFields();
            //DocumentEndpoint docEnd = App.Instance.Container.GetRequiredService<DocumentEndpoint>();
            //DocumentModel doc = docEnd.CreateDocumentAsync(OperatorType);
            //Dictionary<Key, FieldModel> fields = new Dictionary<Key, FieldModel>(5);
            //fields[OperatorKey] = operatorField;
            //LayoutModel layout = new LayoutModel(false, OperatorType);
            //layout.Fields = new Dictionary<Key, TemplateModel>();
            //for (int i = 0; i < inputKeys.Count; ++i)
            //{
            //    fields[inputKeys[i]] = inputs[i];
            //    layout.Fields[inputKeys[i]] = new TextTemplateModel(15, i * 30, FontWeights.Normal);
            //}
            //for (int i = 0; i < outputKeys.Count; ++i)
            //{
            //    fields[outputKeys[i]] = outputs[i];
            //    layout.Fields[outputKeys[i]] = new TextTemplateModel(15, (inputKeys.Count + i) * 30 + 40, FontWeights.Normal);
            //}
            //fields[DocumentModel.LayoutKey] = new LayoutModelFieldModel(layout);
            //doc.SetFields(fields);
            //return doc;
        }
    }
}
