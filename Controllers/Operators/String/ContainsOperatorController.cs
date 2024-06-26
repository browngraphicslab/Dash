﻿using System;
using DashShared;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading.Tasks;

// ReSharper disable once CheckNamespace
namespace Dash
{
    [OperatorType(Op.Name.contains)]
    public sealed class ContainsOperatorController : OperatorController
    {
        public ContainsOperatorController(OperatorModel operatorFieldModel) : base(operatorFieldModel) { }

        public ContainsOperatorController() : base(new OperatorModel(TypeKey.KeyModel)) { }

        public override FieldControllerBase GetDefaultController() => new ContainsOperatorController();

        // input keys
        public static readonly KeyController InputStringKey = KeyController.Get("Input String");
        public static readonly KeyController ContainerStringKey = KeyController.Get("Container String");

        // output keys
        public static readonly KeyController ResultKey = KeyController.Get("Result");


        public override KeyController OperatorType { get; } = TypeKey;
        private static readonly KeyController TypeKey = KeyController.Get("Contains");

        public override ObservableCollection<KeyValuePair<KeyController, IOInfo>> Inputs { get; } = new ObservableCollection<KeyValuePair<KeyController, IOInfo>>
        {
            new KeyValuePair<KeyController, IOInfo>(InputStringKey, new IOInfo(TypeInfo.Text, true)),
            new KeyValuePair<KeyController, IOInfo>(ContainerStringKey, new IOInfo(TypeInfo.Text, true))
        };

        public override ObservableDictionary<KeyController, TypeInfo> Outputs { get; } = new ObservableDictionary<KeyController, TypeInfo>
        {
            [ResultKey] = TypeInfo.Bool,
        };

        public override Task Execute(Dictionary<KeyController, FieldControllerBase> inputs,
            Dictionary<KeyController, FieldControllerBase> outputs,
            DocumentController.DocumentFieldUpdatedEventArgs args, Scope scope = null)
        {
            if (inputs[InputStringKey] is TextController inputStr && inputs[ContainerStringKey] is TextController containerStr)
            {
                outputs[ResultKey] = new BoolController(containerStr.Data.Contains(inputStr.Data));
            }

            throw new ScriptExecutionException(new TextErrorModel("contains() must receive two inputs of type text."));
        }
    }
}
