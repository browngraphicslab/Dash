using System.Collections.Generic;
using DashShared;

namespace Dash
{
    public class CompoundOperatorFieldModel : OperatorFieldModel
    {
        public Dictionary<KeyControllerBase, List<FieldReference>> InputFieldReferences = new Dictionary<KeyControllerBase, List<FieldReference>>();
        public Dictionary<KeyControllerBase, FieldReference> OutputFieldReferences = new Dictionary<KeyControllerBase, FieldReference>();

        public CompoundOperatorFieldModel(string type) : base(type)
        {
            IsCompound = true;
        }
    }
}
