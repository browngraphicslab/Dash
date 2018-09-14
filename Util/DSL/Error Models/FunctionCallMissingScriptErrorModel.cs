// ReSharper disable once CheckNamespace

using DashShared;

namespace Dash
{
    public class FunctionCallMissingScriptErrorModel : ScriptExecutionErrorModel
    {
        private DocumentController _errorDoc;

        public FunctionCallMissingScriptErrorModel(string attemptedFunction) => AttemptedFunction = attemptedFunction;

        public string AttemptedFunction { get; }

        public override string GetHelpfulString() => "InvalidFunctionCallException";

        public override DocumentController BuildErrorDoc()
        {
            _errorDoc = new DocumentController();

            const string title = "InvalidFunctionCallException";

            _errorDoc.DocumentType = DashConstants.TypeStore.ErrorType;
            _errorDoc.SetField<TextController>(KeyStore.TitleKey, title, true);
            _errorDoc.SetField<TextController>(KeyStore.ExceptionKey, Exception(), true);
            _errorDoc.SetField<TextController>(KeyStore.FeedbackKey, Feedback(), true);

            return _errorDoc;
        }

        private string Exception() => $"{AttemptedFunction}() is not currently implemented. Enter <help()> for a complete catalog of valid functions.";

        private string Feedback() => $"Check for misspellings or consider independently defining {AttemptedFunction}().";
    }

}