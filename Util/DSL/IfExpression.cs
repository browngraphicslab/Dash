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

        public override async Task<(FieldControllerBase, ControlFlowFlag)> Execute(Scope scope)
        {
            var boolRes = ((BoolController)(await _parameters[IfOperatorController.BoolKey].Execute(scope)).Item1).Data;

            var ifKey = IfOperatorController.IfBlockKey;
            var elseKey = IfOperatorController.ElseBlockKey;

            if (boolRes)
            {
                return await _parameters[ifKey].Execute(scope);
            }
            else
            {
                return _parameters[elseKey] != null ? await _parameters[elseKey].Execute(scope) : (null, ControlFlowFlag.None);
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
