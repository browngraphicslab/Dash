namespace Dash
{
    public class TooManyParametersGivenScriptErrorModel : ScriptErrorModel
    {
        public TooManyParametersGivenScriptErrorModel(string functionName, string paramValue)
        {
            FunctionName = functionName;
            ParameterValue = paramValue;
        }
        public string FunctionName { get; }
        public string ParameterValue{ get; }

        public override string GetHelpfulString()
        {
            return $"Too many parameters were passed into a function.  Function Name: {FunctionName}    Last given parameter: {ParameterValue}";
        }
    }
    
}
