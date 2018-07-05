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
            //now return val
            scope.SetReturn(val);

            return val;

            //throw new ReturnException();
        }

        public override FieldControllerBase CreateReference(Scope scope)
        {
            throw new NotImplementedException();
        }

        public override DashShared.TypeInfo Type
        {
            get { return TypeInfo.Any; }
        }
    }
}

