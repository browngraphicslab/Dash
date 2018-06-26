using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dash
{
    public class ExpressionChain : ScriptExpression
    {
        private IEnumerable<ScriptExpression> _expressions;

        public ExpressionChain(IEnumerable<ScriptExpression> expressions)
        {
            _expressions = expressions;
        }

        public override FieldControllerBase Execute(Scope scope)
        {
            var newScope = new Scope(scope);

            var exps = _expressions.ToArray();
            var length = exps.Count();
            FieldControllerBase retVal = null;
            for(var i = 0; i < length; i++)
            {
                var ret = exps[i].Execute(newScope);
                if (ret != null)
                {
                    retVal = ret;
                } else
                {
                    break;
                }
                if (exps[i] is BreakLoopExpression)
                {
                    break;
                }
                

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
