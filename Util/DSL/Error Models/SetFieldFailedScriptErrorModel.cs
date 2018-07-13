// ReSharper disable once CheckNamespace
namespace Dash
{
    public class SetFieldFailedScriptErrorModel : ScriptExecutionErrorModel
    {
        private readonly string _key;
        private readonly string _value;

        public SetFieldFailedScriptErrorModel(string key, string value)
        {
            _key = key;
            _value = value;
        }

        public override string GetHelpfulString() => $" Exception:\n            SetDocFieldFailed\n      Feedback:\n            {_key} field could not be set to {_value}";

    }
}