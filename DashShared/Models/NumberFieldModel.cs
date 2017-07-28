using System;
using DashShared;

namespace DashShared
{
    public class NumberFieldModel : FieldModel
    {
        public NumberFieldModel() { }

        public NumberFieldModel(double data)
        {
            Data = data;
        }

        public double Data;

        public override string ToString()
        {
            return $"NumberFieldModel: {Data}";
        }

        protected override FieldModelDTO GetFieldDTOHelper()
        {
            return new FieldModelDTO(TypeInfo.Reference, Data);
        }
    }
}
