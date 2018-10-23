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
    }
}
