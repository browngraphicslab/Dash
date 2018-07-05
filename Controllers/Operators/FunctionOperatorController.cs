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

        public FunctionOperatorController(NodeArray<ParameterDeclaration> paramss, ScriptExpression block, TypeInfo type) : base(new OperatorModel(TypeKey.KeyModel))
        {
            _block = block;
            _returnType = type;

            //set document keys
            foreach (var param in paramss)
            {
                var newKey = KeyController.LookupKeyByName(_inputNames.Count.ToString(), true);
                _inputNames.Add(param.IdentifierStr);
                //TODO: get types of each parameter and set it in making IOInfo
                Inputs.Add(new KeyValuePair<KeyController, IOInfo>(newKey, new IOInfo(TypeInfo.Any, true)));
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
            Scope newScope = new Scope(scope);
            for (int i = 0; i < _inputNames.Count; i++)
            {
                newScope.DeclareVariable(_inputNames[i], inputs[KeyController.LookupKeyByName(i.ToString())]);
            }

            var result = _block.Execute(newScope);
           


            //var functionString = inputs[StringKey];
            //var functionName = (inputs[NameKey] as TextController).Data;

            ////add function as variable
            //scope?.DeclareVariable(functionName, this);

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