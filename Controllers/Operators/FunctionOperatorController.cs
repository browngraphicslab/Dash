using System;
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
        private FunctionOperatorModel FunctionModel => Model as FunctionOperatorModel;

        private bool _initialized = true;
        public FunctionOperatorController(OperatorModel operatorFieldModel) : base(operatorFieldModel)
        {
            _initialized = false;
            Debug.Assert(operatorFieldModel is FunctionOperatorModel);
            var model = (FunctionOperatorModel)operatorFieldModel;

            InitFunc(model.Parameters, TypescriptToOperatorParser.ParseToExpression(model.FunctionCode), model.ReturnType);
        }

        public override async Task InitializeAsync()
        {
            if (_initialized) return;

            _initialized = true;

            if (FunctionModel.CaptureDocumentID != null)
            {
                _documentScope = await RESTClient.Instance.Fields.GetControllerAsync<DocumentController>(FunctionModel.CaptureDocumentID);
                _funcScope = Scope.FromDocument(_documentScope);
            }
        }

        protected override void RefInit()
        {
            base.RefInit();
            ReferenceField(_documentScope);
        }

        protected override void RefDestroy()
        {
            base.RefDestroy();
            ReleaseField(_documentScope);
        }

        public FunctionOperatorController() : base(new FunctionOperatorModel("", new List<KeyValuePair<string, TypeInfo>>(), TypeInfo.None, TypeKey.KeyModel, null)) { }

        public FunctionOperatorController(string functionCode, List<KeyValuePair<string, TypeInfo>> paramss, ScriptExpression block, TypeInfo returnType, Scope scope = null) : this(functionCode, paramss, block, returnType, scope?.ToDocument(true))
        {
        }

        public FunctionOperatorController(string functionCode, List<KeyValuePair<string, TypeInfo>> paramss, ScriptExpression block, TypeInfo returnType, DocumentController scopeDocument)
            : base( new FunctionOperatorModel(functionCode, paramss, returnType, TypeKey.KeyModel, scopeDocument.Id))
        {
            _funcScope = Scope.FromDocument(scopeDocument);
            _documentScope = scopeDocument;
            InitFunc(paramss, block, returnType);
        }

        private void InitFunc(List<KeyValuePair<string, TypeInfo>> paramss, ScriptExpression block, TypeInfo returnType)
        {
            _block = block;
            _returnType = returnType;

            foreach (var param in paramss)
            {
                Inputs.Add(new KeyValuePair<KeyController, IOInfo>(KeyController.Get(param.Key), new IOInfo(param.Value, true)));
                _inputNames.Add(param.Key);
            }

        }

        private readonly List<string> _inputNames = new List<string>();
        private ScriptExpression _block;
        private TypeInfo _returnType;

        private Scope _funcScope;
        private DocumentController _documentScope;

        public override KeyController OperatorType { get; } = TypeKey;
        private static readonly KeyController TypeKey = KeyController.Get("Function");


        //Output keys
        public static readonly KeyController ResultKey = KeyController.Get("Result");

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
            scope = new Scope(_funcScope);
            for (var i = 0; i < _inputNames.Count; i++)
            {
                scope.DeclareVariable(_inputNames[i], inputs[Inputs[i].Key]);
            }

            var (result, _) = await _block.Execute(scope);

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
