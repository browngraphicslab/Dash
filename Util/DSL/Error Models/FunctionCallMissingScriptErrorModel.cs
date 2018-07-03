﻿// ReSharper disable once CheckNamespace
namespace Dash
{
    public class FunctionCallMissingScriptErrorModel : ScriptExecutionErrorModel
    {
        public FunctionCallMissingScriptErrorModel(string attemptedFunction) => AttemptedFunction = attemptedFunction;

        public string AttemptedFunction { get; }

        public override string GetHelpfulString()
        {
            return $" Exception:\n            InvalidFunctionCall\n      Feedback:\n            {AttemptedFunction}() is not currently implemented. Type <help> for a complete catalog of valid functions.";
        }
    }

}