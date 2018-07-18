// ReSharper disable once CheckNamespace

using DashShared;

namespace Dash
{
    public class InvalidReturnStatementErrorModel : ScriptExecutionErrorModel
    {
        private DocumentController _errorDoc;

        public override string GetHelpfulString() => "InvalidReturnException";

        public override DocumentController BuildErrorDoc()
        {
            _errorDoc = new DocumentController();

            const string title = "InvalidReturnException";

            _errorDoc.DocumentType = DashConstants.TypeStore.ErrorType;
            _errorDoc.SetField<TextController>(KeyStore.TitleKey, title, true);
            _errorDoc.SetField<TextController>(KeyStore.ExceptionKey, Exception(), true);

            return _errorDoc;
        }

        private static string Exception() => "For a general, unknown reason, unable to process the body of the return statement. Double check proper spelling, input types and function calls.";
    }
}