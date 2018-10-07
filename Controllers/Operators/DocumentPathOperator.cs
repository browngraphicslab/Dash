using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using DashShared;

namespace Dash.Controllers.Operators
{
    [OperatorType(Op.Name.get_doc, Op.Name.d)]
    public class DocumentPathOperator : OperatorController
    {
        public static readonly KeyController PathKey = new KeyController("Path");


        public static readonly KeyController DocumentKey = new KeyController("Document");


        public DocumentPathOperator() : base(new OperatorModel(TypeKey.KeyModel))
        {
            SaveOnServer();
        }

        public DocumentPathOperator(OperatorModel operatorFieldModel) : base(operatorFieldModel)
        {
        }

        public override KeyController OperatorType { get; } = TypeKey;
        private static readonly KeyController TypeKey = new KeyController("Document from Path", "abdb37c9-324c-41a7-92f6-f093d38b3afe");

        public override FieldControllerBase GetDefaultController()
        {
            return new DocumentPathOperator();
        }

        public override ObservableCollection<KeyValuePair<KeyController, IOInfo>> Inputs { get; } = new ObservableCollection<KeyValuePair<KeyController, IOInfo>>
        {
            new KeyValuePair<KeyController, IOInfo>(PathKey, new IOInfo(TypeInfo.Text, true)),

        };

        public override ObservableDictionary<KeyController, TypeInfo> Outputs { get; } = new ObservableDictionary<KeyController, TypeInfo>
        {
            [DocumentKey] = TypeInfo.Document,

        };

        public override Task Execute(Dictionary<KeyController, FieldControllerBase> inputs,
            Dictionary<KeyController, FieldControllerBase> outputs,
            DocumentController.DocumentFieldUpdatedEventArgs args, Scope scope = null)
        {
            var path = (TextController)inputs[PathKey];
            DocumentController document = Execute(path);
            outputs[DocumentKey] = document;

            return Task.CompletedTask;
        }

        public DocumentController Execute(TextController path)
        {
            //Don't forget about nonexistant paths
            string text = path.Data;
            var doc = DocumentTree.GetDocumentAtPath(text);
            if (doc == null)
            {
                throw new ScriptExecutionException(new TextErrorModel("No document found or invalid path"));
            }
            return doc;
        }
    }
}
