using System;
using System.Collections.Generic;
using System.Linq;

namespace Dash
{
    public class BreakLoopExpression : ScriptExpression
    {
        private readonly Dictionary<KeyController, ScriptExpression> _parameters;
        private Op.Name _opName = Op.Name.invalid;

        public BreakLoopExpression(Dictionary<KeyController, ScriptExpression> parameters = null)
        {
            _parameters = parameters;
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

        public Op.Name GetOperatorName() => _opName;


        public Dictionary<KeyController, ScriptExpression> GetFuncParams() => _parameters;


        public override FieldControllerBase CreateReference(Scope scope)
        {
            throw new NotImplementedException();
        }

        public override DashShared.TypeInfo Type => OperatorScript.GetOutputType(_opName);
    }
}

