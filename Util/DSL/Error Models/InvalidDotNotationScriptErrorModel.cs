using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dash
{
    public class InvalidDotNotationScriptErrorModel : ScriptErrorModel
    {
        public string AttemptedDotNotation { get; private set; }

        public InvalidDotNotationScriptErrorModel(string attmptedDotNotation)
        {
            AttemptedDotNotation = attmptedDotNotation;
        }

        public override string GetHelpfulString()
        {
            return "Invalid dot notation used, attempted usage: " + AttemptedDotNotation;
        }
    }
}
