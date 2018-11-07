using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DashShared;

namespace Dash
{
    public class TernaryExpression : ScriptExpression
    {
        private readonly ScriptExpression _condition, _trueResult, _falseResult;
        public TernaryExpression(ScriptExpression condition, ScriptExpression trueResult,
            ScriptExpression falseResult)
        {
            _condition = condition;
            _trueResult = trueResult;
            _falseResult = falseResult;
        }
        public override async Task<(FieldControllerBase, ControlFlowFlag)> Execute(Scope scope)
        {
            var boolRes = ((BoolController)(await _condition.Execute(scope)).Item1).Data;
            var exp = boolRes ? _trueResult : _falseResult;
            return await exp.Execute(scope);
        }

        public override FieldControllerBase CreateReference(Scope scope)
        {
            throw new NotImplementedException();
        }

        public override TypeInfo Type => TypeInfo.Any;
    }
}
