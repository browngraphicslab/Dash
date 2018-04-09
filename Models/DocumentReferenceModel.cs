using DashShared;

namespace Dash
{
    [FieldModelType(TypeInfo.DocumentReference)]
    public class DocumentReferenceModel : ReferenceModel
    {
        public string DocumentId { get; set; }

        public DocumentReferenceModel(string docId, string keyId, string id = null) : base(keyId, id)
        {
            DocumentId = docId;
        }

        public override bool Equals(object obj)
        {
            DocumentReferenceModel refFieldModel = obj as DocumentReferenceModel;
            if (refFieldModel == null)
            {
                return false;
            }

            return base.Equals(refFieldModel) && refFieldModel.DocumentId.Equals(DocumentId);
        }

        public override int GetHashCode()
        {
            return base.GetHashCode() ^ DocumentId.GetHashCode();
        }
    }
}
