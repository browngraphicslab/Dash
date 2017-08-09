using System;
using DashShared;

namespace Dash
{
    public class NumberFieldModel : FieldModel
    {

        public NumberFieldModel(double data, string id = null) : base(id)
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
            return new FieldModelDTO(TypeInfo.Number, Data);
        }
    }
}
