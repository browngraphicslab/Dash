using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DashShared;

namespace Dash
{
    public class DocumentPointerReferenceFieldModel : ReferenceFieldModel
    {
        public ReferenceFieldModel DocReference;

        public DocumentPointerReferenceFieldModel(ReferenceFieldModel docReference, Key fieldKey) : base(fieldKey)
        {
            DocReference = docReference;
        }

        public override bool Equals(object obj)
        {
            DocumentPointerReferenceFieldModel refFieldModel = obj as DocumentPointerReferenceFieldModel;
            if (refFieldModel == null)
            {
                return false;
            }

            return base.Equals(refFieldModel) && refFieldModel.DocReference.Equals(DocReference);
        }

        public override FieldModelDTO GetFieldDTO()
        {
            return new FieldModelDTO(TypeInfo.Reference, DocReference);

        }

        public override int GetHashCode()
        {
            return base.GetHashCode() ^ DocReference.GetHashCode();
        }
    }
}
