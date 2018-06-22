using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DashShared;

namespace Dash
{
    [OperatorType("lessthan")]
    class LessThanOperatorController : OperatorController
    {

        public LessThanOperatorController(OperatorModel operatorFieldModel) : base(operatorFieldModel)
        {
        }

        public LessThanOperatorController() : base(new OperatorModel(TypeKey.KeyModel))
        {
        }

        public override KeyController OperatorType { get; } = TypeKey;
        private static readonly KeyController TypeKey = new KeyController("074E6327-17FA-4C23-A360-02D955E7E42F", "LessThan");

        //Input keys
        public static readonly KeyController AKey = new KeyController("47608369-77D0-4C98-8267-D8E773F919EA", "A");
        public static readonly KeyController BKey = new KeyController("FA7C82A5-C366-4827-BC78-5DF0B915F275", "B");

        //Output keys
        public static readonly KeyController LessKey = new KeyController("CFEDC209-713D-4E66-AA84-AFB7C0B53FEA", "Less");

        public override ObservableCollection<KeyValuePair<KeyController, IOInfo>> Inputs { get; } = new ObservableCollection<KeyValuePair<KeyController, IOInfo>>
        {
            new KeyValuePair<KeyController, IOInfo>(AKey, new IOInfo(TypeInfo.Number, true)),
            new KeyValuePair<KeyController, IOInfo>(BKey, new IOInfo(TypeInfo.Number, true))
        };
        public override ObservableDictionary<KeyController, TypeInfo> Outputs { get; } = new ObservableDictionary<KeyController, TypeInfo>
        {
            //TODO: change to bool controller, BoolController
            [LessKey] = TypeInfo.Bool,
        };

        public override void Execute(Dictionary<KeyController, FieldControllerBase> inputs, Dictionary<KeyController, FieldControllerBase> outputs, FieldUpdatedEventArgs args, ScriptState state = null)
        {
            var numberA = (NumberController)inputs[AKey];
            var numberB = (NumberController)inputs[BKey];

            var a = numberA.Data;
            var b = numberB.Data;

            //TODO: BoolController
            outputs[LessKey] = new BoolController(a < b);
        }

        public override FieldControllerBase GetDefaultController()
        {
            return new LessThanOperatorController();
        }
    }
}
