using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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

        public ExecDishOperatorController() : base(new OperatorModel(OperatorType.ExecDish))
        {
        }


        public ExecDishOperatorController(OperatorModel operatorFieldModel) : base(operatorFieldModel)
        {
        }

        public override bool SetValue(object value)
        {
            throw new NotImplementedException();
        }

        public override object GetValue(Context context)
        {
            throw new NotImplementedException();
        }

        public override FieldModelController<OperatorModel> Copy()
        {
            throw new NotImplementedException();
        }

        public override ObservableDictionary<KeyController, IOInfo> Inputs { get; } = new ObservableDictionary<KeyController, IOInfo>()
        {
            [ScriptKey] = new IOInfo(TypeInfo.Text, true),
        };
        public override ObservableDictionary<KeyController, TypeInfo> Outputs { get; } = new ObservableDictionary<KeyController, TypeInfo>()
        {
            [ResultKey] = TypeInfo.Any
        };
        public override void Execute(Dictionary<KeyController, FieldControllerBase> inputs, Dictionary<KeyController, FieldControllerBase> outputs, FieldUpdatedEventArgs args)
        {
            var result = OperatorScriptParser.Interpret((inputs[ScriptKey] as TextController)?.Data ?? "");
            outputs[ResultKey] = result;
        }
    }
}
