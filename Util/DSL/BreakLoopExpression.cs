using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dash
{
    class BreakLoopExpression : ScriptExpression
    {
        private Dictionary<KeyController, ScriptExpression> _parameters;
        private string _opName;

        public BreakLoopExpression(Dictionary<KeyController, ScriptExpression> parameters = null)
        {
            this._parameters = parameters;
        }

        public override FieldControllerBase Execute(Scope scope)
        {
            return null;
            //if (_parameters == null) { return null; }
            //var inputs = new List<FieldControllerBase>();
            //foreach (var parameter in _parameters)
            //{
            //    inputs.Add(parameter.Value?.Execute(scope));
            //}

            //try
            //{
            //    var output = OperatorScript.Run(_opName, inputs, scope);
            //    return output;
            //}
            //catch (Exception e)
            //{
            //    throw new ScriptExecutionException(new GeneralScriptExecutionFailureModel(_opName));
            //}
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

