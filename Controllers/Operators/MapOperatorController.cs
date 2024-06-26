﻿using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using DashShared;

namespace Dash
{
    public class MapOperatorController : OperatorController
    {
        public static KeyController InputKey = KeyController.Get("Input Collection");
        public static KeyController OperatorKey = KeyController.Get("Operator");

        public static KeyController OutputKey = KeyController.Get("Output Collection");

        public MapOperatorController() : base(new OperatorModel(TypeKey.KeyModel))
        {
        }

        public MapOperatorController(OperatorModel operatorFieldModel) : base(operatorFieldModel)
        {
        }

        public override KeyController OperatorType { get; } = TypeKey;
        private static readonly KeyController TypeKey = KeyController.Get("Map");

        public override ObservableCollection<KeyValuePair<KeyController, IOInfo>> Inputs { get; } = new ObservableCollection<KeyValuePair<KeyController, IOInfo>>
        {
            new KeyValuePair<KeyController, IOInfo>(InputKey, new IOInfo(TypeInfo.List, true)),
            new KeyValuePair<KeyController, IOInfo>(OperatorKey, new IOInfo(TypeInfo.Operator, true)),
        };

        public override ObservableDictionary<KeyController, TypeInfo> Outputs { get; } = new ObservableDictionary<KeyController, TypeInfo>
        {
            [OutputKey] = TypeInfo.List
        };

        public override FieldControllerBase GetDefaultController()
        {
            return new MapOperatorController();
        }


        public override Task Execute(Dictionary<KeyController, FieldControllerBase> inputs,
            Dictionary<KeyController, FieldControllerBase> outputs,
            DocumentController.DocumentFieldUpdatedEventArgs args, Scope scope = null)
        {
            var input = (IListController) inputs[InputKey];
            var op = (OperatorController)inputs[OperatorKey];
            if (op.Inputs.Count != 1 || op.Outputs.Count != 1)
            {
                return Task.CompletedTask;
            }
            List<FieldControllerBase> outputList = new List<FieldControllerBase>(input.Count);
            Dictionary<KeyController, FieldControllerBase> inDict = new Dictionary<KeyController, FieldControllerBase>();
            Dictionary<KeyController, FieldControllerBase> outDict = new Dictionary<KeyController, FieldControllerBase>();
            var inKey = op.Inputs.First().Key;
            var outKey = op.Outputs.First().Key;
            foreach (var fieldModelController in input.AsEnumerable())
            {
                inDict[inKey] = fieldModelController;
                op.
                    Execute(inDict, outDict, args);
                outputList.Add(outDict[outKey]);
            }

            outputs[OutputKey] = new ListController<FieldControllerBase>(outputList);//TODO Can this be more specific?
            return Task.CompletedTask;
        }
    }
}
