using System.Collections.Generic;
using System.Collections.ObjectModel;
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


        public GetScriptValueAsStringOperatorController() : base(new OperatorModel(TypeKey.KeyModel))
        {
            SaveOnServer();
        }

        public GetScriptValueAsStringOperatorController(OperatorModel operatorFieldModel) : base(operatorFieldModel)
        {
        }

        public override FieldControllerBase GetDefaultController()
        {
            return new GetScriptValueAsStringOperatorController();
        }

        public override ObservableCollection<KeyValuePair<KeyController, IOInfo>> Inputs { get; } = new ObservableCollection<KeyValuePair<KeyController, IOInfo>>()
        {
            new KeyValuePair<KeyController, IOInfo>(ScriptKey, new IOInfo(TypeInfo.Text, true))
        };
        public override ObservableDictionary<KeyController, TypeInfo> Outputs { get; } = new ObservableDictionary<KeyController, TypeInfo>()
        {
            [ResultKey] = TypeInfo.Text
        };

        public override KeyController OperatorType { get; } = TypeKey;
        private static readonly KeyController TypeKey = new KeyController("99E9328B-7341-403F-819B-26CDAB2F9A51", "Exec to string");

        public override void Execute(Dictionary<KeyController, FieldControllerBase> inputs, Dictionary<KeyController, FieldControllerBase> outputs, FieldUpdatedEventArgs args)
        {
            string result;
            try
            {
                result = OperatorScriptParser.Interpret((inputs[ScriptKey] as TextController)?.Data ?? "").GetValue(null).ToString();
            }
            catch (OperatorScriptParser.InvalidDishScriptException e)
            {
                result = e.ScriptErrorModel.GetHelpfulString();
            }
            outputs[ResultKey] = new TextController(result);
        }
    }
}
