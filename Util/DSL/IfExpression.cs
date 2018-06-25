using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dash
{
    class IfExpression : ScriptExpression
    {
        private string _opName;
        private Dictionary<KeyController, ScriptExpression> _parameters;

        public IfExpression(string opName, Dictionary<KeyController, ScriptExpression> parameters)
        {
            this._opName = opName;
            this._parameters = parameters;
        }

        public override FieldControllerBase Execute(Scope scope)
        {
            var inputs = new Dictionary<KeyController, FieldControllerBase>();
            inputs.Add(IfOperatorController.BoolKey, _parameters[IfOperatorController.BoolKey].Execute(scope));
            bool boolRes = ((BoolController)_parameters[IfOperatorController.BoolKey].Execute(scope)).Data;

            var ifKey = IfOperatorController.IfBlockKey;
            var elseKey = IfOperatorController.ElseBlockKey;

            if (boolRes)
            {
                inputs.Add(ifKey, _parameters[ifKey].Execute(scope));
                inputs.Add(elseKey, null);
            }
            else
            {
                inputs.Add(ifKey, null);
                inputs.Add(elseKey, _parameters[elseKey].Execute(scope));
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
