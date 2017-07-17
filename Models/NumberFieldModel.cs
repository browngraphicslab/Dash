using System;
using DashShared;

namespace Dash
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

        public override FieldModelDTO GetFieldDTO()
        {
            return new FieldModelDTO(TypeInfo.Reference, Data);
        }
    }
}
