namespace Dash
{

    public class ScriptException : DSLException
    {
        public ScriptException(ScriptErrorModel error)
        {
            Error = error;
        }
        public ScriptErrorModel Error { get; }
        public override string GetHelpfulString()
        {
            return Error.GetHelpfulString();
        }
    }
    
}
