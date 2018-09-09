using System;
using DashShared;

namespace Dash
{

    public abstract class ScriptExecutionErrorModel : ScriptErrorModel
    {
        public Exception InnerException { get; set; }

        public virtual DocumentController GetErrorDoc() => BuildErrorDoc();

        public abstract DocumentController BuildErrorDoc();
    }

}
