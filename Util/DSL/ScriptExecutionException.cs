namespace Dash
{
    public class ScriptExecutionException : DSLException
    {
        public ScriptExecutionException(ScriptExecutionErrorModel error)
        {
            Error = error;
        }
        public ScriptExecutionErrorModel Error { get; }
    }

}
