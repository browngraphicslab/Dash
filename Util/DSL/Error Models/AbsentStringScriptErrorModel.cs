// ReSharper disable once CheckNamespace
namespace Dash
{
    public class AbsentStringScriptErrorModel : ScriptExecutionErrorModel
    {
        private readonly string _targetText;
        private readonly string _formattedAbsentees;

        public AbsentStringScriptErrorModel(string targetText, string formattedAbsentees)
        {
            _targetText = targetText;
            _formattedAbsentees = formattedAbsentees;
        }

        public override string GetHelpfulString() => $" Exception:\n            AbsentPhrases\n      Feedback:\n            \'{_targetText}\' does not contain any of the specified phrases: {_formattedAbsentees}";
    }
}