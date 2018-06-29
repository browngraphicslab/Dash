using DashShared;

namespace Dash
{
    [FieldModelType(TypeInfo.Bool)]
    public class BoolModel : FieldModel
    {

        public bool Data;

        public BoolModel(bool data, string id = null) : base(id)
        {
            Data = data;
        }

        public override string ToString()
        {
            return $"BoolFieldModel: {Data}";
        }
    }
}
