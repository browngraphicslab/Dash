namespace Dash
{

    public class InvalidStringScriptErrorModel : ScriptErrorModel
    {
        public InvalidStringScriptErrorModel(string attemptedString)
        {
            AttemptedString = attemptedString;
        }

        public string AttemptedString { get; }


        public override string GetHelpfulString()
        {
            return
                $"A string literal or string return value was invalidly formatted.  Attempted String: {AttemptedString}";
        }
    }
}
