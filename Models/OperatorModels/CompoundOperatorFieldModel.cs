using System.Collections.Generic;
using DashShared;
using DashShared.Models;

namespace Dash
{
    public class CompoundOperatorFieldModel : OperatorFieldModel
    {
        public Dictionary<KeyController, List<FieldReference>> InputFieldReferences = new Dictionary<KeyController, List<FieldReference>>();
        public Dictionary<KeyController, FieldReference> OutputFieldReferences = new Dictionary<KeyController, FieldReference>();

        public CompoundOperatorFieldModel() : base(OperatorType.Compound)
        {
            IsCompound = true;
        }
    }
}
