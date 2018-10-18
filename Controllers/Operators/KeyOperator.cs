using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using DashShared;

namespace Dash
{
    [OperatorType(Op.Name.k)]
    public class KeyOperator : OperatorController
    {
        public static readonly KeyController KeyNameKey = KeyController.Get("KeyName");


        public static readonly KeyController KeyKey = KeyController.Get("Key");


        public KeyOperator() : base(new OperatorModel(TypeKey.KeyModel))
        {
        }

        public KeyOperator(OperatorModel operatorFieldModel) : base(operatorFieldModel)
        {
        }

        public override KeyController OperatorType { get; } = TypeKey;
        private static readonly KeyController TypeKey = KeyController.Get("String to Key");

        public override FieldControllerBase GetDefaultController()
        {
            return new KeyOperator();
        }

        public override ObservableCollection<KeyValuePair<KeyController, IOInfo>> Inputs { get; } = new ObservableCollection<KeyValuePair<KeyController, IOInfo>>
        {
            new KeyValuePair<KeyController, IOInfo>(KeyNameKey, new IOInfo(TypeInfo.Text, true)),
        };

        public override ObservableDictionary<KeyController, TypeInfo> Outputs { get; } = new ObservableDictionary<KeyController, TypeInfo>
        {
            [KeyKey] = TypeInfo.Key,
        };

        public override Task Execute(Dictionary<KeyController, FieldControllerBase> inputs,
            Dictionary<KeyController, FieldControllerBase> outputs,
            DocumentController.DocumentFieldUpdatedEventArgs args, Scope scope = null)
        {
            var keyName = (TextController)inputs[KeyNameKey];
            KeyController key = Execute(keyName);
            outputs[KeyKey] = key;
            return Task.CompletedTask;
        }

        public KeyController Execute(TextController keyName)
        {
            return KeyController.Get(keyName.Data);
        }

    }
}
