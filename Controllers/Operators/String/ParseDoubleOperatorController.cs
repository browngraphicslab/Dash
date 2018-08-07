using DashShared;
using System.Collections.Generic;
using System.Collections.ObjectModel;

// ReSharper disable once CheckNamespace
namespace Dash
{
    [OperatorType(Op.Name.parse_num)]
    public sealed class ParseDoubleOperatorController : OperatorController
    {
        public ParseDoubleOperatorController(OperatorModel operatorFieldModel) : base(operatorFieldModel) { }

        public ParseDoubleOperatorController() : base(new OperatorModel(TypeKey.KeyModel)) => SaveOnServer();

        public override FieldControllerBase GetDefaultController() => new ContainsOperatorController();

        // input keys
        public static readonly KeyController InputStringKey = new KeyController("Input String");

        // output keys
        public static readonly KeyController ResultKey = new KeyController("Double");
        
        public override KeyController OperatorType { get; } = TypeKey;
        private static readonly KeyController TypeKey = new KeyController("ParseDouble", "9CBE9126-02B6-4635-B309-FB6F0489FC0E");

        public override ObservableCollection<KeyValuePair<KeyController, IOInfo>> Inputs { get; } = new ObservableCollection<KeyValuePair<KeyController, IOInfo>>
        {
            new KeyValuePair<KeyController, IOInfo>(InputStringKey, new IOInfo(TypeInfo.Text, true)),
        };

        public override ObservableDictionary<KeyController, TypeInfo> Outputs { get; } = new ObservableDictionary<KeyController, TypeInfo>
        {
            [ResultKey] = TypeInfo.Number,
        };

        public override void Execute(Dictionary<KeyController, FieldControllerBase> inputs, Dictionary<KeyController, FieldControllerBase> outputs, DocumentController.DocumentFieldUpdatedEventArgs args, Scope scope = null)
        {
            string str = (inputs[InputStringKey] as TextController)?.Data;

            if (double.TryParse(str, out double num)) outputs[ResultKey] = new NumberController(num);
            else throw new ScriptExecutionException(new TextErrorModel($"Failed to parse \"{str}\" as a double."));
        }
    }
}