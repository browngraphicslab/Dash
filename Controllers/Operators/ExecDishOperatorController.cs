using System.Collections.Generic;
using System.Collections.ObjectModel;
using DashShared;

namespace Dash
{
    /// <summary>
    /// Operator Class used to execute Dish Scripting Language (DSL) as a string and return the return value
    /// </summary>
    [OperatorType("exec")]
    public class ExecDishOperatorController : OperatorController
    {

        //Input keys
        public static readonly KeyController ScriptKey = new KeyController("0F040954-2914-4794-90C4-FE442DD665B4", "Script");

        //Output keys
        public static readonly KeyController ResultKey = new KeyController("5006A73E-2466-4301-9A95-78083000603E", "Result");

        public ExecDishOperatorController() : base(new OperatorModel(TypeKey.KeyModel))
        {
        }


        public ExecDishOperatorController(OperatorModel operatorFieldModel) : base(operatorFieldModel)
        {
        }

        public override ObservableCollection<KeyValuePair<KeyController, IOInfo>> Inputs { get; } = new ObservableCollection<KeyValuePair<KeyController, IOInfo>>()
        {
            new KeyValuePair<KeyController, IOInfo>(ScriptKey, new IOInfo(DashShared.TypeInfo.Text, true))
        };
        public override ObservableDictionary<KeyController, DashShared.TypeInfo> Outputs { get; } = new ObservableDictionary<KeyController, DashShared.TypeInfo>()
        {
            [ResultKey] = DashShared.TypeInfo.Any
        };

        public override KeyController OperatorType { get; } = TypeKey;
        private static readonly KeyController TypeKey = new KeyController("F2AF66A0-81D0-42CD-ADD3-35EC2A949AB0", "Exec");

        public override void Execute(Dictionary<KeyController, FieldControllerBase> inputs, Dictionary<KeyController, FieldControllerBase> outputs, FieldUpdatedEventArgs args, ScriptState state = null)
        {
            try
            {
                var result = OperatorScriptParser.Interpret((inputs[ScriptKey] as TextController)?.Data ?? "");
                outputs[ResultKey] = result;
            }
            catch (InvalidDishScriptException dishScriptException)
            {
                outputs[ResultKey] = new TextController(dishScriptException.ScriptErrorModel.GetHelpfulString());
            }
        }

        public override FieldControllerBase GetDefaultController()
        {
            return new ExecDishOperatorController();
        }
    }
}
