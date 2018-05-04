using DashShared;

namespace Dash
{
    [FieldModelType(TypeInfo.DocumentReference)]
    public class DocumentReferenceModel : ReferenceModel
    {
        public string DocumentId { get; set; }

        public bool CopyOnWrite { get; set; }

        public DocumentReferenceModel(string docId, string keyId, bool copyOnWrite, string id = null) : base(keyId, id)
        {
            CopyOnWrite = copyOnWrite;
            DocumentId = docId;
        }

        public override bool Equals(object obj)
        {
            DocumentReferenceModel refFieldModel = obj as DocumentReferenceModel;
            if (refFieldModel == null)
            {
                return false;
            }

            return base.Equals(refFieldModel) && refFieldModel.DocumentId.Equals(DocumentId) && refFieldModel.CopyOnWrite == CopyOnWrite;
        }

        public override int GetHashCode()
        {
            return base.GetHashCode() ^ DocumentId.GetHashCode() & CopyOnWrite.GetHashCode();
        }
    }
}
