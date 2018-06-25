using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dash
{
    class WhileExpression : ScriptExpression
    {
        private string _opName;
        private Dictionary<KeyController, ScriptExpression> _parameters;

        public WhileExpression(string opName, Dictionary<KeyController, ScriptExpression> parameters)
        {
            this._opName = opName;
            this._parameters = parameters;
        }

        public override FieldControllerBase Execute(Scope scope)
        {
            var inputs = new Dictionary<KeyController, FieldControllerBase>();
            inputs.Add(WhileOperatorController.BoolKey, _parameters[WhileOperatorController.BoolKey].Execute(scope));

            var BlockKey = WhileOperatorController.BlockKey;
            FieldControllerBase output = null ;

            while (true)
            {
                bool boolRes = ((BoolController)_parameters[WhileOperatorController.BoolKey].Execute(scope)).Data;
                if (boolRes)
                {
                    if (inputs.ContainsKey(BlockKey))
                    {
                        inputs[BlockKey] = _parameters[BlockKey].Execute(scope);
                    }
                    else
                    {
                        inputs.Add(BlockKey, _parameters[BlockKey].Execute(scope));
                    }
                    

                    try
                    {
                        output = OperatorScript.Run(_opName, inputs, scope);
                    }
                    catch (Exception e)
                    {
                        throw new ScriptExecutionException(new GeneralScriptExecutionFailureModel(_opName));
                    }
                }
                else
                {
                    if (!inputs.ContainsKey(BlockKey))
                    {
                        inputs.Add(BlockKey, null);
                    }
                   
                    break;
                }
            }

            return output;
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
