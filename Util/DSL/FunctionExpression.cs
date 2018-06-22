using System;
using System.Collections.Generic;
using System.Linq;

namespace Dash
{
    public class FunctionExpression : ScriptExpression
    {
        private string _opName;
        private Dictionary<KeyController, ScriptExpression> _parameters;

        public FunctionExpression(string opName, Dictionary<KeyController, ScriptExpression> parameters)
        {
            this._opName = opName;
            this._parameters = parameters;
        }

        public override FieldControllerBase Execute(Scope scope)
        {
            var inputs = new Dictionary<KeyController, FieldControllerBase>();
            foreach (var parameter in _parameters)
            {
                inputs.Add(parameter.Key, parameter.Value?.Execute(scope));
            }

            try
            {
                var output = OperatorScript.Run(_opName, inputs, scope);
                return output;
            }
            catch (Exception e)
            {
                throw new ScriptExecutionException(new GeneralScriptExecutionFailureModel(_opName));
            }
        }

        public string GetOperatorName()
        {
            return _opName;
        }


        public Dictionary<KeyController, ScriptExpression> GetFuncParams()
        {
            return _parameters;
        }


        public override FieldControllerBase CreateReference(Scope scope)
        {
            return OperatorScript.CreateDocumentForOperator(
                _parameters.Select(
                    kvp => new KeyValuePair<KeyController, FieldControllerBase>(kvp.Key,
                        kvp.Value.CreateReference(scope))), _opName); //recursive linq
        }

        public override DashShared.TypeInfo Type => OperatorScript.GetOutputType(_opName);
    }
}
