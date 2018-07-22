// ReSharper disable once CheckNamespace

using DashShared;

namespace Dash
{
    public class AbsentStringScriptErrorModel : ScriptExecutionErrorModel
    {
        private DocumentController _errorDoc;
        private readonly string _targetText;
        private readonly string _formattedAbsentees;

        public AbsentStringScriptErrorModel(string targetText, string formattedAbsentees)
        {
            _targetText = targetText;
            _formattedAbsentees = formattedAbsentees;
        }

        public override string GetHelpfulString() => "AbsentStringException";        

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

        private string Exception() => $"\'{_targetText}\' does not contain any of the specified phrases: {_formattedAbsentees}";

        private static string Feedback() => "Search for characters or phrases that are present in target text.";
    }
}