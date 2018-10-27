using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Dash
{
    public class FunctionExpression : ScriptExpression
    {
        private readonly List<ScriptExpression> _parameters;
        private readonly ScriptExpression _funcName;

        public FunctionExpression(List<ScriptExpression> parameters, ScriptExpression func)
        {
            _funcName = func;
            _parameters = parameters;
        }

        public FunctionExpression(Op.Name op, List<ScriptExpression> parameters)
        {
            _funcName = new VariableExpression(op.ToString());
            _parameters = parameters;
        }

        public override async Task<FieldControllerBase> Execute(Scope scope)
        {
            //TODO ScriptLang - Don't take _funcName, take a script expression that evaluated to a FuncitonOperatorController
            OperatorController op = null;
            var opName = Op.Name.invalid;
            //try
            //{
            //    op = await _funcName.Execute(scope) as FunctionOperatorController;
            //}
            //catch (ScriptExecutionException)
            {
                if (!(_funcName is VariableExpression variable))
                {
                    throw new Exception();
                }

                var variableName = variable.GetVariableName();
                opName = Op.Parse(variableName);
                if (opName == Op.Name.invalid)
                {
                    throw new Exception();
                }
            }

            var inputs = new List<FieldControllerBase>();
            foreach (var scriptExpression in _parameters)
            {
                if (scriptExpression == null)
                {
                    inputs.Add(null);
                }
                else
                {
                    inputs.Add(await scriptExpression.Execute(scope));
                }
            }

            try
            {
                scope = new ReturnScope();

                var output = /*op != null ? await OperatorScript.Run(op, inputs, scope) : */await OperatorScript.Run(opName, inputs, scope);
                return output;
            }
            catch (ReturnException)
            {
                return scope.GetReturn;
            }
            catch (ScriptExecutionException)
            {
                throw;
            }
            catch (Exception e)
            {
                if (e.Message.Contains("Invalid group name:")) throw new ScriptExecutionException(new TextErrorModel($"Invalid Regex group name encountered: {e.Message.Substring(e.Message.IndexOf("Invalid group name:") + 20).ToLower()}"));
                throw new ScriptExecutionException(new GeneralScriptExecutionFailureModel(opName));
            }
        }

        //TDDO This should be fixed
        public Op.Name GetOperatorName() => Op.Parse((_funcName as VariableExpression)?.GetVariableName() ?? "");


        public List<ScriptExpression> GetFuncParams() => _parameters;

        public override FieldControllerBase CreateReference(Scope scope)
        {
            var func = _funcName.CreateReference(scope);
            if (func is OperatorController op)
            {
                //TODO
                return OperatorScript.CreateDocumentForOperator(_parameters.Select(p => p.CreateReference(scope)), op);
            }
            else if(_funcName is VariableExpression variable)
            {
                op = OperatorScript.GetOperatorWithName(Op.Parse(variable.GetVariableName()));
                return OperatorScript.CreateDocumentForOperator(_parameters.Select(p => p.CreateReference(scope)), op);
            }

            return null;
        }

        public override DashShared.TypeInfo Type => OperatorScript.GetOutputType(Op.Parse((_funcName as VariableExpression)?.GetVariableName() ?? ""));

        public override string ToString()
        {
            var concat = "";
            foreach (var param in _parameters)
            {
                switch (param)
                {
                    case VariableExpression varExp:
                        concat += varExp.GetVariableName() + " ";
                        break;
                    case LiteralExpression litExp:
                        concat += litExp.GetField() + " ";
                        break;
                }
            }

            return concat;
        }
    }
}

