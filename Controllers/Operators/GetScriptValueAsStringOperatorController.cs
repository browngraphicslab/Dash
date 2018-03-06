using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DashShared;

namespace Dash
{
    [OperatorType("execToString")]
    public class GetScriptValueAsStringOperatorController : OperatorController
    {
        //Input keys
        public static readonly KeyController ScriptKey = new KeyController("7C56D13F-007A-45B3-AD42-E62DD14E802B", "Script");

        //Output keys
        public static readonly KeyController ResultKey = new KeyController("D56D3217-88ED-4BB2-A192-A3DB3F6427C2", "Result");


        public GetScriptValueAsStringOperatorController() : base(new OperatorModel(OperatorType.ExecuteDishToString))
        {
        }

        public GetScriptValueAsStringOperatorController(OperatorModel operatorFieldModel) : base(operatorFieldModel)
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

        public override ObservableCollection<KeyValuePair<KeyController, IOInfo>> Inputs { get; } = new ObservableCollection<KeyValuePair<KeyController, IOInfo>>()
        {
            new KeyValuePair<KeyController, IOInfo>(ScriptKey, new IOInfo(TypeInfo.Text, true))
        };
        public override ObservableDictionary<KeyController, TypeInfo> Outputs { get; } = new ObservableDictionary<KeyController, TypeInfo>()
        {
            [ResultKey] = TypeInfo.Text
        };
        public override void Execute(Dictionary<KeyController, FieldControllerBase> inputs, Dictionary<KeyController, FieldControllerBase> outputs, FieldUpdatedEventArgs args)
        {
            string result;
            try
            {
                result = OperatorScriptParser.Interpret((inputs[ScriptKey] as TextController)?.Data ?? "").GetValue(null).ToString();
            }
            catch (OperatorScriptParser.InvalidDishScriptException e)
            {
                result = e.ScriptErrorModel.Serialize();
            }
            outputs[ResultKey] = new TextController(result);
        }
    }
}
