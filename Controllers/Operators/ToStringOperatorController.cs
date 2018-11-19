using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using DashShared;

// ReSharper disable once CheckNamespace
namespace Dash
{
    [OperatorType(Op.Name.to_string)]
    public sealed class ToStringOperatorController : OperatorController
    {
        //Input keys
        public static readonly KeyController InputKey = KeyController.Get("Input");

        //Output keys
        public static readonly KeyController ResultStringKey = KeyController.Get("String");


        public ToStringOperatorController() : base(new OperatorModel(TypeKey.KeyModel)) { }

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
        private static readonly KeyController TypeKey = KeyController.Get("To String");

        public override Task Execute(Dictionary<KeyController, FieldControllerBase> inputs,
            Dictionary<KeyController, FieldControllerBase> outputs,
            DocumentController.DocumentFieldUpdatedEventArgs args, Scope scope = null)
        {
            var input = inputs[InputKey];
            if (input != null) outputs[ResultStringKey] = new TextController(input.GetValue(null).ToString());
            return Task.CompletedTask;
        }
    }
}
