﻿using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using DashShared;

namespace Dash
{
    [OperatorType(Op.Name.for_lp)]
    public class ForOperatorController : OperatorController
    {
        public ForOperatorController(OperatorModel operatorFieldModel) : base(operatorFieldModel)
        {
        }

        public ForOperatorController() : base(new OperatorModel(TypeKey.KeyModel))
        {
        }

        public override KeyController OperatorType { get; } = TypeKey;
        private static readonly KeyController TypeKey = KeyController.Get("For");

        //Input keys
        public static readonly KeyController CounterDeclarationKey = KeyController.Get("Count Declaration");
        public static readonly KeyController BoolKey = KeyController.Get("Bool");
        public static readonly KeyController IncrementKey = KeyController.Get("Increment Size");
        public static readonly KeyController ForBlockKey = KeyController.Get("For Loop Body");

        //Output keys
        public static readonly KeyController ResultKey = KeyController.Get("Result");

        public override ObservableCollection<KeyValuePair<KeyController, IOInfo>> Inputs { get; } = new ObservableCollection<KeyValuePair<KeyController, IOInfo>>
        {
            new KeyValuePair<KeyController, IOInfo>(CounterDeclarationKey, new IOInfo(TypeInfo.Any, true)),
            new KeyValuePair<KeyController, IOInfo>(BoolKey, new IOInfo(TypeInfo.Bool, true)),
            new KeyValuePair<KeyController, IOInfo>(IncrementKey, new IOInfo(TypeInfo.Any, true)),
            new KeyValuePair<KeyController, IOInfo>(ForBlockKey, new IOInfo(TypeInfo.Any, true))
        };
        public override ObservableDictionary<KeyController, TypeInfo> Outputs { get; } = new ObservableDictionary<KeyController, TypeInfo>
        {
            [ResultKey] = TypeInfo.Any,
        };

        //TODO: remove requirement that output exists
        public override Task Execute(Dictionary<KeyController, FieldControllerBase> inputs,
            Dictionary<KeyController, FieldControllerBase> outputs,
            DocumentController.DocumentFieldUpdatedEventArgs args, Scope scope = null)
        {
            outputs[ResultKey] = new NumberController(0);
            return Task.CompletedTask;
        }

        public override FieldControllerBase GetDefaultController() => new ForOperatorController();
    }
}
