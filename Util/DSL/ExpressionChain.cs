using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

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

        public override async Task<FieldControllerBase> Execute(Scope scope)
        {
            var newScope = _newScope ? new Scope(scope) : scope;

            var exps = _expressions.ToArray();
            var length = exps.Length;
            FieldControllerBase retVal = null;
            for (var i = 0; i < length; i++)
            {
                var ret = await exps[i].Execute(newScope);
                if (ret == null) break;
                retVal = ret;        
                if (exps[i] is BreakLoopExpression) break;
            }
            return retVal;
        }

        public override FieldControllerBase CreateReference(Scope scope)
        {
            throw new NotImplementedException();
            //TODO tfs help with operator/doc stuff
        }

        public override DashShared.TypeInfo Type => _expressions.Last().Type;
    }
}
