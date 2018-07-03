namespace Dash
{

    public class FunctionNotFoundScriptErrorModel : ScriptErrorModel
    {
        public FunctionNotFoundScriptErrorModel(string functionName)
        {
            FunctionName = functionName;
        }

        public string FunctionName { get; }

        public override string GetHelpfulString()
        {
            return $"An unknown function was called.  Function: {FunctionName}";
        }
    }
}
