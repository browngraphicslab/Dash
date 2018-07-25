using System;
using System.Collections.Generic;
using System.Linq;
using Zu.TypeScript.TsTypes;

namespace Dash
{
    public class FunctionExpression : ScriptExpression
    {
        private readonly List<ScriptExpression> _parameters;
        private readonly ScriptExpression _funcName;
        private readonly Op.Name _opName;

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

        public override FieldControllerBase Execute(Scope scope)
        {
            //TODO ScriptLang - Don't take _funcName, take a script expression that evaluated to a FuncitonOperatorController
            OperatorController op = null;
            var opName = Op.Name.invalid;
            try
            {
                op = _funcName.Execute(scope) as FunctionOperatorController;
            }
            catch (ScriptExecutionException)
            {
                if (!(_funcName is VariableExpression variable))
                {
                    throw;
                }

                var variableName = variable.GetVariableName();
                opName = Op.Parse(variableName);
                if (opName == Op.Name.invalid)
                {
                    throw;
                }
            }

            var inputs = _parameters.Select(v => v?.Execute(scope)).ToList();

            try
            {
                scope = new ReturnScope();

                var output = op != null ? OperatorScript.Run(op, inputs, scope) : OperatorScript.Run(opName, inputs, scope);
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
            catch (Exception)
            {
                throw new ScriptExecutionException(new GeneralScriptExecutionFailureModel(opName));
            }

            return new TextController("");
        }

        //TDDO This should be fixed
        public Op.Name GetOperatorName() => Op.Parse((_funcName as VariableExpression)?.GetVariableName() ?? "");


        public List<ScriptExpression> GetFuncParams() => _parameters;

        public override FieldControllerBase CreateReference(Scope scope)
        {
            //TODO
            return null;
            //return OperatorScript.CreateDocumentForOperator(
            //    _parameters.Select(
            //        kvp => new KeyValuePair<KeyController, FieldControllerBase>(kvp.CreateReference(scope))), _opName); //recursive linq
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

