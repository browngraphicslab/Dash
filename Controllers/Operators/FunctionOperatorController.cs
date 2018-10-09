using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using DashShared;

// ReSharper disable once CheckNamespace
namespace Dash
{
    [OperatorType(Op.Name.function)]
    public sealed class FunctionOperatorController : OperatorController
    {
        public FunctionOperatorController(OperatorModel operatorFieldModel) : base(operatorFieldModel)
        {
            Debug.Assert(operatorFieldModel is FunctionOperatorModel);
            var model = (FunctionOperatorModel)operatorFieldModel;

            InitFunc(model.Parameters, TypescriptToOperatorParser.ParseToExpression(model.FunctionCode), model.ReturnType);
        }

        public FunctionOperatorController() : base(new FunctionOperatorModel("", new List<KeyValuePair<string, TypeInfo>>(), TypeInfo.None, TypeKey.KeyModel)) => SaveOnServer();

        public FunctionOperatorController(string functionCode, List<KeyValuePair<string, TypeInfo>> paramss, ScriptExpression block, TypeInfo returnType, Scope scope = null) : base(new FunctionOperatorModel(functionCode, paramss, returnType, TypeKey.KeyModel))
        {
            _funcScope = scope;
            InitFunc(paramss, block, returnType);
        }

        private void InitFunc(List<KeyValuePair<string, TypeInfo>> paramss, ScriptExpression block, TypeInfo returnType)
        {
            _block = block;
            _returnType = returnType;

            foreach (var param in paramss)
            {
                Inputs.Add(new KeyValuePair<KeyController, IOInfo>(new KeyController(param.Key), new IOInfo(param.Value, true)));
                _inputNames.Add(param.Key);
            }

            SaveOnServer();
        }

        private readonly List<string> _inputNames = new List<string>();
        private ScriptExpression _block;
        private TypeInfo _returnType;
        private Scope _funcScope;

        public override KeyController OperatorType { get; } = TypeKey;
        private static readonly KeyController TypeKey = new KeyController("Function", "1573E918-19E0-47A9-BB9D-0531233277C9");


        //Output keys
        public static readonly KeyController ResultKey = new KeyController("Result");

        public override ObservableCollection<KeyValuePair<KeyController, IOInfo>> Inputs { get; } = new ObservableCollection<KeyValuePair<KeyController, IOInfo>>
        {
        };

        public override ObservableDictionary<KeyController, TypeInfo> Outputs { get; } = new ObservableDictionary<KeyController, TypeInfo>
        {
            [ResultKey] = TypeInfo.Any,
        };

        public override async Task Execute(Dictionary<KeyController, FieldControllerBase> inputs,
            Dictionary<KeyController, FieldControllerBase> outputs,
            DocumentController.DocumentFieldUpdatedEventArgs args, Scope scope = null)
        {
            scope = new Scope(scope);
            for (var i = 0; i < _inputNames.Count; i++)
            {
                FieldControllerBase value = inputs[Inputs[i].Key];

                TypeInfo expectedType = Inputs[i].Value.Type;

                //if not expected type , don't run
                if (expectedType != TypeInfo.Any && value.TypeInfo != expectedType)
                {
                    throw new ScriptExecutionException(new TextErrorModel("Parameter #" + (i + 1) + " must be of type " + expectedType + ". Potentially other mismatched parameters."));
                }
                scope.DeclareVariable(_inputNames[i], value);
            }

            if (_funcScope != null)
            {
                scope = scope.Merge(_funcScope);
            }

            FieldControllerBase result = await _block.Execute(scope);

            outputs[ResultKey] = result;
        }

        public override FieldControllerBase GetDefaultController() => new FunctionOperatorController();

        public override string ToString()
        {
            var returnType = _returnType == TypeInfo.None ? "void" : _returnType.ToString();
            var parameters = string.Join(", ", Inputs.Select(kv => kv.Value.Type));
            return $"{returnType} function({parameters})";
        }

        public string GetFunctionString() => _block.ToString();
    }
}
