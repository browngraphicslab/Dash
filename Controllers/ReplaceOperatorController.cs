using System;
using DashShared;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading.Tasks;

// ReSharper disable once CheckNamespace
namespace Dash
{
    [OperatorType(Op.Name.replace)]
    public sealed class ReplaceOperatorController : OperatorController
    {
        public ReplaceOperatorController(OperatorModel operatorFieldModel) : base(operatorFieldModel) { }

        public ReplaceOperatorController() : base(new OperatorModel(TypeKey.KeyModel)) { }

        public override FieldControllerBase GetDefaultController() => new ReplaceOperatorController();

        // input keys
        public static readonly KeyController InputStringKey = KeyController.Get("Input String");
        public static readonly KeyController TargetCharKey = KeyController.Get("Target Character");
        public static readonly KeyController ReplaceCharKey = KeyController.Get("Replacement Character");

        // output keys
        public static readonly KeyController ResultKey = KeyController.Get("Result");


        public override KeyController OperatorType { get; } = TypeKey;
        private static readonly KeyController TypeKey = KeyController.Get("Replace");

        public override ObservableCollection<KeyValuePair<KeyController, IOInfo>> Inputs { get; } = new ObservableCollection<KeyValuePair<KeyController, IOInfo>>
        {
            new KeyValuePair<KeyController, IOInfo>(InputStringKey, new IOInfo(TypeInfo.Text, true)),
            new KeyValuePair<KeyController, IOInfo>(TargetCharKey, new IOInfo(TypeInfo.Text, true)),
            new KeyValuePair<KeyController, IOInfo>(ReplaceCharKey, new IOInfo(TypeInfo.Text, true))
        };

        public override ObservableDictionary<KeyController, TypeInfo> Outputs { get; } = new ObservableDictionary<KeyController, TypeInfo>
        {
            [ResultKey] = TypeInfo.Text,
        };

        public override Task Execute(Dictionary<KeyController, FieldControllerBase> inputs,
            Dictionary<KeyController, FieldControllerBase> outputs,
            DocumentController.DocumentFieldUpdatedEventArgs args, Scope scope = null)
        {
            if (inputs[InputStringKey] is TextController inputStr && inputs[TargetCharKey] is TextController targetStr && inputs[ReplaceCharKey] is TextController replaceStr)
            {
                outputs[ResultKey] = new TextController(inputStr.Data.Replace(targetStr.Data, replaceStr.Data));
                return Task.CompletedTask;
            }

            throw new ScriptExecutionException(new TextErrorModel("replace() must receive three inputs of type text."));
        }
    }
}
