using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dash
{
    class ReturnException : DSLException
    {
        public ReturnException()
        {
        }
        public ScriptErrorModel Error { get; }
        public override string GetHelpfulString()
        {
            return "Return called";
        }
    }
}
