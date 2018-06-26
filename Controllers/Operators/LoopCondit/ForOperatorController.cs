using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using DashShared;

namespace Dash
{
    [OperatorType("for")]
    public class ForOperatorController : OperatorController
    {
        public ForOperatorController(OperatorModel operatorFieldModel) : base(operatorFieldModel)
        {
        }

        public ForOperatorController() : base(new OperatorModel(TypeKey.KeyModel))
        {
        }

        public override KeyController OperatorType { get; } = TypeKey;
        private static readonly KeyController TypeKey = new KeyController("3294AA7E-6E6A-4AE7-9D83-5693723A1EEA", "For");

        //Input keys
        public static readonly KeyController CounterDeclarationKey = new KeyController("E7E6C0E7-C0E1-412D-B9F0-6B3E929BB87C", "Count Declaration");
        public static readonly KeyController BoolKey = new KeyController("69E81476-0EAA-4519-A8C1-3D30760D79E2", "Bool");
        public static readonly KeyController IncrementKey = new KeyController("096AF011-3488-4E40-96F4-728ADAB3F01B", "Increment Size");
        public static readonly KeyController ForBlockKey = new KeyController("AC41991B-244B-4ADC-BE63-751FABC005C4", "For Loop Body");

        //Output keys
        public static readonly KeyController ResultKey = new KeyController("72C03CCA-7B4F-4558-906B-72A6EF9BE66C", "Result");

        public override ObservableCollection<KeyValuePair<KeyController, IOInfo>> Inputs { get; } = new ObservableCollection<KeyValuePair<KeyController, IOInfo>>
        {
            new KeyValuePair<KeyController, IOInfo>(CounterDeclarationKey, new IOInfo(TypeInfo.Any, true)),
            new KeyValuePair<KeyController, IOInfo>(BoolKey, new IOInfo(TypeInfo.Bool, true)),
            new KeyValuePair<KeyController, IOInfo>(IncrementKey, new IOInfo(TypeInfo.Any, true)),
            new KeyValuePair<KeyController, IOInfo>(ForBlockKey, new IOInfo(TypeInfo.Any, true))
        };
        public override ObservableDictionary<KeyController, TypeInfo> Outputs { get; } = new ObservableDictionary<KeyController, TypeInfo>
        {
            [ResultKey] = TypeInfo.Any,
        };

        public override void Execute(Dictionary<KeyController, FieldControllerBase> inputs, Dictionary<KeyController, FieldControllerBase> outputs, DocumentController.DocumentFieldUpdatedEventArgs args, Scope scope = null)
        {
            //TODO: get rid of output necesary
            outputs[ResultKey] = new NumberController(0);
        }

        public override FieldControllerBase GetDefaultController()
        {
            return new ForOperatorController();
        }
    }
}
