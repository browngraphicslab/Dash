using DashShared;
using DashShared.Models;

namespace Dash
{
    [FieldModelTypeAttribute(TypeInfo.Reference)]
    public abstract class ReferenceFieldModel : FieldModel
    {
        public string KeyId { get; set; }

        public ReferenceFieldModel(string keyId, string id = null) : base(id)
        {
            KeyId = keyId;
        }

        public override bool Equals(object obj)
        {
            ReferenceFieldModel refFieldModel = obj as ReferenceFieldModel;
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
