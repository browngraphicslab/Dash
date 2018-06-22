using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DashShared;

namespace Dash
{
    [OperatorType("equality")]
    class EqualityOperatorController : OperatorController
    {

        public EqualityOperatorController(OperatorModel operatorFieldModel) : base(operatorFieldModel)
        {
        }

        public EqualityOperatorController() : base(new OperatorModel(TypeKey.KeyModel))
        {
        }

        public override KeyController OperatorType { get; } = TypeKey;
        private static readonly KeyController TypeKey = new KeyController("66988CCF-69BD-4E17-AF70-EBE6E470542E", "Equality");

        //Input keys
        public static readonly KeyController AKey = new KeyController("E02A6760-2ECB-4940-8768-A59FC06FA7E7", "A");
        public static readonly KeyController BKey = new KeyController("06E1B08D-FF41-4722-81CA-8F1BCFB4D830", "B");

        //Output keys
        public static readonly KeyController EqualsEqualsKey = new KeyController("8F6EF94A-0065-4629-AB50-95CA96A8E412", "Equality");

        public override ObservableCollection<KeyValuePair<KeyController, IOInfo>> Inputs { get; } = new ObservableCollection<KeyValuePair<KeyController, IOInfo>>
        {
            new KeyValuePair<KeyController, IOInfo>(AKey, new IOInfo(TypeInfo.Any, true)),
            new KeyValuePair<KeyController, IOInfo>(BKey, new IOInfo(TypeInfo.Any, true))
        };
        public override ObservableDictionary<KeyController, TypeInfo> Outputs { get; } = new ObservableDictionary<KeyController, TypeInfo>
        {
            //TODO: change to bool controller, BoolController
            [EqualsEqualsKey] = TypeInfo.Bool,
        };

        public override void Execute(Dictionary<KeyController, FieldControllerBase> inputs, Dictionary<KeyController, FieldControllerBase> outputs, FieldUpdatedEventArgs args, ScriptState state = null)
        {
            var objectA = inputs[AKey];
            var objectB = inputs[BKey];

            var a = objectA.GetValue(null);
            var b = objectB.GetValue(null);

            //TODO: BoolController
            outputs[EqualsEqualsKey] = new BoolController((objectA.TypeInfo == objectB.TypeInfo) && a.Equals(b));
        }

        public override FieldControllerBase GetDefaultController()
        {
            return new EqualityOperatorController();
        }
    }
}
