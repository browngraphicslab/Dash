using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dash
{
    class TextErrorModel : ScriptExecutionErrorModel
    {
        public TextErrorModel(string text)
        {
            Error = text;
        }

        public string Error { get; }

        public override string GetHelpfulString()
        {
            return Error;
        }
    }
}
