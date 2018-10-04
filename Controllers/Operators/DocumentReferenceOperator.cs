using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using DashShared;

namespace Dash
{
    [OperatorType(Op.Name.dref)]
    public class DocumentReferenceOperator : OperatorController
    {
        public static readonly KeyController DocumentKey = new KeyController("Document");
        public static readonly KeyController KeyKey = new KeyController("Key");


        public static readonly KeyController ReferenceKey = new KeyController("Reference");


        public DocumentReferenceOperator() : base(new OperatorModel(TypeKey.KeyModel))
        {
            SaveOnServer();
        }

        public DocumentReferenceOperator(OperatorModel operatorFieldModel) : base(operatorFieldModel)
        {
        }

        public override KeyController OperatorType { get; } = TypeKey;
        private static readonly KeyController TypeKey = new KeyController("DocumentReference", "a4c20265-528d-4ff4-9066-a431c2711a73");

        public override FieldControllerBase GetDefaultController()
        {
            return new DocumentReferenceOperator();
        }

        public override ObservableCollection<KeyValuePair<KeyController, IOInfo>> Inputs { get; } = new ObservableCollection<KeyValuePair<KeyController, IOInfo>>
        {
            new KeyValuePair<KeyController, IOInfo>(DocumentKey, new IOInfo(TypeInfo.Document, true)),
            new KeyValuePair<KeyController, IOInfo>(KeyKey, new IOInfo(TypeInfo.Key, true)),

        };

        public override ObservableDictionary<KeyController, TypeInfo> Outputs { get; } = new ObservableDictionary<KeyController, TypeInfo>
        {
            [ReferenceKey] = TypeInfo.DocumentReference,
        };

        public override Task Execute(Dictionary<KeyController, FieldControllerBase> inputs,
            Dictionary<KeyController, FieldControllerBase> outputs,
            DocumentController.DocumentFieldUpdatedEventArgs args, Scope scope = null)
        {
            var doc = (DocumentController)inputs[DocumentKey];
            var key = (KeyController)inputs[KeyKey];

            outputs[ReferenceKey] = new DocumentReferenceController(doc, key);
            return Task.CompletedTask;
        }
    }
}
