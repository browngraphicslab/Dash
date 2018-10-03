using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using DashShared;

namespace Dash
{
    [OperatorType(Op.Name.k)]
    public class KeyOperator : OperatorController
    {
        public static readonly KeyController KeyNameKey = new KeyController("KeyName");


        public static readonly KeyController KeyKey = new KeyController("Key");


        public KeyOperator() : base(new OperatorModel(TypeKey.KeyModel))
        {
            SaveOnServer();
        }

        public KeyOperator(OperatorModel operatorFieldModel) : base(operatorFieldModel)
        {
        }

        public override KeyController OperatorType { get; } = TypeKey;
        private static readonly KeyController TypeKey = new KeyController("String to Key", "2071d5ac-4c84-4cfd-bd2c-1a09a1fe02b3");

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

        public override void Execute(Dictionary<KeyController, FieldControllerBase> inputs,
            Dictionary<KeyController, FieldControllerBase> outputs,
            DocumentController.DocumentFieldUpdatedEventArgs args, Scope scope = null)
        {
            var keyName = (TextController)inputs[KeyNameKey];
            KeyController key = Execute(keyName);
            outputs[KeyKey] = key;
        }

        public KeyController Execute(TextController keyName)
        {
            return new KeyController(keyName.Data);
        }

    }
}
