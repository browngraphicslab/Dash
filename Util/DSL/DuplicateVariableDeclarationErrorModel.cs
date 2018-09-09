// ReSharper disable once CheckNamespace

using DashShared;

namespace Dash
{
    internal class DuplicateVariableDeclarationErrorModel : ScriptExecutionErrorModel
    {
        private DocumentController _errorDoc;
        private readonly string _variableName;
        private readonly FieldControllerBase _value;

        public DuplicateVariableDeclarationErrorModel(string variableName, FieldControllerBase value)
        {
            _variableName = variableName;
            _value = value;
        }

        public override string GetHelpfulString() => "DuplicateVariableDeclarationException";

        public override DocumentController BuildErrorDoc()
        {
            _errorDoc = new DocumentController();

            const string title = "DuplicateVariableDeclarationException";

            _errorDoc.DocumentType = DashConstants.TypeStore.ErrorType;
            _errorDoc.SetField<TextController>(KeyStore.TitleKey, title, true);
            _errorDoc.SetField<TextController>(KeyStore.ExceptionKey, Exception(), true);
            _errorDoc.SetField<TextController>(KeyStore.FeedbackKey, Feedback(), true);

            return _errorDoc;
        }

        private string Exception() => $"Variable {_variableName} already exists with value {_value}.";

        private static string Feedback() => "Update current assignment rather than re-declaring.";
    }
}