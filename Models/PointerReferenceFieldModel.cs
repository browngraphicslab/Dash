using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dash
{
    public class PointerReferenceFieldModel : ReferenceFieldModel
    {
        public string ReferenceFieldModelId { get; set; }

        public PointerReferenceFieldModel(string referenceFieldModelId, string keyId, string id = null) : base(keyId, id)
        {
            ReferenceFieldModelId = referenceFieldModelId;
        }

        public override bool Equals(object obj)
        {
            DocumentReferenceFieldModel refFieldModel = obj as DocumentReferenceFieldModel;
            if (refFieldModel == null)
            {
                return false;
            }

            return base.Equals(refFieldModel) && refFieldModel.DocumentId.Equals(ReferenceFieldModelId);
        }

        public override int GetHashCode()
        {
            return base.GetHashCode() ^ ReferenceFieldModelId.GetHashCode();
        }
    }
}
