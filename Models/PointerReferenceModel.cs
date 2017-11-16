using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DashShared;
using DashShared.Models;

namespace Dash
{
    [FieldModelType(TypeInfo.PointerReference)]
    public class PointerReferenceModel : ReferenceModel
    {
        public string ReferenceFieldModelId { get; set; }

        public PointerReferenceModel(string referenceFieldModelId, string keyId, string id = null) : base(keyId, id)
        {
            ReferenceFieldModelId = referenceFieldModelId;
        }

        public override bool Equals(object obj)
        {
            DocumentReferenceModel refFieldModel = obj as DocumentReferenceModel;
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
