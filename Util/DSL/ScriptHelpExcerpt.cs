// ReSharper disable once CheckNamespace
namespace Dash
{
    public class ScriptHelpExcerpt
    {
        private readonly string _functionName;
        private readonly string _description;

        public ScriptHelpExcerpt(Op.Name funcName)
        {
            _functionName = funcName.ToString();
            _description = Op.FuncDescriptions[funcName];
        }

        public TextController GetExcerpt() => new TextController($"{_functionName}()\n{_description}");
    }
}