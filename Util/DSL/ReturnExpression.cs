using DashShared;
using System;

namespace Dash
{
    public class ReturnExpression : ScriptExpression
    {
        private readonly ScriptExpression _value;

        public ReturnExpression(ScriptExpression value) => _value = value;

        public override FieldControllerBase Execute(Scope scope)
        {
            if (_value == null) throw new ScriptExecutionException(new InvalidReturnStatementErrorModel());
            var val = _value.Execute(scope);
            scope.GetFirstAncestor().SetReturn(val);
            throw new ReturnException();
            //return val;
        }

        public override FieldControllerBase CreateReference(Scope scope)
        {
            throw new NotImplementedException();
            //TODO tfs help with operator/doc stuff
        }

        public override DashShared.TypeInfo Type
            //TODO tyler is this correct?
            => TypeInfo.Any;
    }
}

