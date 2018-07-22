// ReSharper disable once CheckNamespace

using System.Collections.Generic;
using DashShared;

namespace Dash
{
    public class FunctionSuggestion : ReplPopupSuggestion
    {
        public FunctionSuggestion(string functionName) : base(functionName)
        {
        }

        public override string FormattedText()
        {
            Op.Name funcName = Op.Parse(Name);
            bool isOverloaded = OperatorScript.IsOverloaded(funcName);
            var inputTypes = OperatorScript.GetDefaultInputTypeListFor(funcName);
            int numInputs = inputTypes.Count;

            var functionEnding = new List<string>();
            for (var i = 0; i < numInputs; i++)
            {
                var symbol = "_";
                if (!isOverloaded)
                {
                    switch (inputTypes[i])
                    {
                        case TypeInfo.Text:
                            symbol = "\"\"";
                            break;
                        case TypeInfo.Number:
                            symbol = "#";
                            break;
                    }
                }
                functionEnding.Add($"{symbol}");
            }

            return $"{Name}({string.Join(", ", functionEnding)})";
        }
    }
}