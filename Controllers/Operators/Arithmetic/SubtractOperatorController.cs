using System.Collections.Generic;
using System.Collections.ObjectModel;
using DashShared;

namespace Dash
{
    [OperatorType("minus", "subtract")]
    public class SubtractOperatorController : OperatorController
    {
        public SubtractOperatorController(OperatorModel operatorFieldModel) : base(operatorFieldModel)
        {
        }
        public SubtractOperatorController() : base(new OperatorModel(TypeKey.KeyModel))
        {
            SaveOnServer();

        }

        public override KeyController OperatorType { get; } = TypeKey;
        private static readonly KeyController TypeKey = new KeyController("Subtract", "D98C45BF-ADD3-4832-A627-ED7DDBB3B04E");

        //Input keys
        public static readonly KeyController AKey = new KeyController("A");
        public static readonly KeyController BKey = new KeyController("B");

        //Output keys
        public static readonly KeyController DifferenceKey = new KeyController("Difference");

        public override ObservableCollection<KeyValuePair<KeyController, IOInfo>> Inputs { get; } = new ObservableCollection<KeyValuePair<KeyController, IOInfo>>
        {
            new KeyValuePair<KeyController, IOInfo>(AKey, new IOInfo(TypeInfo.Number, true)),
            new KeyValuePair<KeyController, IOInfo>(BKey, new IOInfo(TypeInfo.Number, true))
        };
        public override ObservableDictionary<KeyController, TypeInfo> Outputs { get; } = new ObservableDictionary<KeyController, TypeInfo>
        {
            [DifferenceKey] = TypeInfo.Number,
        };

        public override void Execute(Dictionary<KeyController, FieldControllerBase> inputs,
            Dictionary<KeyController, FieldControllerBase> outputs,
            DocumentController.DocumentFieldUpdatedEventArgs args, ScriptState state = null)
        {
            var numberA = (NumberController)inputs[AKey];
            var numberB = (NumberController)inputs[BKey];

            var a = numberA.Data;
            var b = numberB.Data;

            outputs[DifferenceKey] = new NumberController(a - b);
        }

        public override FieldControllerBase GetDefaultController()
        {
            return new SubtractOperatorController();
        }
    }
}