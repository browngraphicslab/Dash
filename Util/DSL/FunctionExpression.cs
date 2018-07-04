using System;
using System.Collections.Generic;
using System.Linq;

namespace Dash
{
    public class FunctionExpression : ScriptExpression
    {
        private readonly Op.Name _opName;
        private readonly List<ScriptExpression> _parameters;

        public FunctionExpression(Op.Name opName, List<ScriptExpression> parameters)
        {
            _opName = opName;
            _parameters = parameters;
        }

        public override FieldControllerBase Execute(Scope scope)
        {
            var inputs = _parameters.Select(v => v?.Execute(scope)).ToList();

            try
            {
                var output = OperatorScript.Run(_opName, inputs, scope);
                return output;
            }
            catch (ScriptExecutionException)
            {
                throw;
            }
            catch (Exception)
            {
                throw new ScriptExecutionException(new GeneralScriptExecutionFailureModel(_opName));
            }
        }

        public Op.Name GetOperatorName() => _opName;


        public List<ScriptExpression> GetFuncParams() => _parameters;

        public override FieldControllerBase CreateReference(Scope scope)
        {
            //TODO
            return null;
            //return OperatorScript.CreateDocumentForOperator(
            //    _parameters.Select(
            //        kvp => new KeyValuePair<KeyController, FieldControllerBase>(kvp.CreateReference(scope))), _opName); //recursive linq
        }

        public override DashShared.TypeInfo Type => OperatorScript.GetOutputType(_opName);

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

