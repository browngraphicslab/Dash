using System;
using DashShared;

namespace Dash
{
    public class GeneralScriptExecutionFailureModel : ScriptExecutionErrorModel
    {
        private DocumentController _errorDoc;
        public Op.Name? FunctionName { get; }
        public string ErrorString { get; }

        public GeneralScriptExecutionFailureModel(Exception e, Op.Name? functionName = null)
        {
            FunctionName = functionName;
            ErrorString = e.Message;
        }

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

        private string Exception()
        {
            return FunctionName == null ? $"Unknown execution error occured: {ErrorString}" : $"The call to {FunctionName}() invoked an unknown execution error: {ErrorString}";
        }

        private static string Feedback() => "Check for misspellings, improper input types or consider using another overload.";
    }
}
