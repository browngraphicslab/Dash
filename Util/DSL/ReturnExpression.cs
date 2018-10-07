using DashShared;
using System;
using System.Threading.Tasks;

namespace Dash
{
    public class ReturnExpression : ScriptExpression
    {
        private readonly ScriptExpression _value;

        public ReturnExpression(ScriptExpression value) => _value = value;

        public override async Task<FieldControllerBase> Execute(Scope scope)
        {
            if (_value == null) throw new ScriptExecutionException(new InvalidReturnStatementErrorModel());
            var val = await _value.Execute(scope);
            //now return val
            scope.SetReturn(val);

            throw new ReturnException();
        }

        public override FieldControllerBase CreateReference(Scope scope)
        {
            throw new NotImplementedException();
        }

        public override DashShared.TypeInfo Type => TypeInfo.Any;
    }
}

