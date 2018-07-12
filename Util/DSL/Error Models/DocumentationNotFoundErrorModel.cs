// ReSharper disable once CheckNamespace
namespace Dash
{
    public class DocumentationNotFoundErrorModel : ScriptExecutionErrorModel
    {
        private readonly Op.Name _functionName;

        public DocumentationNotFoundErrorModel(Op.Name functionName) => _functionName = functionName;

        public override string GetHelpfulString() =>
            $" Exception:\n            DocumentationNotFound\n      Feedback:\n            {_functionName}() is partially or completely missing associated documentation.\n";
    }
}