// ReSharper disable once CheckNamespace

using System.Linq;

// ReSharper disable once CheckNamespace
namespace Dash
{
    public class ScriptHelpExcerpt
    {
        private readonly Op.Name _functionName;
        private readonly string _description;

        public ScriptHelpExcerpt(Op.Name funcName)
        {
            _functionName = funcName;
            _description = Op.FuncDescriptions.TryGetValue(funcName, out var desc) ? desc : "";
        }

        public TextController GetExcerpt()
        {
            var countParamConfigs = OperatorScript.GetOverloadsFor(_functionName).ToList().Count;
            var suffix = countParamConfigs == 1 ? "" : "S";
            return new TextController(
                $"\n      NAME:\n            {_functionName}()\n      INFO:\n            {_description}\n      {countParamConfigs} INPUT CONFIG{suffix}:{OperatorScript.GetStringFormattedTypeListsFor(_functionName)}\n");
        }
    }
}
