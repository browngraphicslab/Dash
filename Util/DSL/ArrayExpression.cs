﻿using System.Collections.Generic;
using System.Linq;
using DashShared;

namespace Dash
{
    internal class ArrayExpression : ScriptExpression
    {
        private readonly List<ScriptExpression> list;

        public ArrayExpression(List<ScriptExpression> list) => this.list = list;

        public override TypeInfo Type => list.Last().Type;

        public override FieldControllerBase CreateReference(Scope scope)
        {
            throw new System.NotImplementedException();
        }

        public override FieldControllerBase Execute(Scope scope)
        {
            return new ListController<FieldControllerBase>(list.Select(se => se.Execute(scope)));
        }
    }
}