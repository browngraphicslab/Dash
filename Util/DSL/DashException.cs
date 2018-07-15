using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dash
{
    public class DashException
    {
        public DashException(OperatorController operatorControllerSource, string userMessage = null, DashException innerDashException = null, Exception innerException = null)
        {
            UserMessage = userMessage ?? UserMessage;
            InnerException = innerException;
            InnerDashException = innerDashException;
            SourceOperatorFunctionName = DSL.GetFuncName(operatorControllerSource);
        }

        public DashException(OperatorController operatorController, DashException innerDashException): this(operatorController, null, innerDashException){}
        public DashException(OperatorController operatorController, Exception innerException) : this(operatorController, null, null, innerException){}

        public Op.Name SourceOperatorFunctionName { get; private set; }
        public DashException InnerDashException { get; private set; }
        public Exception InnerException { get; private set; }
        public string UserMessage { get; private set; } = "An Exception Occurred";
    }
}
