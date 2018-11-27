using DashShared;

// ReSharper disable once CheckNamespace
namespace Dash
{
    public class IncompleteVariableDeclarationErrorModel : ScriptExecutionErrorModel
    {
        private DocumentController _errorDoc;
        private readonly string _variableName;

        public IncompleteVariableDeclarationErrorModel(string variableName) => _variableName = variableName;

        public override string GetHelpfulString() => "Attempted to declare a variable without a corresponding value";

        public override DocumentController BuildErrorDoc()
        {
            _errorDoc = new DocumentController();

            const string title = "IncompleteVariableDeclarationException";

            _errorDoc.DocumentType = DashConstants.TypeStore.ErrorType;
            _errorDoc.SetField<TextController>(KeyStore.TitleKey, title, true);
            _errorDoc.SetField<TextController>(KeyStore.ExceptionKey, Exception(), true);
            _errorDoc.SetField<TextController>(KeyStore.ActionKey, Action(), true);

            return _errorDoc;
        }

        private string Exception() => _variableName.Equals("\"\"") ? "Inadequate information to properly declare variable" : $"Must declare variable \'{_variableName}\' with a corresponding value.";

        private string Action() => _variableName.Equals("\"\"") ? "Provide both a variable name and a value in the declaration" : $"Input an initialization value for \'{_variableName}\' or hit <escape> to continue";
    }
}
