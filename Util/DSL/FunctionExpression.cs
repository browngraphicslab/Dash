using System;
using System.Collections.Generic;
using System.Linq;

namespace Dash
{
    public class FunctionExpression : ScriptExpression
    {
        private readonly List<ScriptExpression> _parameters;
        private readonly string _funcName;

        public FunctionExpression(List<ScriptExpression> parameters, string func)
        {
            _funcName = func;
            _parameters = parameters;
        }

        public FunctionExpression(Op.Name op, List<ScriptExpression> parameters)
        {
            _funcName = op.ToString();
            _parameters = parameters;
        }

        public override FieldControllerBase Execute(Scope scope)
        {
            //TODO ScriptLang - Don't take _funcName, take a script expression that evaluated to a FuncitonOperatorController
            var userFunction = scope.GetVariable(_funcName) as FunctionOperatorController;         
            var opName = Op.Parse(_funcName);
            var inputs = _parameters.Select(v => v?.Execute(scope)).ToList();

            try
            {
                //use user defined function
                if (userFunction != null)
                {
                    //functions shouldn't have acess to any variables outside function
                    scope = new ReturnScope();

                    //check if user defiend function
                    var output = OperatorScript.Run(userFunction, inputs, scope);
                    return output;
                }

                if (opName != Op.Name.invalid)
                {
                    scope = new ReturnScope(scope);

                    var output = OperatorScript.Run(opName, inputs, scope);
                    return output;
                }
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

        public Op.Name GetOperatorName() => Op.Parse(_funcName);


        public List<ScriptExpression> GetFuncParams() => _parameters;

        public override FieldControllerBase CreateReference(Scope scope)
        {
            //TODO
            return OperatorScript.CreateDocumentForOperator(_parameters.Select(p => p.CreateReference(scope)),
                Op.Parse(_funcName)); //recursive linq
        }

        public override DashShared.TypeInfo Type => OperatorScript.GetOutputType(Op.Parse(_funcName));

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

