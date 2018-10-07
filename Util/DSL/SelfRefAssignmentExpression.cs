using System;
using System.Collections.Generic;
using System.Threading.Tasks;

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

        public override async Task<FieldControllerBase> Execute(Scope scope)
        {
            var varCtrl = await _var.Execute(scope);
            var assignCtrl = await _assignExp.Execute(scope);

            var inputs = new List<FieldControllerBase>
            {
                varCtrl,
                assignCtrl
            };

            FieldControllerBase output;
            try
            {
                output = await OperatorScript.Run(_opName, inputs, scope);
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
