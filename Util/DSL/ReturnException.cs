namespace Dash
{
    class ReturnException : DSLException
    {
        public ReturnException()
        {
        }
        public ScriptErrorModel Error { get; }
        public override string GetHelpfulString()
        {
            return "Return called";
        }
    }
}
