using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using DashShared;

// ReSharper disable once CheckNamespace
namespace Dash
{
    [OperatorType(Op.Name.print)]
    public sealed class DebugPrintOperatorController : OperatorController
    {
        public DebugPrintOperatorController(OperatorModel operatorFieldModel) : base(operatorFieldModel)
        {
        }

        public DebugPrintOperatorController() : base(new OperatorModel(TypeKey.KeyModel)) => SaveOnServer();

        public override KeyController OperatorType { get; } = TypeKey;
        private static readonly KeyController TypeKey = new KeyController("DebugPrint", new Guid("57A955CF-81BD-4B5F-A510-753BC4E9B983"));

        //Input keys
        public static readonly KeyController InputKey = new KeyController("Input");

        //Output keys
        public static readonly KeyController ResultKey = new KeyController("Result");

        public override ObservableCollection<KeyValuePair<KeyController, IOInfo>> Inputs { get; } = new ObservableCollection<KeyValuePair<KeyController, IOInfo>>
        {
            new KeyValuePair<KeyController, IOInfo>(InputKey, new IOInfo(TypeInfo.Any, true)),
        };
        public override ObservableDictionary<KeyController, TypeInfo> Outputs { get; } = new ObservableDictionary<KeyController, TypeInfo>
        {
            [ResultKey] = TypeInfo.Any,
        };

        public override void Execute(Dictionary<KeyController, FieldControllerBase> inputs, Dictionary<KeyController, FieldControllerBase> outputs, DocumentController.DocumentFieldUpdatedEventArgs args, Scope scope = null)
        {
            FieldControllerBase input = inputs[InputKey];
            Debug.WriteLine(input.ToString());
            outputs[ResultKey] = input;
        }

        public override FieldControllerBase GetDefaultController() => new DebugPrintOperatorController();
    }
}
