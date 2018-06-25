﻿using System;
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
            for(var i = 0; i < length; i++)
            {
                exps[i].Execute(newScope);
            }
            return exps[exps.Length - 1].Execute(newScope);
        }

        public override FieldControllerBase CreateReference(Scope scope)
        {
            throw new NotImplementedException();
            //TODO tfs help with operator/doc stuff
        }

        public override DashShared.TypeInfo Type => _expressions.Last().Type;
    }
}
