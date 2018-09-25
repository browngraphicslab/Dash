namespace Dash
{

    public class InvalidDishScriptException : DSLException
    {
        public InvalidDishScriptException(string script, ScriptErrorModel scriptErrorModel,
            ScriptException innerScriptException = null)
        {
            Script = script;
            ScriptErrorModel = scriptErrorModel;
            InnerScriptException = innerScriptException;
        }

        public string Script { get; private set; }
        public ScriptException InnerScriptException { get; }
        public ScriptErrorModel ScriptErrorModel { get; private set; }
        public override string GetHelpfulString()
        {
            return ScriptErrorModel.GetHelpfulString();
        }
    }
}
