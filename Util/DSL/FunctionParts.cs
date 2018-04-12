using System.Collections.Generic;
using System.Linq;

namespace Dash
{

    public class FunctionParts
    {
        public FunctionParts()
        {
        }

        public FunctionParts(string functionName, Dictionary<string, string> parameters)
        {
            FunctionName = functionName;
            FunctionParameters = parameters;
        }

        public string FunctionName { get; set; }
        public Dictionary<string, string> FunctionParameters { get; set; }

        public override bool Equals(object obj)
        {
            var parts = obj as FunctionParts;
            if (parts == null)
            {
                return false;
            }
            return parts.FunctionName == FunctionName &&
                   FunctionParameters.All(i => parts.FunctionParameters.ContainsKey(i.Key) &&
                                               parts.FunctionParameters[i.Key] == i.Value) &&
                   parts.FunctionParameters.Count == FunctionParameters.Count;
        }
    }
}
