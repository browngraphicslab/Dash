namespace Dash
{
    public class ScriptExecutionException : DSLException
    {
        public ScriptExecutionException(ScriptExecutionErrorModel error)
        {
            Error = error;
        }
        public ScriptExecutionErrorModel Error { get; }


        public override string GetHelpfulString()
        {
            return string.IsNullOrWhiteSpace(Error?.GetHelpfulString()) ? "Execution Error" : Error.GetHelpfulString();
        }
    }

}
