using System.Collections.Generic;
using DashShared;

// ReSharper disable once CheckNamespace
namespace Dash
{
    public class FunctionOperatorModel : OperatorModel
    {
        public string FunctionCode { get; set; }

        public List<KeyValuePair<string, TypeInfo>> Parameters { get; set; }

        public TypeInfo ReturnType { get; set; }

        public FunctionOperatorModel(string functionCode, List<KeyValuePair<string, TypeInfo>> parameters, TypeInfo returnType, KeyModel type, string id = null) : base(type, id)
        {
            FunctionCode = functionCode;
            Parameters = parameters;
            ReturnType = returnType;
        }
    }
}
