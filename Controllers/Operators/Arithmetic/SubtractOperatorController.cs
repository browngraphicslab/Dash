using System.Collections.Generic;
using System.Collections.ObjectModel;
using DashShared;

namespace Dash
{
    public class SubtractOperatorController : OperatorController
    {
        public SubtractOperatorController(OperatorModel operatorFieldModel) : base(operatorFieldModel)
        {
        }
        public SubtractOperatorController() : base(new OperatorModel(TypeKey.KeyModel))
        {
        }

        public override KeyController OperatorType { get; } = TypeKey;
        private static readonly KeyController TypeKey = new KeyController("D98C45BF-ADD3-4832-A627-ED7DDBB3B04E", "Subtract");

        //Input keys
        public static readonly KeyController AKey = new KeyController("AE143AA5-4959-4B17-9FB4-B7BAB045366F", "A");
        public static readonly KeyController BKey = new KeyController("AE898286-9726-4BA9-9323-687609FF94F2", "B");

        //Output keys
        public static readonly KeyController DifferenceKey = new KeyController("851F7905-1363-4077-BF05-149327A55C34", "Difference");

        public override ObservableCollection<KeyValuePair<KeyController, IOInfo>> Inputs { get; } = new ObservableCollection<KeyValuePair<KeyController, IOInfo>>
        {
            new KeyValuePair<KeyController, IOInfo>(AKey, new IOInfo(TypeInfo.Number, true)),
            new KeyValuePair<KeyController, IOInfo>(BKey, new IOInfo(TypeInfo.Number, true))
        };
        public override ObservableDictionary<KeyController, TypeInfo> Outputs { get; } = new ObservableDictionary<KeyController, TypeInfo>
        {
            [DifferenceKey] = TypeInfo.Number,
        };

        public override void Execute(Dictionary<KeyController, FieldControllerBase> inputs, Dictionary<KeyController, FieldControllerBase> outputs, FieldUpdatedEventArgs args, ScriptState state = null)
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