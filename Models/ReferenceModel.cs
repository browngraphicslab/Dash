using DashShared;

namespace Dash
{
    [FieldModelTypeAttribute(TypeInfo.Reference)]
    public abstract class ReferenceModel : FieldModel
    {
        public string KeyId { get; set; }

        public ReferenceModel(string keyId, string id = null) : base(id)
        {
            KeyId = keyId;
        }

        public override bool Equals(object obj)
        {
            ReferenceModel refFieldModel = obj as ReferenceModel;
            if (refFieldModel == null)
            {
                return false;
            }

            return KeyId == refFieldModel.KeyId;
        }

        public override int GetHashCode()
        {
            return KeyId.GetHashCode();
        }
    }
}
