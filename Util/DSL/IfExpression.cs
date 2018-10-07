using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Dash
{
    public class IfExpression : ScriptExpression
    {
        private readonly Op.Name _opName;
        private readonly Dictionary<KeyController, ScriptExpression> _parameters;

        public IfExpression(Op.Name opName, Dictionary<KeyController, ScriptExpression> parameters)
        {
            _opName = opName;
            _parameters = parameters;
        }

        public override async Task<FieldControllerBase> Execute(Scope scope)
        {
            //var inputs = new Dictionary<KeyController, FieldControllerBase>
            //{
            //    {IfOperatorController.BoolKey, _parameters[IfOperatorController.BoolKey].Execute(scope)}
            //};
            var boolRes = ((BoolController)await _parameters[IfOperatorController.BoolKey].Execute(scope)).Data;

            var ifKey = IfOperatorController.IfBlockKey;
            var elseKey = IfOperatorController.ElseBlockKey;

            if (boolRes)
            {
                //inputs.Add(ifKey, _parameters[ifKey].Execute(scope));
                //inputs.Add(elseKey, null);
                return await _parameters[ifKey].Execute(scope);
            }
            else
            {
                //inputs.Add(ifKey, null);
                //inputs.Add(elseKey, _parameters[elseKey].Execute(scope));
                return _parameters[elseKey] != null ? await _parameters[elseKey].Execute(scope) : new TextController("");
            }

            try
            {
                //TODO
                //var output = OperatorScript.Run(_opName, inputs, scope);
                //return output;
            }
            catch (Exception e)
            {
                throw new ScriptExecutionException(new GeneralScriptExecutionFailureModel(_opName));
            }
        }

        public Op.Name GetOperatorName()
        {
            return _opName;
        }


        public Dictionary<KeyController, ScriptExpression> GetFuncParams()
        {
            return _parameters;
        }


        public override FieldControllerBase CreateReference(Scope scope)
        {
           throw new NotImplementedException();
        }

        public override DashShared.TypeInfo Type => OperatorScript.GetOutputType(_opName);
    }
}
