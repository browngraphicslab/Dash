using System;
using System.Collections.Generic;
using System.Linq;

namespace Dash
{
    public class FunctionExpression : ScriptExpression
    {
        private string opName;
        private Dictionary<KeyController, ScriptExpression> parameters;

        public FunctionExpression(string opName, Dictionary<KeyController, ScriptExpression> parameters)
        {
            this.opName = opName;
            this.parameters = parameters;
        }

        public override FieldControllerBase Execute(ScriptState state)
        {
            var inputs = new Dictionary<KeyController, FieldControllerBase>();
            foreach (var parameter in parameters)
            {
                inputs.Add(parameter.Key, parameter.Value.Execute(state));
            }

            try
            {
                var output = OperatorScript.Run(opName, inputs, state);
                return output;
            }
            catch (Exception e)
            {
                throw new ScriptExecutionException(new GeneralScriptExecutionFailureModel(opName));
            }
        }

        public override FieldControllerBase CreateReference(ScriptState state)
        {
            return OperatorScript.CreateDocumentForOperator(
                parameters.Select(
                    kvp => new KeyValuePair<KeyController, FieldControllerBase>(kvp.Key,
                        kvp.Value.CreateReference(state))), opName); //recursive linq
        }

        public override DashShared.TypeInfo Type => OperatorScript.GetOutputType(opName);
    }
}
