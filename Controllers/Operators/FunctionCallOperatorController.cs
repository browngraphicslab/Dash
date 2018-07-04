using System.Collections.Generic;
using System.Collections.ObjectModel;
using DashShared;

namespace Dash
{
    [OperatorType(Op.Name.function_call)]
    public class FunctionCallOperatorController : OperatorController
    {
        public FunctionCallOperatorController(OperatorModel operatorFieldModel) : base(operatorFieldModel)
        {
            SaveOnServer();
        }

        public FunctionCallOperatorController() : base(new OperatorModel(TypeKey.KeyModel))
        {
        }

        public override KeyController OperatorType { get; } = TypeKey;
        private static readonly KeyController TypeKey = new KeyController("83523074-A418-443F-B770-5BF4D5D27CD6", "FunctionCall");

        //Input keys 
        public static readonly KeyController NameKey = new KeyController("6129049E-6BF8-41CC-91BA-8FFB738A0A8A", "Name");

        //Output keys
        public static readonly KeyController ResultKey = new KeyController("BE91858D-1464-4124-B5EC-B781E7207106", "Result");

        public override ObservableCollection<KeyValuePair<KeyController, IOInfo>> Inputs { get; } = new ObservableCollection<KeyValuePair<KeyController, IOInfo>>
        {
            new KeyValuePair<KeyController, IOInfo>(NameKey, new IOInfo(TypeInfo.Text, true))
        };
        public override ObservableDictionary<KeyController, TypeInfo> Outputs { get; } = new ObservableDictionary<KeyController, TypeInfo>
        {
            [ResultKey] = TypeInfo.Any,
        };

        public override void Execute(Dictionary<KeyController, FieldControllerBase> inputs,
            Dictionary<KeyController, FieldControllerBase> outputs, DocumentController.DocumentFieldUpdatedEventArgs args, Scope scope = null)
        {
            var functionName = (inputs[NameKey] as TextController).Data;

            //get variable from scope
            ///cope?.DeclareVariable(functionName, functionString);
            var function = scope?.GetVariable(functionName) as FunctionOperatorController;


            //function?.Execute(scope);

          //  var functionNode = TypescriptToOperatorParser


            outputs[ResultKey] = new TextController("");
        }

        public override FieldControllerBase GetDefaultController()
        {
            return new WhileOperatorController();
        }
    }
}
