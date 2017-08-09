using DashShared;

namespace Dash
{
    public class ReferenceFieldModel : FieldModel
    {
        public FieldReference Reference;

        public ReferenceFieldModel(FieldReference reference, string id = null) : base(id)
        {
            Reference = reference;
        }

        public override bool Equals(object obj)
        {
            ReferenceFieldModel refFieldModel = obj as ReferenceFieldModel;
            if (refFieldModel == null)
            {
                return false;
            }

            return refFieldModel.Reference.Equals(Reference);
        }

        public override int GetHashCode()
        {
            return Reference.GetHashCode();
        }

        protected override FieldModelDTO GetFieldDTOHelper()
        {
            return new FieldModelDTO(TypeInfo.Reference, Reference);
        }
    }
}
