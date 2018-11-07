using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Zu.TypeScript.TsTypes;

namespace Dash
{
    public class ExpressionChain : ScriptExpression
    {
        private IEnumerable<ScriptExpression> _expressions;
        private bool _newScope;

        public ExpressionChain(IEnumerable<ScriptExpression> expressions, bool newScope = true)
        {
            _expressions = expressions;
            _newScope = newScope;
        }

        public override async Task<(FieldControllerBase, ControlFlowFlag)> Execute(Scope scope)
        {
            var newScope = _newScope ? new Scope(scope) : scope;

            var exps = _expressions.ToArray();
            var length = exps.Count();
            FieldControllerBase retVal = null;
            for (var i = 0; i < length; i++)
            {
                var (ret, flags) = await exps[i].Execute(newScope);
                if (flags != ControlFlowFlag.None)
                {
                    return (ret, flags);
                }
                retVal = ret;
            }
            return (retVal, ControlFlowFlag.None);
        }

        public override FieldControllerBase CreateReference(Scope scope)
        {
            throw new NotImplementedException();
            //TODO tfs help with operator/doc stuff
        }

        public override DashShared.TypeInfo Type => _expressions.Last().Type;
    }
}
