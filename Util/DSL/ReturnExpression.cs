using DashShared;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dash
{
    public class ReturnExpression : ScriptExpression
    {
        private ScriptExpression _value;

        public ReturnExpression(ScriptExpression value)
        {
            _value = value;
        }

        public override FieldControllerBase Execute(Scope scope)
        {
            var val = _value.Execute(scope);
            scope.GetFirstAncestor().SetReturn(val);
            throw new ReturnException();
            //return val;
        }

        public override FieldControllerBase CreateReference(Scope scope)
        {
            throw new NotImplementedException();
            //TODO tfs help with operator/doc stuff
        }

        public override DashShared.TypeInfo Type
        {
            get { return TypeInfo.Any; }
        } //TODO tyler is this correct?
    }
}

