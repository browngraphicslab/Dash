// ReSharper disable once CheckNamespace
namespace Dash
{
    public class InvalidReturnStatementErrorModel : ScriptExecutionErrorModel
    {
        public InvalidReturnStatementErrorModel()
        {
        }

        public override string GetHelpfulString() =>
            $" Exception:\n            InvalidReturnStatement\n      Feedback:\n            Unable to process the body of the return statement. Ensure proper syntax.\n";

        public override DocumentController GetErrorDoc() => new DocumentController();
    }
}