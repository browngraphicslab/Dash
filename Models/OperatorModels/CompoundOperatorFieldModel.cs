using System.Collections.Generic;
using DashShared;

namespace Dash
{
    public class CompoundOperatorFieldModel : OperatorFieldModel
    {
        public Dictionary<KeyController, FieldReference> InputFieldReferences = new Dictionary<KeyController, FieldReference>();
        public Dictionary<KeyController, FieldReference> OutputFieldReferences = new Dictionary<KeyController, FieldReference>();

        public CompoundOperatorFieldModel(string type) : base(type)
        {
            IsCompound = true;
        }
    }
}
