using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DashShared;

namespace Dash
{
    [OperatorType("greaterthan")]
    class GreaterThanOperatorController : OperatorController
    {

        public GreaterThanOperatorController(OperatorModel operatorFieldModel) : base(operatorFieldModel)
        {
        }

        public GreaterThanOperatorController() : base(new OperatorModel(TypeKey.KeyModel))
        {
        }

        public override KeyController OperatorType { get; } = TypeKey;
        private static readonly KeyController TypeKey = new KeyController("340827B1-9D06-4682-BAF2-BBF4537333CC", "GreaterThan");

        //Input keys
        public static readonly KeyController AKey = new KeyController("2D48830B-BA97-4E05-9AC3-44C548DA1DF4", "A");
        public static readonly KeyController BKey = new KeyController("41C1A1EE-7185-4DFD-AC85-DAEDE1B0B9A3", "B");

        //Output keys
        public static readonly KeyController GreaterKey = new KeyController("86E123DA-DD9D-4076-9DFD-6C390D51C846", "Greater");

        public override ObservableCollection<KeyValuePair<KeyController, IOInfo>> Inputs { get; } = new ObservableCollection<KeyValuePair<KeyController, IOInfo>>
        {
            new KeyValuePair<KeyController, IOInfo>(AKey, new IOInfo(TypeInfo.Number, true)),
            new KeyValuePair<KeyController, IOInfo>(BKey, new IOInfo(TypeInfo.Number, true))
        };
        public override ObservableDictionary<KeyController, TypeInfo> Outputs { get; } = new ObservableDictionary<KeyController, TypeInfo>
        {
            //TODO: change to bool controller, BoolController
              [GreaterKey] = TypeInfo.Bool,
        };

        public override void Execute(Dictionary<KeyController, FieldControllerBase> inputs, Dictionary<KeyController, FieldControllerBase> outputs, FieldUpdatedEventArgs args, Scope scope = null)
        {
            var numberA = (NumberController)inputs[AKey];
            var numberB = (NumberController)inputs[BKey];

            var a = numberA.Data;
            var b = numberB.Data;

            //TODO: BoolController
             outputs[GreaterKey] = new BoolController(a > b);
        }

        public override FieldControllerBase GetDefaultController()
        {
            return new GreaterThanOperatorController();
        }
    }
}
