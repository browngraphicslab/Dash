using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Zu.TypeScript.TsTypes;

namespace Dash
{
    class FunctionDeclarationExpression : ScriptExpression
    {
        private readonly string _functionCode;
        private readonly NodeArray<ParameterDeclaration> _parameters;
        private readonly ScriptExpression _funcBlock;
        private readonly DashShared.TypeInfo _returnType;


        public FunctionDeclarationExpression(string functionCode, NodeArray<ParameterDeclaration> param, 
            ScriptExpression fB, DashShared.TypeInfo retur)
        {
            _functionCode = functionCode;
            _parameters = param;
            _funcBlock = fB;
            _returnType = retur;
        }

        public override FieldControllerBase Execute(Scope scope)
        {
            var functionOperator = new FunctionOperatorController(_functionCode, _parameters, _funcBlock, _returnType);

            return functionOperator;
        }

  

        public override FieldControllerBase CreateReference(Scope scope)
        {
            //return OperatorScript.CreateDocumentForOperator(
            //    _parameters.Select(
            //        kvp => new KeyValuePair<KeyController, FieldControllerBase>(kvp.Key,
            //            kvp.Value.CreateReference(scope))), _opName); //recursive linq

            return null;
        }

        public override DashShared.TypeInfo Type => _returnType;
    }
}
