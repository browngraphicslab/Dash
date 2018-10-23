using DashShared;

namespace Dash
{
    [FieldModelType(TypeInfo.PointerReference)]
    public class PointerReferenceModel : ReferenceModel
    {
        public string ReferenceFieldModelId { get; set; }

        public PointerReferenceModel(string referenceFieldModelId, string keyId, string id = null) : base(keyId, id)
        {
            ReferenceFieldModelId = referenceFieldModelId;
        }
    }
}
