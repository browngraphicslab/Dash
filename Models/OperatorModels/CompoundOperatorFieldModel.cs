using System.Collections.Generic;
using DashShared;
using DashShared.Models;

namespace Dash
{
    public class CompoundOperatorFieldModel : OperatorFieldModel
    {
        public Dictionary<string, List<string>> InputFieldReferences = new Dictionary<string, List<string>>();
        public Dictionary<string, string> OutputFieldReferences = new Dictionary<string, string>();

        public Dictionary<string, IOInfo> Inputs = new Dictionary<string, IOInfo>();
        public Dictionary<string, TypeInfo> Outputs = new Dictionary<string, TypeInfo>();

        public CompoundOperatorFieldModel() : base(OperatorType.Compound)
        {
            IsCompound = true;
        }
    }
}
