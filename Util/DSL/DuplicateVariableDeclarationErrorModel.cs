using DashShared;

// ReSharper disable once CheckNamespace
namespace Dash
{
    internal class DuplicateVariableDeclarationErrorModel : ScriptExecutionErrorModel
    {
        private DocumentController _errorDoc;
        private readonly string _variableName;
        private readonly FieldControllerBase _existingValue;
        private readonly FieldControllerBase _targetValue;
        private readonly bool _alreadyEqual;

        public DuplicateVariableDeclarationErrorModel(string variableName, FieldControllerBase targetValue, FieldControllerBase existingValue, bool alreadyEqual)
        {
            _variableName = variableName;
            _existingValue = existingValue;
            _targetValue = targetValue;
            _alreadyEqual = alreadyEqual;
        }

        public override string GetHelpfulString() => "DuplicateVariableDeclarationException";

        public override DocumentController BuildErrorDoc()
        {
            _errorDoc = new DocumentController();

            const string title = "DuplicateVariableDeclarationException";

            _errorDoc.DocumentType = DashConstants.TypeStore.ErrorType;
            _errorDoc.SetField<TextController>(KeyStore.TitleKey, title, true);
            _errorDoc.SetField<TextController>(KeyStore.ExceptionKey, Exception(), true);
            _errorDoc.SetField<TextController>(KeyStore.ActionKey, Action(), true);

            return _errorDoc;
        }

        private string Exception() => $"Variable \'{_variableName}\' already exists with value {_existingValue}.";

        private string Action() => _alreadyEqual ? "No action necessary - redundant reassignment" : $"Hit <enter> to reassign \'{_variableName}\' to \'{_targetValue}\'\n           Hit <escape> to preserve old value";
    }
}
