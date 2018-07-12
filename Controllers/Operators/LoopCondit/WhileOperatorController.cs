using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DashShared;

namespace Dash
{
    [OperatorType(Op.Name.while_lp)]
    public class WhileOperatorController : OperatorController
    {
        public WhileOperatorController(OperatorModel operatorFieldModel) : base(operatorFieldModel)
        {
        }

        public WhileOperatorController() : base(new OperatorModel(TypeKey.KeyModel))
        {
        }

        public override KeyController OperatorType { get; } = TypeKey;
        private static readonly KeyController TypeKey = new KeyController("While", "CC159893-283C-4307-A4E8-A98E75C8EA1E");

        //Input keys
        //public static readonly KeyController BinaryKey 
        public static readonly KeyController BoolKey = new KeyController("Bool");
        public static readonly KeyController BlockKey = new KeyController("Block");

        //Output keys
        public static readonly KeyController ResultKey = new KeyController("Result");

        public override ObservableCollection<KeyValuePair<KeyController, IOInfo>> Inputs { get; } = new ObservableCollection<KeyValuePair<KeyController, IOInfo>>
        {
            new KeyValuePair<KeyController, IOInfo>(BoolKey, new IOInfo(TypeInfo.Bool, true)),
            new KeyValuePair<KeyController, IOInfo>(BlockKey, new IOInfo(TypeInfo.Any, true)),
        };
        public override ObservableDictionary<KeyController, TypeInfo> Outputs { get; } = new ObservableDictionary<KeyController, TypeInfo>
        {
            [ResultKey] = TypeInfo.Any,
        };

        public override void Execute(Dictionary<KeyController, FieldControllerBase> inputs, 
            Dictionary<KeyController, FieldControllerBase> outputs, DocumentController.DocumentFieldUpdatedEventArgs args, Scope scope = null)
        {
            //TODO: get rid of output necesary
            FieldControllerBase result;
            inputs.TryGetValue(BlockKey, out result);
            outputs[ResultKey] = result;
        }

        public override FieldControllerBase GetDefaultController()
        {
            return new WhileOperatorController();
        }
    }
}
