using System;
using DashShared;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading.Tasks;

// ReSharper disable once CheckNamespace
namespace Dash
{
    [OperatorType(Op.Name.parse_num)]
    public sealed class ParseDoubleOperatorController : OperatorController
    {
        public ParseDoubleOperatorController(OperatorModel operatorFieldModel) : base(operatorFieldModel) { }

        public ParseDoubleOperatorController() : base(new OperatorModel(TypeKey.KeyModel)) { }

        public override FieldControllerBase GetDefaultController() => new ParseDoubleOperatorController();

        // input keys
        public static readonly KeyController InputStringKey = KeyController.Get("Input String");

        // output keys
        public static readonly KeyController ResultKey = KeyController.Get("Double");
        
        public override KeyController OperatorType { get; } = TypeKey;
        private static readonly KeyController TypeKey = KeyController.Get("ParseDouble");

        public override ObservableCollection<KeyValuePair<KeyController, IOInfo>> Inputs { get; } = new ObservableCollection<KeyValuePair<KeyController, IOInfo>>
        {
            new KeyValuePair<KeyController, IOInfo>(InputStringKey, new IOInfo(TypeInfo.Text, true)),
        };

        public override ObservableDictionary<KeyController, TypeInfo> Outputs { get; } = new ObservableDictionary<KeyController, TypeInfo>
        {
            [ResultKey] = TypeInfo.Number,
        };

        public override Task Execute(Dictionary<KeyController, FieldControllerBase> inputs,
            Dictionary<KeyController, FieldControllerBase> outputs,
            DocumentController.DocumentFieldUpdatedEventArgs args, Scope scope = null)
        {
            string str = (inputs[InputStringKey] as TextController)?.Data;

            if (double.TryParse(str, out double num)) outputs[ResultKey] = new NumberController(num);
            else throw new ScriptExecutionException(new TextErrorModel($"Failed to parse \"{str}\" as a double."));
            return Task.CompletedTask;
        }
    }
}
