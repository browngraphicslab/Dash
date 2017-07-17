using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DashShared;

namespace Dash
{
    public class DocumentReferenceFieldModel : ReferenceFieldModel
    {
        public string DocId;

        public DocumentReferenceFieldModel(string docId, Key fieldKey) : base(fieldKey)
        {
            DocId = docId;
        }

        public override bool Equals(object obj)
        {
            DocumentReferenceFieldModel refFieldModel = obj as DocumentReferenceFieldModel;
            if (refFieldModel == null)
            {
                return false;
            }

            return base.Equals(refFieldModel) && refFieldModel.DocId.Equals(DocId);
        }

        public override FieldModelDTO GetFieldDTO()
        {
            return new FieldModelDTO(TypeInfo.Reference, new KeyValuePair<Key, string>(FieldKey, DocId));
        }

        public override int GetHashCode()
        {
            return base.GetHashCode() ^ DocId.GetHashCode();
        }
    }
}
