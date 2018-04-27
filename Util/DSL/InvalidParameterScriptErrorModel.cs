namespace Dash
{

    public class InvalidParameterScriptErrorModel : ScriptErrorModel
    {
        public InvalidParameterScriptErrorModel(string parameterName)
        {
            ParameterName = parameterName;
        }

        public string ParameterName { get; }

        public override string GetHelpfulString()
        {
            return
                $"A function's parameter was invalid and not part of the function invoked.  Parameter: {ParameterName}";
        }
    }
}
