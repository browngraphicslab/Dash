using DashShared;

namespace Dash
{
    public class ReferenceFieldModel : FieldModel
    {
        /// <summary>
        /// ID of document that this FieldModel references
        /// </summary>
        public string DocId { get; set; }

        /// <summary>
        /// Key of field within document that is referenced
        /// </summary>
        public Key FieldKey { get; set; }

        public ReferenceFieldModel(string docId, Key fieldKey)
        {
            DocId = docId;
            FieldKey = fieldKey;
        }

        public override bool Equals(object obj)
        {
            ReferenceFieldModel refFieldModel = obj as ReferenceFieldModel;
            if (refFieldModel == null)
            {
                return false;
            }

            return refFieldModel.DocId.Equals(DocId) && refFieldModel.FieldKey.Equals(FieldKey);
        }

        public override int GetHashCode()
        {
            return DocId.GetHashCode() ^ FieldKey.GetHashCode();
        }
    }
}
