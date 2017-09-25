using System;
using DashShared;
using DashShared.Models;

namespace Dash
{
    [FieldModelType(FieldTypeEnum.Number)]
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
    }
}
