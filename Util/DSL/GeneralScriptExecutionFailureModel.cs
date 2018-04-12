namespace Dash
{

    public class GeneralScriptExecutionFailureModel : ScriptExecutionErrorModel
    {
        public GeneralScriptExecutionFailureModel(string functionName)
        {
            FunctionName = functionName;
        }

        public string FunctionName { get; private set; }

        public override string GetHelpfulString()
        {
            return "Unknown execution error occurred.  Function called: " + FunctionName;
        }
    }
}
