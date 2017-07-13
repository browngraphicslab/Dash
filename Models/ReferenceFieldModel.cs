using DashShared;

namespace Dash
{
    public abstract class ReferenceFieldModel : FieldModel
    {
        /// <summary>
        /// Key of field within document that is referenced
        /// </summary>
        public Key FieldKey;

        //TODO Do we ever want to just create a reference field model without the controller?
        //Some/many references are created temporarily so we don't really need a controller
        //for them and we don't need to add them to the content controller, but it might be better to be safe than sorry
        protected ReferenceFieldModel(Key fieldKey)
        {
            FieldKey = fieldKey;
        }

        public override bool Equals(object obj)
        {
            ReferenceFieldModel refFieldModel = obj as ReferenceFieldModel;
            if (refFieldModel == null)
            {
                return false;
            }

            return refFieldModel.FieldKey.Equals(FieldKey);
        }

        public override int GetHashCode()
        {
            return FieldKey.GetHashCode();
        }
    }
}
