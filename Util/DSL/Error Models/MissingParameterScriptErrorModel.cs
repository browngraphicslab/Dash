namespace Dash
{

    public class MissingParameterScriptErrorModel : ScriptErrorModel
    {
        public MissingParameterScriptErrorModel(string functionName, string missingParam)
        {
            FunctionName = functionName;
            MissingParameter = missingParam;
        }

        public string FunctionName { get; }
        public string MissingParameter { get; }

        public override string GetHelpfulString()
        {
            return
                $"A function call was missing a required parameter.  Function Name: {FunctionName}    Missing parameter: {MissingParameter}";
        }
    }
}
