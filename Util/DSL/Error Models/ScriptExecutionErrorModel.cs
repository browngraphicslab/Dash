using System;

namespace Dash
{

    public abstract class ScriptExecutionErrorModel : ScriptErrorModel
    {
        public Exception InnerException { get; set; }
    }

}
