namespace Dash
{

    public class ParameterProvidedMultipleTimesScriptErrorModel : ScriptErrorModel
    {
        public ParameterProvidedMultipleTimesScriptErrorModel(string functionName, string parameterName)
        {
            ParameterName = parameterName;
            FunctionName = functionName;
        }
        public string ParameterName { get; }
        public string FunctionName { get; }

        public override string GetHelpfulString()
        {
            return $"A parameter was passed multiple times into the same function.  Function: {FunctionName}   Parameter: {ParameterName}";
        }
    }
}
