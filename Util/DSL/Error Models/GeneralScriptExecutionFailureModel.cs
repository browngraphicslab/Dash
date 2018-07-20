using DashShared;

namespace Dash
{
    public class GeneralScriptExecutionFailureModel : ScriptExecutionErrorModel
    {
        private DocumentController _errorDoc;
        public Op.Name FunctionName { get; }

        public GeneralScriptExecutionFailureModel(Op.Name functionName) => FunctionName = functionName;

        public override string GetHelpfulString() => "GeneralUnhandledException";

        public override DocumentController BuildErrorDoc()
        {
            _errorDoc = new DocumentController();

            const string title = "GeneralUnhandledException";

            _errorDoc.DocumentType = DashConstants.TypeStore.ErrorType;
            _errorDoc.SetField<TextController>(KeyStore.TitleKey, title, true);
            _errorDoc.SetField<TextController>(KeyStore.ExceptionKey, Exception(), true);
            _errorDoc.SetField<TextController>(KeyStore.FeedbackKey, Feedback(), true);

            return _errorDoc;
        }

        private string Exception() => $"The call to {FunctionName}() invoked an unknown execution error.";

        private static string Feedback() => "Check for misspellings, improper input types or consider using another overload.";
    }
}
