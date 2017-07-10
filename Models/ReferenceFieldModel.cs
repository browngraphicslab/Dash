using DashShared;

namespace Dash
{
    public class ReferenceFieldModel : FieldModel
    {
        /// <summary>
        /// ID of document that this FieldModel references
        /// </summary>
        public string DocId;

        /// <summary>
        /// Key of field within document that is referenced
        /// </summary>
        public Key FieldKey;

        //TODO Do we ever want to just create a reference field model without the controller?
        //Some/many references are created temporarily so we don't really need a controller
        //for them and we don't need to add them to the content controller, but it might be better to be safe than sorry
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
