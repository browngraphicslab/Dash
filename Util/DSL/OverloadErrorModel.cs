using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dash
{
    public class OverloadErrorModel : ScriptExecutionErrorModel
    {
        private readonly bool _ambiguous;
        private readonly string _functionName;

        public OverloadErrorModel(bool ambiguous, string functionName)
        {
            _ambiguous = ambiguous;
            _functionName = functionName;
        }

        public override string GetHelpfulString()
        {
            return _ambiguous ? $"Ambiguous call to function {_functionName}. Multiple valid overloads exist" : $"No valid overloads exist for function {_functionName}";
        }
    }
}
