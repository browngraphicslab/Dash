using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using DashShared;

// ReSharper disable once CheckNamespace
namespace Dash
{
    [OperatorType(Op.Name.to_lower)]
    public sealed class ToLowerOperator : OperatorController
    {
        public ToLowerOperator(OperatorModel operatorFieldModel) : base(operatorFieldModel) { }

        public ToLowerOperator() : base(new OperatorModel(TypeKey.KeyModel)) { }

        public override FieldControllerBase GetDefaultController() => new ToLowerOperator();

        // input keys
        public static readonly KeyController InputStringKey = KeyController.Get("Input String");

        // output keys
        public static readonly KeyController OutputStringKey = KeyController.Get("Output String");
        
        public override KeyController OperatorType { get; } = TypeKey;
        private static readonly KeyController TypeKey = KeyController.Get("ToLower");

        public override ObservableCollection<KeyValuePair<KeyController, IOInfo>> Inputs { get; } = new ObservableCollection<KeyValuePair<KeyController,IOInfo>>
        {
            new KeyValuePair<KeyController, IOInfo>(InputStringKey, new IOInfo(TypeInfo.Text, true))
        };

        public override ObservableDictionary<KeyController, TypeInfo> Outputs { get; } = new ObservableDictionary<KeyController, TypeInfo>
        {
            [OutputStringKey] = TypeInfo.Text,
        };

        public override Task Execute(Dictionary<KeyController, FieldControllerBase> inputs,
            Dictionary<KeyController, FieldControllerBase> outputs,
            DocumentController.DocumentFieldUpdatedEventArgs args, Scope scope = null)
        {
            if (inputs[InputStringKey] is TextController tc)
            {
                outputs[OutputStringKey] = new TextController(tc.TextFieldModel.Data.ToLower());
            }
            return Task.CompletedTask;
        }
    }
}
