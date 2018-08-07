using DashShared;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace Dash
{
    [OperatorType(Op.Name.parse_double)]
    public class ParseDoubleOperatorController : OperatorController
    {
        public ParseDoubleOperatorController(OperatorModel operatorFieldModel) : base(operatorFieldModel)
        {
        }

        public ParseDoubleOperatorController() : base(new OperatorModel(TypeKey.KeyModel))
        {
            SaveOnServer();

        }

        public override FieldControllerBase GetDefaultController() => new ContainsOperatorController();

        // input keys
        public static readonly KeyController InputStringKey = new KeyController("Input String");

        // output keys
        public static readonly KeyController ResultKey = new KeyController("Double");


        public override KeyController OperatorType { get; } = TypeKey;
        private static readonly KeyController TypeKey = new KeyController("ParseDouble", "3DA7315D-2B6C-42B7-B387-124D140687AC");

        public override ObservableCollection<KeyValuePair<KeyController, IOInfo>> Inputs { get; } = new ObservableCollection<KeyValuePair<KeyController, IOInfo>>
        {
            new KeyValuePair<KeyController, IOInfo>(InputStringKey, new IOInfo(TypeInfo.Text, true)),
        };

        public override ObservableDictionary<KeyController, TypeInfo> Outputs { get; } = new ObservableDictionary<KeyController, TypeInfo>
        {
            [ResultKey] = TypeInfo.Number,
        };
        public override void Execute(Dictionary<KeyController, FieldControllerBase> inputs,
            Dictionary<KeyController, FieldControllerBase> outputs,
            DocumentController.DocumentFieldUpdatedEventArgs args, Scope scope = null)
        {
            var str = (inputs[InputStringKey] as TextController).Data;
            double num;
            var result = double.TryParse(str, out num);
            if (result)
            {
                outputs[ResultKey] = new NumberController(num);
            }
            else
            {
                outputs[ResultKey] = null;
            }
           
        }
    }
}