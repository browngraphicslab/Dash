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

        public override FieldControllerBase Execute(ScriptState state)
        {
            var exps = _expressions.ToArray();
            var length = exps.Count();
            for(int i = 0; i < length; i++)
            {
                exps[i].Execute(state);
            }
            return exps[exps.Length - 1].Execute(state);
        }

        public override FieldControllerBase CreateReference(ScriptState state)
        {
            throw new NotImplementedException();
            //TODO tfs help with operator/doc stuff
        }

        public override DashShared.TypeInfo Type
        {
            get { return _expressions.Last().Type; }
        } 
    }
}
