using DashShared;

namespace Dash
{
    public class VariableNotFoundExecutionErrorModel : ScriptExecutionErrorModel
    {
        private DocumentController _errorDoc;

        public VariableNotFoundExecutionErrorModel(string variableName) => VariableName = variableName;

        public string VariableName { get; }

        public override string GetHelpfulString() => "UndefinedVariableException";

        public override DocumentController BuildErrorDoc()
        {
            _errorDoc = new DocumentController();

            const string title = "UndefinedVariableException";

            _errorDoc.DocumentType = DashConstants.TypeStore.ErrorType;
            _errorDoc.SetField<TextController>(KeyStore.TitleKey, title, true);
            _errorDoc.SetField<TextController>(KeyStore.ExceptionKey, Exception(), true);
            _errorDoc.SetField<TextController>(KeyStore.FeedbackKey, Feedback(), true);

            return _errorDoc;
        }

        private string Exception() => $"<{VariableName}> is not currently defined.";

        private string Feedback() => $"Declare definition with <var {VariableName} = ____> syntax or convert to string.";
    }
}
