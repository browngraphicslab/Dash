// ReSharper disable once CheckNamespace
namespace Dash
{
    public class AbsentStringScriptErrorModel : ScriptExecutionErrorModel
    {
        private readonly string _targetText;
        private readonly string _toRemove;

        public AbsentStringScriptErrorModel(string targetText, string toRemove)
        {
            _targetText = targetText;
            _toRemove = toRemove;
        }

        public override string GetHelpfulString() => $"\'{_targetText}\' does not contain any of the specified characters: {_toRemove.ToCharArray()}";
    }
}