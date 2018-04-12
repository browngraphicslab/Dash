using System;
using System.Diagnostics;

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

        public override FieldControllerBase Execute(ScriptState state)
        {
            var val = _value.Execute(state);
            var newState = state.AddOrUpdateValue(_variableName, val) as ScriptState;
            return _expression.Execute(newState);
        }

        public override FieldControllerBase CreateReference(ScriptState state)
        {
            throw new NotImplementedException();
            //TODO tfs help with operator/doc stuff
        }

        public override DashShared.TypeInfo Type
        {
            get { return _expression.Type; }
        } //TODO tyler is this correct?
    }
}
