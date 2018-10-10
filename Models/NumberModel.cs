using System.Diagnostics;
using DashShared;

namespace Dash
{
    [FieldModelTypeAttribute(TypeInfo.Number)]
    public class NumberModel : FieldModel
    {

        public NumberModel(double data, string id = null) : base(id)
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
