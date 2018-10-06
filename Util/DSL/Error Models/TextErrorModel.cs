using DashShared;

namespace Dash
{
    public class TextErrorModel : ScriptExecutionErrorModel
    {
        private DocumentController _errorDoc;

        public TextErrorModel(string text) => Error = text;

        public string Error { get; }

        public override string GetHelpfulString() => Error;

        public override DocumentController BuildErrorDoc()
        {
            _errorDoc = new DocumentController();

            string title = Error;

            _errorDoc.DocumentType = DashConstants.TypeStore.ErrorType;
            _errorDoc.SetField<TextController>(KeyStore.TitleKey, title, true);
            _errorDoc.SetField<TextController>(KeyStore.ExceptionKey, Error, true);

            return _errorDoc;
        }
    }
}
