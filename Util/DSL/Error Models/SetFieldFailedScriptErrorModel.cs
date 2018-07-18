// ReSharper disable once CheckNamespace

using DashShared;

namespace Dash
{
    public class SetFieldFailedScriptErrorModel : ScriptExecutionErrorModel
    {
        private DocumentController _errorDoc;
        private readonly string _key;
        private readonly string _value;

        public SetFieldFailedScriptErrorModel(string key, string value)
        {
            _key = key;
            _value = value;
        }

        public override string GetHelpfulString() => "SetFieldFailureException";

        public override DocumentController BuildErrorDoc()
        {
            _errorDoc = new DocumentController();

            const string title = "SetFieldFailureException";

            _errorDoc.DocumentType = DashConstants.TypeStore.ErrorType;
            _errorDoc.SetField<TextController>(KeyStore.TitleKey, title, true);
            _errorDoc.SetField<TextController>(KeyStore.ExceptionKey, Exception(), true);
            _errorDoc.SetField<TextController>(KeyStore.FeedbackKey, Feedback(), true);

            return _errorDoc;
        }

        private string Exception() => $"{_key} field could not be set to {_value}";

        private static string Feedback() => "Keys are case sensitive, so ensure proper casing in reference";

    }
}