using System;
using System.Diagnostics;
using System.Threading.Tasks;
using DashShared;

// ReSharper disable once CheckNamespace
namespace Dash
{
    public class VariableDeclarationExpression : ScriptExpression
    {
        private readonly string _variableName;
        private readonly ScriptExpression _value;
        private readonly bool _unassignVar;

        public VariableDeclarationExpression(string variableName, ScriptExpression value, bool unassignVar)
        {
            Debug.Assert(variableName != null);
            _variableName = variableName;
            _value = value;
            _unassignVar = unassignVar;
            if (_value == null) throw new ScriptExecutionException(new VariableNotFoundExecutionErrorModel(_variableName));
        }

        public override async Task<FieldControllerBase> Execute(Scope scope)
        {
            if (_unassignVar)
            {
                scope.DeleteVariable(_variableName);

                return new TextController("");
            }

            FieldControllerBase value = scope.GetVariable(_variableName);
            if (value != null) throw new ScriptExecutionException(new DuplicateVariableDeclarationErrorModel(_variableName, value));
            FieldControllerBase val = await _value.Execute(scope);
            scope.DeclareVariable(_variableName, val);

            return val;
        }

        public override FieldControllerBase CreateReference(Scope scope)
        {
            throw new NotImplementedException();
            //TODO tfs help with operator/doc stuff
        }

        //TODO tyler is this correct?
        public override TypeInfo Type => TypeInfo.Any;
    }
}
