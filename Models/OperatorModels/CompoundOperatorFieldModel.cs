using System.Collections.Generic;
using DashShared;

namespace Dash
{
    public class CompoundOperatorFieldModel : OperatorFieldModel
    {
        public Dictionary<KeyController, ReferenceFieldModel> InputFieldReferences = new Dictionary<KeyController, ReferenceFieldModel>();
        public Dictionary<KeyController, ReferenceFieldModel> OutputFieldReferences = new Dictionary<KeyController, ReferenceFieldModel>();

        public CompoundOperatorFieldModel(string type) : base(type)
        {
            IsCompound = true;
        }
    }
}
