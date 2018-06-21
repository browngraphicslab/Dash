using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DashShared;

namespace Dash
{
    [OperatorType("while")]
    class WhileOperatorController : OperatorController
    {
        public WhileOperatorController(OperatorModel operatorFieldModel) : base(operatorFieldModel)
        {
        }

        public WhileOperatorController() : base(new OperatorModel(TypeKey.KeyModel))
        {
        }

        public override KeyController OperatorType { get; } = TypeKey;
        private static readonly KeyController TypeKey = new KeyController("144E6E9D-E430-443E-85A9-29F87CE99DF5", "While");

        //Input keys
        //public static readonly KeyController BinaryKey 
        public static readonly KeyController BoolKey = new KeyController("EDCCAA98-F90A-490B-8F07-ECFF8342C3B3", "Bool");
        public static readonly KeyController BlockKey = new KeyController("5DF879DC-04DF-467D-93C9-38F4A2AB98CF", "Block");

        //Output keys
        public static readonly KeyController ResultKey = new KeyController("982DB305-A593-40B2-A75B-2D65C9212FBD", "Result");

        public override ObservableCollection<KeyValuePair<KeyController, IOInfo>> Inputs { get; } = new ObservableCollection<KeyValuePair<KeyController, IOInfo>>
        {
            new KeyValuePair<KeyController, IOInfo>(BoolKey, new IOInfo(TypeInfo.Number, true)),
            new KeyValuePair<KeyController, IOInfo>(BlockKey, new IOInfo(TypeInfo.Any, true)),
        };
        public override ObservableDictionary<KeyController, TypeInfo> Outputs { get; } = new ObservableDictionary<KeyController, TypeInfo>
        {
            [ResultKey] = TypeInfo.Any,
        };

        public override void Execute(Dictionary<KeyController, FieldControllerBase> inputs, Dictionary<KeyController, FieldControllerBase> outputs, FieldUpdatedEventArgs args, ScriptState state = null)
        {
            var Bool = ((NumberController)inputs[BoolKey]).Data;
            var BlockIf = inputs[BlockKey];


            //Todo: BoolController
            outputs[ResultKey] = Bool == 1 ? BlockIf : null;
        }

        public override FieldControllerBase GetDefaultController()
        {
            return new IfOperatorController();
        }
    }
}
