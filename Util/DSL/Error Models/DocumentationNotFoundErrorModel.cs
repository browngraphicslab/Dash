// ReSharper disable once CheckNamespace

using DashShared;

namespace Dash
{
    public class DocumentationNotFoundErrorModel : ScriptExecutionErrorModel
    {
        private readonly Op.Name _functionName;
        private DocumentController _errorDoc;

        public DocumentationNotFoundErrorModel(Op.Name functionName) => _functionName = functionName;

        public override string GetHelpfulString() => "DocumentationNotFoundException";

        public override DocumentController BuildErrorDoc()
        {
            _errorDoc = new DocumentController();

            const string title = "AbsentStringException";

            _errorDoc.DocumentType = DashConstants.TypeStore.ErrorType;
            _errorDoc.SetField<TextController>(KeyStore.TitleKey, title, true);
            _errorDoc.SetField<TextController>(KeyStore.ExceptionKey, Exception(), true);
            _errorDoc.SetField<TextController>(KeyStore.FeedbackKey, Feedback(), true);

            return _errorDoc;
        }

        private string Exception() => $"{_functionName}() is partially or completely missing associated documentation.";

        private static string Feedback() => $"This documentation will appear in future releases.";

    }
}