using System.Collections.Generic;
using DashShared;

namespace Dash
{
    public class CompoundOperatorFieldModel : OperatorFieldModel
    {
        public Dictionary<Key, ReferenceFieldModel> InputFieldReferences = new Dictionary<Key, ReferenceFieldModel>();
        public Dictionary<Key, ReferenceFieldModel> OutputFieldReferences = new Dictionary<Key, ReferenceFieldModel>();

        public CompoundOperatorFieldModel(string type) : base(type)
        {
            IsCompound = true;
        }
    }
}
