namespace Dash
{
    public class FunctionCallMissingScriptErrorModel : ScriptErrorModel
    {
        public FunctionCallMissingScriptErrorModel(string attemptedFunction)
        {
            AttemptedFunction = attemptedFunction;
        }
        public string AttemptedFunction { get; }

        public override string GetHelpfulString()
        {
            return $"A function was given but not called.  Attemped Function: {AttemptedFunction}";
        }
    }

}
