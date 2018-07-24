// ReSharper disable once CheckNamespace
namespace Dash
{
    public class VariableSuggestion : ReplPopupSuggestion
    {
        public VariableSuggestion(string variableName) : base(variableName)
        {
        }

        public override string FormattedText() => Name;
    }
}