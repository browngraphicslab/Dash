// ReSharper disable once CheckNamespace
namespace Dash
{
    internal class DuplicateVariableDeclarationErrorModel : ScriptExecutionErrorModel
    {
        private readonly string _variableName;
        private readonly FieldControllerBase _value;

        public DuplicateVariableDeclarationErrorModel(string variableName, FieldControllerBase value)
        {
            _variableName = variableName;
            _value = value;
        }

        public override string GetHelpfulString()
        {
            return $" Exception:\n            AttemptedDuplicateVariableDeclaration\n      Feedback:\n            Variable {_variableName} already exists with value {_value}. Update current assignment rather than re-declaring.";
        }
    }
}