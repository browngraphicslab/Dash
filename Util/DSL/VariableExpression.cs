﻿using System;
using System.Diagnostics;
using System.Threading.Tasks;

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

        public override Task<(FieldControllerBase, ControlFlowFlag)> Execute(Scope scope)
        {
            if (scope.TryGetVariable(_variableName, out var value))
            {
                return Task.FromResult((value, ControlFlowFlag.None));
            }

            throw new ScriptExecutionException(new VariableNotFoundExecutionErrorModel(_variableName));
        }

        public override FieldControllerBase CreateReference(Scope scope)
        {
            if (scope.TryGetVariable(_variableName, out var value))
            {
                return value;
            }

            return null;
        }

        public override DashShared.TypeInfo Type => DashShared.TypeInfo.Any;

        public string GetVariableName()
        {
            return _variableName;
        }
    }
}
