using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using DashShared;
using Zu.TypeScript.TsTypes;

// ReSharper disable once CheckNamespace
namespace Dash
{
    public class FunctionDeclarationExpression : ScriptExpression
    {
        private readonly string _functionCode;
        private readonly List<KeyValuePair<string, TypeInfo>> _parameters;
        private readonly ScriptExpression _funcBlock;
        private readonly DashShared.TypeInfo _returnType;
        
        public FunctionDeclarationExpression(string functionCode, NodeArray<ParameterDeclaration> paramss, ScriptExpression fB, DashShared.TypeInfo retur)
        {
            _functionCode = functionCode;
            _parameters = new List<KeyValuePair<string, TypeInfo>>();
            //set document keys
            foreach (ParameterDeclaration p in paramss)
            {
                //restrict types based on user input
                var inputType = TypeInfo.Any;
                string parType = p.Type?.GetText().ToLower();
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
                }
                _parameters.Add(new KeyValuePair<string, TypeInfo>(p.IdentifierStr, inputType));
            }

            _funcBlock = fB;
            _returnType = retur;
        }

        public override Task<FieldControllerBase> Execute(Scope scope)
        {
            return Task.FromResult<FieldControllerBase>(new FunctionOperatorController(_functionCode, _parameters, _funcBlock, _returnType));
        }

        public override FieldControllerBase CreateReference(Scope scope) => throw new NotImplementedException();

        public override DashShared.TypeInfo Type => _returnType;
    }
}
