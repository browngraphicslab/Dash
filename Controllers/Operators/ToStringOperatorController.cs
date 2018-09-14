using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using DashShared;

// ReSharper disable once CheckNamespace
namespace Dash
{
    [OperatorType(Op.Name.to_string)]
    public sealed class ToStringOperatorController : OperatorController
    {
        //Input keys
        public static readonly KeyController InputKey = new KeyController("Input");

        //Output keys
        public static readonly KeyController ResultStringKey = new KeyController("String");


        public ToStringOperatorController() : base(new OperatorModel(TypeKey.KeyModel)) => SaveOnServer();

        public ToStringOperatorController(OperatorModel operatorFieldModel) : base(operatorFieldModel)
        {
        }

        public override FieldControllerBase GetDefaultController() => throw new NotImplementedException();

        public override ObservableCollection<KeyValuePair<KeyController, IOInfo>> Inputs { get; } = new ObservableCollection<KeyValuePair<KeyController, IOInfo>>()
        {
            new KeyValuePair<KeyController, IOInfo>(InputKey, new IOInfo(TypeInfo.Any, true))
        };

        public override ObservableDictionary<KeyController, TypeInfo> Outputs { get; } = new ObservableDictionary<KeyController, TypeInfo>()
        {
            [ResultStringKey] = TypeInfo.Text
        };

        public override KeyController OperatorType { get; } = TypeKey;
        private static readonly KeyController TypeKey = new KeyController("To String", "C9A561E8-D4A1-4C38-A0BD-D9EE3531DACE");

        public override void Execute(Dictionary<KeyController, FieldControllerBase> inputs,
            Dictionary<KeyController, FieldControllerBase> outputs,
            DocumentController.DocumentFieldUpdatedEventArgs args, Scope scope = null)
        {
            var input = inputs[InputKey];
            if (input != null) outputs[ResultStringKey] = new TextController(input.GetValue(null).ToString());
        }
    }
}
