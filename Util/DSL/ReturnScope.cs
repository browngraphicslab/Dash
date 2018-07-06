using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dash
{
    public class ReturnScope : Scope
    {
        private FieldControllerBase _returnValue;

        public ReturnScope() { _dictionary = new Dictionary<string, FieldControllerBase>(); }

        public ReturnScope(Scope parentScope) { Parent = parentScope; _dictionary = new Dictionary<string, FieldControllerBase>(); }       

        public override void SetReturn(FieldControllerBase ret) { _returnValue = ret; }

        public override FieldControllerBase GetReturn => _returnValue;
    }
}
