namespace Dash
{

    public class GeneralScriptExecutionFailureModel : ScriptExecutionErrorModel
    {
        public Op.Name FunctionName { get; }

        public GeneralScriptExecutionFailureModel(Op.Name functionName)
        {
            FunctionName = functionName;
        }

        public override string GetHelpfulString()
        {
            return "Unknown execution error occurred.  Function called: " + FunctionName;
        }

        public override DocumentController GetErrorDoc() => new DocumentController();
    }
}
