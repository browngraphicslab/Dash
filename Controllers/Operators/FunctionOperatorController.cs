using System.Collections.Generic;
using System.Collections.ObjectModel;
using DashShared;
using Zu.TypeScript.TsTypes;

namespace Dash
{
    [OperatorType(Op.Name.function)]
    public class FunctionOperatorController :OperatorController
    {
        public FunctionOperatorController(OperatorModel operatorFieldModel) : base(operatorFieldModel)
        {
            SaveOnServer();
        }

        public FunctionOperatorController() : base(new OperatorModel(TypeKey.KeyModel))
        {
        }

        public FunctionOperatorController(NodeArray<ParameterDeclaration> paramss, ScriptExpression block, TypeInfo returnType) : base(new OperatorModel(TypeKey.KeyModel))
        {
            _block = block;
            _returnType = returnType;

            //set document keys
            foreach (var param in paramss)
            {
                var newKey = new KeyController(param.IdentifierStr);
                _inputNames.Add(param.IdentifierStr);
                
                //restrict types based on user input
                var inputType = TypeInfo.Any;
                var parType = param.Type?.GetText().ToLower();
                //this now only handles numbers, text and bool. If another type is needed, add a case
                switch (parType)
                {
                    case "number":
                        inputType = TypeInfo.Number;
                        break;
                    case "string":
                        inputType = TypeInfo.Text;
                        break;
                    case "boolean":
                        inputType = TypeInfo.Bool;
                        break;
                    case "document":
                        inputType = TypeInfo.Document;
                        break;
                    case "list":
                        inputType = TypeInfo.List;
                        break;
                    default:
                        break;
                }

                Inputs.Add(new KeyValuePair<KeyController, IOInfo>(newKey, new IOInfo(inputType, true)));
            }
            

        }
         
        private List<string> _inputNames = new List<string>();
        private ScriptExpression _block;
        private TypeInfo _returnType;

        public override KeyController OperatorType { get; } = TypeKey;
        private static readonly KeyController TypeKey = new KeyController("FA160F44-26A8-40DC-ABBE-CC3F3EEA9420", "Function");


        //Output keys
        public static readonly KeyController ResultKey = new KeyController("1BFC2A49-CB4C-4D00-99FC-B0A9E61E32D0", "Result");

        public override ObservableCollection<KeyValuePair<KeyController, IOInfo>> Inputs { get; } = new ObservableCollection<KeyValuePair<KeyController, IOInfo>>
        {
        };

        public override ObservableDictionary<KeyController, TypeInfo> Outputs { get; } = new ObservableDictionary<KeyController, TypeInfo>
        {
            [ResultKey] = TypeInfo.Any,
        };

        public override void Execute(Dictionary<KeyController, FieldControllerBase> inputs,
            Dictionary<KeyController, FieldControllerBase> outputs, DocumentController.DocumentFieldUpdatedEventArgs args, Scope scope = null)
        {
            for (int i = 0; i < _inputNames.Count; i++)
            {
                var value = inputs[Inputs[i].Key];
                
                var expectedType = Inputs[i].Value.Type;

                //if not expected type , don't run
                if (expectedType != TypeInfo.Any && value.TypeInfo != expectedType)
                {
                    throw new ScriptExecutionException(new TextErrorModel("Parameter #" + (i + 1) + " must be of type " + expectedType + ". Potentially other mismatched parameters."));
                }
                scope?.DeclareVariable(_inputNames[i], value);
            }

            var result = _block.Execute(scope);

            outputs[ResultKey] = result;
          
        }

        public override FieldControllerBase GetDefaultController()
        {
            return new FunctionOperatorController();
        }

        public string getFunctionString()
        {
            return _block.ToString();
        }
    }
}