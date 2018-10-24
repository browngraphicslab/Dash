using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using DashShared;

namespace Dash
{
    [OperatorType(Op.Name.if_st)]
    class IfOperatorController : OperatorController
    {
        public IfOperatorController(OperatorModel operatorFieldModel) : base(operatorFieldModel)
        {
        }

        public IfOperatorController() : base(new OperatorModel(TypeKey.KeyModel))
        {
        }

        public override KeyController OperatorType { get; } = TypeKey;
        private static readonly KeyController TypeKey = KeyController.Get("If");

        //Input keys
        //public static readonly KeyController BinaryKey 
        public static readonly KeyController BoolKey = KeyController.Get("Bool");
        public static readonly KeyController IfBlockKey = KeyController.Get("IfBlock");
        public static readonly KeyController ElseBlockKey = KeyController.Get("ElseBlock");

        //Output keys
        public static readonly KeyController ResultKey = KeyController.Get("Result");

        public override ObservableCollection<KeyValuePair<KeyController, IOInfo>> Inputs { get; } = new ObservableCollection<KeyValuePair<KeyController, IOInfo>>
        {
            new KeyValuePair<KeyController, IOInfo>(BoolKey, new IOInfo(TypeInfo.Bool, true)),
            new KeyValuePair<KeyController, IOInfo>(IfBlockKey, new IOInfo(TypeInfo.Any, true)),
            new KeyValuePair<KeyController, IOInfo>(ElseBlockKey, new IOInfo(TypeInfo.Any, true))
        };
        public override ObservableDictionary<KeyController, TypeInfo> Outputs { get; } = new ObservableDictionary<KeyController, TypeInfo>
        {
            [ResultKey] = TypeInfo.Any,
        };

        public override Task Execute(Dictionary<KeyController, FieldControllerBase> inputs,
            Dictionary<KeyController, FieldControllerBase> outputs,
            DocumentController.DocumentFieldUpdatedEventArgs args, Scope scope = null)
        {
            var Bool = ((BoolController)inputs[BoolKey]).Data;
            var BlockIf = inputs[IfBlockKey];
            var BlockElse = inputs[ElseBlockKey];

            outputs[ResultKey] = Bool ? BlockIf : BlockElse;
            return Task.CompletedTask;
        }

        public override FieldControllerBase GetDefaultController()
        {
            return new IfOperatorController();
        }
    }
}
