using System;
using System.Diagnostics;
using System.Threading.Tasks;

namespace Dash
{

    public class LetExpression : ScriptExpression
    {
        private string _variableName;
        private ScriptExpression _value;
        private ScriptExpression _expression;

        public LetExpression(string variableName, ScriptExpression value, ScriptExpression expression)
        {
            Debug.Assert(variableName != null);
            _variableName = variableName;
            _value = value;
            _expression = expression;
        }

        public override async Task<(FieldControllerBase, ControlFlowFlag)> Execute(Scope scope)
        {
            var (val, flags) = await _value.Execute(scope);
            scope.SetVariable(_variableName, val);
            return await _expression.Execute(scope);
        }

        public override FieldControllerBase CreateReference(Scope scope)
        {
            throw new NotImplementedException();
            //TODO tfs help with operator/doc stuff
        }

        //TODO tyler is this correct?
        public override DashShared.TypeInfo Type => _expression.Type;
    }
}
