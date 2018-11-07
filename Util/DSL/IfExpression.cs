using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Dash
{
    public class IfExpression : ScriptExpression
    {
        private readonly ScriptExpression _condition, _trueExpression, _falseExpression;
        public IfExpression(ScriptExpression conditionExpression, ScriptExpression trueExpression, ScriptExpression falseExpression)
        {
            _condition = conditionExpression;
            _trueExpression = trueExpression;
            _falseExpression = falseExpression;
        }

        public override async Task<(FieldControllerBase, ControlFlowFlag)> Execute(Scope scope)
        {
            var boolRes = ((BoolController)(await _condition.Execute(scope)).Item1).Data;

            if (boolRes)
            {
                var (field, flags) = await _trueExpression.Execute(scope);
                if (flags == ControlFlowFlag.None)
                {
                    return (null, ControlFlowFlag.None);
                }
                else
                {
                    return (field, flags);
                }
            }
            else
            {
                if (_falseExpression != null)
                {
                    var (field, flags) = await _falseExpression.Execute(scope);
                    if (flags != ControlFlowFlag.None)
                    {
                        return (field, flags);
                    }
                }

                return (null, ControlFlowFlag.None);
            }
        }

        public Op.Name GetOperatorName()
        {
            return Op.Name.invalid;
        }

        public override FieldControllerBase CreateReference(Scope scope)
        {
           throw new NotImplementedException();
        }

        public override DashShared.TypeInfo Type => OperatorScript.GetOutputType(Op.Name.invalid);
    }
}
