// ReSharper disable once CheckNamespace
namespace Dash
{
    public abstract class ReplPopupSuggestion
    {
        public abstract string FormattedText();

        public string Name { get; }

        protected ReplPopupSuggestion(string name)
        {
            Name = name;
        }
    }
}