using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using DashShared;

namespace Dash
{
    public enum SelfRefAssignment
    {
        None,
        Addition,
        Subtraction,
        Multiplication,
        Division,
        Modulo,
        StringSearch
    }

    public class SelfRefAssignmentExpression : ScriptExpression
    {
        private readonly VariableExpression _var;
        private readonly ScriptExpression _assignExp;
        private readonly Op.Name _opName;

        public SelfRefAssignmentExpression(VariableExpression var, ScriptExpression assignExp, Op.Name opName)
        {
            _var = var;
            _assignExp = assignExp;
            _opName = opName;
        }

        public override FieldControllerBase Execute(Scope scope)
        {
            var varCtrl = _var.Execute(scope);
            var assignCtrl = _assignExp.Execute(scope);

            var inputs = new List<FieldControllerBase>
            {
                varCtrl,
                assignCtrl
            };

            FieldControllerBase output;
            try
            {
                output = OperatorScript.Run(_opName, inputs, scope);
                scope.SetVariable(_var.GetVariableName(), output);
            }
            catch (ScriptExecutionException)
            {
                throw;
            }
            catch (Exception)
            {
                throw new ScriptExecutionException(new GeneralScriptExecutionFailureModel(_opName));
            }
            return output;
        }

        public override FieldControllerBase CreateReference(Scope scope) => throw new NotImplementedException();

        public override DashShared.TypeInfo Type => OperatorScript.GetOutputType(_opName);
    }
}
