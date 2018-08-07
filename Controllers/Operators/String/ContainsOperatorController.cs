using DashShared;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace Dash
{
    [OperatorType(Op.Name.contains)]
    public class ContainsOperatorController : OperatorController
    {
        public ContainsOperatorController(OperatorModel operatorFieldModel) : base(operatorFieldModel)
        {
        }

        public ContainsOperatorController() : base(new OperatorModel(TypeKey.KeyModel))
        {
            SaveOnServer();

        }

        public override FieldControllerBase GetDefaultController() => new ContainsOperatorController();

        // input keys
        public static readonly KeyController InputStringKey = new KeyController("Input String");
        public static readonly KeyController ContainerStringKey = new KeyController("Container String");

        // output keys
        public static readonly KeyController ResultKey = new KeyController("Result");


        public override KeyController OperatorType { get; } = TypeKey;
        private static readonly KeyController TypeKey = new KeyController("Contains", "F9CD7950-4133-47F6-A1AA-CC78E3562FD3");

        public override ObservableCollection<KeyValuePair<KeyController, IOInfo>> Inputs { get; } = new ObservableCollection<KeyValuePair<KeyController, IOInfo>>
        {
            new KeyValuePair<KeyController, IOInfo>(InputStringKey, new IOInfo(TypeInfo.Text, true)),
            new KeyValuePair<KeyController, IOInfo>(ContainerStringKey, new IOInfo(TypeInfo.Text, true))
        };

        public override ObservableDictionary<KeyController, TypeInfo> Outputs { get; } = new ObservableDictionary<KeyController, TypeInfo>
        {
            [ResultKey] = TypeInfo.Bool,
        };
        public override void Execute(Dictionary<KeyController, FieldControllerBase> inputs,
            Dictionary<KeyController, FieldControllerBase> outputs,
            DocumentController.DocumentFieldUpdatedEventArgs args, Scope scope = null)
        {
            var str = (inputs[InputStringKey] as TextController).Data;
            var res = (inputs[ContainerStringKey] as TextController).Data;
            outputs[ResultKey] = new BoolController(res.Contains(str));
        }
    }
}