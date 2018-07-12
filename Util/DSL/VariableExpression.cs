using System.Diagnostics;

namespace Dash
{
        /*
        private class LambdaExpression : ScriptExpression
        {
            public LambdaExpression(string variableName)
            public override FieldControllerBase Execute(ScriptState state)
            {
                throw new NotImplementedException();
            }

            public override FieldControllerBase CreateReference(ScriptState state)
            {
                throw new NotImplementedException();
            }

            public override TypeInfo Type { get; }
        }*/

    public class VariableExpression : ScriptExpression
    {
        private string _variableName;

        public VariableExpression(string variableName)
        {
            Debug.Assert(variableName != null);
            _variableName = variableName;
        }

        public override FieldControllerBase Execute(Scope scope)
        {
            if (scope[_variableName] != null)
            {
                return scope[_variableName];
            }
            throw new ScriptExecutionException(new VariableNotFoundExecutionErrorModel(_variableName));
        }

        public override FieldControllerBase CreateReference(Scope scope)
        {
            return Execute(scope);
        }

        public override DashShared.TypeInfo Type => DashShared.TypeInfo.Any;

        public string GetVariableName()
        {
            return _variableName;
        }
    }
}
